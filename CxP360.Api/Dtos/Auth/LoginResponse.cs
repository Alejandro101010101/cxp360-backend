namespace CxP360.Api.Dtos.Auth;

/// <summary>Respuesta de un inicio de sesión correcto.</summary>
public record LoginResponse(string Token, DateTime Expira, UsuarioSesionDto Usuario);

/// <summary>Datos del usuario que la pantalla necesita mostrar (nunca la contraseña).</summary>
public record UsuarioSesionDto(int Id, string NombreUsuario, string NombreCompleto, string Rol);
