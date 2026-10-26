using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using CxP360.Api.Data;
using CxP360.Api.Dtos.Proveedores;
using CxP360.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CxP360.Api.Controllers;

/// <summary>
/// Módulo de proveedores (RF-09 a RF-13). Lo usan Administrador y Operador.
/// Los proveedores no se eliminan: se inactivan (RN-05).
/// </summary>
[ApiController]
[Route("api/proveedores")]
public class ProveedoresController(CxP360DbContext db) : ControllerBase
{
    /// <summary>
    /// Convierte un proveedor en ProveedorDto. SQL Server calcula el saldo
    /// (RN-09: suma de saldos de facturas no anuladas) en la misma consulta.
    /// </summary>
    private static readonly Expression<Func<Proveedor, ProveedorDto>> AProveedorDto = p => new ProveedorDto(
        p.Id, p.Nit, p.Nombre, p.Contacto, p.Telefono, p.Correo, p.Direccion,
        p.DiasCredito, p.Activo, p.FechaRegistro,
        p.Facturas.Where(f => f.Estado != EstadoFactura.Anulada).Sum(f => (decimal?)f.Saldo) ?? 0m,
        p.Facturas.Count(f => f.Estado == EstadoFactura.Pendiente || f.Estado == EstadoFactura.Parcial));

    /// <summary>GET /api/proveedores?buscar=cemento&amp;soloActivos=true (RF-10)</summary>
    [HttpGet]
    public async Task<ActionResult<List<ProveedorDto>>> Listar(string? buscar = null, bool soloActivos = false)
    {
        var consulta = db.Proveedores.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var texto = buscar.Trim();
            consulta = consulta.Where(p => p.Nit.Contains(texto) || p.Nombre.Contains(texto));
        }

        if (soloActivos)
            consulta = consulta.Where(p => p.Activo);

        return Ok(await consulta.OrderBy(p => p.Nombre).Select(AProveedorDto).ToListAsync());
    }

    /// <summary>GET /api/proveedores/{id} (RF-13: incluye el saldo)</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProveedorDto>> ObtenerPorId(int id)
    {
        var proveedor = await db.Proveedores.AsNoTracking()
            .Where(p => p.Id == id)
            .Select(AProveedorDto)
            .FirstOrDefaultAsync()
            ?? throw new NoEncontradoException($"No existe el proveedor {id}.");

        return Ok(proveedor);
    }

    /// <summary>POST /api/proveedores (RF-09)</summary>
    [HttpPost]
    public async Task<ActionResult<ProveedorDto>> Crear(GuardarProveedorRequest request)
    {
        var nit = NormalizarNit(request.Nit);
        if (await db.Proveedores.AnyAsync(p => p.Nit == nit))
            throw new ConflictoException($"Ya existe un proveedor con el NIT {nit}.");          // RN-06

        // RN-07: si no se indican días de crédito, se usan los de la configuración
        var diasCredito = request.DiasCredito
            ?? await db.ConfiguracionEmpresa.Select(c => c.DiasCreditoDefault).FirstAsync();

        var proveedor = new Proveedor
        {
            Nit = nit,
            Nombre = request.Nombre.Trim(),
            Contacto = Limpiar(request.Contacto),
            Telefono = Limpiar(request.Telefono),
            Correo = ValidarCorreo(request.Correo),
            Direccion = Limpiar(request.Direccion),
            DiasCredito = diasCredito,
            Activo = true
        };

        db.Proveedores.Add(proveedor);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(ObtenerPorId), new { id = proveedor.Id }, await LeerDto(proveedor.Id));
    }

    /// <summary>PUT /api/proveedores/{id} (RF-11)</summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProveedorDto>> Actualizar(int id, GuardarProveedorRequest request)
    {
        var proveedor = await db.Proveedores.FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new NoEncontradoException($"No existe el proveedor {id}.");

        var nit = NormalizarNit(request.Nit);
        if (await db.Proveedores.AnyAsync(p => p.Nit == nit && p.Id != id))
            throw new ConflictoException($"Ya existe otro proveedor con el NIT {nit}.");        // RN-06

        proveedor.Nit = nit;
        proveedor.Nombre = request.Nombre.Trim();
        proveedor.Contacto = Limpiar(request.Contacto);
        proveedor.Telefono = Limpiar(request.Telefono);
        proveedor.Correo = ValidarCorreo(request.Correo);
        proveedor.Direccion = Limpiar(request.Direccion);
        proveedor.DiasCredito = request.DiasCredito ?? proveedor.DiasCredito;

        await db.SaveChangesAsync();
        return Ok(await LeerDto(id));
    }

    /// <summary>PUT /api/proveedores/{id}/estado (RF-12)</summary>
    [HttpPut("{id:int}/estado")]
    public async Task<ActionResult<ProveedorDto>> CambiarEstado(int id, CambiarEstadoRequest request)
    {
        var proveedor = await db.Proveedores.FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new NoEncontradoException($"No existe el proveedor {id}.");

        proveedor.Activo = request.Activo;
        await db.SaveChangesAsync();

        return Ok(await LeerDto(id));
    }

    // ---------- Auxiliares ----------

    private async Task<ProveedorDto> LeerDto(int id) =>
        await db.Proveedores.AsNoTracking().Where(p => p.Id == id).Select(AProveedorDto).FirstAsync();

    private static string NormalizarNit(string nit) => nit.Trim().ToUpperInvariant();

    /// <summary>Convierte textos vacíos en NULL.</summary>
    private static string? Limpiar(string? texto) =>
        string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();

    private static string? ValidarCorreo(string? correo)
    {
        var limpio = Limpiar(correo);
        if (limpio is not null && !new EmailAddressAttribute().IsValid(limpio))
            throw new ReglaNegocioException("El correo electrónico no es válido.");
        return limpio;
    }
}
