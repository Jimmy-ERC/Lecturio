namespace Lecturio.Services;

public class SupabaseAuthResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// Cuando el signup tiene éxito pero Supabase requiere confirmar el correo antes de
    /// entregar una sesión (access_token/refresh_token ausentes en la respuesta).
    /// </summary>
    public bool RequiresEmailConfirmation { get; init; }

    public static SupabaseAuthResult Fail(string errorMessage) =>
        new() { Success = false, ErrorMessage = errorMessage };
}
