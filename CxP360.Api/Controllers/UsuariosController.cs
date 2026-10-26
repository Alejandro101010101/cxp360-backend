using CxP360.Api.Data;
using CxP360.Api.Dtos.Usuarios;
using CxP360.Api.Extensiones;
using CxP360.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CxP360.Api.Controllers;

/// <summary>
/// Administración de usuarios (RF-33). Todo el controlador es solo para Administrador (RN-31).
/// </summary>
[ApiController]
[Route("api/usuarios")]
[Authorize(Roles = nameof(Rol.Administrador))]
public class UsuariosController(CxP360DbContext db) : ControllerBase
{
    /// <summary>GET /api/usuarios</summary>
    [HttpGet]
    public async Task<ActionResult<List<UsuarioDto>>> Listar()
    {
        var usuarios = await db.Usuarios.AsNoTracking().OrderBy(u => u.NombreCompleto).ToListAsync();
        return Ok(usuarios.Select(Mapear).ToList());
    }

    /// <summary>GET /api/usuarios/{id}</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<UsuarioDto>> ObtenerPorId(int id)
    {
        var usuario = await db.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id)
            ?? throw new NoEncontradoException($"No existe el usuario {id}.");

        return Ok(Mapear(usuario));
    }

    /// <summary>POST /api/usuarios</summary>
    [HttpPost]
    public async Task<ActionResult<UsuarioDto>> Crear(CrearUsuarioRequest request)
    {
        var nombreUsuario = request.NombreUsuario.Trim().ToLowerInvariant();
        if (await db.Usuarios.AnyAsync(u => u.NombreUsuario == nombreUsuario))
            throw new ConflictoException($"Ya existe el usuario '{nombreUsuario}'.");

        var usuario = new Usuario
        {
            NombreCompleto = request.NombreCompleto.Trim(),
            NombreUsuario = nombreUsuario,
            Rol = LeerRol(request.Rol),
            Activo = true
        };
        usuario.EstablecerPassword(request.Password);          // se guarda cifrada (RN-03)

        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(ObtenerPorId), new { id = usuario.Id }, Mapear(usuario));
    }

    /// <summary>PUT /api/usuarios/{id}: nombre, rol y activo.</summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<UsuarioDto>> Actualizar(int id, ActualizarUsuarioRequest request)
    {
        var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Id == id)
            ?? throw new NoEncontradoException($"No existe el usuario {id}.");

        var rol = LeerRol(request.Rol);

        // Evita que el Administrador se bloquee a sí mismo
        if (id == User.ObtenerUsuarioId() && (!request.Activo || rol != Rol.Administrador))
            throw new ReglaNegocioException("No puede desactivar su propio usuario ni quitarse el rol de Administrador.");

        usuario.NombreCompleto = request.NombreCompleto.Trim();
        usuario.Rol = rol;
        usuario.Activo = request.Activo;
        await db.SaveChangesAsync();

        return Ok(Mapear(usuario));
    }

    /// <summary>PUT /api/usuarios/{id}/password: el Administrador restablece una contraseña.</summary>
    [HttpPut("{id:int}/password")]
    public async Task<IActionResult> RestablecerPassword(int id, RestablecerPasswordRequest request)
    {
        var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Id == id)
            ?? throw new NoEncontradoException($"No existe el usuario {id}.");

        usuario.EstablecerPassword(request.PasswordNueva);
        await db.SaveChangesAsync();

        return NoContent();
    }

    private static Rol LeerRol(string texto) =>
        Enum.TryParse<Rol>(texto, ignoreCase: true, out var rol) && Enum.IsDefined(rol) && !int.TryParse(texto, out _)
            ? rol
            : throw new ReglaNegocioException("El rol debe ser 'Administrador' u 'Operador'.");

    private static UsuarioDto Mapear(Usuario u) =>
        new(u.Id, u.NombreCompleto, u.NombreUsuario, u.Rol.ToString(), u.Activo, u.FechaCreacion);
}
