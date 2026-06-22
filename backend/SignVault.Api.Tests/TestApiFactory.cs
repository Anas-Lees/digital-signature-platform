using Microsoft.AspNetCore.Mvc.Testing;

namespace SignVault.Api.Tests;

/// <summary>
/// Boots the real app in-memory (WebApplicationFactory) against an isolated SQLite database,
/// storage folder and signing key in a unique temp directory, so integration tests exercise
/// the full HTTP + EF Core + signing pipeline without touching dev data.
/// Overrides are passed via environment variables because Program.cs reads configuration at
/// the top level, before any WebHostBuilder customization would apply.
/// </summary>
public sealed class TestApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dir =
        Path.Combine(Path.GetTempPath(), "signvault-tests", Guid.NewGuid().ToString("N"));

    public TestApiFactory()
    {
        Directory.CreateDirectory(_dir);
        Environment.SetEnvironmentVariable("DATABASE_URL", null);            // force the SQLite path
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", $"Data Source={Path.Combine(_dir, "test.db")}");
        Environment.SetEnvironmentVariable("Storage__Path", Path.Combine(_dir, "uploads"));
        Environment.SetEnvironmentVariable("Signing__PfxPath", Path.Combine(_dir, "signing.pfx"));
        Environment.SetEnvironmentVariable("Signing__PfxPassword", "test-pfx-password");
        Environment.SetEnvironmentVariable("Storage__MaxBytesPerUser", "50000"); // small cap so the quota test is fast
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        try { Directory.Delete(_dir, recursive: true); } catch { /* best-effort cleanup */ }
    }
}
