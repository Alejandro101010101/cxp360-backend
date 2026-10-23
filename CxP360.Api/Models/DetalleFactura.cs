namespace CxP360.Api.Models;

public class DetalleFactura
{
    public int Id { get; set; }
    public int FacturaId { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public decimal Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }

    /// <summary>
    /// Columna calculada por SQL Server (Cantidad × PrecioUnitario).
    /// Solo se lee; nunca se envía al guardar.
    /// </summary>
    public decimal Subtotal { get; private set; }

    public Factura Factura { get; set; } = null!;

    /// <summary>Mismo cálculo que hace SQL Server, para usarlo antes de guardar.</summary>
    public decimal CalcularSubtotal() =>
        Math.Round(Cantidad * PrecioUnitario, 2, MidpointRounding.AwayFromZero);
}
