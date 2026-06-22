using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SignVault.Api.Data;

/// <summary>
/// Used only by the EF Core CLI (`dotnet ef migrations add` / `database update`). It targets
/// PostgreSQL so the generated migrations carry Postgres column types — production runs on
/// Postgres and applies these migrations. (Local dev/tests on SQLite build the schema from the
/// model via EnsureCreated, so they don't need migrations.) No real connection is opened to
/// scaffold a migration; the connection string just has to be valid Npgsql syntax.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=signvault_design;Username=design;Password=design")
            .Options;
        return new AppDbContext(options);
    }
}
