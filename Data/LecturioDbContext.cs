using Lecturio.Models;
using Microsoft.EntityFrameworkCore;

namespace Lecturio.Data;

/// <summary>
/// Mapea las tablas ya existentes en Supabase (creadas manualmente, no por migraciones de EF Core).
/// No se generan ni aplican migraciones desde este DbContext: el esquema es la fuente de verdad.
/// </summary>
public class LecturioDbContext(DbContextOptions<LecturioDbContext> options) : DbContext(options)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Genero> Generos => Set<Genero>();
    public DbSet<Presentacion> Presentaciones => Set<Presentacion>();
    public DbSet<Libro> Libros => Set<Libro>();
    public DbSet<LibroGenero> LibrosGeneros => Set<LibroGenero>();
    public DbSet<Compartido> Compartidos => Set<Compartido>();
    public DbSet<ProgresoLectura> ProgresosLectura => Set<ProgresoLectura>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Usuario>(e =>
        {
            e.ToTable("usuarios");
            e.HasKey(u => u.Id);
            e.Property(u => u.Id).HasColumnName("id").ValueGeneratedNever();
            e.Property(u => u.Nombre).HasColumnName("nombre").IsRequired();
            e.Property(u => u.Email).HasColumnName("email");
            e.Property(u => u.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            e.HasIndex(u => u.Email);
        });

        modelBuilder.Entity<Genero>(e =>
        {
            e.ToTable("generos");
            e.HasKey(g => g.Id);
            e.Property(g => g.Id).HasColumnName("id");
            e.Property(g => g.Nombre).HasColumnName("nombre").IsRequired();
            e.HasIndex(g => g.Nombre).IsUnique();
        });

        modelBuilder.Entity<Presentacion>(e =>
        {
            e.ToTable("presentacion");
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).HasColumnName("id");
            e.Property(p => p.Nombre).HasColumnName("nombre").IsRequired();
            e.HasIndex(p => p.Nombre).IsUnique();
        });

        modelBuilder.Entity<Libro>(e =>
        {
            e.ToTable("libros");
            e.HasKey(l => l.Id);
            e.Property(l => l.Id).HasColumnName("id");
            e.Property(l => l.UsuarioId).HasColumnName("usuario_id");
            e.Property(l => l.PresentacionId).HasColumnName("presentacion_id");
            e.Property(l => l.Titulo).HasColumnName("titulo").IsRequired();
            e.Property(l => l.Autor).HasColumnName("autor");
            e.Property(l => l.PdfPath).HasColumnName("pdf_path").IsRequired();
            e.Property(l => l.PortadaUrl).HasColumnName("portada_url");
            e.Property(l => l.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");

            e.HasOne(l => l.Usuario)
                .WithMany(u => u.Libros)
                .HasForeignKey(l => l.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(l => l.Presentacion)
                .WithMany(p => p.Libros)
                .HasForeignKey(l => l.PresentacionId);
        });

        modelBuilder.Entity<LibroGenero>(e =>
        {
            e.ToTable("libros_generos");
            e.HasKey(lg => new { lg.LibroId, lg.GeneroId });
            e.Property(lg => lg.LibroId).HasColumnName("libro_id");
            e.Property(lg => lg.GeneroId).HasColumnName("genero_id");

            e.HasOne(lg => lg.Libro)
                .WithMany(l => l.LibrosGeneros)
                .HasForeignKey(lg => lg.LibroId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(lg => lg.Genero)
                .WithMany(g => g.LibrosGeneros)
                .HasForeignKey(lg => lg.GeneroId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Compartido>(e =>
        {
            e.ToTable("compartidos");
            e.HasKey(c => c.Id);
            e.Property(c => c.Id).HasColumnName("id");
            e.Property(c => c.LibroId).HasColumnName("libro_id");
            e.Property(c => c.CompartidoPor).HasColumnName("compartido_por");
            e.Property(c => c.CompartidoCon).HasColumnName("compartido_con");
            e.Property(c => c.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            e.HasIndex(c => new { c.LibroId, c.CompartidoCon }).IsUnique();

            e.HasOne(c => c.Libro)
                .WithMany(l => l.Compartidos)
                .HasForeignKey(c => c.LibroId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(c => c.UsuarioQueComparte)
                .WithMany(u => u.LibrosCompartidosPorMi)
                .HasForeignKey(c => c.CompartidoPor)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(c => c.UsuarioDestinatario)
                .WithMany(u => u.LibrosCompartidosConmigo)
                .HasForeignKey(c => c.CompartidoCon)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProgresoLectura>(e =>
        {
            e.ToTable("progreso_lectura");
            e.HasKey(p => new { p.LibroId, p.UsuarioId });
            e.Property(p => p.LibroId).HasColumnName("libro_id");
            e.Property(p => p.UsuarioId).HasColumnName("usuario_id");
            e.Property(p => p.PaginaActual).HasColumnName("pagina_actual").HasDefaultValue(0);
            e.Property(p => p.TotalPaginas).HasColumnName("total_paginas");
            e.Property(p => p.Estado).HasColumnName("estado").HasDefaultValue(EstadoLibro.SinLeer).IsRequired();

            e.HasOne(p => p.Libro)
                .WithMany(l => l.ProgresosLectura)
                .HasForeignKey(p => p.LibroId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(p => p.Usuario)
                .WithMany(u => u.ProgresosLectura)
                .HasForeignKey(p => p.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
