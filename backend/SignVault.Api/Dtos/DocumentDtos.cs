using SignVault.Api.Domain;

namespace SignVault.Api.Dtos;

public record DocumentDto(
    Guid Id,
    string FileName,
    string ContentType,
    string ContentHash,
    long SizeBytes,
    string Status,
    DateTime CreatedAt,
    SignatureDto? Signature);

public record SignatureDto(
    Guid Id,
    Guid DocumentId,
    string SignerName,
    string Algorithm,
    string CertThumbprint,
    DateTime SignedAt);

public record VerifyResponse(
    bool Valid,
    string Message,
    Guid? DocumentId,
    string? FileName,
    string? SignerName,
    DateTime? SignedAt,
    bool CoversWholeDocument,
    string? Note);

public static class MappingExtensions
{
    public static SignatureDto ToDto(this Signature s) =>
        new(s.Id, s.DocumentId, s.SignerName, s.Algorithm, s.CertThumbprint, s.SignedAt);

    public static DocumentDto ToDto(this Document d) =>
        new(d.Id, d.FileName, d.ContentType, d.ContentHash, d.SizeBytes,
            d.Status.ToString(), d.CreatedAt, d.Signature?.ToDto());

    public static UserDto ToDto(this AppUser u) =>
        new(u.Id, u.Email, u.DisplayName, u.Role.ToString());
}
