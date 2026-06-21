namespace SignVault.Api.Services;

/// <summary>Stores raw file bytes outside the database (files belong in object storage, not SQL).</summary>
public interface IFileStore
{
    Task<string> SaveAsync(byte[] bytes, string extension);
    Task<byte[]> ReadAsync(string storageKey);
    bool Exists(string storageKey);
}

public sealed class LocalFileStore : IFileStore
{
    private readonly string _root;

    public LocalFileStore(IWebHostEnvironment env, IConfiguration config)
    {
        var configured = config["Storage:Path"] ?? "storage/uploads";
        _root = Path.IsPathRooted(configured)
            ? configured
            : Path.Combine(env.ContentRootPath, configured);
        Directory.CreateDirectory(_root);
    }

    public async Task<string> SaveAsync(byte[] bytes, string extension)
    {
        var safeExt = string.IsNullOrWhiteSpace(extension) ? "" : extension.StartsWith('.') ? extension : "." + extension;
        var key = $"{Guid.NewGuid():N}{safeExt}";
        await File.WriteAllBytesAsync(Path.Combine(_root, key), bytes);
        return key;
    }

    public Task<byte[]> ReadAsync(string storageKey) =>
        File.ReadAllBytesAsync(Path.Combine(_root, storageKey));

    public bool Exists(string storageKey) =>
        File.Exists(Path.Combine(_root, storageKey));
}
