namespace Lecturio.Models;

public class Presentacion
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;

    public ICollection<Libro> Libros { get; set; } = [];
}
