namespace CxP360.Api.Configuracion;

/// <summary>
/// Configuración del token JWT. Se lee de la sección "Jwt" de appsettings.json.
/// La Clave NO se guarda en el repositorio: va en user-secrets (desarrollo)
/// o en una variable de entorno (servidor publicado).
/// </summary>
public class JwtOpciones
{
    public string Issuer { get; set; } = string.Empty;      // quién emite el token
    public string Audience { get; set; } = string.Empty;    // para quién es el token
    public string Clave { get; set; } = string.Empty;       // clave secreta de firma
    public int ExpiracionHoras { get; set; } = 8;           // una jornada de trabajo
}
