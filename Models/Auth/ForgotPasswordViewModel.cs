using System.ComponentModel.DataAnnotations;

namespace Lecturio.Models.Auth;

public class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "Ingresa un correo válido.")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;
}
