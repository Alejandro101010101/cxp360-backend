namespace CxP360.Api.Models;

/// <summary>
/// Error que se produce cuando una operación viola una regla del negocio
/// (por ejemplo, un pago mayor al saldo). La API lo devolverá como un
/// mensaje claro para el usuario (RF-32).
/// </summary>
public class ReglaNegocioException(string mensaje) : Exception(mensaje);
