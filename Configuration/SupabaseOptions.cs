namespace Lecturio.Configuration;

public class SupabaseOptions
{
    public const string SectionName = "Supabase";

    public string Url { get; set; } = string.Empty;
    public string AnonKey { get; set; } = string.Empty;
    public string ServiceRoleKey { get; set; } = string.Empty;
    public string DbConnectionString { get; set; } = string.Empty;
    public string StorageBucket { get; set; } = string.Empty;
}
