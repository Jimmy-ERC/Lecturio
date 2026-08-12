namespace Lecturio.Models;

public class Compartido
{
    public Guid Id { get; set; }
    public Guid LibroId { get; set; }
    public Guid CompartidoPor { get; set; }
    public Guid CompartidoCon { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Libro Libro { get; set; } = null!;
    public Usuario UsuarioQueComparte { get; set; } = null!;
    public Usuario UsuarioDestinatario { get; set; } = null!;
}
