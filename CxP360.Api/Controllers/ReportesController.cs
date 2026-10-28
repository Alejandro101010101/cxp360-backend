using CxP360.Api.Data;
using CxP360.Api.Dtos.Pagos;
using CxP360.Api.Dtos.Reportes;
using CxP360.Api.Models;
using CxP360.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CxP360.Api.Controllers;

/// <summary>
/// Consultas y reportes (RF-25 a RF-28). Las facturas y pagos anulados
/// nunca se incluyen en saldos ni totales (RN-28).
/// La impresión y el PDF (RF-29) se generan en el frontend a partir de estos datos.
/// </summary>
[ApiController]
[Route("api/reportes")]
public class ReportesController(CxP360DbContext db) : ControllerBase
{
    // ==================================================================
    // ESTADO DE CUENTA POR PROVEEDOR (RF-25, RN-27)
    // GET /api/reportes/estado-cuenta?proveedorId=1&desde=2026-10-01&hasta=2026-10-31
    // ==================================================================
    [HttpGet("estado-cuenta")]
    public async Task<ActionResult<EstadoCuentaDto>> EstadoCuenta(int proveedorId, DateOnly? desde = null, DateOnly? hasta = null)
    {
        var (inicio, fin) = Periodo(desde, hasta);

        var proveedor = await db.Proveedores.AsNoTracking().FirstOrDefaultAsync(p => p.Id == proveedorId)
            ?? throw new NoEncontradoException($"No existe el proveedor {proveedorId}.");

        var facturas = db.Facturas.AsNoTracking()
            .Where(f => f.ProveedorId == proveedorId && f.Estado != EstadoFactura.Anulada);
        var pagos = db.Pagos.AsNoTracking()
            .Where(p => p.Factura.ProveedorId == proveedorId && !p.Anulado);

        // RN-27: saldo inicial = facturas menos pagos ANTES del período
        var cargosAntes = await facturas.Where(f => f.FechaEmision < inicio).SumAsync(f => (decimal?)f.Total) ?? 0m;
        var abonosAntes = await pagos.Where(p => p.FechaPago < inicio).SumAsync(p => (decimal?)p.Monto) ?? 0m;
        var saldoInicial = cargosAntes - abonosAntes;

        // Cargos: facturas emitidas en el período
        var cargos = await facturas
            .Where(f => f.FechaEmision >= inicio && f.FechaEmision <= fin)
            .Select(f => new { f.Id, f.FechaEmision, f.Serie, f.Numero, f.FechaVencimiento, f.Total, Usuario = f.Usuario.NombreCompleto })
            .ToListAsync();

        // Abonos: pagos realizados en el período
        var abonos = await pagos
            .Where(p => p.FechaPago >= inicio && p.FechaPago <= fin)
            .Select(p => new
            {
                p.Id, p.FechaPago, p.Monto, p.NumeroReferencia,
                FormaPago = p.FormaPago.Nombre,
                Banco = p.Banco != null ? p.Banco.Nombre : null,
                p.Factura.Serie, p.Factura.Numero,
                Usuario = p.Usuario.NombreCompleto
            })
            .ToListAsync();

        // Se unen en orden cronológico; el mismo día va primero la factura
        var lineas = cargos
            .Select(f => (Fecha: f.FechaEmision, Orden: 0, f.Id, Tipo: "Factura",
                          Documento: $"{f.Serie}-{f.Numero}",
                          Descripcion: $"Factura de compra, vence {f.FechaVencimiento:dd/MM/yyyy}",
                          RegistradoPor: f.Usuario,
                          Cargo: f.Total, Abono: 0m))
            .Concat(abonos.Select(p => (Fecha: p.FechaPago, Orden: 1, p.Id, Tipo: "Pago",
                          Documento: $"{p.Serie}-{p.Numero}",
                          Descripcion: p.NumeroReferencia is null
                              ? $"Pago en {p.FormaPago}"
                              : $"Pago con {p.FormaPago} No. {p.NumeroReferencia} ({p.Banco})",
                          RegistradoPor: p.Usuario,
                          Cargo: 0m, Abono: p.Monto)))
            .OrderBy(l => l.Fecha).ThenBy(l => l.Orden).ThenBy(l => l.Id)
            .ToList();

        var saldo = saldoInicial;
        var movimientos = new List<MovimientoCuentaDto>();
        foreach (var l in lineas)
        {
            saldo += l.Cargo - l.Abono;
            movimientos.Add(new MovimientoCuentaDto(l.Fecha, l.Tipo, l.Documento, l.Descripcion, l.RegistradoPor, l.Cargo, l.Abono, saldo));
        }

        var totalCargos = lineas.Sum(l => l.Cargo);
        var totalAbonos = lineas.Sum(l => l.Abono);

        return Ok(new EstadoCuentaDto(
            await Encabezado("Estado de cuenta"),
            proveedor.Id, proveedor.Nit, proveedor.Nombre, proveedor.DiasCredito,
            inicio, fin,
            saldoInicial, totalCargos, totalAbonos,
            saldoInicial + totalCargos - totalAbonos,
            movimientos));
    }

    // ==================================================================
    // FACTURAS PENDIENTES Y VENCIDAS (RF-26)
    // GET /api/reportes/pendientes?proveedorId=1&soloVencidas=true
    // ==================================================================
    [HttpGet("pendientes")]
    public async Task<ActionResult<FacturasPendientesDto>> Pendientes(int? proveedorId = null, bool soloVencidas = false)
    {
        var hoy = Reloj.Hoy;
        var consulta = db.Facturas.AsNoTracking()
            .Where(f => f.Estado == EstadoFactura.Pendiente || f.Estado == EstadoFactura.Parcial);

        if (proveedorId is not null)
            consulta = consulta.Where(f => f.ProveedorId == proveedorId);
        if (soloVencidas)
            consulta = consulta.Where(f => f.FechaVencimiento < hoy);

        var filas = await consulta
            .OrderBy(f => f.FechaVencimiento).ThenBy(f => f.Proveedor.Nombre)
            .Select(f => new
            {
                f.Id, ProveedorNit = f.Proveedor.Nit, Proveedor = f.Proveedor.Nombre,
                Documento = f.Serie + "-" + f.Numero,
                f.FechaEmision, f.FechaVencimiento, f.Total, f.Saldo, f.Estado
            })
            .ToListAsync();

        var facturas = filas.Select(f =>
        {
            var vencida = f.FechaVencimiento < hoy;
            return new FacturaPendienteDto(f.Id, f.ProveedorNit, f.Proveedor, f.Documento,
                f.FechaEmision, f.FechaVencimiento, f.Total, f.Saldo, f.Estado,
                vencida, vencida ? hoy.DayNumber - f.FechaVencimiento.DayNumber : 0);
        }).ToList();

        return Ok(new FacturasPendientesDto(
            await Encabezado(soloVencidas ? "Facturas vencidas" : "Facturas pendientes y vencidas"),
            facturas.Count,
            facturas.Sum(f => f.Saldo),
            facturas.Count(f => f.Vencida),
            facturas.Where(f => f.Vencida).Sum(f => f.Saldo),
            facturas));
    }

    // ==================================================================
    // ANTIGÜEDAD DE SALDOS (RF-27, RN-29)
    // GET /api/reportes/antiguedad-saldos
    // ==================================================================
    [HttpGet("antiguedad-saldos")]
    public async Task<ActionResult<AntiguedadSaldosDto>> AntiguedadSaldos()
    {
        var hoy = Reloj.Hoy;

        var abiertas = await db.Facturas.AsNoTracking()
            .Where(f => f.Estado == EstadoFactura.Pendiente || f.Estado == EstadoFactura.Parcial)
            .Select(f => new { f.ProveedorId, f.Proveedor.Nit, f.Proveedor.Nombre, f.FechaVencimiento, f.Saldo })
            .ToListAsync();

        // RN-29: clasificación según los días transcurridos desde el vencimiento
        var filas = abiertas
            .GroupBy(f => new { f.ProveedorId, f.Nit, f.Nombre })
            .Select(g =>
            {
                decimal Rango(int min, int max) => g
                    .Where(f => { var d = hoy.DayNumber - f.FechaVencimiento.DayNumber; return d >= min && d <= max; })
                    .Sum(f => f.Saldo);

                return new AntiguedadFilaDto(
                    g.Key.ProveedorId, g.Key.Nit, g.Key.Nombre,
                    PorVencer: Rango(int.MinValue, 0),
                    De1a30: Rango(1, 30),
                    De31a60: Rango(31, 60),
                    De61a90: Rango(61, 90),
                    MasDe90: Rango(91, int.MaxValue),
                    Total: g.Sum(f => f.Saldo));
            })
            .OrderByDescending(f => f.Total)
            .ToList();

        var totales = new AntiguedadFilaDto(0, "", "TOTAL",
            filas.Sum(f => f.PorVencer), filas.Sum(f => f.De1a30), filas.Sum(f => f.De31a60),
            filas.Sum(f => f.De61a90), filas.Sum(f => f.MasDe90), filas.Sum(f => f.Total));

        return Ok(new AntiguedadSaldosDto(await Encabezado("Antigüedad de saldos"), hoy, filas, totales));
    }

    // ==================================================================
    // PAGOS POR PERÍODO (RF-28)
    // GET /api/reportes/pagos?desde=2026-10-01&hasta=2026-10-31&proveedorId=1&formaPagoId=2
    // ==================================================================
    [HttpGet("pagos")]
    public async Task<ActionResult<PagosPeriodoDto>> Pagos(
        DateOnly? desde = null, DateOnly? hasta = null, int? proveedorId = null, int? formaPagoId = null)
    {
        var (inicio, fin) = Periodo(desde, hasta);

        var consulta = db.Pagos.AsNoTracking()
            .Where(p => !p.Anulado && p.FechaPago >= inicio && p.FechaPago <= fin);

        if (proveedorId is not null)
            consulta = consulta.Where(p => p.Factura.ProveedorId == proveedorId);
        if (formaPagoId is not null)
            consulta = consulta.Where(p => p.FormaPagoId == formaPagoId);

        var pagos = await consulta
            .OrderBy(p => p.FechaPago).ThenBy(p => p.Id)
            .Select(PagoMapeo.APagoDto)
            .ToListAsync();

        var porForma = pagos
            .GroupBy(p => p.FormaPago)
            .Select(g => new ResumenFormaPagoDto(g.Key, g.Count(), g.Sum(p => p.Monto)))
            .OrderByDescending(r => r.Total)
            .ToList();

        return Ok(new PagosPeriodoDto(
            await Encabezado("Pagos realizados por período"),
            inicio, fin, pagos.Count, pagos.Sum(p => p.Monto), porForma, pagos));
    }

    // ---------- Auxiliares ----------

    /// <summary>Si no se indican fechas, se usa el mes actual hasta hoy.</summary>
    private static (DateOnly Inicio, DateOnly Fin) Periodo(DateOnly? desde, DateOnly? hasta)
    {
        var hoy = Reloj.Hoy;
        var inicio = desde ?? new DateOnly(hoy.Year, hoy.Month, 1);
        var fin = hasta ?? hoy;

        if (inicio > fin)
            throw new ReglaNegocioException("La fecha inicial no puede ser posterior a la fecha final.");

        return (inicio, fin);
    }

    private async Task<EncabezadoReporteDto> Encabezado(string titulo)
    {
        var empresa = await db.ConfiguracionEmpresa.AsNoTracking().FirstAsync();
        return new EncabezadoReporteDto(empresa.Nombre, empresa.Nit, empresa.Direccion, titulo, Reloj.Ahora);
    }
}
