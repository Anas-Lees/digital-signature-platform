using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using SignVault.Api.Data;
using SignVault.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Cloud hosts (Render, Railway, etc.) inject the port via $PORT — honor it.
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// Behind a platform proxy (Render), trust X-Forwarded-* for real client IP + scheme.
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownNetworks.Clear();
    o.KnownProxies.Clear();
});

// ── Database ──────────────────────────────────────────────────────────────────
// SQLite by default (zero-install, local). Set DATABASE_URL (or ConnectionStrings__Default)
// to a Postgres connection string/URL for a PERSISTENT cloud database — nothing else changes.
var rawConn = Environment.GetEnvironmentVariable("DATABASE_URL") is { Length: > 0 } url
    ? url
    : builder.Configuration.GetConnectionString("Default") ?? "Data Source=signvault.db";
var usePostgres = Db.IsPostgres(rawConn);
builder.Services.AddDbContext<AppDbContext>(opt =>
{
    if (usePostgres) opt.UseNpgsql(Db.ToNpgsql(rawConn));
    else opt.UseSqlite(rawConn);
});

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
// Signing:PfxBase64 (env var) keeps the SAME signing identity across redeploys — set it in
// production so signatures stay verifiable. Otherwise a key is generated/loaded from disk.
var pfxB64 = builder.Configuration["Signing:PfxBase64"];
X509Certificate2 signingCert = !string.IsNullOrWhiteSpace(pfxB64)
    ? X509CertificateLoader.LoadPkcs12(Convert.FromBase64String(pfxB64), pfxPwd, X509KeyStorageFlags.Exportable)
    : SigningCertificate.LoadOrCreate(pfxPath, pfxPwd, subject);
builder.Services.AddSingleton(signingCert);
builder.Services.AddSingleton<ISigner, RsaSigner>();
builder.Services.AddSingleton<IPdfSigner, PadesPdfSigner>();   // PAdES signing (embeds the signature in the PDF)
builder.Services.AddSingleton<IPdfVerifier, PadesPdfVerifier>();

// ── App services ─────────────────────────────────────────────────────────────
builder.Services.AddSingleton<IFileStore, LocalFileStore>();
builder.Services.AddScoped<IAuditLogger, AuditLogger>();

// Abuse protection: a generous global cap per client IP.
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            ctx.Connection.RemoteIpAddress?.ToString() ?? "anon",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = 240, Window = TimeSpan.FromMinutes(1) }));
});

builder.Services.AddControllers();
builder.Services.AddOpenApi();   // built-in OpenAPI doc (.NET 10)

const string SpaCors = "spa";
builder.Services.AddCors(o => o.AddPolicy(SpaCors, p => p
    .WithOrigins("http://localhost:4200", "http://localhost:4300")
    .AllowAnyHeader()
    .AllowAnyMethod()));

var app = builder.Build();

// Create/upgrade the schema and seed a demo account on startup.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (db.Database.IsSqlite())
    {
        db.Database.Migrate();   // SQLite ships versioned migrations
    }
    else
    {
        db.Database.EnsureCreated();   // Postgres: create schema from the model
        // EnsureCreated does not evolve an existing schema, so reconcile the columns that
        // changed after the DB was first created. Idempotent (demo-grade; real apps use migrations).
        db.Database.ExecuteSqlRaw(@"ALTER TABLE ""Documents"" ADD COLUMN IF NOT EXISTS ""SignedStorageKey"" text;");
        db.Database.ExecuteSqlRaw(@"ALTER TABLE ""Signatures"" DROP COLUMN IF EXISTS ""SignatureBase64"";");
    }
    Seed.Run(db);
}

app.UseForwardedHeaders();
if (!app.Environment.IsDevelopment()) app.UseHsts();

app.Use(async (ctx, next) =>
{
    ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
    ctx.Response.Headers["X-Frame-Options"] = "DENY";
    ctx.Response.Headers["Referrer-Policy"] = "no-referrer";
    await next();
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();                          // JSON: /openapi/v1.json
    app.MapScalarApiReference(o => o.WithTitle("SignVault API"));  // UI: /scalar/v1
}

// Serve the built Angular SPA from wwwroot (single origin in production).
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseRateLimiter();
app.UseCors(SpaCors);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Any non-API, non-file route is a client-side Angular route → return index.html.
app.MapFallbackToFile("index.html");

app.Run();

// Exposes the implicit Program class to the test project (WebApplicationFactory<Program>).
public partial class Program { }
