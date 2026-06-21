namespace SignVault.Api.Domain;

/// <summary>Append-only audit trail — the backbone of non-repudiation. Never updated or deleted.</summary>
public class AuditEntry
{
    public long Id { get; set; }
    public Guid? ActorId { get; set; }
    public string Action { get; set; } = "";       // e.g. DOC_UPLOADED, DOC_SIGNED, DOC_VERIFIED
    public Guid? EntityId { get; set; }
    public string? IpAddress { get; set; }
    public string? Detail { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
