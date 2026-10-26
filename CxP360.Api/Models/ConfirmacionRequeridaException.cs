namespace CxP360.Api.Models;

/// <summary>
/// La operación es válida pero parece un error del usuario (RN-25: pago idéntico
/// el mismo día). La API responde 409 con "requiereConfirmacion": true y la
/// pantalla debe preguntar antes de reenviar con confirmación.
/// </summary>
public class ConfirmacionRequeridaException(string mensaje) : Exception(mensaje);
