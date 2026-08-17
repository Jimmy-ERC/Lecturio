namespace Lecturio.Models;

/// <summary>
/// Avance de lectura de UN usuario sobre UN libro. Vive separado de Libro para que,
/// cuando exista la función de compartir, cada persona (dueño incluido) tenga su
/// propio progreso sin pisar el de los demás lectores del mismo libro.
/// </summary>
public class ProgresoLectura
{
    public Guid LibroId { get; set; }
    public Guid UsuarioId { get; set; }
    public int PaginaActual { get; set; }
    public int? TotalPaginas { get; set; }
    public string Estado { get; set; } = EstadoLibro.SinLeer;

    public Libro Libro { get; set; } = null!;
    public Usuario Usuario { get; set; } = null!;
}
