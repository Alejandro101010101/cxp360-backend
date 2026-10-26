using CxP360.Api.Models;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CxP360.Api.Infraestructura;

/// <summary>
/// Convierte cualquier error en una respuesta clara y uniforme (RF-32):
///   { "status": 400, "title": "El pago supera el saldo pendiente..." }
/// </summary>
public class ManejadorExcepciones(ILogger<ManejadorExcepciones> logger, IHostEnvironment env) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext http, Exception ex, CancellationToken ct)
    {
        var (status, titulo) = ex switch
        {
            NoEncontradoException       => (StatusCodes.Status404NotFound, ex.Message),
            ConflictoException          => (StatusCodes.Status409Conflict, ex.Message),
            ReglaNegocioException       => (StatusCodes.Status400BadRequest, ex.Message),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, ex.Message),

            // Respaldo: errores que detecta SQL Server (sección 10.2 del análisis)
            DbUpdateException { InnerException: SqlException { Number: 2627 or 2601 } }
                => (StatusCodes.Status409Conflict, "Ya existe un registro con esos datos."),
            DbUpdateException { InnerException: SqlException { Number: 547 } }
                => (StatusCodes.Status400BadRequest, "La operación no cumple una regla de integridad de la base de datos."),

            _ => (StatusCodes.Status500InternalServerError, "Ocurrió un error inesperado. Intente de nuevo.")
        };

        if (status == StatusCodes.Status500InternalServerError)
            logger.LogError(ex, "Error no controlado");

        var problema = new ProblemDetails
        {
            Status = status,
            Title = titulo,
            // El detalle técnico solo se muestra en desarrollo
            Detail = status == StatusCodes.Status500InternalServerError && env.IsDevelopment() ? ex.ToString() : null
        };

        http.Response.StatusCode = status;
        await http.Response.WriteAsJsonAsync(problema, options: null, contentType: "application/problem+json", ct);
        return true;
    }
}
