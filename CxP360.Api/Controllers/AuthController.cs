using CxP360.Api.Data;
using CxP360.Api.Dtos.Auth;
using CxP360.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CxP360.Api.Controllers;

/// <summary>
/// Módulo de seguridad (RF-01, RF-02).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController(CxP360DbContext db, TokenService tokens) : ControllerBase
{
    /// <summary>
    /// POST /api/auth/login
    /// Verifica usuario y contraseña y devuelve un token JWT.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]                                   // se puede usar sin haber iniciado sesión
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var usuario = await db.Usuarios
            .FirstOrDefaultAsync(u => u.NombreUsuario == request.NombreUsuario.Trim());

        // Mismo mensaje para cualquier falla: no se revela si el usuario existe
        if (usuario is null || !usuario.Activo || !usuario.VerificarPassword(request.Password))
        {
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Usuario o contraseña incorrectos."
            });
        }

        var (token, expira) = tokens.GenerarToken(usuario);

        return Ok(new LoginResponse(
            token,
            expira,
            new UsuarioSesionDto(usuario.Id, usuario.NombreUsuario, usuario.NombreCompleto, usuario.Rol.ToString())));
    }

    /// <summary>
    /// GET /api/auth/perfil
    /// Devuelve los datos del usuario dueño del token. Requiere iniciar sesión.
    /// </summary>
    [HttpGet("perfil")]
    public IActionResult Perfil()
    {
        return Ok(new
        {
            id = User.FindFirst("sub")?.Value,
            nombreUsuario = User.Identity?.Name,
            nombreCompleto = User.FindFirst("nombre")?.Value,
            rol = User.FindFirst("role")?.Value
        });
    }
}
