using iText.Commons.Bouncycastle.Cert;   // IX509Certificate
using iText.Kernel.Pdf;                   // PdfReader, PdfDocument
using iText.Signatures;                   // SignatureUtil, PdfPKCS7, CertificateInfo

namespace SignVault.Api.Services;

public sealed record PdfSignatureInfo(
    string FieldName,
    string SignerCommonName,
    DateTime SigningTimeUtc,
    bool IntegrityValid,
    bool CoversWholeDocument);

public interface IPdfVerifier
{
    /// <summary>Reads and cryptographically verifies every embedded signature in the PDF.</summary>
    IReadOnlyList<PdfSignatureInfo> Verify(byte[] pdf);
}

/// <summary>
/// Verifies the PAdES/CMS signatures embedded in a PDF: integrity (signed bytes unchanged
/// and signature matches the embedded certificate) and whether the signature covers the
/// whole document. This is integrity verification — independent of trust (a self-signed
/// certificate is integrity-valid but identity-untrusted, which is correct).
/// </summary>
public sealed class PadesPdfVerifier : IPdfVerifier
{
    public IReadOnlyList<PdfSignatureInfo> Verify(byte[] pdf)
    {
        var results = new List<PdfSignatureInfo>();

        using var input = new MemoryStream(pdf, writable: false);
        using var pdfDoc = new PdfDocument(new PdfReader(input));

        var util = new SignatureUtil(pdfDoc);
        foreach (var name in util.GetSignatureNames())
        {
            PdfPKCS7 pkcs7 = util.ReadSignatureData(name);

            bool integrityValid = pkcs7.VerifySignatureIntegrityAndAuthenticity();
            bool coversWholeDoc = util.SignatureCoversWholeDocument(name);

            IX509Certificate signingCert = pkcs7.GetSigningCertificate();
            string cn = CertificateInfo.GetSubjectFields(signingCert).GetField("CN") ?? "(unknown)";

            results.Add(new PdfSignatureInfo(
                FieldName: name,
                SignerCommonName: cn,
                SigningTimeUtc: pkcs7.GetSignDate().ToUniversalTime(),
                IntegrityValid: integrityValid,
                CoversWholeDocument: coversWholeDoc));
        }

        return results;
    }
}
