namespace CxP360.Api.Services;

/// <summary>
/// Fecha y hora de Guatemala (UTC-6 todo el año, sin horario de verano).
/// Se usa en lugar de la hora del servidor porque el servidor publicado
/// puede estar en otra zona horaria (RN-11, RN-19, RN-22).
/// </summary>
public static class Reloj
{
    private static readonly TimeSpan DesfaseGuatemala = TimeSpan.FromHours(-6);

    public static DateTime Ahora => DateTime.UtcNow + DesfaseGuatemala;

    public static DateOnly Hoy => DateOnly.FromDateTime(Ahora);
}
