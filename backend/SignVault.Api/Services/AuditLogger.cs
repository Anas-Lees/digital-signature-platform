using SignVault.Api.Data;
using SignVault.Api.Domain;

namespace SignVault.Api.Services;

/// <summary>Writes append-only audit entries. Callers SaveChanges (often inside a transaction).</summary>
public interface IAuditLogger
{
    void Record(Guid? actorId, string action, Guid? entityId, string? ip, string? detail = null);
}

public sealed class AuditLogger : IAuditLogger
{
    private readonly AppDbContext _db;
    public AuditLogger(AppDbContext db) => _db = db;

    public void Record(Guid? actorId, string action, Guid? entityId, string? ip, string? detail = null)
    {
        _db.AuditEntries.Add(new AuditEntry
        {
            ActorId = actorId,
            Action = action,
            EntityId = entityId,
            IpAddress = ip,
            Detail = detail
        });
    }
}
