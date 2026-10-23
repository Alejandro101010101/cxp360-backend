namespace CxP360.Api.Models;

public class Usuario
{
    public int Id { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string NombreUsuario { get; set; } = string.Empty;

    /// <summary>RN-03: contraseña cifrada, nunca en texto plano.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    public Rol Rol { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; }
}
