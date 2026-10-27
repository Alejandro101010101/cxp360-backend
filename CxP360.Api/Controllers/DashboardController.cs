using CxP360.Api.Data;
using CxP360.Api.Dtos.Reportes;
using CxP360.Api.Models;
using CxP360.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CxP360.Api.Controllers;

/// <summary>
/// GET /api/dashboard: indicadores del panel de inicio (RF-04).
/// </summary>
[ApiController]
[Route("api/dashboard")]
public class DashboardController(CxP360DbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<DashboardDto>> Obtener()
    {
        var hoy = Reloj.Hoy;
        var limite = hoy.AddDays(7);
        var inicioMes = new DateOnly(hoy.Year, hoy.Month, 1);

        // Facturas con saldo pendiente (pendientes o parciales)
        var abiertas = await db.Facturas.AsNoTracking()
            .Where(f => f.Estado == EstadoFactura.Pendiente || f.Estado == EstadoFactura.Parcial)
            .Select(f => new
            {
                f.Id, f.ProveedorId, Proveedor = f.Proveedor.Nombre,
                Documento = f.Serie + "-" + f.Numero, f.FechaVencimiento, f.Saldo
            })
            .ToListAsync();

        var vencidas = abiertas.Where(f => f.FechaVencimiento < hoy).ToList();
        var proximas = abiertas.Where(f => f.FechaVencimiento >= hoy && f.FechaVencimiento <= limite).ToList();

        var pagosMes = db.Pagos.AsNoTracking()
            .Where(p => !p.Anulado && p.FechaPago >= inicioMes && p.FechaPago <= hoy);

        return Ok(new DashboardDto(
            TotalAdeudado: abiertas.Sum(f => f.Saldo),
            FacturasPendientes: abiertas.Count,
            FacturasVencidas: vencidas.Count,
            MontoVencido: vencidas.Sum(f => f.Saldo),
            FacturasPorVencer7Dias: proximas.Count,
            MontoPorVencer7Dias: proximas.Sum(f => f.Saldo),
            PagadoMes: await pagosMes.SumAsync(p => (decimal?)p.Monto) ?? 0m,
            PagosMes: await pagosMes.CountAsync(),
            ProveedoresActivos: await db.Proveedores.CountAsync(p => p.Activo),
            ProximasAVencer: proximas
                .OrderBy(f => f.FechaVencimiento)
                .Take(10)
                .Select(f => new FacturaAlertaDto(f.Id, f.Proveedor, f.Documento, f.FechaVencimiento, f.Saldo,
                    f.FechaVencimiento.DayNumber - hoy.DayNumber))
                .ToList(),
            MasVencidas: vencidas
                .OrderBy(f => f.FechaVencimiento)
                .Take(5)
                .Select(f => new FacturaAlertaDto(f.Id, f.Proveedor, f.Documento, f.FechaVencimiento, f.Saldo,
                    hoy.DayNumber - f.FechaVencimiento.DayNumber))
                .ToList(),
            MayoresSaldos: abiertas
                .GroupBy(f => new { f.ProveedorId, f.Proveedor })
                .Select(g => new SaldoProveedorDto(g.Key.ProveedorId, g.Key.Proveedor, g.Sum(f => f.Saldo)))
                .OrderByDescending(s => s.Saldo)
                .Take(5)
                .ToList()));
    }
}
