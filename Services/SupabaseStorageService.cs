using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text.Json.Nodes;
using Lecturio.Configuration;
using Microsoft.Extensions.Options;

namespace Lecturio.Services;

/// <summary>
/// Habla directamente con la API Storage de Supabase (storage/v1) por HTTP.
/// No usa el SDK supabase-csharp, según la decisión de arquitectura del proyecto
/// (ver SupabaseAuthService). El HttpClient inyectado se autentica con la Service
/// Role Key (ver Program.cs), así que todas las escrituras aquí corren con
/// privilegios de servidor y esta key nunca debe llegar al cliente.
/// </summary>
public class SupabaseStorageService(HttpClient httpClient, IOptions<SupabaseOptions> options) : ISupabaseStorageService
{
    private readonly SupabaseOptions _options = options.Value;

    public Task<SupabaseStorageResult> UploadPdfAsync(string path, Stream content, string contentType, CancellationToken cancellationToken = default) =>
        UploadAsync(_options.StorageBucket, path, content, contentType, cancellationToken);

    public Task<SupabaseStorageResult> UploadPortadaAsync(string path, Stream content, string contentType, CancellationToken cancellationToken = default) =>
        UploadAsync(_options.CoverBucket, path, content, contentType, cancellationToken);

    public Task<Stream?> DownloadPdfAsync(string path, CancellationToken cancellationToken = default) =>
        DownloadAsync(_options.StorageBucket, path, cancellationToken);

    public Task<Stream?> DownloadPortadaAsync(string path, CancellationToken cancellationToken = default) =>
        DownloadAsync(_options.CoverBucket, path, cancellationToken);

    public Task<bool> DeletePdfAsync(string path, CancellationToken cancellationToken = default) =>
        DeleteAsync(_options.StorageBucket, path, cancellationToken);

    public Task<bool> DeletePortadaAsync(string path, CancellationToken cancellationToken = default) =>
        DeleteAsync(_options.CoverBucket, path, cancellationToken);

    public Task<string?> GetPdfUrlAsync(string path, int expiresInSeconds = 3600, CancellationToken cancellationToken = default) =>
        GetUrlAsync(_options.StorageBucket, path, _options.PdfBucketIsPublic, expiresInSeconds, cancellationToken);

    public Task<string?> GetPortadaUrlAsync(string path, int expiresInSeconds = 3600, CancellationToken cancellationToken = default) =>
        GetUrlAsync(_options.CoverBucket, path, _options.CoverBucketIsPublic, expiresInSeconds, cancellationToken);

    private async Task<SupabaseStorageResult> UploadAsync(string bucket, string path, Stream content, string contentType, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"object/{bucket}/{EncodePath(path)}");
        request.Headers.Add("x-upsert", "true");
        request.Content = new StreamContent(content);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        var response = await httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return SupabaseStorageResult.Fail(await ExtractErrorMessageAsync(response, cancellationToken));
        }

        return SupabaseStorageResult.Ok(path);
    }

    private async Task<Stream?> DownloadAsync(string bucket, string path, CancellationToken cancellationToken)
    {
        var response = await httpClient.GetAsync($"object/{bucket}/{EncodePath(path)}", HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadAsStreamAsync(cancellationToken);
    }

    private async Task<bool> DeleteAsync(string bucket, string path, CancellationToken cancellationToken)
    {
        var response = await httpClient.DeleteAsync($"object/{bucket}/{EncodePath(path)}", cancellationToken);
        return response.IsSuccessStatusCode;
    }

    private async Task<string?> GetUrlAsync(string bucket, string path, bool isPublic, int expiresInSeconds, CancellationToken cancellationToken)
    {
        if (isPublic)
        {
            return $"{_options.Url.TrimEnd('/')}/storage/v1/object/public/{bucket}/{EncodePath(path)}";
        }

        var response = await httpClient.PostAsJsonAsync(
            $"object/sign/{bucket}/{EncodePath(path)}",
            new { expiresIn = expiresInSeconds },
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var body = await response.Content.ReadFromJsonAsync<JsonObject>(cancellationToken: cancellationToken);
        var signedUrl = body?["signedURL"]?.GetValue<string>();

        return signedUrl is null ? null : $"{_options.Url.TrimEnd('/')}/storage/v1{signedUrl}";
    }

    private static async Task<string> ExtractErrorMessageAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(content))
        {
            return $"Supabase Storage respondió {(int)response.StatusCode}.";
        }

        try
        {
            var body = JsonNode.Parse(content);
            var message = body?["message"]?.GetValue<string>() ?? body?["error"]?.GetValue<string>();
            return message ?? content;
        }
        catch
        {
            return content;
        }
    }

    private static string EncodePath(string path) =>
        string.Join('/', path.Split('/', StringSplitOptions.RemoveEmptyEntries).Select(Uri.EscapeDataString));
}
