namespace CxP360.Api.Models;

public class Proveedor
{
    public int Id { get; set; }
    public string Nit { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Contacto { get; set; }
    public string? Telefono { get; set; }
    public string? Correo { get; set; }
    public string? Direccion { get; set; }
    public int DiasCredito { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaRegistro { get; set; }

    // Relación: un proveedor recibe cero o muchas facturas
    public List<Factura> Facturas { get; set; } = new();

    /// <summary>
    /// RN-09: saldo = suma de los saldos de las facturas no anuladas.
    /// Requiere que las facturas estén cargadas.
    /// </summary>
    public decimal CalcularSaldo() =>
        Facturas.Where(f => f.Estado != EstadoFactura.Anulada).Sum(f => f.Saldo);

    /// <summary>RN-08: un proveedor inactivo no recibe facturas nuevas.</summary>
    public bool PuedeRecibirFacturas() => Activo;
}
