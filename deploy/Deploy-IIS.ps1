#Requires -RunAsAdministrator
<#
  Deploys the published SignVault app to IIS.
  Run from an ELEVATED PowerShell:  powershell -ExecutionPolicy Bypass -File .\deploy\Deploy-IIS.ps1

  Prerequisite (one-time, admin): install the ASP.NET Core 10 Hosting Bundle, e.g.
      winget install Microsoft.DotNet.HostingBundle.10
  or download "ASP.NET Core 10.0 Runtime — Hosting Bundle" from
      https://dotnet.microsoft.com/download/dotnet/10.0
#>
param(
  [string]$PublishPath = (Join-Path $PSScriptRoot '..\publish'),
  [string]$DeployPath  = 'C:\inetpub\SignVault',
  [string]$SiteName    = 'SignVault',
  [string]$AppPool     = 'SignVaultPool',
  [int]   $Port        = 8090
)
$ErrorActionPreference = 'Stop'
Import-Module WebAdministration

# The ASP.NET Core Module ships in different locations depending on bundle version
# (System32\inetsrv OR Program Files\IIS\Asp.Net Core Module). Also accept the
# applicationHost.config registration as proof it's present.
function Test-AncmInstalled {
  foreach ($p in @("$env:windir\System32\inetsrv\aspnetcorev2.dll",
                   "${env:ProgramFiles}\IIS\Asp.Net Core Module\V2\aspnetcorev2.dll")) {
    if (Test-Path $p) { return $true }
  }
  $cfg = "$env:windir\System32\inetsrv\config\applicationHost.config"
  return ((Test-Path $cfg) -and (Select-String -Path $cfg -Pattern 'AspNetCoreModuleV2' -SimpleMatch -Quiet))
}

# 0) Hosting Bundle present?
if (-not (Test-AncmInstalled)) {
  Write-Warning 'ASP.NET Core Module (Hosting Bundle) is NOT installed — IIS cannot host the API yet.'
  Write-Host    'Install it, then re-run this script:'
  Write-Host    '  winget install Microsoft.DotNet.HostingBundle.10'
  Write-Host    '  (or the Hosting Bundle from https://dotnet.microsoft.com/download/dotnet/10.0)'
  exit 1
}

$PublishPath = (Resolve-Path $PublishPath).Path
if (-not (Test-Path (Join-Path $PublishPath 'SignVault.Api.dll'))) {
  throw "No published app at $PublishPath. Run deploy\Publish.ps1 first."
}

# 1) Copy the published app to the IIS folder (keeps your build folder independent)
Write-Host "==> Copying published app to $DeployPath ..." -ForegroundColor Cyan
if (Test-Path $DeployPath) { Remove-Item $DeployPath -Recurse -Force }
New-Item -ItemType Directory -Path $DeployPath | Out-Null
Copy-Item (Join-Path $PublishPath '*') $DeployPath -Recurse

# 2) App pool — "No Managed Code" because ASP.NET Core runs its own runtime
if (Test-Path "IIS:\AppPools\$AppPool") { Remove-WebAppPool -Name $AppPool }
New-WebAppPool -Name $AppPool | Out-Null
Set-ItemProperty "IIS:\AppPools\$AppPool" -Name managedRuntimeVersion -Value ''
Set-ItemProperty "IIS:\AppPools\$AppPool" -Name startMode -Value 'AlwaysRunning'

# 3) Web site
if (Test-Path "IIS:\Sites\$SiteName") { Remove-Website -Name $SiteName }
New-Website -Name $SiteName -PhysicalPath $DeployPath -ApplicationPool $AppPool -Port $Port | Out-Null

# 4) Grant the app-pool identity write access (SQLite DB, signing key, uploads)
$identity = "IIS AppPool\$AppPool"
icacls $DeployPath /grant "${identity}:(OI)(CI)M" /T | Out-Null

# 5) Start it
Start-WebAppPool -Name $AppPool
Start-Website -Name $SiteName

Write-Host ''
Write-Host "==> Deployed. Browse:  http://localhost:$Port" -ForegroundColor Green
Write-Host '    Demo login: demo@signvault.local / Demo1234!'
