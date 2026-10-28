namespace CxP360.Api.Models;

public class Factura
{
    public int Id { get; set; }
    public int ProveedorId { get; set; }
    public string Serie { get; set; } = string.Empty;
    public string Numero { get; set; } = string.Empty;
    public DateOnly FechaEmision { get; set; }
    public DateOnly FechaVencimiento { get; set; }
    public decimal Base { get; set; }
    public decimal Iva { get; set; }
    public decimal Total { get; set; }
    public decimal Saldo { get; set; }
    public EstadoFactura Estado { get; set; } = EstadoFactura.Pendiente;
    public int UsuarioId { get; set; }
    public DateTime FechaRegistro { get; set; }
    public string? MotivoAnulacion { get; set; }
    public int? UsuarioAnulacionId { get; set; }
    public DateTime? FechaAnulacion { get; set; }

    // Relaciones
    public Proveedor Proveedor { get; set; } = null!;
    public Usuario Usuario { get; set; } = null!;
    public Usuario? UsuarioAnulacion { get; set; }
    public List<DetalleFactura> Detalles { get; set; } = new();   // composición
    public List<Pago> Pagos { get; set; } = new();
    public List<Adjunto> Adjuntos { get; set; } = new();                  // RN-32: factura escaneada o foto

    /// <summary>
    /// RN-14: calcula el total a partir de las líneas (precios con IVA incluido)
    /// y desglosa base e IVA. Deja la factura en estado Pendiente.
    /// </summary>
    public void CalcularTotales(decimal porcentajeIva)
    {
        if (Detalles.Count == 0)
            throw new ReglaNegocioException("La factura debe tener al menos una línea de detalle.");   // RN-12

        Total = Detalles.Sum(d => d.CalcularSubtotal());
        Base = Math.Round(Total / (1 + porcentajeIva / 100m), 2, MidpointRounding.AwayFromZero);
        Iva = Total - Base;
        Saldo = Total;
        Estado = EstadoFactura.Pendiente;
    }

    /// <summary>RN-13: vencimiento = emisión + días de crédito.</summary>
    public void CalcularVencimiento(int diasCredito) =>
        FechaVencimiento = FechaEmision.AddDays(diasCredito);

    /// <summary>RN-19: "Vencida" se calcula, no se guarda.</summary>
    public bool EstaVencida(DateOnly hoy) =>
        (Estado == EstadoFactura.Pendiente || Estado == EstadoFactura.Parcial) && FechaVencimiento < hoy;

    /// <summary>RN-20, RN-21 y RN-22: rebaja el saldo y actualiza el estado.</summary>
    public void AplicarPago(decimal monto)
    {
        if (Estado != EstadoFactura.Pendiente && Estado != EstadoFactura.Parcial)
            throw new ReglaNegocioException($"Solo se pueden registrar pagos a facturas pendientes o parciales. Esta factura está {Estado}.");
        if (monto <= 0)
            throw new ReglaNegocioException("El monto del pago debe ser mayor a cero.");
        if (monto > Saldo)
            throw new ReglaNegocioException($"El pago (Q{monto:N2}) supera el saldo pendiente de la factura (Q{Saldo:N2}).");

        Saldo -= monto;
        ActualizarEstado();
    }

    /// <summary>RN-26: al anular un pago se restaura el saldo.</summary>
    public void RevertirPago(decimal monto)
    {
        if (Saldo + monto > Total)
            throw new ReglaNegocioException("El saldo restaurado no puede superar el total de la factura.");

        Saldo += monto;
        ActualizarEstado();
    }

    /// <summary>RN-16 y RN-17: anula la factura si no tiene pagos activos.</summary>
    public void Anular(string motivo, int usuarioId, DateTime fecha)
    {
        if (Estado == EstadoFactura.Anulada)
            throw new ReglaNegocioException("La factura ya está anulada.");
        if (Pagos.Any(p => !p.Anulado))
            throw new ReglaNegocioException("No se puede anular una factura con pagos activos. Anule primero sus pagos.");
        if (string.IsNullOrWhiteSpace(motivo))
            throw new ReglaNegocioException("Debe indicar el motivo de la anulación.");

        Estado = EstadoFactura.Anulada;
        MotivoAnulacion = motivo.Trim();
        UsuarioAnulacionId = usuarioId;
        FechaAnulacion = fecha;
    }

    /// <summary>RN-18: el estado depende del saldo.</summary>
    private void ActualizarEstado() =>
        Estado = Saldo == 0 ? EstadoFactura.Pagada
               : Saldo == Total ? EstadoFactura.Pendiente
               : EstadoFactura.Parcial;
}
