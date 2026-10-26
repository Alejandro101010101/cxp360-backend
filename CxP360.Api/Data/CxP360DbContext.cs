using CxP360.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CxP360.Api.Data;

/// <summary>
/// Puente entre las clases de C# y las tablas de SQL Server.
/// La base de datos ya existe (script 01), así que aquí solo se indica
/// cómo se corresponde cada clase con su tabla.
/// </summary>
public class CxP360DbContext(DbContextOptions<CxP360DbContext> options) : DbContext(options)
{
    public DbSet<ConfiguracionEmpresa> ConfiguracionEmpresa => Set<ConfiguracionEmpresa>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Proveedor> Proveedores => Set<Proveedor>();
    public DbSet<Factura> Facturas => Set<Factura>();
    public DbSet<DetalleFactura> DetallesFactura => Set<DetalleFactura>();
    public DbSet<Pago> Pagos => Set<Pago>();
    public DbSet<FormaPago> FormasPago => Set<FormaPago>();
    public DbSet<Banco> Bancos => Set<Banco>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Todos los montos son DECIMAL(12,2) salvo que se indique otra precisión (RN-01)
        configurationBuilder.Properties<decimal>().HavePrecision(12, 2);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ---------- Parámetros ----------
        modelBuilder.Entity<ConfiguracionEmpresa>(e =>
        {
            e.ToTable("ConfiguracionEmpresa");
            e.Property(x => x.Id).HasColumnName("ConfiguracionId");
            e.Property(x => x.PorcentajeIva).HasPrecision(5, 2);
        });

        modelBuilder.Entity<FormaPago>(e =>
        {
            e.ToTable("FormasPago");
            e.Property(x => x.Id).HasColumnName("FormaPagoId");
        });

        modelBuilder.Entity<Banco>(e =>
        {
            e.ToTable("Bancos");
            e.Property(x => x.Id).HasColumnName("BancoId");
        });

        // ---------- Seguridad ----------
        modelBuilder.Entity<Usuario>(e =>
        {
            e.ToTable("Usuarios");
            e.Property(x => x.Id).HasColumnName("UsuarioId");
            e.Property(x => x.Rol).HasConversion<string>();                       // se guarda como texto
            e.Property(x => x.FechaCreacion).HasDefaultValueSql("SYSDATETIME()");
        });

        // ---------- Proveedores ----------
        modelBuilder.Entity<Proveedor>(e =>
        {
            e.ToTable("Proveedores");
            e.Property(x => x.Id).HasColumnName("ProveedorId");
            e.Property(x => x.FechaRegistro).HasDefaultValueSql("SYSDATETIME()");

            e.HasMany(x => x.Facturas)
             .WithOne(f => f.Proveedor)
             .HasForeignKey(f => f.ProveedorId)
             .OnDelete(DeleteBehavior.Restrict);                                   // RN-05
        });

        // ---------- Facturas ----------
        modelBuilder.Entity<Factura>(e =>
        {
            e.ToTable("Facturas");
            e.Property(x => x.Id).HasColumnName("FacturaId");
            e.Property(x => x.Estado).HasConversion<string>();                    // se guarda como texto
            e.Property(x => x.FechaRegistro).HasDefaultValueSql("SYSDATETIME()");

            // Control de concurrencia: si dos usuarios pagan la misma factura al mismo
            // tiempo, el segundo recibe un error en lugar de sobrescribir el saldo.
            e.Property(x => x.Saldo).IsConcurrencyToken();

            e.HasOne(x => x.Usuario)
             .WithMany()
             .HasForeignKey(x => x.UsuarioId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.UsuarioAnulacion)
             .WithMany()
             .HasForeignKey(x => x.UsuarioAnulacionId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasMany(x => x.Detalles)                                             // composición
             .WithOne(d => d.Factura)
             .HasForeignKey(d => d.FacturaId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasMany(x => x.Pagos)
             .WithOne(p => p.Factura)
             .HasForeignKey(p => p.FacturaId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DetalleFactura>(e =>
        {
            e.ToTable("DetalleFactura");
            e.Property(x => x.Id).HasColumnName("DetalleId");
            e.Property(x => x.Cantidad).HasPrecision(10, 2);
            e.Property(x => x.Subtotal)
             .HasComputedColumnSql("CAST(ROUND([Cantidad] * [PrecioUnitario], 2) AS DECIMAL(12,2))", stored: true);
        });

        // ---------- Pagos ----------
        modelBuilder.Entity<Pago>(e =>
        {
            e.ToTable("Pagos");
            e.Property(x => x.Id).HasColumnName("PagoId");
            e.Property(x => x.FechaRegistro).HasDefaultValueSql("SYSDATETIME()");

            e.HasOne(x => x.FormaPago)
             .WithMany()
             .HasForeignKey(x => x.FormaPagoId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Banco)
             .WithMany()
             .HasForeignKey(x => x.BancoId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Usuario)
             .WithMany()
             .HasForeignKey(x => x.UsuarioId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.UsuarioAnulacion)
             .WithMany()
             .HasForeignKey(x => x.UsuarioAnulacionId)
             .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
