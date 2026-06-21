namespace SignVault.Api.Domain;

/// <summary>Metadata for an uploaded file. The bytes live in the file store, not the DB.</summary>
public class Document
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OwnerId { get; set; }
    public AppUser? Owner { get; set; }

    public string FileName { get; set; } = "";
    public string ContentType { get; set; } = "application/octet-stream";
    public string StorageKey { get; set; } = "";      // path/key in the file store
    public string ContentHash { get; set; } = "";      // SHA-256 hex of the original bytes
    public long SizeBytes { get; set; }
    public DocumentStatus Status { get; set; } = DocumentStatus.Uploaded;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Signature? Signature { get; set; }
}
