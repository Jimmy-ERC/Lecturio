using System.Diagnostics;
using System.Security.Claims;
using Lecturio.Data;
using Lecturio.Models;
using Lecturio.Models.Libros;
using Lecturio.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Lecturio.Controllers
{
    [Authorize]
    public class HomeController(LecturioDbContext db, ISupabaseStorageService storageService) : Controller
    {
        public async Task<IActionResult> Index()
        {
            var usuarioId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var libros = await db.Libros
                .Where(l => l.UsuarioId == usuarioId)
                .Include(l => l.Presentacion)
                .Include(l => l.LibrosGeneros).ThenInclude(lg => lg.Genero)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            var progresos = await db.ProgresosLectura
                .Where(p => p.UsuarioId == usuarioId)
                .ToDictionaryAsync(p => p.LibroId);

            var resumenes = new List<LibroResumenViewModel>(libros.Count);
            foreach (var libro in libros)
            {
                progresos.TryGetValue(libro.Id, out var progreso);

                resumenes.Add(new LibroResumenViewModel
                {
                    Id = libro.Id,
                    Titulo = libro.Titulo,
                    Autor = libro.Autor,
                    Estado = progreso?.Estado ?? EstadoLibro.SinLeer,
                    Progreso = progreso?.TotalPaginas is > 0
                        ? Math.Clamp((int)Math.Round(progreso.PaginaActual * 100.0 / progreso.TotalPaginas.Value), 0, 100)
                        : 0,
                    PortadaUrl = libro.PortadaUrl is null
                        ? null
                        : await storageService.GetPortadaUrlAsync(libro.PortadaUrl),
                    Presentacion = libro.Presentacion?.Nombre,
                    Generos = libro.LibrosGeneros.Select(lg => lg.Genero.Nombre).OrderBy(n => n).ToList(),
                });
            }

            return View(resumenes);
        }

        public async Task<IActionResult> Compartidos()
        {
            var usuarioId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var compartidos = await db.Compartidos
                .Where(c => c.CompartidoCon == usuarioId)
                .Include(c => c.Libro).ThenInclude(l => l.Presentacion)
                .Include(c => c.Libro).ThenInclude(l => l.LibrosGeneros).ThenInclude(lg => lg.Genero)
                .Include(c => c.UsuarioQueComparte)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            var libroIds = compartidos.Select(c => c.LibroId).ToList();
            var progresos = await db.ProgresosLectura
                .Where(p => p.UsuarioId == usuarioId && libroIds.Contains(p.LibroId))
                .ToDictionaryAsync(p => p.LibroId);

            var resumenes = new List<LibroResumenViewModel>(compartidos.Count);
            foreach (var compartido in compartidos)
            {
                progresos.TryGetValue(compartido.LibroId, out var progreso);

                resumenes.Add(new LibroResumenViewModel
                {
                    Id = compartido.Libro.Id,
                    Titulo = compartido.Libro.Titulo,
                    Autor = compartido.Libro.Autor,
                    Estado = progreso?.Estado ?? EstadoLibro.SinLeer,
                    Progreso = progreso?.TotalPaginas is > 0
                        ? Math.Clamp((int)Math.Round(progreso.PaginaActual * 100.0 / progreso.TotalPaginas.Value), 0, 100)
                        : 0,
                    PortadaUrl = compartido.Libro.PortadaUrl is null
                        ? null
                        : await storageService.GetPortadaUrlAsync(compartido.Libro.PortadaUrl),
                    Presentacion = compartido.Libro.Presentacion?.Nombre,
                    Generos = compartido.Libro.LibrosGeneros.Select(lg => lg.Genero.Nombre).OrderBy(n => n).ToList(),
                    CompartidoPorNombre = compartido.UsuarioQueComparte.Nombre,
                });
            }

            return View(resumenes);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
