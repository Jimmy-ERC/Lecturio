using System.Security.Claims;
using Lecturio.Data;
using Lecturio.Models;
using Lecturio.Models.Auth;
using Lecturio.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Lecturio.Controllers;

public class AuthController(ISupabaseAuthService authService, LecturioDbContext db) : Controller
{
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToLocal(returnUrl);
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await authService.SignInWithPasswordAsync(model.Email, model.Password);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Correo o contraseña incorrectos.");
            return View(model);
        }

        var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Id == result.UserId);
        if (usuario is null)
        {
            // Sesión válida en Supabase Auth pero sin fila de perfil todavía (p.ej. quedó pendiente
            // tras un signup con confirmación de correo). La creamos ahora con un nombre por defecto.
            usuario = new Usuario
            {
                Id = result.UserId,
                Nombre = result.Email,
                Email = result.Email.ToLowerInvariant(),
                CreatedAt = DateTimeOffset.UtcNow,
            };
            db.Usuarios.Add(usuario);
            await db.SaveChangesAsync();
        }
        else if (!string.Equals(usuario.Email, result.Email, StringComparison.OrdinalIgnoreCase))
        {
            // Mantiene el email espejado al día (necesario para poder compartir libros por correo).
            usuario.Email = result.Email.ToLowerInvariant();
            await db.SaveChangesAsync();
        }

        await SignInUserAsync(usuario.Id, result.Email, usuario.Nombre, model.Recuerdame);

        return RedirectToLocal(model.ReturnUrl);
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await authService.SignUpAsync(model.Email, model.Password, model.Nombre);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "No se pudo crear la cuenta.");
            return View(model);
        }

        var yaExiste = await db.Usuarios.AnyAsync(u => u.Id == result.UserId);
        if (!yaExiste)
        {
            db.Usuarios.Add(new Usuario
            {
                Id = result.UserId,
                Nombre = model.Nombre,
                Email = model.Email.ToLowerInvariant(),
                CreatedAt = DateTimeOffset.UtcNow,
            });
            await db.SaveChangesAsync();
        }

        if (result.RequiresEmailConfirmation)
        {
            TempData["Mensaje"] = "Cuenta creada. Revisa tu correo para confirmarla antes de iniciar sesión.";
            return RedirectToAction(nameof(Login));
        }

        await SignInUserAsync(result.UserId, result.Email, model.Nombre, persistent: false);

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult ForgotPassword()
    {
        return View(new ForgotPasswordViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        await authService.SendPasswordRecoveryAsync(model.Email);

        // Siempre mostramos el mismo mensaje exista o no la cuenta, para no filtrar qué
        // correos están registrados.
        TempData["Mensaje"] = "Si el correo está registrado, te enviamos un enlace para restablecer tu contraseña.";
        return RedirectToAction(nameof(Login));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    private async Task SignInUserAsync(Guid userId, string email, string nombre, bool persistent)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Name, nombre),
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties { IsPersistent = persistent });
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }
}
