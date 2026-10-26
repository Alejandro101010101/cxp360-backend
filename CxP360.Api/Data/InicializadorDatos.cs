using CxP360.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CxP360.Api.Data;

/// <summary>
/// Se ejecuta al arrancar la API. Si no existe ningún usuario,
/// crea el Administrador inicial con los datos de la sección "AdminInicial".
/// </summary>
public static class InicializadorDatos
{
    public static async Task CrearAdministradorInicialAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CxP360DbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("InicializadorDatos");

        try
        {
            if (await db.Usuarios.AnyAsync())
                return;                                         // ya hay usuarios: no se hace nada

            var seccion = app.Configuration.GetSection("AdminInicial");
            var password = seccion["Password"];

            if (string.IsNullOrWhiteSpace(password))
            {
                logger.LogWarning("No hay usuarios y falta AdminInicial:Password. No se creó el Administrador.");
                return;
            }

            var admin = new Usuario
            {
                NombreUsuario = seccion["NombreUsuario"] ?? "admin",
                NombreCompleto = seccion["NombreCompleto"] ?? "Administrador del sistema",
                Rol = Rol.Administrador,
                Activo = true
            };
            admin.EstablecerPassword(password);                 // se guarda cifrada (RN-03)

            db.Usuarios.Add(admin);
            await db.SaveChangesAsync();

            logger.LogInformation("Se creó el usuario Administrador inicial '{Usuario}'.", admin.NombreUsuario);
        }
        catch (Exception ex)
        {
            // Si la base de datos no responde, la API arranca igual y /api/salud mostrará el problema
            logger.LogError(ex, "No se pudo verificar o crear el Administrador inicial.");
        }
    }
}
