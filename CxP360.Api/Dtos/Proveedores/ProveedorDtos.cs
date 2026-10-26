using System.ComponentModel.DataAnnotations;

namespace CxP360.Api.Dtos.Proveedores;

/// <summary>Proveedor con su saldo actual (RF-13).</summary>
public record ProveedorDto(
    int Id,
    string Nit,
    string Nombre,
    string? Contacto,
    string? Telefono,
    string? Correo,
    string? Direccion,
    int DiasCredito,
    bool Activo,
    DateTime FechaRegistro,
    decimal Saldo,
    int FacturasPendientes);

/// <summary>Datos para registrar o modificar un proveedor (RF-09, RF-11).</summary>
public record GuardarProveedorRequest(
    [Required(ErrorMessage = "El NIT es obligatorio.")]
    [StringLength(20, ErrorMessage = "El NIT admite como máximo 20 caracteres.")]
    string Nit,

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(150, ErrorMessage = "El nombre admite como máximo 150 caracteres.")]
    string Nombre,

    [StringLength(100, ErrorMessage = "El contacto admite como máximo 100 caracteres.")]
    string? Contacto,

    [StringLength(20, ErrorMessage = "El teléfono admite como máximo 20 caracteres.")]
    string? Telefono,

    [StringLength(100, ErrorMessage = "El correo admite como máximo 100 caracteres.")]
    string? Correo,

    [StringLength(250, ErrorMessage = "La dirección admite como máximo 250 caracteres.")]
    string? Direccion,

    // Opcional: si no se envía, se usan los días de crédito por defecto (RN-07)
    [Range(0, 120, ErrorMessage = "Los días de crédito deben estar entre 0 y 120.")]
    int? DiasCredito);

/// <summary>Activar o inactivar (RF-12).</summary>
public record CambiarEstadoRequest(bool Activo);
