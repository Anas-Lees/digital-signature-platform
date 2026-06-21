using Microsoft.EntityFrameworkCore;
using SignVault.Api.Domain;

namespace SignVault.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<Signature> Signatures => Set<Signature>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<AppUser>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Email).HasMaxLength(255).IsRequired();
            e.Property(u => u.DisplayName).HasMaxLength(120).IsRequired();
            e.Property(u => u.Role).HasConversion<string>().HasMaxLength(20);
        });

        b.Entity<Document>(e =>
        {
            e.HasIndex(d => d.OwnerId);
            e.Property(d => d.FileName).HasMaxLength(260).IsRequired();
            e.Property(d => d.ContentHash).HasMaxLength(64);
            e.Property(d => d.Status).HasConversion<string>().HasMaxLength(20);
            e.HasOne(d => d.Owner)
             .WithMany(u => u.Documents)
             .HasForeignKey(d => d.OwnerId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(d => d.Signature)
             .WithOne(s => s.Document!)
             .HasForeignKey<Signature>(s => s.DocumentId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Signature>(e =>
        {
            e.Property(s => s.Algorithm).HasMaxLength(40);
            e.Property(s => s.CertThumbprint).HasMaxLength(64);
        });

        b.Entity<AuditEntry>(e =>
        {
            e.HasIndex(a => a.EntityId);
            e.Property(a => a.Action).HasMaxLength(80).IsRequired();
        });
    }
}
