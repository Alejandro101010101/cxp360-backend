using System.Linq.Expressions;
using CxP360.Api.Data;
using CxP360.Api.Dtos.Comun;
using CxP360.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CxP360.Api.Controllers;

/// <summary>
/// Descarga de comprobantes adjuntos (RF-34).
/// Los comprobantes no se eliminan (RN-37): son evidencia de cada movimiento.
/// </summary>
[ApiController]
[Route("api/adjuntos")]
public class AdjuntosController(CxP360DbContext db) : ControllerBase
{
    /// <summary>Datos del comprobante sin su contenido (para listas).</summary>
    public static readonly Expression<Func<Adjunto, AdjuntoDto>> AAdjuntoDto = a => new AdjuntoDto(
        a.Id, a.NombreArchivo, a.TipoContenido, a.TamanoBytes, a.Usuario.NombreCompleto, a.FechaRegistro);

    /// <summary>GET /api/adjuntos/{id}: devuelve el archivo (imagen o PDF)</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Descargar(int id)
    {
        var archivo = await db.Adjuntos.AsNoTracking()
            .Where(a => a.Id == id)
            .Select(a => new { a.NombreArchivo, a.TipoContenido, a.Contenido })
            .FirstOrDefaultAsync()
            ?? throw new NoEncontradoException($"No existe el comprobante {id}.");

        // Sin nombre de descarga: el navegador lo muestra en lugar de descargarlo
        return File(archivo.Contenido, archivo.TipoContenido);
    }

    internal static async Task<AdjuntoDto> LeerDto(CxP360DbContext db, int id) =>
        await db.Adjuntos.AsNoTracking().Where(a => a.Id == id).Select(AAdjuntoDto).FirstAsync();
}
