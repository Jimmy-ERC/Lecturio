namespace Lecturio.Models;

/// <summary>
/// Valores permitidos por el check constraint de libros.estado en la base de datos.
/// Deben coincidir exactamente (mayúsculas/tildes) con los literales del CHECK en Postgres.
/// </summary>
public static class EstadoLibro
{
    public const string SinLeer = "Sin leer";
    public const string Leyendo = "Leyendo";
    public const string Leido = "Leído";

    public static readonly string[] Todos = [SinLeer, Leyendo, Leido];
}
