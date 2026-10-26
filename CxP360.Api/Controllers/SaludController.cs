using CxP360.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CxP360.Api.Controllers;

/// <summary>
/// GET /api/salud
/// Comprueba que la API está encendida y que puede leer la base de datos.
/// También servirá para verificar la versión publicada en internet.
/// Es público a propósito: no muestra datos sensibles.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class SaludController(CxP360DbContext db, IWebHostEnvironment env) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        try
        {
            var empresa = await db.ConfiguracionEmpresa
                .AsNoTracking()
                .Select(c => c.Nombre)
                .FirstOrDefaultAsync();

            return Ok(new
            {
                estado = "ok",
                baseDatos = "conectada",
                empresa,
                proveedores = await db.Proveedores.CountAsync(),
                formasPago = await db.FormasPago.CountAsync(),
                bancos = await db.Bancos.CountAsync(),
                fechaServidor = DateTime.Now
            });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                estado = "error",
                baseDatos = "sin conexión",
                detalle = env.IsDevelopment() ? ex.Message : null   // el detalle técnico solo en desarrollo
            });
        }
    }
}
