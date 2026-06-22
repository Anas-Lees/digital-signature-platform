# Generates a STABLE signing key for production so signatures stay verifiable across
# redeploys. Run it once, then paste the two printed values into your host's environment
# variables (on Render: your service -> Environment).
param([string]$Password = (-join ((48..57)+(65..90)+(97..122) | Get-Random -Count 24 | ForEach-Object { [char]$_ })))

$cert = New-SelfSignedCertificate `
  -Subject 'CN=SignVault Signing Authority, O=SignVault' `
  -KeyAlgorithm RSA -KeyLength 3072 -HashAlgorithm SHA256 `
  -KeyUsage DigitalSignature -KeyExportPolicy Exportable `
  -CertStoreLocation Cert:\CurrentUser\My -NotAfter (Get-Date).AddYears(5)

$bytes = $cert.Export([System.Security.Cryptography.X509Certificates.X509ContentType]::Pfx, $Password)
$b64   = [Convert]::ToBase64String($bytes)
Remove-Item ("Cert:\CurrentUser\My\" + $cert.Thumbprint) -Force

Write-Host ''
Write-Host 'Set these two environment variables on your host (keep them secret):' -ForegroundColor Green
Write-Host ''
Write-Host 'Signing__PfxPassword' -ForegroundColor Cyan
Write-Host $Password
Write-Host ''
Write-Host 'Signing__PfxBase64' -ForegroundColor Cyan
Write-Host $b64
Write-Host ''
Write-Host 'With these set, every redeploy keeps the SAME signing identity, so previously'
Write-Host 'signed documents keep verifying.'
