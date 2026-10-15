#!/usr/bin/env bash
# Deploys UI-NG (Angular) and API-NET (.NET) to the existing AKS cluster, next to the
# React/Java apps, on their own 2-node pool. Run from Azure Cloud Shell (Bash):
#
#   git clone https://github.com/rdonalson/FPFL-Angular-NET.git
#   cd FPFL-Angular-NET
#   bash k8s/deploy-fpfl-net.sh
#
# Safe to re-run: existing pieces are kept, images are rebuilt and the pods restarted.
set -euo pipefail

# ----------------------------------------------------------------------------- names
RESOURCE_GROUP="FPFL-React-Java"
CLUSTER="fpfl-cluster"
NODEPOOL="netpool"
NODE_COUNT=2
NODE_SIZE="Standard_B2s"
ACR="fpflacr"
NAMESPACE="fpfl"
JAVA_DB_SECRET="api-db-secret"      # existing Java secret: DB_URL, DB_USERNAME, DB_PASSWORD
NET_SECRET="api-net-secret"         # new: ConnectionStrings__Fpfl, Auth__SigningKey
DOMAIN="ledger-finance.com"
UI_HOST="ui-ng"
API_HOST="api-net"
API_URL="https://${API_HOST}.${DOMAIN}"

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
TAG="$(git -C "$REPO_ROOT" rev-parse --short HEAD 2>/dev/null || date +%Y%m%d%H%M)"

step() { printf '\n\033[1;36m==> %s\033[0m\n' "$*"; }

# ----------------------------------------------------------------------------- 1. cluster
step "Connecting kubectl to ${CLUSTER}"
az aks get-credentials -g "$RESOURCE_GROUP" -n "$CLUSTER" --overwrite-existing >/dev/null

# ----------------------------------------------------------------------------- 2. node pool
step "Node pool ${NODEPOOL} (${NODE_COUNT} x ${NODE_SIZE})"
if az aks nodepool show -g "$RESOURCE_GROUP" --cluster-name "$CLUSTER" -n "$NODEPOOL" >/dev/null 2>&1; then
  echo "Already exists; leaving it as is."
else
  # The taint keeps everything else off these nodes; the UI-NG/API-NET pods tolerate it.
  az aks nodepool add \
    -g "$RESOURCE_GROUP" --cluster-name "$CLUSTER" -n "$NODEPOOL" \
    --mode User --os-type Linux \
    --node-count "$NODE_COUNT" --node-vm-size "$NODE_SIZE" \
    --labels workload=fpfl-net \
    --node-taints workload=fpfl-net:NoSchedule \
    -o table
fi

# ----------------------------------------------------------------------------- 3. images
step "Building images in ACR ${ACR} (tag ${TAG} + latest)"
az acr build -r "$ACR" -t "fpflapinetimg:${TAG}" -t "fpflapinetimg:latest" \
  "$REPO_ROOT/API-NET" -o table
az acr build -r "$ACR" -t "fpfluingimg:${TAG}" -t "fpfluingimg:latest" \
  --build-arg "API_URL=${API_URL}" \
  "$REPO_ROOT/UI-NG" -o table

# ----------------------------------------------------------------------------- 4. secret
step "Secret ${NET_SECRET}"
if kubectl get secret "$NET_SECRET" -n "$NAMESPACE" >/dev/null 2>&1; then
  echo "Already exists; keeping it (and the signing key, so nobody gets signed out)."
else
  secret_value() { kubectl get secret "$JAVA_DB_SECRET" -n "$NAMESPACE" -o "jsonpath={.data.$1}" | base64 -d; }
  DB_URL="$(secret_value DB_URL)"            # jdbc:postgresql://host:5432/FPFL-V2
  DB_USER="$(secret_value DB_USERNAME)"
  DB_PASS="$(secret_value DB_PASSWORD)"

  hostport="${DB_URL#jdbc:postgresql://}"; hostport="${hostport%%/*}"
  DB_HOST="${hostport%%:*}"
  DB_PORT="${hostport##*:}"; [[ "$DB_PORT" == "$DB_HOST" ]] && DB_PORT=5432
  DB_NAME="${DB_URL##*/}"; DB_NAME="${DB_NAME%%\?*}"

  # Quote the password so characters like ; don't break the connection string.
  CONN="Host=${DB_HOST};Port=${DB_PORT};Database=${DB_NAME};Username=${DB_USER};Password=\"${DB_PASS//\"/\"\"}\";SSL Mode=Require"

  kubectl create secret generic "$NET_SECRET" -n "$NAMESPACE" \
    --from-literal="ConnectionStrings__Fpfl=${CONN}" \
    --from-literal="Auth__SigningKey=$(openssl rand -base64 48)"
  echo "Created from ${JAVA_DB_SECRET} (database ${DB_NAME} on ${DB_HOST}) with a new signing key."
fi

# ----------------------------------------------------------------------------- 5. apply
step "Applying deployments, services and ingresses"
kubectl apply -f "$REPO_ROOT/k8s/api-net/"
kubectl apply -f "$REPO_ROOT/k8s/ui-ng/"
kubectl rollout restart deployment/api-net deployment/ui-ng -n "$NAMESPACE"   # pick up the new :latest
kubectl rollout status deployment/api-net -n "$NAMESPACE" --timeout=5m
kubectl rollout status deployment/ui-ng -n "$NAMESPACE" --timeout=5m

# ----------------------------------------------------------------------------- 6. DNS
step "DNS for ${UI_HOST}.${DOMAIN} and ${API_HOST}.${DOMAIN}"
INGRESS_IP="$(kubectl get ingress fpfl-api-ingress -n "$NAMESPACE" -o jsonpath='{.status.loadBalancer.ingress[0].ip}' 2>/dev/null || true)"
if [[ -z "$INGRESS_IP" ]]; then
  INGRESS_IP="$(kubectl get svc -A -l app.kubernetes.io/component=controller \
    -o jsonpath='{.items[0].status.loadBalancer.ingress[0].ip}' 2>/dev/null || true)"
fi
echo "Ingress IP: ${INGRESS_IP:-unknown}"

ZONE_RG="$(az network dns zone list --query "[?name=='${DOMAIN}'].resourceGroup | [0]" -o tsv 2>/dev/null || true)"
if [[ -n "$ZONE_RG" && -n "$INGRESS_IP" ]]; then
  for host in "$UI_HOST" "$API_HOST"; do
    if az network dns record-set a show -g "$ZONE_RG" -z "$DOMAIN" -n "$host" >/dev/null 2>&1; then
      echo "A record ${host}.${DOMAIN} already exists; leaving it."
    else
      az network dns record-set a add-record -g "$ZONE_RG" -z "$DOMAIN" -n "$host" -a "$INGRESS_IP" -o none
      echo "Added A record ${host}.${DOMAIN} -> ${INGRESS_IP}"
    fi
  done
else
  echo "No Azure DNS zone for ${DOMAIN} found here. At your DNS provider, add:"
  echo "  A  ${UI_HOST}   -> ${INGRESS_IP:-<ingress IP>}"
  echo "  A  ${API_HOST}  -> ${INGRESS_IP:-<ingress IP>}"
fi

# ----------------------------------------------------------------------------- 7. status
step "Status"
kubectl get pods -n "$NAMESPACE" -l 'app in (fpfl-api-net,fpfl-ui-ng)' -o wide
kubectl get certificate -n "$NAMESPACE" 2>/dev/null | grep -E 'NAME|net|ng' || true
cat <<DONE

Done. Certificates are issued once DNS resolves (usually a few minutes). Then try:
  https://${UI_HOST}.${DOMAIN}
  https://${API_HOST}.${DOMAIN}/swagger-ui
  https://${API_HOST}.${DOMAIN}/health
DONE
