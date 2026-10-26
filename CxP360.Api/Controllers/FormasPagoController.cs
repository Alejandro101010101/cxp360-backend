using CxP360.Api.Data;
using CxP360.Api.Dtos.Parametros;
using CxP360.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CxP360.Api.Controllers;

/// <summary>
/// Catálogo de formas de pago (RF-07). No se eliminan: se inactivan (RN-05).
/// </summary>
[ApiController]
[Route("api/formas-pago")]
public class FormasPagoController(CxP360DbContext db) : ControllerBase
{
    /// <summary>GET /api/formas-pago?soloActivas=true</summary>
    [HttpGet]
    public async Task<ActionResult<List<FormaPagoDto>>> Listar(bool soloActivas = false)
    {
        var consulta = db.FormasPago.AsNoTracking();
        if (soloActivas)
            consulta = consulta.Where(f => f.Activo);

        return Ok(await consulta
            .OrderBy(f => f.Nombre)
            .Select(f => new FormaPagoDto(f.Id, f.Nombre, f.RequiereReferencia, f.Activo))
            .ToListAsync());
    }

    /// <summary>GET /api/formas-pago/{id}</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<FormaPagoDto>> ObtenerPorId(int id)
    {
        var f = await db.FormasPago.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new NoEncontradoException($"No existe la forma de pago {id}.");

        return Ok(new FormaPagoDto(f.Id, f.Nombre, f.RequiereReferencia, f.Activo));
    }

    /// <summary>POST /api/formas-pago</summary>
    [HttpPost]
    [Authorize(Roles = nameof(Rol.Administrador))]
    public async Task<ActionResult<FormaPagoDto>> Crear(GuardarFormaPagoRequest request)
    {
        var nombre = request.Nombre.Trim();
        if (await db.FormasPago.AnyAsync(f => f.Nombre == nombre))
            throw new ConflictoException($"Ya existe la forma de pago '{nombre}'.");

        var forma = new FormaPago { Nombre = nombre, RequiereReferencia = request.RequiereReferencia, Activo = request.Activo };
        db.FormasPago.Add(forma);
        await db.SaveChangesAsync();

        var dto = new FormaPagoDto(forma.Id, forma.Nombre, forma.RequiereReferencia, forma.Activo);
        return CreatedAtAction(nameof(ObtenerPorId), new { id = forma.Id }, dto);
    }

    /// <summary>PUT /api/formas-pago/{id}</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = nameof(Rol.Administrador))]
    public async Task<ActionResult<FormaPagoDto>> Actualizar(int id, GuardarFormaPagoRequest request)
    {
        var forma = await db.FormasPago.FirstOrDefaultAsync(f => f.Id == id)
            ?? throw new NoEncontradoException($"No existe la forma de pago {id}.");

        var nombre = request.Nombre.Trim();
        if (await db.FormasPago.AnyAsync(f => f.Nombre == nombre && f.Id != id))
            throw new ConflictoException($"Ya existe la forma de pago '{nombre}'.");

        forma.Nombre = nombre;
        forma.RequiereReferencia = request.RequiereReferencia;
        forma.Activo = request.Activo;
        await db.SaveChangesAsync();

        return Ok(new FormaPagoDto(forma.Id, forma.Nombre, forma.RequiereReferencia, forma.Activo));
    }
}
