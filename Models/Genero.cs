namespace Lecturio.Models;

public class Genero
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;

    public ICollection<LibroGenero> LibrosGeneros { get; set; } = [];
}
