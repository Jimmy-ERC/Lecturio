namespace Lecturio.Services;

public interface ISupabaseStorageService
{
    Task<SupabaseStorageResult> UploadPdfAsync(string path, Stream content, string contentType, CancellationToken cancellationToken = default);
    Task<SupabaseStorageResult> UploadPortadaAsync(string path, Stream content, string contentType, CancellationToken cancellationToken = default);

    Task<Stream?> DownloadPdfAsync(string path, CancellationToken cancellationToken = default);
    Task<Stream?> DownloadPortadaAsync(string path, CancellationToken cancellationToken = default);

    Task<bool> DeletePdfAsync(string path, CancellationToken cancellationToken = default);
    Task<bool> DeletePortadaAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// URL para servir el PDF al cliente: firmada si Supabase:PdfBucketIsPublic es false,
    /// pública si es true. Null si el objeto no existe o Supabase rechazó la firma.
    /// </summary>
    Task<string?> GetPdfUrlAsync(string path, int expiresInSeconds = 3600, CancellationToken cancellationToken = default);

    /// <summary>
    /// URL para servir la portada al cliente: firmada si Supabase:CoverBucketIsPublic es false,
    /// pública si es true. Null si el objeto no existe o Supabase rechazó la firma.
    /// </summary>
    Task<string?> GetPortadaUrlAsync(string path, int expiresInSeconds = 3600, CancellationToken cancellationToken = default);
}
