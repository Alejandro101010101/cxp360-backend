namespace CxP360.Api.Models;

/// <summary>
/// Estados que se guardan en la base de datos (RN-18).
/// "Vencida" no está aquí: se calcula al consultar con Factura.EstaVencida (RN-19).
/// </summary>
public enum EstadoFactura
{
    Pendiente,
    Parcial,
    Pagada,
    Anulada
}
