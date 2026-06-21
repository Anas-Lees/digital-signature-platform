using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SignVault.Api.Data;
using SignVault.Api.Domain;
using SignVault.Api.Dtos;
using SignVault.Api.Services;

namespace SignVault.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITokenService _tokens;
    private readonly IAuditLogger _audit;

    public AuthController(AppDbContext db, ITokenService tokens, IAuditLogger audit)
    {
        _db = db;
        _tokens = tokens;
        _audit = audit;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest req)
    {
        var email = req.Email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email == email))
            return Conflict(new { message = "An account with that email already exists." });

        var user = new AppUser
        {
            Email = email,
            DisplayName = req.DisplayName.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password, workFactor: 12),
            Role = UserRole.User
        };
        _db.Users.Add(user);
        _audit.Record(user.Id, "USER_REGISTERED", user.Id, HttpContext.Ip());
        await _db.SaveChangesAsync();

        return Ok(BuildAuth(user));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest req)
    {
        var email = req.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user is null || !user.IsActive ||
            !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        _audit.Record(user.Id, "USER_LOGIN", user.Id, HttpContext.Ip());
        await _db.SaveChangesAsync();
        return Ok(BuildAuth(user));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> Me()
    {
        var user = await _db.Users.FindAsync(User.Id());
        return user is null ? NotFound() : Ok(user.ToDto());
    }

    private AuthResponse BuildAuth(AppUser user)
    {
        var (token, expires) = _tokens.Create(user);
        return new AuthResponse(token, expires, user.ToDto());
    }
}
