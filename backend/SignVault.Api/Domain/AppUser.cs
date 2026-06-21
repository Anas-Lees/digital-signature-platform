namespace SignVault.Api.Domain;

/// <summary>An authenticated account. Passwords are stored only as a BCrypt hash.</summary>
public class AppUser
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public UserRole Role { get; set; } = UserRole.User;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<Document> Documents { get; set; } = new();
}
