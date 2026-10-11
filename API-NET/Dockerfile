# syntax=docker/dockerfile:1
# FPFL API-NET: multi-stage build (SDK -> ASP.NET runtime).
# Build from the API-NET folder:  docker build -t fpflapinetimg .

# ---------- build ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Project files first so the restore layer is cached until a dependency changes.
COPY src/Fpfl.Api/Fpfl.Api.csproj         src/Fpfl.Api/
COPY src/Fpfl.Auth/Fpfl.Auth.csproj       src/Fpfl.Auth/
COPY src/Fpfl.Common/Fpfl.Common.csproj   src/Fpfl.Common/
COPY src/Fpfl.Display/Fpfl.Display.csproj src/Fpfl.Display/
COPY src/Fpfl.Items/Fpfl.Items.csproj     src/Fpfl.Items/
RUN dotnet restore src/Fpfl.Api/Fpfl.Api.csproj

COPY src/ src/
RUN dotnet publish src/Fpfl.Api/Fpfl.Api.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ---------- runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# 8080 is the default non-root port; Azure Container Apps / App Service terminate TLS
# at their ingress and forward plain HTTP plus X-Forwarded-* headers.
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_FORWARDEDHEADERS_ENABLED=true \
    DOTNET_RUNNING_IN_CONTAINER=true

COPY --from=build /app/publish .

EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "Fpfl.Api.dll"]

# Required at run time (never baked into the image):
#   ConnectionStrings__Fpfl   Npgsql connection string
#   Auth__SigningKey          >= 32 chars (base64 of 48 random bytes recommended)
#   Cors__AllowedOrigins__0   the UI origin, e.g. http://localhost:4280
