namespace Lecturio.Models;

/// <summary>
/// Perfil de aplicación que extiende auth.users de Supabase. El Id es el mismo
/// que el de auth.users (no lo genera EF Core, se asigna al completar el registro).
/// </summary>
public class Usuario
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<Libro> Libros { get; set; } = [];
    public ICollection<Compartido> LibrosCompartidosPorMi { get; set; } = [];
    public ICollection<Compartido> LibrosCompartidosConmigo { get; set; } = [];
}
