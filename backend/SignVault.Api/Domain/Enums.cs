namespace SignVault.Api.Domain;

/// <summary>Role-based access control levels.</summary>
public enum UserRole
{
    User = 0,
    Admin = 1
}

/// <summary>Lifecycle state of an uploaded document.</summary>
public enum DocumentStatus
{
    Uploaded = 0,
    Signed = 1,
    Revoked = 2
}
