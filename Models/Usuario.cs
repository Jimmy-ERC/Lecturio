namespace Lecturio.Models;

/// <summary>
/// Perfil de aplicación que extiende auth.users de Supabase. El Id es el mismo
/// que el de auth.users (no lo genera EF Core, se asigna al completar el registro).
/// </summary>
public class Usuario
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// Espejo del email de auth.users, usado para buscar a quién compartir un libro.
    /// Se sincroniza cada vez que el usuario inicia sesión.
    /// </summary>
    public string? Email { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<Libro> Libros { get; set; } = [];
    public ICollection<Compartido> LibrosCompartidosPorMi { get; set; } = [];
    public ICollection<Compartido> LibrosCompartidosConmigo { get; set; } = [];
    public ICollection<ProgresoLectura> ProgresosLectura { get; set; } = [];
}
