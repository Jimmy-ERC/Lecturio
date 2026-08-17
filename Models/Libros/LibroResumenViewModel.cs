namespace Lecturio.Models.Libros;

public class LibroResumenViewModel
{
    public Guid Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string? Autor { get; set; }
    public string Estado { get; set; } = EstadoLibro.SinLeer;
    public int Progreso { get; set; }
    public string? PortadaUrl { get; set; }
    public string? Presentacion { get; set; }

    /// <summary>No se muestran en la card; solo alimentan el filtro por género de la biblioteca.</summary>
    public List<string> Generos { get; set; } = [];

    /// <summary>Solo se usa en "Compartidos conmigo": nombre de quien compartió el libro.</summary>
    public string? CompartidoPorNombre { get; set; }
}
