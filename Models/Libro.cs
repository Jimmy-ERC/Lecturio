namespace Lecturio.Models;

public class Libro
{
    public Guid Id { get; set; }
    public Guid UsuarioId { get; set; }
    public Guid? PresentacionId { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string? Autor { get; set; }
    public string PdfPath { get; set; } = string.Empty;
    public string? PortadaUrl { get; set; }
    public int PaginaActual { get; set; }
    public int? TotalPaginas { get; set; }
    public string Estado { get; set; } = EstadoLibro.SinLeer;
    public DateTimeOffset CreatedAt { get; set; }

    public Usuario Usuario { get; set; } = null!;
    public Presentacion? Presentacion { get; set; }
    public ICollection<LibroGenero> LibrosGeneros { get; set; } = [];
    public ICollection<Compartido> Compartidos { get; set; } = [];
}
