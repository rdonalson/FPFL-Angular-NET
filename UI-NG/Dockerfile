# syntax=docker/dockerfile:1
# FPFL UI-NG: Angular build on Node, served by unprivileged nginx.
# Build from the UI-NG folder:
#   docker build --build-arg API_URL=http://localhost:5132 -t fpfluingimg .

# ---------- build ----------
FROM node:24-alpine AS build
WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci --no-audit --no-fund

COPY . .

# Point the production environment at the API (same idea as UI-TSX's VITE_API_URL build arg).
ARG API_URL=https://api-net.ledger-finance.com
RUN sed -i "s|apiUrl: '[^']*'|apiUrl: '${API_URL}'|" src/environments/environment.ts \
 && grep -q "apiUrl: '${API_URL}'" src/environments/environment.ts \
 && npx ng build --configuration production

# ---------- runtime ----------
FROM nginxinc/nginx-unprivileged:1.29-alpine AS runtime
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist/ui-ng/browser /usr/share/nginx/html
EXPOSE 8080
