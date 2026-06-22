# Builds the Angular SPA, copies it into the API's wwwroot, and publishes the
# API (Release) to ..\publish — a single-origin, IIS-ready package.
# Usage:  pwsh ./deploy/Publish.ps1   (or run in Windows PowerShell)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path

Write-Host '==> Building Angular (production)...' -ForegroundColor Cyan
Push-Location (Join-Path $root 'frontend')
npm ci
if ($LASTEXITCODE -ne 0) {
  Pop-Location
  throw "npm ci failed. If the dev server (ng serve) is running it locks node_modules — stop it and retry."
}
npm run build
if ($LASTEXITCODE -ne 0) { Pop-Location; throw 'Angular build failed — aborting (will not deploy a stale bundle).' }
Pop-Location

$spa     = Join-Path $root 'frontend\dist\frontend\browser'
$wwwroot = Join-Path $root 'backend\SignVault.Api\wwwroot'

Write-Host '==> Copying SPA into API wwwroot...' -ForegroundColor Cyan
if (Test-Path $wwwroot) { Remove-Item $wwwroot -Recurse -Force }
New-Item -ItemType Directory -Path $wwwroot | Out-Null
Copy-Item (Join-Path $spa '*') $wwwroot -Recurse

Write-Host '==> Publishing API (Release)...' -ForegroundColor Cyan
$publish = Join-Path $root 'publish'
dotnet publish (Join-Path $root 'backend\SignVault.Api') -c Release -o $publish

Write-Host "==> Done. Publish output: $publish" -ForegroundColor Green
Write-Host '    Test it directly with:  dotnet .\publish\SignVault.Api.dll --urls http://localhost:8090'
