using System.ComponentModel.DataAnnotations;

namespace CxP360.Api.Dtos.Parametros;

public record BancoDto(int Id, string Nombre, bool Activo);

public record GuardarBancoRequest(
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre admite como máximo 100 caracteres.")]
    string Nombre,
    bool Activo = true);
