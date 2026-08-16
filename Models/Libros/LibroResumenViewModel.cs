namespace Lecturio.Models.Libros;

public class LibroResumenViewModel
{
    public Guid Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string? Autor { get; set; }
    public string Estado { get; set; } = EstadoLibro.SinLeer;
    public int Progreso { get; set; }
    public string? PortadaUrl { get; set; }
}
