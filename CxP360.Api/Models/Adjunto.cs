namespace CxP360.Api.Models;

/// <summary>
/// Comprobante adjunto (foto o PDF) de una factura o de un pago (RN-32, RN-33).
/// Pertenece a una factura O a un pago. No se elimina (RN-37).
/// </summary>
public class Adjunto
{
    public int Id { get; set; }
    public int? FacturaId { get; set; }
    public int? PagoId { get; set; }
    public string NombreArchivo { get; set; } = string.Empty;
    public string TipoContenido { get; set; } = string.Empty;
    public int TamanoBytes { get; set; }
    public byte[] Contenido { get; set; } = [];
    public int UsuarioId { get; set; }
    public DateTime FechaRegistro { get; set; }

    public Factura? Factura { get; set; }
    public Pago? Pago { get; set; }
    public Usuario Usuario { get; set; } = null!;
}
