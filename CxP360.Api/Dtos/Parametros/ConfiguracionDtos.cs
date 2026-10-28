using System.ComponentModel.DataAnnotations;

namespace CxP360.Api.Dtos.Parametros;

public record ConfiguracionDto(
    string Nombre,
    string Nit,
    string Direccion,
    string Telefono,
    decimal PorcentajeIva,
    int DiasCreditoDefault,
    decimal? LimitePagoEfectivo);

public record ActualizarConfiguracionRequest(
    [Required(ErrorMessage = "El nombre de la empresa es obligatorio.")]
    [StringLength(150, ErrorMessage = "El nombre admite como máximo 150 caracteres.")]
    string Nombre,

    [Required(ErrorMessage = "El NIT es obligatorio.")]
    [StringLength(20, ErrorMessage = "El NIT admite como máximo 20 caracteres.")]
    string Nit,

    [Required(ErrorMessage = "La dirección es obligatoria.")]
    [StringLength(250, ErrorMessage = "La dirección admite como máximo 250 caracteres.")]
    string Direccion,

    [Required(ErrorMessage = "El teléfono es obligatorio.")]
    [StringLength(20, ErrorMessage = "El teléfono admite como máximo 20 caracteres.")]
    string Telefono,

    [Range(typeof(decimal), "0", "100", ErrorMessage = "El porcentaje de IVA debe estar entre 0 y 100.")]
    decimal PorcentajeIva,

    [Range(0, 120, ErrorMessage = "Los días de crédito deben estar entre 0 y 120.")]
    int DiasCreditoDefault,

    // RN-36: vacío = sin límite para pagos en efectivo
    [Range(typeof(decimal), "0.01", "9999999999.99",
        ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true,
        ErrorMessage = "El límite de pago en efectivo debe ser mayor a cero, o dejarse vacío.")]
    decimal? LimitePagoEfectivo = null);
