using Npgsql;

namespace SignVault.Api.Data;

/// <summary>Helpers to pick the database provider from a connection string / URL.</summary>
public static class Db
{
    public static bool IsPostgres(string conn) =>
        conn.StartsWith("postgres", StringComparison.OrdinalIgnoreCase) ||
        conn.Contains("Host=", StringComparison.OrdinalIgnoreCase);

    /// <summary>Converts a postgres://user:pass@host:port/db URL to an Npgsql
    /// key=value connection string. Passes a key=value string through unchanged.</summary>
    public static string ToNpgsql(string conn)
    {
        if (!conn.Contains("://")) return conn;
        var uri = new Uri(conn);
        var parts = uri.UserInfo.Split(':', 2);
        return new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Username = Uri.UnescapeDataString(parts[0]),
            Password = parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : "",
            Database = uri.AbsolutePath.TrimStart('/'),
            SslMode = SslMode.Require,
            TrustServerCertificate = true
        }.ConnectionString;
    }
}
