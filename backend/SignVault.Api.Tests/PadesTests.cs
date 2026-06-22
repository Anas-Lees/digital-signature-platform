using SignVault.Api.Services;

namespace SignVault.Api.Tests;

/// <summary>The heart of the product: embedding a real PAdES signature and verifying it.</summary>
public class PadesTests
{
    [Fact]
    public void SignPdf_embeds_a_PAdES_signature_that_verifies()
    {
        using var cert = TestPdf.NewCert("PAdES Test Authority");
        var signer = new PadesPdfSigner(cert);
        var verifier = new PadesPdfVerifier();

        var signed = signer.SignPdf(TestPdf.OnePage(), reason: "test", location: "test");

        // It is still a PDF, and it carries the ISO 32000 / ETSI signature markers.
        Assert.Equal("%PDF-", System.Text.Encoding.ASCII.GetString(signed, 0, 5));
        Assert.Contains("ByteRange", AsLatin1(signed));
        Assert.Contains("ETSI.CAdES.detached", AsLatin1(signed));

        var sigs = verifier.Verify(signed);
        var sig = Assert.Single(sigs);
        Assert.True(sig.IntegrityValid, "signature integrity should verify");
        Assert.True(sig.CoversWholeDocument, "signature should cover the whole document");
        Assert.Equal("PAdES Test Authority", sig.SignerCommonName);
    }

    [Fact]
    public void Tampering_with_a_signed_pdf_breaks_verification()
    {
        using var cert = TestPdf.NewCert();
        var signer = new PadesPdfSigner(cert);
        var verifier = new PadesPdfVerifier();
        var signed = signer.SignPdf(TestPdf.OnePage(), "test", "test");

        // Flip a byte inside the signed region — this must not still read as a valid signature.
        var tampered = (byte[])signed.Clone();
        tampered[signed.Length / 2] ^= 0xFF;

        bool stillValid;
        try
        {
            var sigs = verifier.Verify(tampered);
            stillValid = sigs.Count == 1 && sigs[0].IntegrityValid && sigs[0].CoversWholeDocument;
        }
        catch
        {
            stillValid = false; // a corrupted PDF that won't even parse is also "not valid"
        }

        Assert.False(stillValid);
    }

    [Fact]
    public void Verify_returns_nothing_for_an_unsigned_pdf()
    {
        var verifier = new PadesPdfVerifier();

        var sigs = verifier.Verify(TestPdf.OnePage());

        Assert.Empty(sigs);
    }

    private static string AsLatin1(byte[] bytes) => System.Text.Encoding.Latin1.GetString(bytes);
}
