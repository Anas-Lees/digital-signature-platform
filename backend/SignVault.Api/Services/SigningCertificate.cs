using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace SignVault.Api.Services;

/// <summary>
/// Ensures the platform has a private signing key. On first run it generates a
/// self-signed X.509 certificate (RSA-3072) and persists it as a password-protected
/// PFX so the SAME identity is reused across restarts. This PFX is the private key
/// that makes THIS server the signature authority — it is git-ignored and must be
/// protected (in production: an HSM / key vault, never a file on disk).
/// </summary>
public static class SigningCertificate
{
    public static X509Certificate2 LoadOrCreate(string pfxPath, string password, string subject)
    {
        var fullPath = Path.GetFullPath(pfxPath);
        var dir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        if (File.Exists(fullPath))
        {
            return X509CertificateLoader.LoadPkcs12FromFile(
                fullPath, password, X509KeyStorageFlags.Exportable);
        }

        using var rsa = RSA.Create(3072);
        var req = new CertificateRequest(
            subject, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        req.CertificateExtensions.Add(
            new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, critical: true));
        req.CertificateExtensions.Add(
            new X509BasicConstraintsExtension(false, false, 0, critical: true));

        using var cert = req.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(5));

        var pfxBytes = cert.Export(X509ContentType.Pfx, password);
        File.WriteAllBytes(fullPath, pfxBytes);

        return X509CertificateLoader.LoadPkcs12(
            pfxBytes, password, X509KeyStorageFlags.Exportable);
    }
}
