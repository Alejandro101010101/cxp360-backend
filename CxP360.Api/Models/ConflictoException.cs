namespace CxP360.Api.Models;

/// <summary>
/// El dato ya existe, por ejemplo un NIT repetido (la API responde 409).
/// </summary>
public class ConflictoException(string mensaje) : Exception(mensaje);
