namespace SignVault.Api.Domain;

/// <summary>A cryptographic signature produced by the platform's signing authority.</summary>
public class Signature
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DocumentId { get; set; }
    public Document? Document { get; set; }

    public Guid SignerId { get; set; }
    public string Algorithm { get; set; } = "SHA256withRSA";
    public string SignatureBase64 { get; set; } = "";   // the signature bytes, base64
    public string CertThumbprint { get; set; } = "";     // which signing cert produced it
    public DateTime SignedAt { get; set; } = DateTime.UtcNow;
}
