namespace CxP360.Api.Models;

/// <summary>
/// Parámetros del sistema. Existe un solo registro (Id = 1).
/// </summary>
public class ConfiguracionEmpresa
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Nit { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public decimal PorcentajeIva { get; set; }
    public int DiasCreditoDefault { get; set; }

    /// <summary>RN-36: monto máximo de un pago en efectivo. NULL = sin límite.</summary>
    public decimal? LimitePagoEfectivo { get; set; }

    /// <summary>RN-14: base imponible de un total con IVA incluido.</summary>
    public decimal CalcularBase(decimal total) =>
        Math.Round(total / (1 + PorcentajeIva / 100m), 2, MidpointRounding.AwayFromZero);

    /// <summary>RN-14: IVA contenido en un total con IVA incluido.</summary>
    public decimal CalcularIva(decimal total) => total - CalcularBase(total);
}
