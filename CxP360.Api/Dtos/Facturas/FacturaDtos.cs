using System.ComponentModel.DataAnnotations;
using CxP360.Api.Dtos.Comun;
using CxP360.Api.Dtos.Pagos;
using CxP360.Api.Models;

namespace CxP360.Api.Dtos.Facturas;

/// <summary>Fila de la lista de facturas (RF-17).</summary>
public record FacturaResumenDto(
    int Id,
    int ProveedorId,
    string ProveedorNombre,
    string ProveedorNit,
    string Serie,
    string Numero,
    DateOnly FechaEmision,
    DateOnly FechaVencimiento,
    decimal Total,
    decimal Saldo,
    EstadoFactura Estado,
    bool Vencida,          // RN-19: se calcula con la fecha de hoy
    int DiasVencida);

/// <summary>Línea del detalle de una factura.</summary>
public record LineaFacturaDto(int Id, string Descripcion, decimal Cantidad, decimal PrecioUnitario, decimal Subtotal);

/// <summary>Factura completa con su detalle y su historial de pagos (RF-18).</summary>
public record FacturaDto(
    int Id,
    int ProveedorId,
    string ProveedorNombre,
    string ProveedorNit,
    string Serie,
    string Numero,
    DateOnly FechaEmision,
    DateOnly FechaVencimiento,
    decimal Base,
    decimal Iva,
    decimal Total,
    decimal TotalPagado,
    decimal Saldo,
    EstadoFactura Estado,
    bool Vencida,
    int DiasVencida,
    string UsuarioRegistro,
    DateTime FechaRegistro,
    string? MotivoAnulacion,
    string? UsuarioAnulacion,
    DateTime? FechaAnulacion,
    List<LineaFacturaDto> Lineas,
    List<PagoDto> Pagos,
    List<AdjuntoDto> Comprobantes);

/// <summary>Línea que se envía al registrar una factura.</summary>
public record LineaFacturaRequest(
    [Required(ErrorMessage = "La descripción del producto es obligatoria.")]
    [StringLength(200, ErrorMessage = "La descripción admite como máximo 200 caracteres.")]
    string Descripcion,

    [Range(typeof(decimal), "0.01", "99999999.99",
        ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true,
        ErrorMessage = "La cantidad debe ser mayor a cero.")]
    decimal Cantidad,

    [Range(typeof(decimal), "0.01", "9999999999.99",
        ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true,
        ErrorMessage = "El precio unitario debe ser mayor a cero.")]
    decimal PrecioUnitario);

/// <summary>Datos para registrar una factura de compra (RF-14).</summary>
public record CrearFacturaRequest(
    [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar el proveedor.")]
    int ProveedorId,

    [Required(ErrorMessage = "La serie es obligatoria.")]
    [StringLength(10, ErrorMessage = "La serie admite como máximo 10 caracteres.")]
    string Serie,

    [Required(ErrorMessage = "El número de factura es obligatorio.")]
    [StringLength(20, ErrorMessage = "El número admite como máximo 20 caracteres.")]
    string Numero,

    DateOnly FechaEmision,

    // RN-12: al menos una línea de detalle
    [Required(ErrorMessage = "La factura debe tener al menos una línea de detalle.")]
    [MinLength(1, ErrorMessage = "La factura debe tener al menos una línea de detalle.")]
    List<LineaFacturaRequest> Lineas,

    // RN-35: total impreso en la factura; debe coincidir con la suma de las líneas
    [Range(typeof(decimal), "0.01", "9999999999.99",
        ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true,
        ErrorMessage = "Indique el total que aparece en la factura.")]
    decimal TotalDocumento,

    // RN-32: foto o PDF de la factura (obligatorio)
    [Required(ErrorMessage = "Debe adjuntar la foto o el PDF de la factura.")]
    ArchivoRequest Comprobante);
