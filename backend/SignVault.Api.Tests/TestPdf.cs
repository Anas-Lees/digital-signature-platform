using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using iText.Kernel.Pdf;

namespace SignVault.Api.Tests;

/// <summary>Shared helpers: a fresh self-signed signing cert and a minimal real PDF.</summary>
internal static class TestPdf
{
    /// <summary>A throwaway self-signed RSA cert, mirroring how the app builds its signing identity.</summary>
    public static X509Certificate2 NewCert(string commonName = "Unit Test Authority")
    {
        using var rsa = RSA.Create(2048); // smaller than prod (3072) purely for test speed
        var req = new CertificateRequest(
            $"CN={commonName}, O=SignVault Tests", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        req.CertificateExtensions.Add(
            new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, critical: true));
        using var cert = req.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));

        // Round-trip through PFX so the private key is exportable/usable, exactly as the app loads it.
        var pfx = cert.Export(X509ContentType.Pfx, "p");
        return X509CertificateLoader.LoadPkcs12(pfx, "p", X509KeyStorageFlags.Exportable);
    }

    /// <summary>A valid one-page PDF generated with iText (so it has a real %PDF structure).</summary>
    public static byte[] OnePage()
    {
        var ms = new MemoryStream();
        using (var pdf = new PdfDocument(new PdfWriter(ms)))
            pdf.AddNewPage();
        return ms.ToArray(); // MemoryStream.ToArray works after the stream is closed
    }

    /// <summary>A PDF padded to <paramref name="totalBytes"/> (keeps the %PDF- header) for size/quota tests.</summary>
    public static byte[] Padded(int totalBytes)
    {
        var pdf = OnePage();
        if (totalBytes <= pdf.Length) return pdf;
        var padded = new byte[totalBytes];
        Array.Copy(pdf, padded, pdf.Length); // header stays intact so the upload still reads as a PDF
        return padded;
    }
}
