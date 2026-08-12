namespace Lecturio.Models;

/// <summary>
/// Tabla puente: un libro puede tener varios géneros. Clave primaria compuesta (LibroId, GeneroId).
/// </summary>
public class LibroGenero
{
    public Guid LibroId { get; set; }
    public Guid GeneroId { get; set; }

    public Libro Libro { get; set; } = null!;
    public Genero Genero { get; set; } = null!;
}
