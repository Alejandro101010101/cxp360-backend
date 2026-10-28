using CxP360.Api.Data;
using CxP360.Api.Dtos.Parametros;
using CxP360.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CxP360.Api.Controllers;

/// <summary>
/// Parámetros generales: datos de la empresa, % de IVA y días de crédito (RF-05, RF-06).
/// Cualquier usuario puede consultarlos; solo el Administrador los modifica (RN-31).
/// </summary>
[ApiController]
[Route("api/configuracion")]
public class ConfiguracionController(CxP360DbContext db) : ControllerBase
{
    /// <summary>GET /api/configuracion</summary>
    [HttpGet]
    public async Task<ActionResult<ConfiguracionDto>> Obtener()
    {
        var config = await db.ConfiguracionEmpresa.AsNoTracking().FirstOrDefaultAsync()
            ?? throw new NoEncontradoException("No existe la configuración de la empresa.");

        return Ok(Mapear(config));
    }

    /// <summary>PUT /api/configuracion</summary>
    [HttpPut]
    [Authorize(Roles = nameof(Rol.Administrador))]
    public async Task<ActionResult<ConfiguracionDto>> Actualizar(ActualizarConfiguracionRequest request)
    {
        var config = await db.ConfiguracionEmpresa.FirstOrDefaultAsync()
            ?? throw new NoEncontradoException("No existe la configuración de la empresa.");

        config.Nombre = request.Nombre.Trim();
        config.Nit = request.Nit.Trim().ToUpperInvariant();
        config.Direccion = request.Direccion.Trim();
        config.Telefono = request.Telefono.Trim();
        config.PorcentajeIva = request.PorcentajeIva;
        config.DiasCreditoDefault = request.DiasCreditoDefault;
        config.LimitePagoEfectivo = request.LimitePagoEfectivo;

        await db.SaveChangesAsync();
        return Ok(Mapear(config));
    }

    private static ConfiguracionDto Mapear(ConfiguracionEmpresa c) =>
        new(c.Nombre, c.Nit, c.Direccion, c.Telefono, c.PorcentajeIva, c.DiasCreditoDefault, c.LimitePagoEfectivo);
}
