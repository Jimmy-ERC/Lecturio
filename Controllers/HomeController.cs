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
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            var resumenes = new List<LibroResumenViewModel>(libros.Count);
            foreach (var libro in libros)
            {
                resumenes.Add(new LibroResumenViewModel
                {
                    Id = libro.Id,
                    Titulo = libro.Titulo,
                    Autor = libro.Autor,
                    Estado = libro.Estado,
                    Progreso = libro.TotalPaginas is > 0
                        ? Math.Clamp((int)Math.Round(libro.PaginaActual * 100.0 / libro.TotalPaginas.Value), 0, 100)
                        : 0,
                    PortadaUrl = libro.PortadaUrl is null
                        ? null
                        : await storageService.GetPortadaUrlAsync(libro.PortadaUrl),
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
