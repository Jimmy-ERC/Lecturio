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

    // Firma la URL con vigencia larga para que las peticiones por rangos que hace el
    // visor durante toda la sesión de lectura no expiren a mitad de la lectura.
    private const int PdfReadUrlExpirationSeconds = 4 * 60 * 60;

    [HttpGet]
    public async Task<IActionResult> Leer(Guid id)
    {
        var usuarioId = GetUsuarioId();
        var libro = await db.Libros.FirstOrDefaultAsync(l => l.Id == id);
        if (libro is null || !await TieneAccesoAsync(id, libro.UsuarioId, usuarioId))
        {
            return NotFound();
        }

        var pdfUrl = await storageService.GetPdfUrlAsync(libro.PdfPath, PdfReadUrlExpirationSeconds);
        if (pdfUrl is null)
        {
            return Problem("No se pudo generar el enlace de lectura del PDF.");
        }

        var progreso = await db.ProgresosLectura
            .FirstOrDefaultAsync(p => p.LibroId == id && p.UsuarioId == usuarioId);

        return View(new LeerLibroViewModel
        {
            Id = libro.Id,
            Titulo = libro.Titulo,
            Autor = libro.Autor,
            PdfUrl = pdfUrl,
            PaginaInicial = progreso is { PaginaActual: > 0 } ? progreso.PaginaActual : 1,
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ActualizarProgreso([FromForm] Guid id, [FromForm] int pagina, [FromForm] int totalPaginas)
    {
        var usuarioId = GetUsuarioId();
        var libro = await db.Libros.FirstOrDefaultAsync(l => l.Id == id);
        if (libro is null || !await TieneAccesoAsync(id, libro.UsuarioId, usuarioId))
        {
            return NotFound();
        }

        var progreso = await db.ProgresosLectura
            .FirstOrDefaultAsync(p => p.LibroId == id && p.UsuarioId == usuarioId);

        if (progreso is null)
        {
            progreso = new ProgresoLectura { LibroId = id, UsuarioId = usuarioId };
            db.ProgresosLectura.Add(progreso);
        }

        var paginaActual = Math.Max(0, pagina);

        progreso.PaginaActual = paginaActual;
        if (totalPaginas > 0)
        {
            progreso.TotalPaginas = totalPaginas;
        }

        progreso.Estado = progreso.TotalPaginas is > 0 && paginaActual >= progreso.TotalPaginas
            ? EstadoLibro.Leido
            : paginaActual > 0
                ? EstadoLibro.Leyendo
                : EstadoLibro.SinLeer;

        await db.SaveChangesAsync();

        return Ok();
    }

    [HttpGet]
    public async Task<IActionResult> Crear()
    {
        return View(new CrearLibroViewModel
        {
            GenerosDisponibles = await GetGenerosSelectListAsync(),
            PresentacionesDisponibles = await GetPresentacionesSelectListAsync(),
        });
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
            model.PresentacionesDisponibles = await GetPresentacionesSelectListAsync();
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
            PresentacionId = model.PresentacionId,
            Titulo = model.Titulo,
            Autor = model.Autor,
            PdfPath = pdfPath,
            PortadaUrl = portadaPath,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        db.Libros.Add(libro);
        db.ProgresosLectura.Add(new ProgresoLectura { LibroId = libro.Id, UsuarioId = usuarioId });

        foreach (var generoId in model.GeneroIds.Distinct())
        {
            db.LibrosGeneros.Add(new LibroGenero { LibroId = libro.Id, GeneroId = generoId });
        }

        await db.SaveChangesAsync();

        TempData["Mensaje"] = "Libro agregado correctamente.";
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public async Task<IActionResult> Editar(Guid id)
    {
        var usuarioId = GetUsuarioId();
        var libro = await db.Libros
            .Include(l => l.LibrosGeneros)
            .FirstOrDefaultAsync(l => l.Id == id && l.UsuarioId == usuarioId);

        if (libro is null)
        {
            return NotFound();
        }

        var portadaUrlActual = libro.PortadaUrl is null
            ? null
            : await storageService.GetPortadaUrlAsync(libro.PortadaUrl);

        return View(new EditarLibroViewModel
        {
            Id = libro.Id,
            Titulo = libro.Titulo,
            Autor = libro.Autor,
            PresentacionId = libro.PresentacionId,
            GeneroIds = libro.LibrosGeneros.Select(lg => lg.GeneroId).ToList(),
            PortadaUrlActual = portadaUrlActual,
            GenerosDisponibles = await GetGenerosSelectListAsync(),
            PresentacionesDisponibles = await GetPresentacionesSelectListAsync(),
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(PortadaMaxBytes + 1024 * 1024)]
    public async Task<IActionResult> Editar(EditarLibroViewModel model)
    {
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

        var usuarioId = GetUsuarioId();
        var libro = await db.Libros
            .Include(l => l.LibrosGeneros)
            .FirstOrDefaultAsync(l => l.Id == model.Id && l.UsuarioId == usuarioId);

        if (libro is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            model.PortadaUrlActual = libro.PortadaUrl is null ? null : await storageService.GetPortadaUrlAsync(libro.PortadaUrl);
            model.GenerosDisponibles = await GetGenerosSelectListAsync();
            model.PresentacionesDisponibles = await GetPresentacionesSelectListAsync();
            return View(model);
        }

        if (model.Portada is not null)
        {
            var portadaPath = $"{usuarioId}/{libro.Id}{Path.GetExtension(model.Portada.FileName)}";
            await using var portadaStream = model.Portada.OpenReadStream();
            var portadaUpload = await storageService.UploadPortadaAsync(portadaPath, portadaStream, model.Portada.ContentType);

            if (!portadaUpload.Success)
            {
                ModelState.AddModelError(string.Empty, portadaUpload.ErrorMessage ?? "No se pudo subir la portada a Supabase Storage.");
                model.PortadaUrlActual = libro.PortadaUrl is null ? null : await storageService.GetPortadaUrlAsync(libro.PortadaUrl);
                model.GenerosDisponibles = await GetGenerosSelectListAsync();
                model.PresentacionesDisponibles = await GetPresentacionesSelectListAsync();
                return View(model);
            }

            if (libro.PortadaUrl is not null && libro.PortadaUrl != portadaPath)
            {
                await storageService.DeletePortadaAsync(libro.PortadaUrl);
            }

            libro.PortadaUrl = portadaPath;
        }

        libro.Titulo = model.Titulo;
        libro.Autor = model.Autor;
        libro.PresentacionId = model.PresentacionId;

        db.LibrosGeneros.RemoveRange(libro.LibrosGeneros);
        foreach (var generoId in model.GeneroIds.Distinct())
        {
            db.LibrosGeneros.Add(new LibroGenero { LibroId = libro.Id, GeneroId = generoId });
        }

        await db.SaveChangesAsync();

        TempData["Mensaje"] = "Libro actualizado correctamente.";
        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Compartir(Guid id, string email)
    {
        var usuarioId = GetUsuarioId();
        var libro = await db.Libros.FirstOrDefaultAsync(l => l.Id == id && l.UsuarioId == usuarioId);
        if (libro is null)
        {
            return NotFound();
        }

        var emailNormalizado = (email ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(emailNormalizado))
        {
            TempData["Mensaje"] = "Ingresa un correo válido.";
            return RedirectToAction("Index", "Home");
        }

        var destinatario = await db.Usuarios.FirstOrDefaultAsync(u => u.Email == emailNormalizado);
        if (destinatario is null)
        {
            TempData["Mensaje"] = "No hay ningún usuario registrado con ese correo.";
            return RedirectToAction("Index", "Home");
        }

        if (destinatario.Id == usuarioId)
        {
            TempData["Mensaje"] = "No puedes compartir un libro contigo mismo.";
            return RedirectToAction("Index", "Home");
        }

        var yaCompartido = await db.Compartidos.AnyAsync(c => c.LibroId == id && c.CompartidoCon == destinatario.Id);
        if (yaCompartido)
        {
            TempData["Mensaje"] = $"Ya habías compartido «{libro.Titulo}» con {destinatario.Nombre}.";
            return RedirectToAction("Index", "Home");
        }

        db.Compartidos.Add(new Compartido
        {
            Id = Guid.NewGuid(),
            LibroId = id,
            CompartidoPor = usuarioId,
            CompartidoCon = destinatario.Id,
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();

        TempData["Mensaje"] = $"Libro compartido con {destinatario.Nombre}.";
        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(Guid id)
    {
        var usuarioId = GetUsuarioId();
        var libro = await db.Libros.FirstOrDefaultAsync(l => l.Id == id && l.UsuarioId == usuarioId);
        if (libro is null)
        {
            return NotFound();
        }

        await storageService.DeletePdfAsync(libro.PdfPath);
        if (libro.PortadaUrl is not null)
        {
            await storageService.DeletePortadaAsync(libro.PortadaUrl);
        }

        db.Libros.Remove(libro);
        await db.SaveChangesAsync();

        TempData["Mensaje"] = "Libro eliminado.";
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

    private async Task<bool> TieneAccesoAsync(Guid libroId, Guid libroUsuarioId, Guid usuarioId) =>
        libroUsuarioId == usuarioId ||
        await db.Compartidos.AnyAsync(c => c.LibroId == libroId && c.CompartidoCon == usuarioId);

    private async Task<List<SelectListItem>> GetGenerosSelectListAsync() =>
        await db.Generos
            .OrderBy(g => g.Nombre)
            .Select(g => new SelectListItem { Value = g.Id.ToString(), Text = g.Nombre })
            .ToListAsync();

    private async Task<List<SelectListItem>> GetPresentacionesSelectListAsync() =>
        await db.Presentaciones
            .OrderBy(p => p.Nombre)
            .Select(p => new SelectListItem { Value = p.Id.ToString(), Text = p.Nombre })
            .ToListAsync();
}
