using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace SignVault.Api.Services;

/// <summary>Helpers to read the authenticated user's id off the validated JWT claims.</summary>
public static class CurrentUser
{
    public static Guid Id(this ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue(JwtRegisteredClaimNames.Sub)
                  ?? user.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? throw new UnauthorizedAccessException("No subject claim on token.");
        return Guid.Parse(sub);
    }

    public static string? Ip(this HttpContext ctx) =>
        ctx.Connection.RemoteIpAddress?.ToString();
}
