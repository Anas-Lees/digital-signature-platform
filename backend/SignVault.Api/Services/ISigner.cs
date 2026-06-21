using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace SignVault.Api.Services;

/// <summary>Abstraction over the crypto signer (Dependency Inversion — swap RSA for an HSM later).</summary>
public interface ISigner
{
    byte[] Sign(byte[] data);
    bool Verify(byte[] data, byte[] signature);
    string Thumbprint { get; }
    string Algorithm { get; }
    string PublicKeyPem { get; }
}

/// <summary>RSA + SHA-256 signer backed by the platform's X.509 certificate.</summary>
public sealed class RsaSigner : ISigner
{
    private readonly X509Certificate2 _cert;

    public RsaSigner(X509Certificate2 cert) => _cert = cert;

    public string Thumbprint => _cert.Thumbprint;
    public string Algorithm => "SHA256withRSA";

    public byte[] Sign(byte[] data)
    {
        using var rsa = _cert.GetRSAPrivateKey()
            ?? throw new InvalidOperationException("Signing certificate has no private key.");
        return rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    }

    public bool Verify(byte[] data, byte[] signature)
    {
        using var rsa = _cert.GetRSAPublicKey()
            ?? throw new InvalidOperationException("Signing certificate has no public key.");
        return rsa.VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    }

    public string PublicKeyPem
    {
        get
        {
            using var rsa = _cert.GetRSAPublicKey()!;
            return rsa.ExportSubjectPublicKeyInfoPem();
        }
    }
}
