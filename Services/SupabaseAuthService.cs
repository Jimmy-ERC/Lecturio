using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace Lecturio.Services;

/// <summary>
/// Habla directamente con la API GoTrue de Supabase Auth (auth/v1) por HTTP.
/// No usa el SDK supabase-csharp, según la decisión de arquitectura del proyecto.
/// </summary>
public class SupabaseAuthService(HttpClient httpClient) : ISupabaseAuthService
{
    public async Task<SupabaseAuthResult> SignUpAsync(string email, string password, string nombre)
    {
        var response = await httpClient.PostAsJsonAsync("signup", new
        {
            email,
            password,
            data = new { nombre },
        });

        var body = await ReadJsonAsync(response);

        if (!response.IsSuccessStatusCode)
        {
            return SupabaseAuthResult.Fail(ExtractErrorMessage(body));
        }

        // Con confirmación de correo activada, GoTrue responde 200 con el objeto de usuario
        // pero sin access_token/refresh_token (todavía no hay sesión).
        var userNode = body?["user"] as JsonObject ?? body as JsonObject;
        var userId = userNode?["id"]?.GetValue<string>();
        var userEmail = userNode?["email"]?.GetValue<string>() ?? email;

        if (userId is null)
        {
            return SupabaseAuthResult.Fail("Supabase no devolvió un usuario válido.");
        }

        var hasSession = body?["access_token"] is not null;

        return new SupabaseAuthResult
        {
            Success = true,
            UserId = Guid.Parse(userId),
            Email = userEmail,
            RequiresEmailConfirmation = !hasSession,
        };
    }

    public async Task<SupabaseAuthResult> SignInWithPasswordAsync(string email, string password)
    {
        var response = await httpClient.PostAsJsonAsync("token?grant_type=password", new
        {
            email,
            password,
        });

        var body = await ReadJsonAsync(response);

        if (!response.IsSuccessStatusCode)
        {
            return SupabaseAuthResult.Fail(ExtractErrorMessage(body) ?? "Correo o contraseña incorrectos.");
        }

        var userId = body?["user"]?["id"]?.GetValue<string>();
        var userEmail = body?["user"]?["email"]?.GetValue<string>() ?? email;

        if (userId is null)
        {
            return SupabaseAuthResult.Fail("Supabase no devolvió un usuario válido.");
        }

        return new SupabaseAuthResult
        {
            Success = true,
            UserId = Guid.Parse(userId),
            Email = userEmail,
        };
    }

    public async Task<bool> SendPasswordRecoveryAsync(string email)
    {
        var response = await httpClient.PostAsJsonAsync("recover", new { email });
        return response.IsSuccessStatusCode;
    }

    private static async Task<JsonNode?> ReadJsonAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        try
        {
            return JsonNode.Parse(content);
        }
        catch
        {
            return null;
        }
    }

    private static string ExtractErrorMessage(JsonNode? body)
    {
        var message = body?["msg"]?.GetValue<string>()
            ?? body?["error_description"]?.GetValue<string>()
            ?? body?["message"]?.GetValue<string>()
            ?? body?["error"]?.GetValue<string>();

        return message ?? "Ocurrió un error al comunicarse con Supabase Auth.";
    }
}
