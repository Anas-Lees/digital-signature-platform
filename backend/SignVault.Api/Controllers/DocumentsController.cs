using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SignVault.Api.Data;
using SignVault.Api.Domain;
using SignVault.Api.Dtos;
using SignVault.Api.Services;

namespace SignVault.Api.Controllers;

[ApiController]
[Route("api/documents")]
[Authorize]
public class DocumentsController : ControllerBase
{
    // 100 MB per account by default. Override with Storage:MaxBytesPerUser. Deletion is
    // disabled (retention), so this is a hard cap rather than a reclaimable quota.
    private const long DefaultMaxBytesPerUser = 100L * 1024 * 1024;

    private readonly AppDbContext _db;
    private readonly IFileStore _files;
    private readonly ISigner _signer;
    private readonly IPdfSigner _pdfSigner;
    private readonly IAuditLogger _audit;
    private readonly IConfiguration _config;

    public DocumentsController(AppDbContext db, IFileStore files, ISigner signer,
                               IPdfSigner pdfSigner, IAuditLogger audit, IConfiguration config)
    {
        _db = db;
        _files = files;
        _signer = signer;
        _pdfSigner = pdfSigner;
        _audit = audit;
        _config = config;
    }

    private static bool IsPdf(byte[] bytes) =>
        bytes.Length > 4 && Encoding.ASCII.GetString(bytes, 0, 5) == "%PDF-";

    /// <summary>List the signed-in user's documents, newest first.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<DocumentDto>>> List()
    {
        var userId = User.Id();
        var docs = await _db.Documents
            .Include(d => d.Signature)
            .Where(d => d.OwnerId == userId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
        return Ok(docs.Select(d => d.ToDto()));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DocumentDto>> Get(Guid id)
    {
        var doc = await _db.Documents.Include(d => d.Signature)
            .FirstOrDefaultAsync(d => d.Id == id);
        if (doc is null) return NotFound();
        if (doc.OwnerId != User.Id()) return Forbid();
        return Ok(doc.ToDto());
    }

    /// <summary>Upload a PDF. Only PDF files are accepted (the signature is embedded into the PDF).</summary>
    [HttpPost("upload")]
    [RequestSizeLimit(25_000_000)]
    public async Task<ActionResult<DocumentDto>> Upload(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "No file provided." });

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        var bytes = ms.ToArray();

        if (!IsPdf(bytes))
            return BadRequest(new { message = "Only PDF files are supported. Please upload a .pdf document." });

        var maxPerUser = _config.GetValue("Storage:MaxBytesPerUser", DefaultMaxBytesPerUser);
        var used = await _db.Documents.Where(d => d.OwnerId == User.Id())
            .SumAsync(d => (long?)d.SizeBytes) ?? 0L;
        if (used + bytes.LongLength > maxPerUser)
            return BadRequest(new { message = $"Storage limit reached for this account ({maxPerUser / (1024 * 1024)} MB)." });

        var hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        var key = await _files.SaveAsync(bytes, ".pdf");

        var doc = new Document
        {
            OwnerId = User.Id(),
            FileName = Path.GetFileName(file.FileName),
            ContentType = "application/pdf",
            StorageKey = key,
            ContentHash = hash,
            SizeBytes = bytes.LongLength,
            Status = DocumentStatus.Uploaded
        };
        _db.Documents.Add(doc);
        _audit.Record(User.Id(), "DOC_UPLOADED", doc.Id, HttpContext.Ip(), doc.FileName);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id = doc.Id }, doc.ToDto());
    }

    /// <summary>
    /// Sign a PDF: embeds a PAdES B-B signature into the document (Adobe-readable) using the
    /// platform's private key. The original is kept; the signed PDF is stored alongside it.
    /// </summary>
    [HttpPost("{id:guid}/sign")]
    public async Task<ActionResult<SignatureDto>> Sign(Guid id)
    {
        var userId = User.Id();
        await using var tx = await _db.Database.BeginTransactionAsync();

        var doc = await _db.Documents.Include(d => d.Signature)
            .FirstOrDefaultAsync(d => d.Id == id);
        if (doc is null) return NotFound();
        if (doc.OwnerId != userId) return Forbid();
        if (doc.Status == DocumentStatus.Signed)
            return Conflict(new { message = "Document is already signed." });

        var original = await _files.ReadAsync(doc.StorageKey);
        var signedPdf = _pdfSigner.SignPdf(original,
            reason: $"Signed via SignVault by {User.Name()}",
            location: "SignVault");

        var signedKey = await _files.SaveAsync(signedPdf, ".pdf");

        var sig = new Signature
        {
            DocumentId = doc.Id,
            SignerId = userId,
            SignerName = User.Name(),
            Algorithm = "PAdES B-B (SHA256withRSA)",
            CertThumbprint = _signer.Thumbprint
        };
        doc.SignedStorageKey = signedKey;
        doc.Status = DocumentStatus.Signed;
        _db.Signatures.Add(sig);
        _audit.Record(userId, "DOC_SIGNED", doc.Id, HttpContext.Ip(), _signer.Thumbprint);

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        return Ok(sig.ToDto());
    }

    /// <summary>Download the original (unsigned) PDF.</summary>
    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> Download(Guid id)
    {
        var doc = await _db.Documents.FindAsync(id);
        if (doc is null) return NotFound();
        if (doc.OwnerId != User.Id()) return Forbid();
        if (!_files.Exists(doc.StorageKey))
            return NotFound(new { message = "Stored file is missing." });

        var bytes = await _files.ReadAsync(doc.StorageKey);
        return File(bytes, "application/pdf", doc.FileName);
    }

    /// <summary>Download the signed PDF — the signature is embedded inside, visible in Adobe Acrobat.</summary>
    [HttpGet("{id:guid}/signed")]
    public async Task<IActionResult> Signed(Guid id)
    {
        var doc = await _db.Documents.FindAsync(id);
        if (doc is null) return NotFound();
        if (doc.OwnerId != User.Id()) return Forbid();
        if (string.IsNullOrEmpty(doc.SignedStorageKey) || !_files.Exists(doc.SignedStorageKey))
            return BadRequest(new { message = "This document has not been signed yet." });

        var bytes = await _files.ReadAsync(doc.SignedStorageKey);
        var name = Path.GetFileNameWithoutExtension(doc.FileName);
        return File(bytes, "application/pdf", $"{name}-signed.pdf");
    }
}
