using System.Text;
using SignVault.Api.Services;

namespace SignVault.Api.Tests;

public class RsaSignerTests
{
    private static readonly byte[] Data = Encoding.UTF8.GetBytes("the quick brown fox");

    [Fact]
    public void Sign_then_Verify_with_the_same_key_is_valid()
    {
        using var cert = TestPdf.NewCert();
        var signer = new RsaSigner(cert);

        var signature = signer.Sign(Data);

        Assert.True(signer.Verify(Data, signature));
    }

    [Fact]
    public void Verify_fails_when_the_data_is_tampered()
    {
        using var cert = TestPdf.NewCert();
        var signer = new RsaSigner(cert);
        var signature = signer.Sign(Data);

        var tampered = Encoding.UTF8.GetBytes("the quick brown FOX"); // one char changed

        Assert.False(signer.Verify(tampered, signature));
    }

    [Fact]
    public void Verify_fails_for_a_signature_from_a_different_key_wrong_owner()
    {
        using var ours = TestPdf.NewCert("Authority A");
        using var theirs = TestPdf.NewCert("Authority B");
        var ourSigner = new RsaSigner(ours);
        var theirSigner = new RsaSigner(theirs);

        var foreignSignature = theirSigner.Sign(Data);

        Assert.False(ourSigner.Verify(Data, foreignSignature));
    }

    [Fact]
    public void Thumbprint_and_public_key_are_exposed()
    {
        using var cert = TestPdf.NewCert();
        var signer = new RsaSigner(cert);

        Assert.Equal(cert.Thumbprint, signer.Thumbprint);
        Assert.Equal("SHA256withRSA", signer.Algorithm);
        Assert.Contains("BEGIN PUBLIC KEY", signer.PublicKeyPem);
    }
}
