/* ================================================================
   CxP360 - Sistema de Cuentas por Pagar
   Script 05: limpiar movimientos de prueba

   BORRA todas las facturas (ingresos por compra), su detalle,
   los pagos y los comprobantes adjuntos, y reinicia sus
   numeradores para que el siguiente registro sea el No. 1.

   CONSERVA: usuarios, proveedores, formas de pago, bancos y la
   configuracion de la empresa (incluido el limite de efectivo).

   Opcional: tambien elimina el proveedor de prueba creado por
   CxP360.Api.http (NIT 2468135-9), solo si no tiene facturas.

   Uselo antes de cargar los datos reales de la demostracion.
   ================================================================ */

-- Proteccion: no ejecutar sobre las bases del sistema
IF DB_NAME() <> N'CxP360'
BEGIN
    RAISERROR(N'Seleccione la base de datos CxP360 antes de ejecutar este script.', 16, 1);
    SET NOEXEC ON;
END
GO

SET XACT_ABORT ON;
BEGIN TRANSACTION;

    -- El orden respeta las llaves foraneas (primero los hijos)
    DELETE FROM dbo.Adjuntos;
    DELETE FROM dbo.Pagos;
    DELETE FROM dbo.DetalleFactura;
    DELETE FROM dbo.Facturas;

    -- Proveedor de prueba de CxP360.Api.http (ya no tiene facturas)
    DELETE FROM dbo.Proveedores WHERE Nit = N'2468135-9';

    -- Reiniciar numeradores: el proximo registro sera el 1
    DBCC CHECKIDENT ('dbo.Adjuntos',       RESEED, 0) WITH NO_INFOMSGS;
    DBCC CHECKIDENT ('dbo.Pagos',          RESEED, 0) WITH NO_INFOMSGS;
    DBCC CHECKIDENT ('dbo.DetalleFactura', RESEED, 0) WITH NO_INFOMSGS;
    DBCC CHECKIDENT ('dbo.Facturas',       RESEED, 0) WITH NO_INFOMSGS;

COMMIT TRANSACTION;
GO

-- Verificacion: movimientos en 0 y catalogos intactos
SELECT 'Facturas' AS Tabla, COUNT(*) AS Registros FROM dbo.Facturas
UNION ALL SELECT 'DetalleFactura', COUNT(*) FROM dbo.DetalleFactura
UNION ALL SELECT 'Pagos',          COUNT(*) FROM dbo.Pagos
UNION ALL SELECT 'Adjuntos',       COUNT(*) FROM dbo.Adjuntos
UNION ALL SELECT 'Proveedores',    COUNT(*) FROM dbo.Proveedores
UNION ALL SELECT 'Usuarios',       COUNT(*) FROM dbo.Usuarios
UNION ALL SELECT 'Bancos',         COUNT(*) FROM dbo.Bancos
UNION ALL SELECT 'FormasPago',     COUNT(*) FROM dbo.FormasPago;
GO

SET NOEXEC OFF;
GO
