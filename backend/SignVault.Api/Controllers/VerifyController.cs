using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SignVault.Api.Data;
using SignVault.Api.Dtos;
using SignVault.Api.Services;

namespace SignVault.Api.Controllers;

/// <summary>Public, anonymous verification — anyone can confirm a document's authenticity.</summary>
[ApiController]
[Route("api/verify")]
[AllowAnonymous]
public class VerifyController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IFileStore _files;
    private readonly ISigner _signer;
    private readonly IAuditLogger _audit;

    public VerifyController(AppDbContext db, IFileStore files, ISigner signer, IAuditLogger audit)
    {
        _db = db;
        _files = files;
        _signer = signer;
        _audit = audit;
    }

    /// <summary>The platform's public key + signing identity, so anyone can verify independently.</summary>
    [HttpGet("public-key")]
    public IActionResult PublicKey() => Ok(new
    {
        algorithm = _signer.Algorithm,
        thumbprint = _signer.Thumbprint,
        publicKeyPem = _signer.PublicKeyPem
    });

    /// <summary>Verify the stored copy of a document against its stored signature.</summary>
    [HttpGet("{documentId:guid}")]
    public async Task<ActionResult<VerifyResponse>> VerifyStored(Guid documentId)
    {
        var doc = await _db.Documents.Include(d => d.Signature)
            .FirstOrDefaultAsync(d => d.Id == documentId);
        if (doc is null)
            return NotFound(new VerifyResponse(false, "Document not found.", documentId, null, null, null));
        if (doc.Signature is null)
            return Ok(new VerifyResponse(false, "Document has not been signed yet.", documentId, null, null, null));

        var bytes = await _files.ReadAsync(doc.StorageKey);
        var ok = _signer.Verify(bytes, Convert.FromBase64String(doc.Signature.SignatureBase64));

        _audit.Record(null, "DOC_VERIFIED", documentId, HttpContext.Ip(), ok ? "valid" : "invalid");
        await _db.SaveChangesAsync();

        return Ok(BuildResponse(ok, documentId, doc.Signature.Algorithm,
            doc.Signature.CertThumbprint, doc.Signature.SignedAt));
    }

    /// <summary>Verify an uploaded file against a document's stored signature (detects tampering).</summary>
    [HttpPost("{documentId:guid}")]
    [RequestSizeLimit(25_000_000)]
    public async Task<ActionResult<VerifyResponse>> VerifyUpload(Guid documentId, IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "No file provided." });

        var doc = await _db.Documents.Include(d => d.Signature)
            .FirstOrDefaultAsync(d => d.Id == documentId);
        if (doc?.Signature is null)
            return NotFound(new VerifyResponse(false, "No signed document with that id.", documentId, null, null, null));

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        var ok = _signer.Verify(ms.ToArray(), Convert.FromBase64String(doc.Signature.SignatureBase64));

        _audit.Record(null, "DOC_VERIFIED_UPLOAD", documentId, HttpContext.Ip(), ok ? "valid" : "invalid");
        await _db.SaveChangesAsync();

        return Ok(BuildResponse(ok, documentId, doc.Signature.Algorithm,
            doc.Signature.CertThumbprint, doc.Signature.SignedAt));
    }

    private static VerifyResponse BuildResponse(bool ok, Guid id, string alg, string thumb, DateTime signedAt) =>
        new(ok,
            ok ? "Authentic and untampered." : "Invalid — the file does not match the signature.",
            id, alg, thumb, signedAt);
}
