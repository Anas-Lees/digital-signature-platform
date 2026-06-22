using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace SignVault.Api.Tests;

/// <summary>End-to-end HTTP tests through the real pipeline: register -> sign -> verify.</summary>
public class ApiIntegrationTests : IClassFixture<TestApiFactory>
{
    private readonly TestApiFactory _factory;
    public ApiIntegrationTests(TestApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Upload_sign_and_verify_a_pdf_end_to_end()
    {
        var client = _factory.CreateClient();
        await AuthenticateAsync(client);

        // Upload a real PDF.
        var docId = await UploadPdfAsync(client, TestPdf.OnePage(), "contract.pdf");

        // Sign it -> PAdES.
        var sign = await client.PostAsync($"/api/documents/{docId}/sign", null);
        Assert.Equal(HttpStatusCode.OK, sign.StatusCode);
        var signDoc = await ReadJsonAsync(sign);
        Assert.Equal("PAdES B-B (SHA256withRSA)", signDoc.GetProperty("algorithm").GetString());

        // The signed PDF download carries the embedded signature.
        var signed = await client.GetAsync($"/api/documents/{docId}/signed");
        Assert.Equal(HttpStatusCode.OK, signed.StatusCode);
        var signedBytes = await signed.Content.ReadAsByteArrayAsync();
        Assert.Equal("%PDF-", System.Text.Encoding.ASCII.GetString(signedBytes, 0, 5));
        Assert.Contains("ETSI.CAdES.detached", System.Text.Encoding.Latin1.GetString(signedBytes));

        // Public verify-by-file says valid.
        var verify = await VerifyFileAsync(client, signedBytes, "signed.pdf");
        Assert.True(verify.GetProperty("valid").GetBoolean());
        Assert.False(string.IsNullOrEmpty(verify.GetProperty("signerName").GetString()));
        Assert.True(verify.GetProperty("coversWholeDocument").GetBoolean());

        // Public verify-by-id (share link), no auth needed.
        using var anon = _factory.CreateClient();
        var byId = await anon.GetAsync($"/api/verify/{docId}");
        Assert.Equal(HttpStatusCode.OK, byId.StatusCode);
        Assert.True((await ReadJsonAsync(byId)).GetProperty("valid").GetBoolean());
    }

    [Fact]
    public async Task Uploading_a_non_pdf_is_rejected()
    {
        var client = _factory.CreateClient();
        await AuthenticateAsync(client);

        var txt = System.Text.Encoding.UTF8.GetBytes("I am not a PDF");
        using var form = new MultipartFormDataContent();
        var part = new ByteArrayContent(txt);
        part.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        form.Add(part, "file", "note.txt");

        var resp = await client.PostAsync("/api/documents/upload", form);

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Documents_require_authentication()
    {
        using var anon = _factory.CreateClient();

        var resp = await anon.GetAsync("/api/documents");

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task Deleting_a_document_is_not_allowed()
    {
        var client = _factory.CreateClient();
        await AuthenticateAsync(client);
        var docId = await UploadPdfAsync(client, TestPdf.OnePage(), "keep-me.pdf");

        var resp = await client.DeleteAsync($"/api/documents/{docId}");

        // There is intentionally no delete endpoint (retention / non-repudiation).
        Assert.True(resp.StatusCode is HttpStatusCode.MethodNotAllowed or HttpStatusCode.NotFound,
            $"expected 404/405, got {(int)resp.StatusCode}");
    }

    [Fact]
    public async Task Verify_certificate_endpoint_returns_an_x509_cert()
    {
        using var anon = _factory.CreateClient();

        var resp = await anon.GetAsync("/api/verify/certificate");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Equal("application/x-x509-ca-cert", resp.Content.Headers.ContentType?.MediaType);
        Assert.True((await resp.Content.ReadAsByteArrayAsync()).Length > 0);
    }

    // ── helpers ──────────────────────────────────────────────────────────────────

    private static async Task AuthenticateAsync(HttpClient client)
    {
        var email = $"user-{Guid.NewGuid():N}@test.local";
        var reg = await client.PostAsJsonAsync("/api/auth/register",
            new { email, password = "Passw0rd!", displayName = "Test User" });
        reg.EnsureSuccessStatusCode();
        var token = (await ReadJsonAsync(reg)).GetProperty("token").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private static async Task<string> UploadPdfAsync(HttpClient client, byte[] pdf, string fileName)
    {
        using var form = new MultipartFormDataContent();
        var part = new ByteArrayContent(pdf);
        part.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        form.Add(part, "file", fileName);
        var resp = await client.PostAsync("/api/documents/upload", form);
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        return (await ReadJsonAsync(resp)).GetProperty("id").GetString()!;
    }

    private static async Task<JsonElement> VerifyFileAsync(HttpClient client, byte[] pdf, string fileName)
    {
        using var form = new MultipartFormDataContent();
        var part = new ByteArrayContent(pdf);
        part.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        form.Add(part, "file", fileName);
        var resp = await client.PostAsync("/api/verify", form);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        return await ReadJsonAsync(resp);
    }

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage resp) =>
        JsonDocument.Parse(await resp.Content.ReadAsStringAsync()).RootElement.Clone();
}
