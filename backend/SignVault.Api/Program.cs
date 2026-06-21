using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using SignVault.Api.Data;
using SignVault.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Cloud hosts (Render, Railway, Heroku, etc.) inject the port via $PORT — honor it.
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// ── Database ──────────────────────────────────────────────────────────────────
// SQLite by default (zero-install). To use MySQL / SQL Server / PostgreSQL, change
// only this provider line + swap the EF Core NuGet package — the rest is untouched.
var conn = builder.Configuration.GetConnectionString("Default") ?? "Data Source=signvault.db";
builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlite(conn));

// ── Authentication (JWT) ─────────────────────────────────────────────────────
var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
if (string.IsNullOrWhiteSpace(jwt.Key) || jwt.Key.Length < 32)
    jwt.Key = "dev-only-change-me-signing-key-please-use-32+-bytes"; // dev fallback only
builder.Services.AddSingleton(jwt);
builder.Services.AddSingleton<ITokenService, JwtTokenService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key))
        };
    });
builder.Services.AddAuthorization();

// ── Signing authority (this server holds the private key) ────────────────────
var pfxPath = builder.Configuration["Signing:PfxPath"] ?? "keys/signvault.pfx";
var pfxPwd = builder.Configuration["Signing:PfxPassword"] ?? "dev-pfx-password";
var subject = builder.Configuration["Signing:Subject"] ?? "CN=SignVault Signing Authority, O=SignVault";
var signingCert = SigningCertificate.LoadOrCreate(pfxPath, pfxPwd, subject);
builder.Services.AddSingleton(signingCert);
builder.Services.AddSingleton<ISigner, RsaSigner>();

// ── App services ─────────────────────────────────────────────────────────────
builder.Services.AddSingleton<IFileStore, LocalFileStore>();
builder.Services.AddScoped<IAuditLogger, AuditLogger>();

builder.Services.AddControllers();
builder.Services.AddOpenApi();   // built-in OpenAPI doc at /openapi/v1.json (.NET 10)

const string SpaCors = "spa";
builder.Services.AddCors(o => o.AddPolicy(SpaCors, p => p
    .WithOrigins("http://localhost:4200", "http://localhost:4300")
    .AllowAnyHeader()
    .AllowAnyMethod()));

var app = builder.Build();

// Apply migrations and seed a demo account on startup.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    Seed.Run(db);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();                          // JSON: /openapi/v1.json
    app.MapScalarApiReference(o => o.WithTitle("SignVault API"));  // interactive UI: /scalar/v1
}

// Serve the built Angular SPA from wwwroot (single origin in production).
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseCors(SpaCors);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Any non-API, non-file route is a client-side Angular route → return index.html.
app.MapFallbackToFile("index.html");

app.Run();
