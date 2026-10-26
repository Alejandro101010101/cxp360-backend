using System.ComponentModel.DataAnnotations;

namespace CxP360.Api.Dtos.Auth;

/// <summary>Datos que envía la pantalla de inicio de sesión.</summary>
public record LoginRequest(
    [Required(ErrorMessage = "El usuario es obligatorio.")] string NombreUsuario,
    [Required(ErrorMessage = "La contraseña es obligatoria.")] string Password);
