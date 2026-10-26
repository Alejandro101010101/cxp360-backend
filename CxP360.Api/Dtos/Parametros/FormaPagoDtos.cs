using System.ComponentModel.DataAnnotations;

namespace CxP360.Api.Dtos.Parametros;

public record FormaPagoDto(int Id, string Nombre, bool RequiereReferencia, bool Activo);

public record GuardarFormaPagoRequest(
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(50, ErrorMessage = "El nombre admite como máximo 50 caracteres.")]
    string Nombre,
    bool RequiereReferencia,
    bool Activo = true);
