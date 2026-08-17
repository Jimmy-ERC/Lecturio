namespace Lecturio.Models.Libros;

public class LeerLibroViewModel
{
    public Guid Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string? Autor { get; set; }
    public string PdfUrl { get; set; } = string.Empty;
    public int PaginaInicial { get; set; } = 1;
}
