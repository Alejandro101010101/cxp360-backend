using CxP360.Api.Data;
using CxP360.Api.Dtos.Comun;
using CxP360.Api.Dtos.Facturas;
using CxP360.Api.Dtos.Pagos;
using CxP360.Api.Extensiones;
using CxP360.Api.Models;
using CxP360.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CxP360.Api.Controllers;

/// <summary>
/// Ingresos por compra (RF-14 a RF-19, RF-30). Lo usan Administrador y Operador.
/// Las facturas no se modifican ni se eliminan: se anulan (RN-16).
/// </summary>
[ApiController]
[Route("api/facturas")]
public class FacturasController(CxP360DbContext db) : ControllerBase
{
    /// <summary>
    /// GET /api/facturas?proveedorId=1&amp;estado=Vencida&amp;desde=2026-10-01&amp;hasta=2026-10-31 (RF-17)
    /// estado: Pendiente, Parcial, Pagada, Anulada o Vencida
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<FacturaResumenDto>>> Listar(
        int? proveedorId = null, string? estado = null, DateOnly? desde = null, DateOnly? hasta = null)
    {
        var hoy = Reloj.Hoy;
        var consulta = db.Facturas.AsNoTracking();

        if (proveedorId is not null)
            consulta = consulta.Where(f => f.ProveedorId == proveedorId);

        if (!string.IsNullOrWhiteSpace(estado))
        {
            if (estado.Trim().Equals("Vencida", StringComparison.OrdinalIgnoreCase))
                consulta = consulta.Where(f =>
                    (f.Estado == EstadoFactura.Pendiente || f.Estado == EstadoFactura.Parcial) && f.FechaVencimiento < hoy);
            else if (Enum.TryParse<EstadoFactura>(estado, ignoreCase: true, out var e) && !int.TryParse(estado, out _))
                consulta = consulta.Where(f => f.Estado == e);
            else
                throw new ReglaNegocioException("Estado no válido. Use Pendiente, Parcial, Pagada, Anulada o Vencida.");
        }

        if (desde is not null)
            consulta = consulta.Where(f => f.FechaEmision >= desde);
        if (hasta is not null)
            consulta = consulta.Where(f => f.FechaEmision <= hasta);

        var filas = await consulta
            .OrderByDescending(f => f.FechaEmision).ThenByDescending(f => f.Id)
            .Select(f => new
            {
                f.Id, f.ProveedorId, ProveedorNombre = f.Proveedor.Nombre, ProveedorNit = f.Proveedor.Nit,
                f.Serie, f.Numero, f.FechaEmision, f.FechaVencimiento, f.Total, f.Saldo, f.Estado
            })
            .ToListAsync();

        return Ok(filas.Select(f =>
        {
            var (vencida, dias) = CalcularVencida(f.Estado, f.FechaVencimiento, hoy);
            return new FacturaResumenDto(f.Id, f.ProveedorId, f.ProveedorNombre, f.ProveedorNit, f.Serie, f.Numero,
                f.FechaEmision, f.FechaVencimiento, f.Total, f.Saldo, f.Estado, vencida, dias);
        }).ToList());
    }

    /// <summary>GET /api/facturas/{id}: detalle con líneas e historial de pagos (RF-18)</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<FacturaDto>> ObtenerPorId(int id) => Ok(await LeerDetalle(id));

    /// <summary>POST /api/facturas (RF-14, RF-15, RF-16, RF-30)</summary>
    [HttpPost]
    public async Task<ActionResult<FacturaDto>> Crear(CrearFacturaRequest request)
    {
        var proveedor = await db.Proveedores.FirstOrDefaultAsync(p => p.Id == request.ProveedorId)
            ?? throw new NoEncontradoException($"No existe el proveedor {request.ProveedorId}.");

        // RN-08: un proveedor inactivo no recibe facturas nuevas
        if (!proveedor.PuedeRecibirFacturas())
            throw new ReglaNegocioException($"El proveedor {proveedor.Nombre} está inactivo y no puede recibir facturas nuevas.");

        var serie = request.Serie.Trim().ToUpperInvariant();
        var numero = request.Numero.Trim().ToUpperInvariant();

        // RN-10 / RF-30: no se repite proveedor + serie + número
        if (await db.Facturas.AnyAsync(f => f.ProveedorId == proveedor.Id && f.Serie == serie && f.Numero == numero))
            throw new ConflictoException($"La factura {serie}-{numero} ya está registrada para {proveedor.Nombre}.");

        // RN-11: la fecha de emisión no puede ser futura
        if (request.FechaEmision > Reloj.Hoy)
            throw new ReglaNegocioException("La fecha de emisión no puede ser posterior a hoy.");

        var porcentajeIva = await db.ConfiguracionEmpresa.Select(c => c.PorcentajeIva).FirstAsync();

        var factura = new Factura
        {
            ProveedorId = proveedor.Id,
            Serie = serie,
            Numero = numero,
            FechaEmision = request.FechaEmision,
            UsuarioId = User.ObtenerUsuarioId(),          // RN-04: quién la registró
            FechaRegistro = Reloj.Ahora,
            Detalles = request.Lineas.Select(l => new DetalleFactura
            {
                Descripcion = l.Descripcion.Trim(),
                Cantidad = Math.Round(l.Cantidad, 2, MidpointRounding.AwayFromZero),
                PrecioUnitario = Math.Round(l.PrecioUnitario, 2, MidpointRounding.AwayFromZero)
            }).ToList()
        };

        factura.CalcularVencimiento(proveedor.DiasCredito);   // RN-13
        factura.CalcularTotales(porcentajeIva);               // RN-14: total, base e IVA

        // RN-35: el total de las líneas debe coincidir con el total impreso en la factura
        var totalDocumento = Math.Round(request.TotalDocumento, 2, MidpointRounding.AwayFromZero);
        if (factura.Total != totalDocumento)
            throw new ReglaNegocioException(
                $"La suma de las líneas (Q{factura.Total:N2}) no coincide con el total de la factura " +
                $"(Q{totalDocumento:N2}). Diferencia: Q{Math.Abs(factura.Total - totalDocumento):N2}. Revise cantidades y precios.");

        // RN-32: la factura se guarda junto con su foto o PDF
        factura.Adjuntos.Add(Comprobantes.Crear(request.Comprobante, factura.UsuarioId));

        db.Facturas.Add(factura);
        await db.SaveChangesAsync();                          // encabezado + líneas + comprobante en una transacción

        return CreatedAtAction(nameof(ObtenerPorId), new { id = factura.Id }, await LeerDetalle(factura.Id));
    }

    /// <summary>PUT /api/facturas/{id}/anular (RF-19, RN-17)</summary>
    [HttpPut("{id:int}/anular")]
    public async Task<ActionResult<FacturaDto>> Anular(int id, AnularRequest request)
    {
        var factura = await db.Facturas.Include(f => f.Pagos).FirstOrDefaultAsync(f => f.Id == id)
            ?? throw new NoEncontradoException($"No existe la factura {id}.");

        factura.Anular(request.Motivo, User.ObtenerUsuarioId(), Reloj.Ahora);
        await db.SaveChangesAsync();

        return Ok(await LeerDetalle(id));
    }

    /// <summary>POST /api/facturas/{id}/comprobantes: agrega otro documento a la factura (RF-34)</summary>
    [HttpPost("{id:int}/comprobantes")]
    public async Task<ActionResult<AdjuntoDto>> AgregarComprobante(int id, ArchivoRequest archivo)
    {
        if (!await db.Facturas.AnyAsync(f => f.Id == id))
            throw new NoEncontradoException($"No existe la factura {id}.");

        var adjunto = Comprobantes.Crear(archivo, User.ObtenerUsuarioId());
        adjunto.FacturaId = id;
        db.Adjuntos.Add(adjunto);
        await db.SaveChangesAsync();

        return Created($"/api/adjuntos/{adjunto.Id}", await AdjuntosController.LeerDto(db, adjunto.Id));
    }

    // ---------- Auxiliares ----------

    private async Task<FacturaDto> LeerDetalle(int id)
    {
        var f = await db.Facturas.AsNoTracking()
            .Include(x => x.Proveedor)
            .Include(x => x.Usuario)
            .Include(x => x.UsuarioAnulacion)
            .Include(x => x.Detalles)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new NoEncontradoException($"No existe la factura {id}.");

        var pagos = await db.Pagos.AsNoTracking()
            .Where(p => p.FacturaId == id)
            .OrderBy(p => p.FechaPago).ThenBy(p => p.Id)
            .Select(PagoMapeo.APagoDto)
            .ToListAsync();

        var comprobantes = await db.Adjuntos.AsNoTracking()
            .Where(a => a.FacturaId == id)
            .OrderBy(a => a.Id)
            .Select(AdjuntosController.AAdjuntoDto)
            .ToListAsync();

        var (vencida, dias) = CalcularVencida(f.Estado, f.FechaVencimiento, Reloj.Hoy);
        var totalPagado = f.Estado == EstadoFactura.Anulada ? 0m : f.Total - f.Saldo;

        return new FacturaDto(
            f.Id, f.ProveedorId, f.Proveedor.Nombre, f.Proveedor.Nit, f.Serie, f.Numero,
            f.FechaEmision, f.FechaVencimiento, f.Base, f.Iva, f.Total, totalPagado, f.Saldo,
            f.Estado, vencida, dias,
            f.Usuario.NombreCompleto, f.FechaRegistro,
            f.MotivoAnulacion, f.UsuarioAnulacion?.NombreCompleto, f.FechaAnulacion,
            f.Detalles.OrderBy(d => d.Id)
                .Select(d => new LineaFacturaDto(d.Id, d.Descripcion, d.Cantidad, d.PrecioUnitario, d.Subtotal))
                .ToList(),
            pagos,
            comprobantes);
    }

    /// <summary>RN-19: vencida = pendiente o parcial con fecha de vencimiento pasada.</summary>
    private static (bool Vencida, int Dias) CalcularVencida(EstadoFactura estado, DateOnly vencimiento, DateOnly hoy)
    {
        var vencida = (estado == EstadoFactura.Pendiente || estado == EstadoFactura.Parcial) && vencimiento < hoy;
        return (vencida, vencida ? hoy.DayNumber - vencimiento.DayNumber : 0);
    }
}
