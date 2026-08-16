using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Lecturio.Models.Libros;

public class CrearLibroViewModel
{
    [Required(ErrorMessage = "El título es obligatorio.")]
    [Display(Name = "Título")]
    public string Titulo { get; set; } = string.Empty;

    [Display(Name = "Autor")]
    public string? Autor { get; set; }

    [Display(Name = "Géneros")]
    public List<Guid> GeneroIds { get; set; } = [];

    [Required(ErrorMessage = "Selecciona el archivo PDF del libro.")]
    [Display(Name = "Archivo PDF")]
    public IFormFile Pdf { get; set; } = null!;

    [Display(Name = "Portada")]
    public IFormFile? Portada { get; set; }

    public List<SelectListItem> GenerosDisponibles { get; set; } = [];
}
