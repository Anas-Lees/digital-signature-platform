using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Org.BouncyCastle.X509;                  // X509CertificateParser
using iText.Bouncycastle.X509;                // X509CertificateBC  (namespace is .X509, not .Cert)
using iText.Commons.Bouncycastle.Cert;        // IX509Certificate
using iText.Kernel.Pdf;                        // PdfReader, StampingProperties
using iText.Signatures;                        // PdfSigner, SignerProperties, IExternalSignature, ...

namespace SignVault.Api.Services;

public interface IPdfSigner
{
    /// <summary>Embeds a PAdES B-B signature into the PDF and returns the signed PDF.</summary>
    byte[] SignPdf(byte[] pdf, string reason, string location);
}

/// <summary>
/// Real PAdES B-B signer (ETSI EN 319 142): embeds a CAdES-detached CMS
/// (SubFilter ETSI.CAdES.detached) into the PDF per ISO 32000, so Adobe Acrobat
/// shows the signature. The RSA-3072 private key never leaves System.Security.Cryptography.
/// </summary>
public sealed class PadesPdfSigner : IPdfSigner
{
    private readonly X509Certificate2 _cert;

    public PadesPdfSigner(X509Certificate2 cert) => _cert = cert;

    public byte[] SignPdf(byte[] pdf, string reason, string location)
    {
        if (pdf is null || pdf.Length == 0)
            throw new ArgumentException("Empty PDF.", nameof(pdf));

        // Keep the RSA alive for the whole SignDetached call (iText calls back into it).
        var rsa = _cert.GetRSAPrivateKey()
                  ?? throw new InvalidOperationException("Signing certificate has no private key.");

        IExternalSignature signature = new DotNetRsaSignature(rsa);
        IX509Certificate[] chain = ToITextChain(new[] { _cert }); // self-signed => single cert

        using var input = new MemoryStream(pdf, writable: false);
        using var output = new MemoryStream();
        using (var reader = new PdfReader(input))
        {
            var signer = new PdfSigner(reader, output, new StampingProperties());

            // Invisible signature (no on-page stamp). It is still a full, valid PAdES
            // signature and appears in Adobe Acrobat's Signature Panel.
            var props = new SignerProperties()
                .SetFieldName("SignVaultSignature")
                .SetReason(reason)
                .SetLocation(location);
            signer.SetSignerProperties(props);

            // PAdES B-B: no CRL/OCSP/TSA. CADES == ETSI.CAdES.detached.
            signer.SignDetached(
                signature, chain,
                crlList: null, ocspClient: null, tsaClient: null,
                estimatedSize: 0,
                sigtype: PdfSigner.CryptoStandard.CADES);
        }
        return output.ToArray();
    }

    private static IX509Certificate[] ToITextChain(X509Certificate2[] dotNetChain)
    {
        var parser = new X509CertificateParser();
        var result = new IX509Certificate[dotNetChain.Length];
        for (int i = 0; i < dotNetChain.Length; i++)
            result[i] = new X509CertificateBC(parser.ReadCertificate(dotNetChain[i].RawData));
        return result;
    }
}

/// <summary>Bridges iText's signing callback to the .NET RSA private key (PKCS#1 v1.5, SHA-256).</summary>
public sealed class DotNetRsaSignature : IExternalSignature
{
    private readonly RSA _rsa;
    public DotNetRsaSignature(RSA rsa) => _rsa = rsa;

    public string GetDigestAlgorithmName() => "SHA-256";
    public string GetSignatureAlgorithmName() => "RSA";
    public ISignatureMechanismParams? GetSignatureMechanismParameters() => null; // null => PKCS#1 v1.5

    public byte[] Sign(byte[] message) =>
        _rsa.SignData(message, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
}
