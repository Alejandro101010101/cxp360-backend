using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using CxP360.Api.Dtos.Comun;
using CxP360.Api.Models;

namespace CxP360.Api.Dtos.Pagos;

/// <summary>Pago con los datos de su factura, forma de pago y banco.</summary>
public record PagoDto(
    int Id,
    int FacturaId,
    string FacturaSerie,
    string FacturaNumero,
    int ProveedorId,
    string ProveedorNombre,
    DateOnly FechaPago,
    decimal Monto,
    int FormaPagoId,
    string FormaPago,
    bool FormaRequiereReferencia,   // false = efectivo
    int? BancoId,
    string? Banco,
    string? NumeroReferencia,
    bool Anulado,
    string? MotivoAnulacion,
    DateTime? FechaAnulacion,
    string UsuarioRegistro,
    DateTime FechaRegistro,
    decimal SaldoFactura,
    EstadoFactura EstadoFactura,
    List<AdjuntoDto> Comprobantes);

/// <summary>Datos para registrar un pago total o parcial (RF-20, RF-21).</summary>
public record RegistrarPagoRequest(
    [Range(1, int.MaxValue, ErrorMessage = "Debe indicar la factura.")]
    int FacturaId,

    DateOnly FechaPago,

    [Range(typeof(decimal), "0.01", "9999999999.99",
        ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true,
        ErrorMessage = "El monto debe ser mayor a cero.")]
    decimal Monto,

    [Range(1, int.MaxValue, ErrorMessage = "Debe indicar la forma de pago.")]
    int FormaPagoId,

    int? BancoId,

    [StringLength(50, ErrorMessage = "La referencia admite como máximo 50 caracteres.")]
    string? NumeroReferencia,

    // RN-25: true cuando el usuario ya confirmó un posible pago duplicado
    bool ConfirmarDuplicado = false,

    // RN-33: obligatorio en cheque, transferencia y depósito; opcional en efectivo
    ArchivoRequest? Comprobante = null);

public static class PagoMapeo
{
    /// <summary>Proyección que SQL Server resuelve en una sola consulta.</summary>
    public static readonly Expression<Func<Pago, PagoDto>> APagoDto = p => new PagoDto(
        p.Id,
        p.FacturaId,
        p.Factura.Serie,
        p.Factura.Numero,
        p.Factura.ProveedorId,
        p.Factura.Proveedor.Nombre,
        p.FechaPago,
        p.Monto,
        p.FormaPagoId,
        p.FormaPago.Nombre,
        p.FormaPago.RequiereReferencia,
        p.BancoId,
        p.Banco != null ? p.Banco.Nombre : null,
        p.NumeroReferencia,
        p.Anulado,
        p.MotivoAnulacion,
        p.FechaAnulacion,
        p.Usuario.NombreCompleto,
        p.FechaRegistro,
        p.Factura.Saldo,
        p.Factura.Estado,
        p.Adjuntos
            .OrderBy(a => a.Id)
            .Select(a => new AdjuntoDto(a.Id, a.NombreArchivo, a.TipoContenido, a.TamanoBytes,
                                        a.Usuario.NombreCompleto, a.FechaRegistro))
            .ToList());
}
