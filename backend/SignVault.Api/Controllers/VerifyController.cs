using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SignVault.Api.Data;
using SignVault.Api.Dtos;
using SignVault.Api.Services;

namespace SignVault.Api.Controllers;

/// <summary>Public, anonymous verification of the signature embedded inside a signed PDF.</summary>
[ApiController]
[Route("api/verify")]
[AllowAnonymous]
public class VerifyController : ControllerBase
{
    private const string TrustNote =
        "Cryptographically valid PAdES signature. The signing certificate is self-signed, so Adobe " +
        "Acrobat shows the signer's identity as \"unknown\" until you add our certificate to trusted " +
        "identities. The document is tamper-evident either way.";

    private readonly AppDbContext _db;
    private readonly IFileStore _files;
    private readonly IPdfVerifier _verifier;
    private readonly ISigner _signer;
    private readonly X509Certificate2 _cert;
    private readonly IAuditLogger _audit;

    public VerifyController(AppDbContext db, IFileStore files, IPdfVerifier verifier,
                            ISigner signer, X509Certificate2 cert, IAuditLogger audit)
    {
        _db = db;
        _files = files;
        _verifier = verifier;
        _signer = signer;
        _cert = cert;
        _audit = audit;
    }

    /// <summary>The platform's public signing identity (informational).</summary>
    [HttpGet("public-key")]
    public IActionResult PublicKey() => Ok(new
    {
        algorithm = "PAdES B-B (SHA256withRSA)",
        thumbprint = _signer.Thumbprint,
        publicKeyPem = _signer.PublicKeyPem
    });

    /// <summary>Download the platform's public certificate (.cer) to add it to Adobe trusted identities.</summary>
    [HttpGet("certificate")]
    public IActionResult Certificate()
    {
        var der = _cert.Export(X509ContentType.Cert);
        return File(der, "application/x-x509-ca-cert", "signvault.cer");
    }

    /// <summary>Verify the signature embedded in an uploaded PDF.</summary>
    [HttpPost]
    [RequestSizeLimit(25_000_000)]
    public async Task<ActionResult<VerifyResponse>> VerifyByFile(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "No file provided." });

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);

        VerifyResponse response;
        try
        {
            var sigs = _verifier.Verify(ms.ToArray());
            response = FromSignatures(sigs, documentId: null, fileName: file.FileName);
        }
        catch
        {
            response = new VerifyResponse(false,
                "Could not read this PDF — it is not a readable PDF, or it was corrupted or altered after signing.",
                null, file.FileName, null, null, false, null);
        }

        _audit.Record(null, "FILE_VERIFIED", null, HttpContext.Ip(), response.Valid ? "valid" : "invalid");
        await _db.SaveChangesAsync();
        return Ok(response);
    }

    /// <summary>Verify the stored signed PDF for a document id (used by share links).</summary>
    [HttpGet("{documentId:guid}")]
    public async Task<ActionResult<VerifyResponse>> VerifyStored(Guid documentId)
    {
        var doc = await _db.Documents.FirstOrDefaultAsync(d => d.Id == documentId);
        if (doc is null)
            return NotFound(new VerifyResponse(false, "Document not found.", documentId, null, null, null, false, null));
        if (string.IsNullOrEmpty(doc.SignedStorageKey) || !_files.Exists(doc.SignedStorageKey))
            return Ok(new VerifyResponse(false, "This document has not been signed yet.", documentId, doc.FileName, null, null, false, null));

        var bytes = await _files.ReadAsync(doc.SignedStorageKey);
        var sigs = _verifier.Verify(bytes);
        var response = FromSignatures(sigs, doc.Id, doc.FileName);

        _audit.Record(null, "DOC_VERIFIED", documentId, HttpContext.Ip(), response.Valid ? "valid" : "invalid");
        await _db.SaveChangesAsync();
        return Ok(response);
    }

    private static VerifyResponse FromSignatures(IReadOnlyList<PdfSignatureInfo> sigs, Guid? documentId, string? fileName)
    {
        if (sigs.Count == 0)
            return new VerifyResponse(false, "No digital signature was found in this PDF.", documentId, fileName, null, null, false, null);

        var s = sigs[0];
        var valid = s.IntegrityValid && s.CoversWholeDocument;
        var message = !s.IntegrityValid
            ? "Invalid — the signature does not verify; the document does not match it."
            : s.CoversWholeDocument
                ? "Valid — the document is signed and unaltered."
                : "Signed, but the document was changed after signing.";

        return new VerifyResponse(valid, message, documentId, fileName, s.SignerCommonName,
            s.SigningTimeUtc, s.CoversWholeDocument, TrustNote);
    }
}
