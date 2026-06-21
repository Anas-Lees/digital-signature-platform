using SignVault.Api.Domain;

namespace SignVault.Api.Data;

/// <summary>Seeds a demo account on first run so the app is usable immediately.</summary>
public static class Seed
{
    public const string DemoEmail = "demo@signvault.local";
    public const string DemoPassword = "Demo1234!";

    public static void Run(AppDbContext db)
    {
        if (db.Users.Any()) return;

        db.Users.Add(new AppUser
        {
            Email = DemoEmail,
            DisplayName = "Demo User",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(DemoPassword, workFactor: 12),
            Role = UserRole.Admin
        });
        db.SaveChanges();
    }
}
