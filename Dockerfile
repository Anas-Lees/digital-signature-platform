# syntax=docker/dockerfile:1
# Single-origin production image: the .NET API serves the Angular SPA + /api.

# 1) Build the Angular SPA
FROM node:22-alpine AS web
WORKDIR /web
COPY frontend/package*.json ./
RUN npm ci
COPY frontend/ ./
RUN npm run build

# 2) Publish the .NET API with the SPA baked into wwwroot
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS api
WORKDIR /src
COPY backend/SignVault.Api/SignVault.Api.csproj ./SignVault.Api/
RUN dotnet restore ./SignVault.Api/SignVault.Api.csproj
COPY backend/SignVault.Api/ ./SignVault.Api/
COPY --from=web /web/dist/frontend/browser/ ./SignVault.Api/wwwroot/
RUN dotnet publish ./SignVault.Api/SignVault.Api.csproj -c Release -o /app /p:UseAppHost=false

# 3) Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=api /app ./
# Writable data dir for the SQLite DB, signing key and uploads (kept out of the app dir)
RUN mkdir -p /data && chown app /data
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:8080 \
    ConnectionStrings__Default="Data Source=/data/signvault.db" \
    Signing__PfxPath="/data/signvault.pfx" \
    Storage__Path="/data/uploads"
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "SignVault.Api.dll"]
