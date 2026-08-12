namespace Lecturio.Services;

public interface ISupabaseAuthService
{
    Task<SupabaseAuthResult> SignUpAsync(string email, string password, string nombre);
    Task<SupabaseAuthResult> SignInWithPasswordAsync(string email, string password);
    Task<bool> SendPasswordRecoveryAsync(string email);
}
