using CxP360.Api.Data;
using CxP360.Api.Dtos.Comun;
using CxP360.Api.Dtos.Pagos;
using CxP360.Api.Extensiones;
using CxP360.Api.Models;
using CxP360.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CxP360.Api.Controllers;

/// <summary>
/// Movimientos de pagos (RF-20 a RF-24, RF-31). Lo usan Administrador y Operador.
/// Cada pago se aplica a una sola factura. Los pagos no se eliminan: se anulan.
/// </summary>
[ApiController]
[Route("api/pagos")]
public class PagosController(CxP360DbContext db) : ControllerBase
{
    /// <summary>
    /// GET /api/pagos?proveedorId=1&amp;facturaId=5&amp;desde=2026-10-01&amp;hasta=2026-10-31&amp;incluirAnulados=false (RF-23)
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<PagoDto>>> Listar(
        int? proveedorId = null, int? facturaId = null,
        DateOnly? desde = null, DateOnly? hasta = null, bool incluirAnulados = false)
    {
        var consulta = db.Pagos.AsNoTracking();

        if (proveedorId is not null) consulta = consulta.Where(p => p.Factura.ProveedorId == proveedorId);
        if (facturaId is not null)   consulta = consulta.Where(p => p.FacturaId == facturaId);
        if (desde is not null)       consulta = consulta.Where(p => p.FechaPago >= desde);
        if (hasta is not null)       consulta = consulta.Where(p => p.FechaPago <= hasta);
        if (!incluirAnulados)        consulta = consulta.Where(p => !p.Anulado);

        return Ok(await consulta
            .OrderByDescending(p => p.FechaPago).ThenByDescending(p => p.Id)
            .Select(PagoMapeo.APagoDto)
            .ToListAsync());
    }

    /// <summary>GET /api/pagos/{id}</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<PagoDto>> ObtenerPorId(int id) => Ok(await LeerDto(id));

    /// <summary>POST /api/pagos: pago total o parcial (RF-20, RF-21, RF-22, RF-31)</summary>
    [HttpPost]
    public async Task<ActionResult<PagoDto>> Registrar(RegistrarPagoRequest request)
    {
        var factura = await db.Facturas.FirstOrDefaultAsync(f => f.Id == request.FacturaId)
            ?? throw new NoEncontradoException($"No existe la factura {request.FacturaId}.");

        var forma = await db.FormasPago.FirstOrDefaultAsync(f => f.Id == request.FormaPagoId)
            ?? throw new ReglaNegocioException("La forma de pago seleccionada no existe.");
        if (!forma.Activo)
            throw new ReglaNegocioException($"La forma de pago {forma.Nombre} está inactiva.");

        // RN-23: cheque, transferencia y depósito exigen banco y referencia; efectivo no
        int? bancoId = null;
        string? referencia = null;

        if (forma.RequiereReferencia)
        {
            if (request.BancoId is null)
                throw new ReglaNegocioException($"Para pagos con {forma.Nombre} debe indicar el banco.");
            if (string.IsNullOrWhiteSpace(request.NumeroReferencia))
                throw new ReglaNegocioException($"Para pagos con {forma.Nombre} debe indicar el número de referencia.");

            var banco = await db.Bancos.FirstOrDefaultAsync(b => b.Id == request.BancoId)
                ?? throw new ReglaNegocioException("El banco seleccionado no existe.");
            if (!banco.Activo)
                throw new ReglaNegocioException($"El banco {banco.Nombre} está inactivo.");

            bancoId = banco.Id;
            referencia = request.NumeroReferencia.Trim().ToUpperInvariant();

            // RN-24: la misma referencia no se registra dos veces (solo pagos no anulados)
            if (await db.Pagos.AnyAsync(p => !p.Anulado && p.FormaPagoId == forma.Id
                                             && p.BancoId == bancoId && p.NumeroReferencia == referencia))
                throw new ConflictoException($"Ya está registrado un pago con {forma.Nombre} No. {referencia} de {banco.Nombre}.");
        }

        // RN-22: fecha entre la emisión de la factura y hoy
        if (request.FechaPago < factura.FechaEmision)
            throw new ReglaNegocioException($"La fecha del pago no puede ser anterior a la emisión de la factura ({factura.FechaEmision:dd/MM/yyyy}).");
        if (request.FechaPago > Reloj.Hoy)
            throw new ReglaNegocioException("La fecha del pago no puede ser posterior a hoy.");

        var monto = Math.Round(request.Monto, 2, MidpointRounding.AwayFromZero);

        // RN-20 y RN-21: no superar el saldo; solo facturas pendientes o parciales.
        // Rebaja el saldo y actualiza el estado (RF-22).
        factura.AplicarPago(monto);

        // RN-25: posible duplicado (misma factura, monto y fecha) → pedir confirmación
        if (!request.ConfirmarDuplicado &&
            await db.Pagos.AnyAsync(p => !p.Anulado && p.FacturaId == factura.Id
                                         && p.Monto == monto && p.FechaPago == request.FechaPago))
            throw new ConfirmacionRequeridaException(
                $"Ya existe un pago de Q{monto:N2} a la factura {factura.Serie}-{factura.Numero} " +
                $"con fecha {request.FechaPago:dd/MM/yyyy}. ¿Desea registrarlo de todos modos?");

        var pago = new Pago
        {
            FacturaId = factura.Id,
            FormaPagoId = forma.Id,
            BancoId = bancoId,
            FechaPago = request.FechaPago,
            Monto = monto,
            NumeroReferencia = referencia,
            UsuarioId = User.ObtenerUsuarioId(),              // RN-04
            FechaRegistro = Reloj.Ahora
        };

        db.Pagos.Add(pago);
        await db.SaveChangesAsync();   // el pago y el nuevo saldo de la factura se guardan en UNA transacción

        return CreatedAtAction(nameof(ObtenerPorId), new { id = pago.Id }, await LeerDto(pago.Id));
    }

    /// <summary>PUT /api/pagos/{id}/anular: restaura el saldo de la factura (RF-24, RN-26)</summary>
    [HttpPut("{id:int}/anular")]
    public async Task<ActionResult<PagoDto>> Anular(int id, AnularRequest request)
    {
        var pago = await db.Pagos.Include(p => p.Factura).FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new NoEncontradoException($"No existe el pago {id}.");

        pago.Anular(request.Motivo, User.ObtenerUsuarioId(), Reloj.Ahora);
        pago.Factura.RevertirPago(pago.Monto);

        await db.SaveChangesAsync();   // anulación y saldo restaurado en UNA transacción
        return Ok(await LeerDto(id));
    }

    private async Task<PagoDto> LeerDto(int id) =>
        await db.Pagos.AsNoTracking().Where(p => p.Id == id).Select(PagoMapeo.APagoDto).FirstOrDefaultAsync()
        ?? throw new NoEncontradoException($"No existe el pago {id}.");
}
