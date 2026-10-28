using CxP360.Api.Dtos.Comun;
using CxP360.Api.Models;

namespace CxP360.Api.Services;

/// <summary>
/// Valida y crea comprobantes adjuntos (RN-34).
/// Además del tipo declarado, revisa los primeros bytes del archivo ("firma")
/// para impedir que se suba, por ejemplo, un ejecutable renombrado como .pdf.
/// </summary>
public static class Comprobantes
{
    public const int TamanoMaximo = 2 * 1024 * 1024;   // 2 MB

    private static readonly Dictionary<string, byte[]> Firmas = new()
    {
        ["image/jpeg"] = [0xFF, 0xD8, 0xFF],
        ["image/png"] = [0x89, 0x50, 0x4E, 0x47],
        ["application/pdf"] = [0x25, 0x50, 0x44, 0x46],   // "%PDF"
    };

    public static Adjunto Crear(ArchivoRequest archivo, int usuarioId)
    {
        var tipo = archivo.TipoContenido.Trim().ToLowerInvariant();
        if (tipo == "image/jpg") tipo = "image/jpeg";

        if (!Firmas.TryGetValue(tipo, out var firma))
            throw new ReglaNegocioException("El comprobante debe ser una imagen JPG o PNG, o un documento PDF.");

        var contenido = archivo.Contenido ?? [];
        if (contenido.Length == 0)
            throw new ReglaNegocioException("El archivo del comprobante está vacío.");
        if (contenido.Length > TamanoMaximo)
            throw new ReglaNegocioException("El comprobante no puede superar 2 MB. Reduzca la calidad de la foto o comprima el PDF.");
        if (contenido.Length < firma.Length || !contenido.AsSpan(0, firma.Length).SequenceEqual(firma))
            throw new ReglaNegocioException("El contenido del archivo no corresponde a una imagen JPG/PNG ni a un PDF válido.");

        var nombre = Path.GetFileName(archivo.NombreArchivo.Trim());
        if (string.IsNullOrWhiteSpace(nombre)) nombre = "comprobante";
        if (nombre.Length > 200) nombre = nombre[^200..];

        return new Adjunto
        {
            NombreArchivo = nombre,
            TipoContenido = tipo,
            TamanoBytes = contenido.Length,
            Contenido = contenido,
            UsuarioId = usuarioId,
            FechaRegistro = Reloj.Ahora
        };
    }
}
