# Deploying SignVault to IIS (production)

In production the app runs as a **single origin**: the ASP.NET Core API serves both
the `/api` endpoints **and** the built Angular app (from `wwwroot`), with a SPA
fallback so client-side routes like `/verify` work on refresh. No proxy, no CORS.

## Easiest: one command (recommended)

From a normal PowerShell in the repo root:
```powershell
powershell -ExecutionPolicy Bypass -File .\deploy\Deploy-IIS-All.ps1
```
Approve the one Windows admin prompt. It installs the Hosting Bundle (if missing),
builds + publishes, creates the IIS site on **port 8090**, and opens it. Done.

> If the dev server (`ng serve`) is running, stop it first — it locks the build
> files and the publish step will fail.

The manual steps below do the same thing in pieces, if you prefer control.

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

## Continuous deployment (CD)

CI already builds and publishes the image to GHCR on every push to `main`. To make
pushes also **auto-deploy**:

### Option A — Cloud (Render), push-to-deploy
1. Create the service once: click **Deploy to Render** in the root `README.md`
   (it reads `render.yaml`). You get a `https://…onrender.com` URL.
2. In Render: **Settings → Deploy Hook** → copy the URL.
3. In GitHub: **Settings → Secrets and variables → Actions → New repository secret**
   - Name: `RENDER_DEPLOY_HOOK`  ·  Value: the hook URL.
4. Done — the `deploy` job in `ci.yml` now redeploys on every push. (Without the
   secret it just skips, so CI stays green.)

> Render also auto-deploys natively once the repo is connected, so the hook is
> optional belt-and-suspenders.

### Option B — Your own IIS server, push-to-deploy
1. On the IIS machine: **GitHub repo → Settings → Actions → Runners → New self-hosted
   runner** and follow the steps (installs a small agent that listens for jobs).
2. Run the agent (`./run.cmd`), ideally as a service.
3. Trigger `.github/workflows/deploy-iis.yml` from the **Actions** tab (or uncomment
   its `push:` trigger to deploy on every push). It runs `Publish.ps1` + `Deploy-IIS.ps1`
   on your server. (The runner must run with rights to manage IIS.)

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
