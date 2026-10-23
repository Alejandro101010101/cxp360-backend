/* ==================================================================
   CxP360 - Sistema de Cuentas por Pagar
   Script 02: pruebas de las restricciones de la base de datos

   Cada prueba intenta guardar un dato valido o invalido y compara
   el resultado con lo esperado. Todo se ejecuta dentro de una
   transaccion que al final se REVIERTE (ROLLBACK): la base de
   datos queda exactamente como estaba.
   ================================================================== */

USE CxP360;
GO

SET NOCOUNT ON;
SET XACT_ABORT OFF;   -- un error en una prueba no cancela las demas

DECLARE @R TABLE (
    No       INT,
    Prueba   NVARCHAR(150),
    Esperado NVARCHAR(10),
    Obtenido NVARCHAR(10),
    Detalle  NVARCHAR(400)
);

BEGIN TRANSACTION;

/* ---------- Datos temporales para las pruebas ---------- */
INSERT INTO dbo.Usuarios (NombreCompleto, NombreUsuario, PasswordHash, Rol)
VALUES (N'Usuario de prueba', N'prueba.temporal', N'hash-de-prueba', N'Operador');
DECLARE @U INT = SCOPE_IDENTITY();

DECLARE @P1 INT = (SELECT ProveedorId FROM dbo.Proveedores WHERE Nit = N'4521367-1');
DECLARE @P2 INT = (SELECT ProveedorId FROM dbo.Proveedores WHERE Nit = N'3698524-7');
DECLARE @Efectivo INT = (SELECT FormaPagoId FROM dbo.FormasPago WHERE Nombre = N'Efectivo');
DECLARE @Cheque   INT = (SELECT FormaPagoId FROM dbo.FormasPago WHERE Nombre = N'Cheque');
DECLARE @Banrural INT = (SELECT BancoId FROM dbo.Bancos WHERE Nombre = N'Banrural');

-- Factura valida base: Q112.00 = Q100.00 + Q12.00 de IVA
INSERT INTO dbo.Facturas (ProveedorId, Serie, Numero, FechaEmision, FechaVencimiento, Base, Iva, Total, Saldo, Estado, UsuarioId)
VALUES (@P1, N'A', N'1001', '2026-10-01', '2026-10-31', 100.00, 12.00, 112.00, 112.00, N'Pendiente', @U);
DECLARE @F INT = SCOPE_IDENTITY();

/* ================== FACTURAS ================== */

-- 1
BEGIN TRY
    INSERT INTO dbo.Facturas (ProveedorId, Serie, Numero, FechaEmision, FechaVencimiento, Base, Iva, Total, Saldo, Estado, UsuarioId)
    VALUES (@P1, N'A', N'1001', '2026-10-02', '2026-11-01', 100.00, 12.00, 112.00, 112.00, N'Pendiente', @U);
    INSERT INTO @R VALUES (1, N'Factura duplicada (mismo proveedor, serie y número)', N'Rechazada', N'Aceptada', NULL);
END TRY
BEGIN CATCH
    INSERT INTO @R VALUES (1, N'Factura duplicada (mismo proveedor, serie y número)', N'Rechazada', N'Rechazada', ERROR_MESSAGE());
END CATCH;

-- 2
BEGIN TRY
    INSERT INTO dbo.Facturas (ProveedorId, Serie, Numero, FechaEmision, FechaVencimiento, Base, Iva, Total, Saldo, Estado, UsuarioId)
    VALUES (@P2, N'A', N'1001', '2026-10-02', '2026-12-01', 100.00, 12.00, 112.00, 112.00, N'Pendiente', @U);
    INSERT INTO @R VALUES (2, N'Misma serie y número, pero de otro proveedor', N'Aceptada', N'Aceptada', NULL);
END TRY
BEGIN CATCH
    INSERT INTO @R VALUES (2, N'Misma serie y número, pero de otro proveedor', N'Aceptada', N'Rechazada', ERROR_MESSAGE());
END CATCH;

-- 3
BEGIN TRY
    INSERT INTO dbo.Facturas (ProveedorId, Serie, Numero, FechaEmision, FechaVencimiento, Base, Iva, Total, Saldo, Estado, UsuarioId)
    VALUES (@P1, N'A', N'1003', '2026-10-10', '2026-10-01', 100.00, 12.00, 112.00, 112.00, N'Pendiente', @U);
    INSERT INTO @R VALUES (3, N'Vencimiento anterior a la emisión', N'Rechazada', N'Aceptada', NULL);
END TRY
BEGIN CATCH
    INSERT INTO @R VALUES (3, N'Vencimiento anterior a la emisión', N'Rechazada', N'Rechazada', ERROR_MESSAGE());
END CATCH;

-- 4
BEGIN TRY
    INSERT INTO dbo.Facturas (ProveedorId, Serie, Numero, FechaEmision, FechaVencimiento, Base, Iva, Total, Saldo, Estado, UsuarioId)
    VALUES (@P1, N'A', N'1004', '2026-10-01', '2026-10-31', 100.00, 12.00, 112.00, 200.00, N'Pendiente', @U);
    INSERT INTO @R VALUES (4, N'Saldo mayor que el total', N'Rechazada', N'Aceptada', NULL);
END TRY
BEGIN CATCH
    INSERT INTO @R VALUES (4, N'Saldo mayor que el total', N'Rechazada', N'Rechazada', ERROR_MESSAGE());
END CATCH;

-- 5
BEGIN TRY
    INSERT INTO dbo.Facturas (ProveedorId, Serie, Numero, FechaEmision, FechaVencimiento, Base, Iva, Total, Saldo, Estado, UsuarioId)
    VALUES (@P1, N'A', N'1005', '2026-10-01', '2026-10-31', 100.00, 10.00, 112.00, 112.00, N'Pendiente', @U);
    INSERT INTO @R VALUES (5, N'Base + IVA no cuadra con el total', N'Rechazada', N'Aceptada', NULL);
END TRY
BEGIN CATCH
    INSERT INTO @R VALUES (5, N'Base + IVA no cuadra con el total', N'Rechazada', N'Rechazada', ERROR_MESSAGE());
END CATCH;

-- 6
BEGIN TRY
    INSERT INTO dbo.Facturas (ProveedorId, Serie, Numero, FechaEmision, FechaVencimiento, Base, Iva, Total, Saldo, Estado, UsuarioId)
    VALUES (@P1, N'A', N'1006', '2026-10-01', '2026-10-31', 100.00, 12.00, 112.00, 112.00, N'Vencida', @U);
    INSERT INTO @R VALUES (6, N'Estado "Vencida" guardado (debe calcularse)', N'Rechazada', N'Aceptada', NULL);
END TRY
BEGIN CATCH
    INSERT INTO @R VALUES (6, N'Estado "Vencida" guardado (debe calcularse)', N'Rechazada', N'Rechazada', ERROR_MESSAGE());
END CATCH;

-- 7
BEGIN TRY
    INSERT INTO dbo.Facturas (ProveedorId, Serie, Numero, FechaEmision, FechaVencimiento, Base, Iva, Total, Saldo, Estado, UsuarioId)
    VALUES (@P1, N'A', N'1007', '2026-10-01', '2026-10-31', 100.00, 12.00, 112.00, 50.00, N'Pagada', @U);
    INSERT INTO @R VALUES (7, N'Estado "Pagada" con saldo pendiente', N'Rechazada', N'Aceptada', NULL);
END TRY
BEGIN CATCH
    INSERT INTO @R VALUES (7, N'Estado "Pagada" con saldo pendiente', N'Rechazada', N'Rechazada', ERROR_MESSAGE());
END CATCH;

-- 8
BEGIN TRY
    INSERT INTO dbo.Facturas (ProveedorId, Serie, Numero, FechaEmision, FechaVencimiento, Base, Iva, Total, Saldo, Estado, UsuarioId)
    VALUES (@P1, N'A', N'1008', '2026-10-01', '2026-10-31', 100.00, 12.00, 112.00, 112.00, N'Anulada', @U);
    INSERT INTO @R VALUES (8, N'Factura anulada sin motivo ni usuario', N'Rechazada', N'Aceptada', NULL);
END TRY
BEGIN CATCH
    INSERT INTO @R VALUES (8, N'Factura anulada sin motivo ni usuario', N'Rechazada', N'Rechazada', ERROR_MESSAGE());
END CATCH;

-- 9 (simula un pago mayor al saldo: Q112.00 - Q500.00)
BEGIN TRY
    UPDATE dbo.Facturas SET Saldo = Saldo - 500.00, Estado = N'Parcial' WHERE FacturaId = @F;
    INSERT INTO @R VALUES (9, N'Saldo negativo (pago mayor al saldo)', N'Rechazada', N'Aceptada', NULL);
END TRY
BEGIN CATCH
    INSERT INTO @R VALUES (9, N'Saldo negativo (pago mayor al saldo)', N'Rechazada', N'Rechazada', ERROR_MESSAGE());
END CATCH;

/* ================== DETALLE DE FACTURA ================== */

-- 10
BEGIN TRY
    INSERT INTO dbo.DetalleFactura (FacturaId, Descripcion, Cantidad, PrecioUnitario)
    VALUES (@F, N'Saco de cemento 42.5 kg', 0, 85.00);
    INSERT INTO @R VALUES (10, N'Línea de detalle con cantidad 0', N'Rechazada', N'Aceptada', NULL);
END TRY
BEGIN CATCH
    INSERT INTO @R VALUES (10, N'Línea de detalle con cantidad 0', N'Rechazada', N'Rechazada', ERROR_MESSAGE());
END CATCH;

-- 11
BEGIN TRY
    INSERT INTO dbo.DetalleFactura (FacturaId, Descripcion, Cantidad, PrecioUnitario)
    VALUES (@F, N'Galón de pintura blanca', 4, 28.00);
    DECLARE @Sub DECIMAL(12,2) = (SELECT Subtotal FROM dbo.DetalleFactura WHERE DetalleId = SCOPE_IDENTITY());
    INSERT INTO @R VALUES (11, N'Línea válida: subtotal calculado (4 × Q28.00)', N'Aceptada', N'Aceptada',
                           CONCAT(N'Subtotal calculado por SQL Server: Q', @Sub));
END TRY
BEGIN CATCH
    INSERT INTO @R VALUES (11, N'Línea válida: subtotal calculado (4 × Q28.00)', N'Aceptada', N'Rechazada', ERROR_MESSAGE());
END CATCH;

/* ================== PAGOS ================== */

-- 12
BEGIN TRY
    INSERT INTO dbo.Pagos (FacturaId, FormaPagoId, FechaPago, Monto, UsuarioId)
    VALUES (@F, @Efectivo, '2026-10-05', 0.00, @U);
    INSERT INTO @R VALUES (12, N'Pago con monto 0', N'Rechazada', N'Aceptada', NULL);
END TRY
BEGIN CATCH
    INSERT INTO @R VALUES (12, N'Pago con monto 0', N'Rechazada', N'Rechazada', ERROR_MESSAGE());
END CATCH;

-- 13
BEGIN TRY
    INSERT INTO dbo.Pagos (FacturaId, FormaPagoId, FechaPago, Monto, UsuarioId)
    VALUES (@F, @Efectivo, '2026-10-05', 12.00, @U);
    INSERT INTO @R VALUES (13, N'Pago en efectivo sin banco ni referencia', N'Aceptada', N'Aceptada', NULL);
END TRY
BEGIN CATCH
    INSERT INTO @R VALUES (13, N'Pago en efectivo sin banco ni referencia', N'Aceptada', N'Rechazada', ERROR_MESSAGE());
END CATCH;

-- 14
BEGIN TRY
    INSERT INTO dbo.Pagos (FacturaId, FormaPagoId, BancoId, FechaPago, Monto, UsuarioId)
    VALUES (@F, @Cheque, @Banrural, '2026-10-05', 20.00, @U);
    INSERT INTO @R VALUES (14, N'Pago con banco pero sin número de referencia', N'Rechazada', N'Aceptada', NULL);
END TRY
BEGIN CATCH
    INSERT INTO @R VALUES (14, N'Pago con banco pero sin número de referencia', N'Rechazada', N'Rechazada', ERROR_MESSAGE());
END CATCH;

-- 15
DECLARE @PagoCheque INT;
BEGIN TRY
    INSERT INTO dbo.Pagos (FacturaId, FormaPagoId, BancoId, FechaPago, Monto, NumeroReferencia, UsuarioId)
    VALUES (@F, @Cheque, @Banrural, '2026-10-06', 50.00, N'4521', @U);
    SET @PagoCheque = SCOPE_IDENTITY();
    INSERT INTO @R VALUES (15, N'Cheque #4521 de Banrural', N'Aceptada', N'Aceptada', NULL);
END TRY
BEGIN CATCH
    INSERT INTO @R VALUES (15, N'Cheque #4521 de Banrural', N'Aceptada', N'Rechazada', ERROR_MESSAGE());
END CATCH;

-- 16
BEGIN TRY
    INSERT INTO dbo.Pagos (FacturaId, FormaPagoId, BancoId, FechaPago, Monto, NumeroReferencia, UsuarioId)
    VALUES (@F, @Cheque, @Banrural, '2026-10-07', 30.00, N'4521', @U);
    INSERT INTO @R VALUES (16, N'Mismo cheque #4521 de Banrural otra vez', N'Rechazada', N'Aceptada', NULL);
END TRY
BEGIN CATCH
    INSERT INTO @R VALUES (16, N'Mismo cheque #4521 de Banrural otra vez', N'Rechazada', N'Rechazada', ERROR_MESSAGE());
END CATCH;

-- 17 (se anula el cheque y se registra de nuevo)
BEGIN TRY
    UPDATE dbo.Pagos
    SET Anulado = 1, MotivoAnulacion = N'Prueba de anulación', UsuarioAnulacionId = @U, FechaAnulacion = SYSDATETIME()
    WHERE PagoId = @PagoCheque;

    INSERT INTO dbo.Pagos (FacturaId, FormaPagoId, BancoId, FechaPago, Monto, NumeroReferencia, UsuarioId)
    VALUES (@F, @Cheque, @Banrural, '2026-10-07', 50.00, N'4521', @U);
    INSERT INTO @R VALUES (17, N'Mismo cheque, si el anterior fue anulado', N'Aceptada', N'Aceptada', NULL);
END TRY
BEGIN CATCH
    INSERT INTO @R VALUES (17, N'Mismo cheque, si el anterior fue anulado', N'Aceptada', N'Rechazada', ERROR_MESSAGE());
END CATCH;

/* ================== PROVEEDORES, USUARIOS Y PARAMETROS ================== */

-- 18
BEGIN TRY
    INSERT INTO dbo.Proveedores (Nit, Nombre, DiasCredito) VALUES (N'4521367-1', N'Proveedor con NIT repetido', 30);
    INSERT INTO @R VALUES (18, N'Proveedor con NIT duplicado', N'Rechazada', N'Aceptada', NULL);
END TRY
BEGIN CATCH
    INSERT INTO @R VALUES (18, N'Proveedor con NIT duplicado', N'Rechazada', N'Rechazada', ERROR_MESSAGE());
END CATCH;

-- 19
BEGIN TRY
    INSERT INTO dbo.Proveedores (Nit, Nombre, DiasCredito) VALUES (N'0000001-1', N'Proveedor con crédito excesivo', 150);
    INSERT INTO @R VALUES (19, N'Proveedor con 150 días de crédito', N'Rechazada', N'Aceptada', NULL);
END TRY
BEGIN CATCH
    INSERT INTO @R VALUES (19, N'Proveedor con 150 días de crédito', N'Rechazada', N'Rechazada', ERROR_MESSAGE());
END CATCH;

-- 20
BEGIN TRY
    DELETE FROM dbo.Proveedores WHERE ProveedorId = @P1;
    INSERT INTO @R VALUES (20, N'Eliminar un proveedor que tiene facturas', N'Rechazada', N'Aceptada', NULL);
END TRY
BEGIN CATCH
    INSERT INTO @R VALUES (20, N'Eliminar un proveedor que tiene facturas', N'Rechazada', N'Rechazada', ERROR_MESSAGE());
END CATCH;

-- 21
BEGIN TRY
    INSERT INTO dbo.Usuarios (NombreCompleto, NombreUsuario, PasswordHash, Rol)
    VALUES (N'Usuario inválido', N'rol.invalido', N'hash', N'Supervisor');
    INSERT INTO @R VALUES (21, N'Usuario con rol "Supervisor"', N'Rechazada', N'Aceptada', NULL);
END TRY
BEGIN CATCH
    INSERT INTO @R VALUES (21, N'Usuario con rol "Supervisor"', N'Rechazada', N'Rechazada', ERROR_MESSAGE());
END CATCH;

-- 22
BEGIN TRY
    INSERT INTO dbo.ConfiguracionEmpresa (Nombre, Nit, Direccion, Telefono) VALUES (N'Otra empresa', N'1-1', N'Otra dirección', N'0000-0000');
    INSERT INTO @R VALUES (22, N'Segundo registro de configuración', N'Rechazada', N'Aceptada', NULL);
END TRY
BEGIN CATCH
    INSERT INTO @R VALUES (22, N'Segundo registro de configuración', N'Rechazada', N'Rechazada', ERROR_MESSAGE());
END CATCH;

/* ---------- Se deshace todo: la base queda como estaba ---------- */
IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;

/* ---------- Resultados ---------- */
SELECT No,
       Prueba,
       Esperado,
       Obtenido,
       CASE WHEN Esperado = Obtenido THEN N'OK' ELSE N'FALLA' END AS Resultado,
       Detalle
FROM @R
ORDER BY No;

SELECT COUNT(*) AS TotalPruebas,
       SUM(CASE WHEN Esperado = Obtenido THEN 1 ELSE 0 END) AS Correctas,
       SUM(CASE WHEN Esperado <> Obtenido THEN 1 ELSE 0 END) AS Fallidas
FROM @R;
GO
