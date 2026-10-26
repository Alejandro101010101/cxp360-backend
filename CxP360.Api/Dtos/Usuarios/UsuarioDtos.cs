using System.ComponentModel.DataAnnotations;

namespace CxP360.Api.Dtos.Usuarios;

/// <summary>Datos de un usuario. Nunca incluye la contraseña ni su hash.</summary>
public record UsuarioDto(
    int Id,
    string NombreCompleto,
    string NombreUsuario,
    string Rol,
    bool Activo,
    DateTime FechaCreacion);

public record CrearUsuarioRequest(
    [Required(ErrorMessage = "El nombre completo es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre admite como máximo 100 caracteres.")]
    string NombreCompleto,

    [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
    [StringLength(50, ErrorMessage = "El nombre de usuario admite como máximo 50 caracteres.")]
    string NombreUsuario,

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener entre 8 y 100 caracteres.")]
    string Password,

    [Required(ErrorMessage = "El rol es obligatorio.")]
    string Rol);

public record ActualizarUsuarioRequest(
    [Required(ErrorMessage = "El nombre completo es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre admite como máximo 100 caracteres.")]
    string NombreCompleto,

    [Required(ErrorMessage = "El rol es obligatorio.")]
    string Rol,

    bool Activo);

public record RestablecerPasswordRequest(
    [Required(ErrorMessage = "La contraseña nueva es obligatoria.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener entre 8 y 100 caracteres.")]
    string PasswordNueva);
