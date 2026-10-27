using CxP360.Api.Dtos.Pagos;
using CxP360.Api.Models;

namespace CxP360.Api.Dtos.Reportes;

/// <summary>Encabezado común para imprimir cualquier reporte (RF-29).</summary>
public record EncabezadoReporteDto(
    string Empresa,
    string Nit,
    string Direccion,
    string Titulo,
    DateTime Generado);

// ======================================================================
// PANEL DE INICIO (RF-04)
// ======================================================================

public record FacturaAlertaDto(
    int Id,
    string Proveedor,
    string Documento,
    DateOnly FechaVencimiento,
    decimal Saldo,
    int Dias);            // días de atraso (vencidas) o días restantes (próximas)

public record SaldoProveedorDto(int ProveedorId, string Proveedor, decimal Saldo);

public record DashboardDto(
    decimal TotalAdeudado,
    int FacturasPendientes,
    int FacturasVencidas,
    decimal MontoVencido,
    int FacturasPorVencer7Dias,
    decimal MontoPorVencer7Dias,
    decimal PagadoMes,
    int PagosMes,
    int ProveedoresActivos,
    List<FacturaAlertaDto> ProximasAVencer,
    List<FacturaAlertaDto> MasVencidas,
    List<SaldoProveedorDto> MayoresSaldos);

// ======================================================================
// ESTADO DE CUENTA (RF-25, RN-27)
// ======================================================================

public record MovimientoCuentaDto(
    DateOnly Fecha,
    string Tipo,          // "Factura" (cargo) o "Pago" (abono)
    string Documento,
    string Descripcion,
    decimal Cargo,
    decimal Abono,
    decimal Saldo);       // saldo acumulado después del movimiento

public record EstadoCuentaDto(
    EncabezadoReporteDto Encabezado,
    int ProveedorId,
    string ProveedorNit,
    string ProveedorNombre,
    int DiasCredito,
    DateOnly Desde,
    DateOnly Hasta,
    decimal SaldoInicial,
    decimal TotalCargos,
    decimal TotalAbonos,
    decimal SaldoFinal,
    List<MovimientoCuentaDto> Movimientos);

// ======================================================================
// FACTURAS PENDIENTES Y VENCIDAS (RF-26)
// ======================================================================

public record FacturaPendienteDto(
    int Id,
    string ProveedorNit,
    string Proveedor,
    string Documento,
    DateOnly FechaEmision,
    DateOnly FechaVencimiento,
    decimal Total,
    decimal Saldo,
    EstadoFactura Estado,
    bool Vencida,
    int DiasVencida);

public record FacturasPendientesDto(
    EncabezadoReporteDto Encabezado,
    int Cantidad,
    decimal TotalSaldo,
    int CantidadVencidas,
    decimal TotalVencido,
    List<FacturaPendienteDto> Facturas);

// ======================================================================
// ANTIGÜEDAD DE SALDOS (RF-27, RN-29)
// ======================================================================

public record AntiguedadFilaDto(
    int ProveedorId,
    string ProveedorNit,
    string Proveedor,
    decimal PorVencer,
    decimal De1a30,
    decimal De31a60,
    decimal De61a90,
    decimal MasDe90,
    decimal Total);

public record AntiguedadSaldosDto(
    EncabezadoReporteDto Encabezado,
    DateOnly FechaCorte,
    List<AntiguedadFilaDto> Proveedores,
    AntiguedadFilaDto Totales);

// ======================================================================
// PAGOS POR PERÍODO (RF-28)
// ======================================================================

public record ResumenFormaPagoDto(string FormaPago, int Cantidad, decimal Total);

public record PagosPeriodoDto(
    EncabezadoReporteDto Encabezado,
    DateOnly Desde,
    DateOnly Hasta,
    int Cantidad,
    decimal Total,
    List<ResumenFormaPagoDto> PorFormaPago,
    List<PagoDto> Pagos);
