using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using SignVault.Api.Domain;

namespace SignVault.Api.Services;

public interface ITokenService
{
    (string token, DateTime expiresAt) Create(AppUser user);
}

public class JwtOptions
{
    public string Issuer { get; set; } = "SignVault";
    public string Audience { get; set; } = "SignVaultClient";
    public string Key { get; set; } = "";
    public int ExpiryMinutes { get; set; } = 120;
}

public sealed class JwtTokenService : ITokenService
{
    private readonly JwtOptions _opt;
    public JwtTokenService(JwtOptions opt) => _opt = opt;

    public (string token, DateTime expiresAt) Create(AppUser user)
    {
        var expires = DateTime.UtcNow.AddMinutes(_opt.ExpiryMinutes);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("name", user.DisplayName),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opt.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var jwt = new JwtSecurityToken(
            issuer: _opt.Issuer,
            audience: _opt.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: creds);

        return (new JwtSecurityTokenHandler().WriteToken(jwt), expires);
    }
}
