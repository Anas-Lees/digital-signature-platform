# Deploying SignVault to IIS (production)

In production the app runs as a **single origin**: the ASP.NET Core API serves both
the `/api` endpoints **and** the built Angular app (from `wwwroot`), with a SPA
fallback so client-side routes like `/verify` work on refresh. No proxy, no CORS.

## Prerequisites (one-time, as Administrator)

1. **IIS** enabled (Windows feature "Internet Information Services").
2. **.NET 10 SDK/Runtime** + the **ASP.NET Core Hosting Bundle** (this installs the
   IIS `AspNetCoreModuleV2` that runs .NET apps):
   ```powershell
   winget install Microsoft.DotNet.HostingBundle.10
   # or download "ASP.NET Core 10.0 Hosting Bundle":
   # https://dotnet.microsoft.com/download/dotnet/10.0
   ```
   After installing, restart IIS: `net stop was /y && net start w3svc`.

## Step 1 — Build the production package

```powershell
./deploy/Publish.ps1
```
This builds Angular, copies it into `backend/SignVault.Api/wwwroot`, and publishes the
API (with an IIS `web.config`) to `./publish`.

## Step 2 — Deploy to IIS (Administrator)

```powershell
powershell -ExecutionPolicy Bypass -File .\deploy\Deploy-IIS.ps1
```
This copies `./publish` to `C:\inetpub\SignVault`, creates an app pool
(**No Managed Code**) and a website **SignVault** on **port 8090**, grants the app-pool
identity write access (for the SQLite DB, signing key, and uploads), and starts it.

Then browse: **http://localhost:8090**  ·  demo login `demo@signvault.local` / `Demo1234!`

## Verify it's up

```powershell
(Invoke-WebRequest http://localhost:8090/ -UseBasicParsing).StatusCode              # 200 (SPA)
(Invoke-WebRequest http://localhost:8090/api/verify/public-key -UseBasicParsing).StatusCode  # 200 (API)
```

## Notes for a real production server
- Put the **signing key in an HSM / certificate store**, not a PFX in the app folder
  (`appsettings.json` → `Signing`). The folder-based key is for local/demo use.
- Move the **database** to SQL Server / PostgreSQL (one EF Core provider line — see the
  root `README.md`) and the **uploads** to durable storage.
- Set a strong `Jwt:Key` via an environment variable or `appsettings.Production.json`.
- Front it with **HTTPS** (bind a certificate in IIS) and enable HSTS.

## Without IIS (quick production run)
The published app is self-hosted Kestrel — you can run it directly:
```powershell
dotnet .\publish\SignVault.Api.dll --urls http://localhost:8090
```
