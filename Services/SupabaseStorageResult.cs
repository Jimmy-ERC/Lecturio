namespace Lecturio.Services;

public class SupabaseStorageResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public string? Path { get; init; }

    public static SupabaseStorageResult Ok(string path) => new() { Success = true, Path = path };
    public static SupabaseStorageResult Fail(string errorMessage) => new() { Success = false, ErrorMessage = errorMessage };
}
