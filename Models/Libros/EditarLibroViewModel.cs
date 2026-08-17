using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Lecturio.Models.Libros;

public class EditarLibroViewModel
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "El título es obligatorio.")]
    [Display(Name = "Título")]
    public string Titulo { get; set; } = string.Empty;

    [Display(Name = "Autor")]
    public string? Autor { get; set; }

    [Display(Name = "Géneros")]
    public List<Guid> GeneroIds { get; set; } = [];

    [Display(Name = "Presentación")]
    public Guid? PresentacionId { get; set; }

    [Display(Name = "Portada")]
    public IFormFile? Portada { get; set; }

    public string? PortadaUrlActual { get; set; }

    public List<SelectListItem> GenerosDisponibles { get; set; } = [];
    public List<SelectListItem> PresentacionesDisponibles { get; set; } = [];
}
