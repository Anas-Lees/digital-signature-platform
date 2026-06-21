using System.ComponentModel.DataAnnotations;

namespace SignVault.Api.Dtos;

public record RegisterRequest(
    [Required, EmailAddress] string Email,
    [Required, MinLength(6)] string Password,
    [Required, MaxLength(120)] string DisplayName);

public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);

public record AuthResponse(string Token, DateTime ExpiresAt, UserDto User);

public record UserDto(Guid Id, string Email, string DisplayName, string Role);
