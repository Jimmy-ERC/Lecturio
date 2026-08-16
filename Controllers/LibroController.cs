using System.Security.Claims;
using Lecturio.Data;
using Lecturio.Models;
using Lecturio.Models.Libros;
using Lecturio.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Lecturio.Controllers;

[Authorize]
public class LibroController(ISupabaseStorageService storageService, LecturioDbContext db) : Controller
{
    private const long PdfMaxBytes = 100 * 1024 * 1024;
    private const long PortadaMaxBytes = 5 * 1024 * 1024;

    // Kestrel rechaza (cortando la conexión, sin mensaje) cualquier request de más de ~28.6 MB
    // por defecto. Sin este límite explícito por encima de PdfMaxBytes + PortadaMaxBytes, subir
    // un PDF de tamaño normal nunca llega a ejecutar ValidarArchivos.
    private const long RequestBodyMaxBytes = PdfMaxBytes + PortadaMaxBytes + (10 * 1024 * 1024);
    private static readonly string[] PortadaContentTypesPermitidos = ["image/jpeg", "image/png", "image/webp"];

    [HttpGet]
    public async Task<IActionResult> Crear()
    {
        return View(new CrearLibroViewModel { GenerosDisponibles = await GetGenerosSelectListAsync() });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(RequestBodyMaxBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = RequestBodyMaxBytes)]
    public async Task<IActionResult> Crear(CrearLibroViewModel model)
    {
        ValidarArchivos(model);

        if (!ModelState.IsValid)
        {
            model.GenerosDisponibles = await GetGenerosSelectListAsync();
            return View(model);
        }

        var usuarioId = GetUsuarioId();
        var libroId = Guid.NewGuid();

        var pdfPath = $"{usuarioId}/{libroId}{Path.GetExtension(model.Pdf.FileName)}";
        await using (var pdfStream = model.Pdf.OpenReadStream())
        {
            var pdfUpload = await storageService.UploadPdfAsync(pdfPath, pdfStream, model.Pdf.ContentType);
            if (!pdfUpload.Success)
            {
                ModelState.AddModelError(string.Empty, pdfUpload.ErrorMessage ?? "No se pudo subir el PDF a Supabase Storage.");
                model.GenerosDisponibles = await GetGenerosSelectListAsync();
                return View(model);
            }
        }

        string? portadaPath = null;
        if (model.Portada is not null)
        {
            portadaPath = $"{usuarioId}/{libroId}{Path.GetExtension(model.Portada.FileName)}";
            await using var portadaStream = model.Portada.OpenReadStream();
            var portadaUpload = await storageService.UploadPortadaAsync(portadaPath, portadaStream, model.Portada.ContentType);

            if (!portadaUpload.Success)
            {
                await storageService.DeletePdfAsync(pdfPath);
                ModelState.AddModelError(string.Empty, portadaUpload.ErrorMessage ?? "No se pudo subir la portada a Supabase Storage.");
                model.GenerosDisponibles = await GetGenerosSelectListAsync();
                return View(model);
            }
        }

        var libro = new Libro
        {
            Id = libroId,
            UsuarioId = usuarioId,
            Titulo = model.Titulo,
            Autor = model.Autor,
            PdfPath = pdfPath,
            PortadaUrl = portadaPath,
            Estado = EstadoLibro.SinLeer,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        db.Libros.Add(libro);

        foreach (var generoId in model.GeneroIds.Distinct())
        {
            db.LibrosGeneros.Add(new LibroGenero { LibroId = libro.Id, GeneroId = generoId });
        }

        await db.SaveChangesAsync();

        TempData["Mensaje"] = "Libro agregado correctamente.";
        return RedirectToAction("Index", "Home");
    }

    private void ValidarArchivos(CrearLibroViewModel model)
    {
        if (model.Pdf is not null)
        {
            if (!string.Equals(model.Pdf.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(model.Pdf), "El archivo debe ser un PDF.");
            }
            else if (model.Pdf.Length > PdfMaxBytes)
            {
                ModelState.AddModelError(nameof(model.Pdf), "El PDF no puede superar los 100 MB.");
            }
        }

        if (model.Portada is not null)
        {
            if (!PortadaContentTypesPermitidos.Contains(model.Portada.ContentType, StringComparer.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(model.Portada), "La portada debe ser una imagen JPG, PNG o WEBP.");
            }
            else if (model.Portada.Length > PortadaMaxBytes)
            {
                ModelState.AddModelError(nameof(model.Portada), "La portada no puede superar los 5 MB.");
            }
        }
    }

    private Guid GetUsuarioId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private async Task<List<SelectListItem>> GetGenerosSelectListAsync() =>
        await db.Generos
            .OrderBy(g => g.Nombre)
            .Select(g => new SelectListItem { Value = g.Id.ToString(), Text = g.Nombre })
            .ToListAsync();
}
