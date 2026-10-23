namespace CxP360.Api.Models;

public class FormaPago
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;

    /// <summary>RN-23: cheque, transferencia y depósito exigen banco y referencia.</summary>
    public bool RequiereReferencia { get; set; }

    public bool Activo { get; set; } = true;
}
