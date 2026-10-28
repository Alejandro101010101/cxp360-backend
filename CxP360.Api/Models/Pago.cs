namespace CxP360.Api.Models;

public class Pago
{
    public int Id { get; set; }
    public int FacturaId { get; set; }
    public int FormaPagoId { get; set; }
    public int? BancoId { get; set; }                  // NULL en efectivo (RN-23)
    public DateOnly FechaPago { get; set; }
    public decimal Monto { get; set; }
    public string? NumeroReferencia { get; set; }
    public bool Anulado { get; set; }
    public int UsuarioId { get; set; }
    public DateTime FechaRegistro { get; set; }
    public string? MotivoAnulacion { get; set; }
    public int? UsuarioAnulacionId { get; set; }
    public DateTime? FechaAnulacion { get; set; }

    // Relaciones
    public Factura Factura { get; set; } = null!;
    public FormaPago FormaPago { get; set; } = null!;
    public Banco? Banco { get; set; }
    public Usuario Usuario { get; set; } = null!;
    public Usuario? UsuarioAnulacion { get; set; }
    public List<Adjunto> Adjuntos { get; set; } = new();                  // RN-33: voucher o recibo firmado

    /// <summary>RN-26: anula el pago registrando motivo, usuario y fecha.</summary>
    public void Anular(string motivo, int usuarioId, DateTime fecha)
    {
        if (Anulado)
            throw new ReglaNegocioException("El pago ya está anulado.");
        if (string.IsNullOrWhiteSpace(motivo))
            throw new ReglaNegocioException("Debe indicar el motivo de la anulación.");

        Anulado = true;
        MotivoAnulacion = motivo.Trim();
        UsuarioAnulacionId = usuarioId;
        FechaAnulacion = fecha;
    }
}
