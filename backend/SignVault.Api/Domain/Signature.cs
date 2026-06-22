namespace SignVault.Api.Domain;

/// <summary>A cryptographic signature produced by the platform's signing authority.</summary>
public class Signature
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DocumentId { get; set; }
    public Document? Document { get; set; }

    public Guid SignerId { get; set; }
    public string SignerName { get; set; } = "";        // who signed it, e.g. "Anas"
    public string Algorithm { get; set; } = "PAdES B-B (SHA256withRSA)";
    public string CertThumbprint { get; set; } = "";     // which signing cert produced it
    public DateTime SignedAt { get; set; } = DateTime.UtcNow;
    // Note: the actual signature bytes live INSIDE the signed PDF (PAdES), not in the DB.
}
