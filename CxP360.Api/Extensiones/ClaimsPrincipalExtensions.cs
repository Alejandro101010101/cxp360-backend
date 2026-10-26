using System.Security.Claims;

namespace CxP360.Api.Extensiones;

public static class ClaimsPrincipalExtensions
{
    /// <summary>Id del usuario que inició sesión, tomado del token ("sub").</summary>
    public static int ObtenerUsuarioId(this ClaimsPrincipal usuario) =>
        int.TryParse(usuario.FindFirst("sub")?.Value, out var id)
            ? id
            : throw new UnauthorizedAccessException("El token no contiene el identificador del usuario.");
}
