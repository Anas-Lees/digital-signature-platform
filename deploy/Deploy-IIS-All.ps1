<#
  Deploy-IIS-All.ps1  —  one command to put SignVault on IIS.

  Run it from a NORMAL PowerShell (it will ask for admin itself):
      powershell -ExecutionPolicy Bypass -File .\deploy\Deploy-IIS-All.ps1

  It will:
    1) self-elevate (one Windows "Yes" prompt),
    2) install the ASP.NET Core 10 Hosting Bundle if it's missing,
    3) build + publish the app (Angular + .NET),
    4) create/refresh the IIS site and open it in your browser.
#>
param([int]$Port = 8090)

# ---- self-elevate (relaunch as Administrator) ----
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()
          ).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
  Write-Host 'Requesting administrator rights (approve the prompt)...' -ForegroundColor Yellow
  Start-Process powershell.exe -Verb RunAs -ArgumentList @(
    '-ExecutionPolicy','Bypass','-NoExit','-File',"`"$PSCommandPath`"",'-Port',"$Port")
  return
}

$ErrorActionPreference = 'Stop'
$here = $PSScriptRoot

# The module lives in System32\inetsrv OR Program Files\IIS\Asp.Net Core Module
# depending on bundle version; the applicationHost.config entry also proves it's there.
function Test-AncmInstalled {
  foreach ($p in @("$env:windir\System32\inetsrv\aspnetcorev2.dll",
                   "${env:ProgramFiles}\IIS\Asp.Net Core Module\V2\aspnetcorev2.dll")) {
    if (Test-Path $p) { return $true }
  }
  $cfg = "$env:windir\System32\inetsrv\config\applicationHost.config"
  return ((Test-Path $cfg) -and (Select-String -Path $cfg -Pattern 'AspNetCoreModuleV2' -SimpleMatch -Quiet))
}

# 1) ASP.NET Core Hosting Bundle (lets IIS run .NET apps)
if (Test-AncmInstalled) {
  Write-Host '==> ASP.NET Core Module already installed.' -ForegroundColor Green
} else {
  Write-Host '==> Installing ASP.NET Core 10 Hosting Bundle (winget)...' -ForegroundColor Cyan
  try {
    winget install --id Microsoft.DotNet.HostingBundle.10 --silent `
      --accept-package-agreements --accept-source-agreements
  } catch { Write-Warning "winget install failed: $($_.Exception.Message)" }

  if (-not (Test-AncmInstalled)) {
    Write-Warning 'Hosting Bundle still not detected.'
    Write-Host    'Download it manually (ASP.NET Core Runtime 10.0 > "Hosting Bundle"), install, then re-run this script:'
    Write-Host    '  https://dotnet.microsoft.com/download/dotnet/10.0' -ForegroundColor Cyan
    Read-Host 'Press Enter to exit'
    return
  }
  cmd /c 'net stop was /y & net start w3svc' | Out-Null   # restart IIS to load the module
}

# 2) Build + publish
Write-Host '==> Building and publishing...' -ForegroundColor Cyan
& (Join-Path $here 'Publish.ps1')

# 3) Create/refresh the IIS site
& (Join-Path $here 'Deploy-IIS.ps1') -Port $Port

Start-Process "http://localhost:$Port"
Write-Host "`n==> Live on IIS:  http://localhost:$Port" -ForegroundColor Green
Read-Host 'Press Enter to close'
