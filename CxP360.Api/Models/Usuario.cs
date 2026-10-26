using Microsoft.AspNetCore.Identity;

namespace CxP360.Api.Models;

public class Usuario
{
    // Algoritmo de cifrado de contraseñas de ASP.NET Core (PBKDF2 con sal aleatoria)
    private static readonly PasswordHasher<Usuario> Hasher = new();

    public int Id { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string NombreUsuario { get; set; } = string.Empty;

    /// <summary>
    /// RN-03: contraseña cifrada (hash), nunca en texto plano.
    /// Solo se modifica con EstablecerPassword().
    /// </summary>
    public string PasswordHash { get; private set; } = string.Empty;

    public Rol Rol { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; }

    /// <summary>Cifra y guarda una contraseña nueva.</summary>
    public void EstablecerPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
            throw new ReglaNegocioException("La contraseña debe tener al menos 8 caracteres.");

        PasswordHash = Hasher.HashPassword(this, password);
    }

    /// <summary>Compara una contraseña escrita con el hash guardado.</summary>
    public bool VerificarPassword(string password)
    {
        if (string.IsNullOrEmpty(PasswordHash) || string.IsNullOrEmpty(password))
            return false;

        var resultado = Hasher.VerifyHashedPassword(this, PasswordHash, password);
        return resultado != PasswordVerificationResult.Failed;
    }
}
