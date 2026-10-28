using System.ComponentModel.DataAnnotations;

namespace CxP360.Api.Dtos.Comun;

/// <summary>
/// Archivo enviado por la pantalla. El contenido viaja en Base64 dentro del JSON
/// (System.Text.Json lo convierte automáticamente a byte[]).
/// </summary>
public record ArchivoRequest(
    [Required(ErrorMessage = "El nombre del archivo es obligatorio.")]
    [StringLength(200, ErrorMessage = "El nombre del archivo admite como máximo 200 caracteres.")]
    string NombreArchivo,

    [Required(ErrorMessage = "El tipo de archivo es obligatorio.")]
    string TipoContenido,

    [Required(ErrorMessage = "El contenido del archivo es obligatorio.")]
    byte[] Contenido);

/// <summary>Datos de un comprobante adjunto, sin el contenido.</summary>
public record AdjuntoDto(
    int Id,
    string NombreArchivo,
    string TipoContenido,
    int TamanoBytes,
    string UsuarioRegistro,
    DateTime FechaRegistro);
