using Npgsql;

namespace Lecturio.Configuration;

public class SupabaseOptions
{
    public const string SectionName = "Supabase";

    public string Url { get; set; } = string.Empty;
    public string AnonKey { get; set; } = string.Empty;
    public string ServiceRoleKey { get; set; } = string.Empty;
    public string DbConnectionString { get; set; } = string.Empty;
    public string StorageBucket { get; set; } = string.Empty;
    public string CoverBucket { get; set; } = string.Empty;

    /// <summary>
    /// Debe coincidir con la política real del bucket en Supabase: true si el bucket
    /// libros-pdf está marcado como público, false si es privado y hay que servirlo
    /// con URLs firmadas.
    /// </summary>
    public bool PdfBucketIsPublic { get; set; }

    /// <summary>
    /// Igual que PdfBucketIsPublic pero para el bucket portadas.
    /// </summary>
    public bool CoverBucketIsPublic { get; set; }

    /// <summary>
    /// Supabase entrega DbConnectionString como URI (postgresql://user:pass@host:port/db),
    /// pero Npgsql espera el formato clave=valor. Esta conversión evita tener que mantener
    /// dos formatos distintos de la misma cadena en la configuración.
    /// </summary>
    public string GetNpgsqlConnectionString()
    {
        var uri = new Uri(DbConnectionString);
        var userInfo = uri.UserInfo.Split(':', 2);

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port,
            Username = Uri.UnescapeDataString(userInfo[0]),
            Password = Uri.UnescapeDataString(userInfo[1]),
            Database = uri.AbsolutePath.TrimStart('/'),
            SslMode = SslMode.Require,

            // Los proyectos gratuitos de Supabase se pausan por inactividad y la primera
            // conexión tras "despertar" puede tardar 20-30s. Con los valores por defecto de
            // Npgsql (Timeout=15s, CommandTimeout=30s) esa primera consulta expira.
            Timeout = 30,
            CommandTimeout = 60,
        };

        return builder.ConnectionString;
    }
}
