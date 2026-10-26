using System.Security.Claims;
using System.Text;
using CxP360.Api.Configuracion;
using CxP360.Api.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace CxP360.Api.Services;

/// <summary>
/// Genera el token JWT que recibe el usuario al iniciar sesión.
/// </summary>
public class TokenService(IOptions<JwtOpciones> opciones)
{
    private readonly JwtOpciones _jwt = opciones.Value;

    public (string Token, DateTime Expira) GenerarToken(Usuario usuario)
    {
        var expira = DateTime.UtcNow.AddHours(_jwt.ExpiracionHoras);

        // Datos (claims) que viajan dentro del token
        var claims = new List<Claim>
        {
            new("sub", usuario.Id.ToString()),                    // identificador del usuario
            new("name", usuario.NombreUsuario),                   // nombre de usuario
            new("nombre", usuario.NombreCompleto),                // nombre para mostrar
            new("role", usuario.Rol.ToString()),                  // RN-30: Administrador u Operador
            new("jti", Guid.NewGuid().ToString())                 // identificador único del token
        };

        var credenciales = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Clave)),
            SecurityAlgorithms.HmacSha256);

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = _jwt.Issuer,
            Audience = _jwt.Audience,
            Expires = expira,
            SigningCredentials = credenciales
        };

        var token = new JsonWebTokenHandler().CreateToken(descriptor);
        return (token, expira);
    }
}
