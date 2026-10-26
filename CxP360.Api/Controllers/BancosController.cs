using CxP360.Api.Data;
using CxP360.Api.Dtos.Parametros;
using CxP360.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CxP360.Api.Controllers;

/// <summary>
/// Catálogo de bancos (RF-08). No se eliminan: se inactivan (RN-05).
/// </summary>
[ApiController]
[Route("api/bancos")]
public class BancosController(CxP360DbContext db) : ControllerBase
{
    /// <summary>GET /api/bancos?soloActivos=true</summary>
    [HttpGet]
    public async Task<ActionResult<List<BancoDto>>> Listar(bool soloActivos = false)
    {
        var consulta = db.Bancos.AsNoTracking();
        if (soloActivos)
            consulta = consulta.Where(b => b.Activo);

        return Ok(await consulta
            .OrderBy(b => b.Nombre)
            .Select(b => new BancoDto(b.Id, b.Nombre, b.Activo))
            .ToListAsync());
    }

    /// <summary>GET /api/bancos/{id}</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<BancoDto>> ObtenerPorId(int id)
    {
        var b = await db.Bancos.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new NoEncontradoException($"No existe el banco {id}.");

        return Ok(new BancoDto(b.Id, b.Nombre, b.Activo));
    }

    /// <summary>POST /api/bancos</summary>
    [HttpPost]
    [Authorize(Roles = nameof(Rol.Administrador))]
    public async Task<ActionResult<BancoDto>> Crear(GuardarBancoRequest request)
    {
        var nombre = request.Nombre.Trim();
        if (await db.Bancos.AnyAsync(b => b.Nombre == nombre))
            throw new ConflictoException($"Ya existe el banco '{nombre}'.");

        var banco = new Banco { Nombre = nombre, Activo = request.Activo };
        db.Bancos.Add(banco);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(ObtenerPorId), new { id = banco.Id }, new BancoDto(banco.Id, banco.Nombre, banco.Activo));
    }

    /// <summary>PUT /api/bancos/{id}</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = nameof(Rol.Administrador))]
    public async Task<ActionResult<BancoDto>> Actualizar(int id, GuardarBancoRequest request)
    {
        var banco = await db.Bancos.FirstOrDefaultAsync(b => b.Id == id)
            ?? throw new NoEncontradoException($"No existe el banco {id}.");

        var nombre = request.Nombre.Trim();
        if (await db.Bancos.AnyAsync(b => b.Nombre == nombre && b.Id != id))
            throw new ConflictoException($"Ya existe el banco '{nombre}'.");

        banco.Nombre = nombre;
        banco.Activo = request.Activo;
        await db.SaveChangesAsync();

        return Ok(new BancoDto(banco.Id, banco.Nombre, banco.Activo));
    }
}
