using System.ComponentModel.DataAnnotations;

namespace CxP360.Api.Dtos.Comun;

/// <summary>Motivo obligatorio para anular una factura o un pago (RN-17, RN-26).</summary>
public record AnularRequest(
    [Required(ErrorMessage = "El motivo de la anulación es obligatorio.")]
    [StringLength(250, ErrorMessage = "El motivo admite como máximo 250 caracteres.")]
    string Motivo);
