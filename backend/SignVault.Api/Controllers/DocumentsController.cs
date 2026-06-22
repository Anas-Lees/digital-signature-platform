using System.Security.Cryptography;
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
    private readonly AppDbContext _db;
    private readonly IFileStore _files;
    private readonly ISigner _signer;
    private readonly IAuditLogger _audit;

    public DocumentsController(AppDbContext db, IFileStore files, ISigner signer, IAuditLogger audit)
    {
        _db = db;
        _files = files;
        _signer = signer;
        _audit = audit;
    }

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

    /// <summary>Upload a file. Its bytes are stored and its SHA-256 hash recorded.</summary>
    [HttpPost("upload")]
    [RequestSizeLimit(25_000_000)]
    public async Task<ActionResult<DocumentDto>> Upload(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "No file provided." });

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        var bytes = ms.ToArray();

        var hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        var ext = Path.GetExtension(file.FileName);
        var key = await _files.SaveAsync(bytes, ext);

        var doc = new Document
        {
            OwnerId = User.Id(),
            FileName = Path.GetFileName(file.FileName),
            ContentType = string.IsNullOrWhiteSpace(file.ContentType)
                ? "application/octet-stream" : file.ContentType,
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

    /// <summary>Sign a document with the platform's private key. Signature + audit commit atomically.</summary>
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

        var bytes = await _files.ReadAsync(doc.StorageKey);
        var signatureBytes = _signer.Sign(bytes);

        var sig = new Signature
        {
            DocumentId = doc.Id,
            SignerId = userId,
            SignerName = User.Name(),
            Algorithm = _signer.Algorithm,
            SignatureBase64 = Convert.ToBase64String(signatureBytes),
            CertThumbprint = _signer.Thumbprint
        };
        doc.Status = DocumentStatus.Signed;
        _db.Signatures.Add(sig);
        _audit.Record(userId, "DOC_SIGNED", doc.Id, HttpContext.Ip(), _signer.Thumbprint);

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        return Ok(sig.ToDto());
    }

    /// <summary>Download the original uploaded file.</summary>
    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> Download(Guid id)
    {
        var doc = await _db.Documents.FindAsync(id);
        if (doc is null) return NotFound();
        if (doc.OwnerId != User.Id()) return Forbid();
        if (!_files.Exists(doc.StorageKey))
            return NotFound(new { message = "Stored file is missing." });

        var bytes = await _files.ReadAsync(doc.StorageKey);
        return File(bytes, doc.ContentType, doc.FileName);
    }

    /// <summary>Download a one-page PDF certificate/receipt for a signed document.</summary>
    [HttpGet("{id:guid}/certificate")]
    public async Task<IActionResult> Certificate(Guid id)
    {
        var doc = await _db.Documents.Include(d => d.Signature)
            .FirstOrDefaultAsync(d => d.Id == id);
        if (doc is null) return NotFound();
        if (doc.OwnerId != User.Id()) return Forbid();
        if (doc.Signature is null) return BadRequest(new { message = "Document is not signed yet." });

        var pdf = CertificatePdf.Build(doc, doc.Signature);
        var name = Path.GetFileNameWithoutExtension(doc.FileName);
        return File(pdf, "application/pdf", $"{name}-certificate.pdf");
    }

    /// <summary>Delete a document (and its file + signature). Owner only.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var doc = await _db.Documents.FindAsync(id);
        if (doc is null) return NotFound();
        if (doc.OwnerId != User.Id()) return Forbid();

        try { _files.Delete(doc.StorageKey); } catch { /* best effort */ }
        _db.Documents.Remove(doc);   // cascades to the signature
        _audit.Record(User.Id(), "DOC_DELETED", doc.Id, HttpContext.Ip(), doc.FileName);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
