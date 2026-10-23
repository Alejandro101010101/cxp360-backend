/* ==================================================================
   CxP360 - Sistema de Cuentas por Pagar
   Ferreteria Los Esclavos, Cuilapa, Santa Rosa

   Script 01: creacion de la base de datos, tablas, restricciones,
              indices y datos iniciales.
   Motor:     Microsoft SQL Server 2025

   ADVERTENCIA: si la base de datos CxP360 ya existe, este script
   la ELIMINA y la crea de nuevo. Usarlo solo durante el desarrollo.
   ================================================================== */

USE master;
GO

IF DB_ID(N'CxP360') IS NOT NULL
BEGIN
    ALTER DATABASE CxP360 SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE CxP360;
END
GO

-- Modern_Spanish_CI_AI: no distingue mayusculas/minusculas ni tildes
-- (buscar "Ferreteria" encuentra "Ferretería").
CREATE DATABASE CxP360 COLLATE Modern_Spanish_CI_AI;
GO

USE CxP360;
GO

/* ==================================================================
   1. PARAMETROS DEL SISTEMA
   ================================================================== */

-- Registro unico con los datos de la empresa y la configuracion general.
CREATE TABLE dbo.ConfiguracionEmpresa (
    ConfiguracionId    INT IDENTITY(1,1) NOT NULL,
    Nombre             NVARCHAR(150) NOT NULL,
    Nit                NVARCHAR(20)  NOT NULL,
    Direccion          NVARCHAR(250) NOT NULL,
    Telefono           NVARCHAR(20)  NOT NULL,
    PorcentajeIva      DECIMAL(5,2)  NOT NULL CONSTRAINT DF_Config_PorcentajeIva DEFAULT (12.00),
    DiasCreditoDefault INT           NOT NULL CONSTRAINT DF_Config_DiasCredito   DEFAULT (30),

    CONSTRAINT PK_ConfiguracionEmpresa PRIMARY KEY (ConfiguracionId),
    CONSTRAINT CK_Config_RegistroUnico CHECK (ConfiguracionId = 1),                         -- solo puede existir un registro
    CONSTRAINT CK_Config_PorcentajeIva CHECK (PorcentajeIva BETWEEN 0 AND 100),
    CONSTRAINT CK_Config_DiasCredito   CHECK (DiasCreditoDefault BETWEEN 0 AND 120)         -- RN-07
);
GO

CREATE TABLE dbo.FormasPago (
    FormaPagoId        INT IDENTITY(1,1) NOT NULL,
    Nombre             NVARCHAR(50) NOT NULL,
    RequiereReferencia BIT          NOT NULL,                                               -- RN-23
    Activo             BIT          NOT NULL CONSTRAINT DF_FormasPago_Activo DEFAULT (1),

    CONSTRAINT PK_FormasPago PRIMARY KEY (FormaPagoId),
    CONSTRAINT UQ_FormasPago_Nombre UNIQUE (Nombre)
);
GO

CREATE TABLE dbo.Bancos (
    BancoId INT IDENTITY(1,1) NOT NULL,
    Nombre  NVARCHAR(100) NOT NULL,
    Activo  BIT           NOT NULL CONSTRAINT DF_Bancos_Activo DEFAULT (1),

    CONSTRAINT PK_Bancos PRIMARY KEY (BancoId),
    CONSTRAINT UQ_Bancos_Nombre UNIQUE (Nombre)
);
GO

/* ==================================================================
   2. SEGURIDAD
   ================================================================== */

CREATE TABLE dbo.Usuarios (
    UsuarioId      INT IDENTITY(1,1) NOT NULL,
    NombreCompleto NVARCHAR(100) NOT NULL,
    NombreUsuario  NVARCHAR(50)  NOT NULL,
    PasswordHash   NVARCHAR(255) NOT NULL,                                                  -- RN-03: nunca texto plano
    Rol            NVARCHAR(15)  NOT NULL,
    Activo         BIT           NOT NULL CONSTRAINT DF_Usuarios_Activo        DEFAULT (1),
    FechaCreacion  DATETIME2(0)  NOT NULL CONSTRAINT DF_Usuarios_FechaCreacion DEFAULT (SYSDATETIME()),

    CONSTRAINT PK_Usuarios PRIMARY KEY (UsuarioId),
    CONSTRAINT UQ_Usuarios_NombreUsuario UNIQUE (NombreUsuario),
    CONSTRAINT CK_Usuarios_Rol CHECK (Rol IN (N'Administrador', N'Operador'))                -- RN-30
);
GO

/* ==================================================================
   3. PROVEEDORES
   ================================================================== */

CREATE TABLE dbo.Proveedores (
    ProveedorId   INT IDENTITY(1,1) NOT NULL,
    Nit           NVARCHAR(20)  NOT NULL,
    Nombre        NVARCHAR(150) NOT NULL,
    Contacto      NVARCHAR(100) NULL,
    Telefono      NVARCHAR(20)  NULL,
    Correo        NVARCHAR(100) NULL,
    Direccion     NVARCHAR(250) NULL,
    DiasCredito   INT           NOT NULL,
    Activo        BIT           NOT NULL CONSTRAINT DF_Proveedores_Activo        DEFAULT (1),
    FechaRegistro DATETIME2(0)  NOT NULL CONSTRAINT DF_Proveedores_FechaRegistro DEFAULT (SYSDATETIME()),

    CONSTRAINT PK_Proveedores PRIMARY KEY (ProveedorId),
    CONSTRAINT UQ_Proveedores_Nit UNIQUE (Nit),                                             -- RN-06
    CONSTRAINT CK_Proveedores_DiasCredito CHECK (DiasCredito BETWEEN 0 AND 120)             -- RN-07
);
GO

/* ==================================================================
   4. INGRESOS POR COMPRA (FACTURAS)
   ================================================================== */

CREATE TABLE dbo.Facturas (
    FacturaId          INT IDENTITY(1,1) NOT NULL,
    ProveedorId        INT           NOT NULL,
    Serie              NVARCHAR(10)  NOT NULL,
    Numero             NVARCHAR(20)  NOT NULL,
    FechaEmision       DATE          NOT NULL,
    FechaVencimiento   DATE          NOT NULL,
    Base               DECIMAL(12,2) NOT NULL,
    Iva                DECIMAL(12,2) NOT NULL,
    Total              DECIMAL(12,2) NOT NULL,
    Saldo              DECIMAL(12,2) NOT NULL,
    Estado             NVARCHAR(10)  NOT NULL CONSTRAINT DF_Facturas_Estado        DEFAULT (N'Pendiente'),
    UsuarioId          INT           NOT NULL,
    FechaRegistro      DATETIME2(0)  NOT NULL CONSTRAINT DF_Facturas_FechaRegistro DEFAULT (SYSDATETIME()),
    MotivoAnulacion    NVARCHAR(250) NULL,
    UsuarioAnulacionId INT           NULL,
    FechaAnulacion     DATETIME2(0)  NULL,

    CONSTRAINT PK_Facturas PRIMARY KEY (FacturaId),
    CONSTRAINT FK_Facturas_Proveedores      FOREIGN KEY (ProveedorId)        REFERENCES dbo.Proveedores (ProveedorId),
    CONSTRAINT FK_Facturas_Usuarios         FOREIGN KEY (UsuarioId)          REFERENCES dbo.Usuarios (UsuarioId),
    CONSTRAINT FK_Facturas_UsuarioAnulacion FOREIGN KEY (UsuarioAnulacionId) REFERENCES dbo.Usuarios (UsuarioId),

    -- RN-10: no se repite la misma factura del mismo proveedor
    CONSTRAINT UQ_Facturas_Proveedor_Serie_Numero UNIQUE (ProveedorId, Serie, Numero),

    -- RN-13: el vencimiento no puede ser anterior a la emision
    CONSTRAINT CK_Facturas_Vencimiento CHECK (FechaVencimiento >= FechaEmision),

    -- RN-14: total positivo y desglose cuadrado (Base + IVA = Total)
    CONSTRAINT CK_Facturas_Total    CHECK (Total > 0),
    CONSTRAINT CK_Facturas_Desglose CHECK (Base >= 0 AND Iva >= 0 AND Base + Iva = Total),

    -- RN-15 / RN-20: el saldo nunca es negativo ni mayor al total
    CONSTRAINT CK_Facturas_Saldo CHECK (Saldo >= 0 AND Saldo <= Total),

    -- RN-18: solo estados validos ("Vencida" se calcula al consultar, RN-19)
    CONSTRAINT CK_Facturas_Estado CHECK (Estado IN (N'Pendiente', N'Parcial', N'Pagada', N'Anulada')),

    -- RN-18: el estado debe corresponder al saldo
    CONSTRAINT CK_Facturas_EstadoSaldo CHECK (
           (Estado = N'Pendiente' AND Saldo = Total)
        OR (Estado = N'Parcial'   AND Saldo > 0 AND Saldo < Total)
        OR (Estado = N'Pagada'    AND Saldo = 0)
        OR (Estado = N'Anulada')
    ),

    -- RN-04 / RN-17: una factura anulada guarda motivo, usuario y fecha
    CONSTRAINT CK_Facturas_Anulacion CHECK (
           (Estado =  N'Anulada' AND MotivoAnulacion IS NOT NULL AND UsuarioAnulacionId IS NOT NULL AND FechaAnulacion IS NOT NULL)
        OR (Estado <> N'Anulada' AND MotivoAnulacion IS NULL     AND UsuarioAnulacionId IS NULL     AND FechaAnulacion IS NULL)
    )
);
GO

CREATE TABLE dbo.DetalleFactura (
    DetalleId      INT IDENTITY(1,1) NOT NULL,
    FacturaId      INT           NOT NULL,
    Descripcion    NVARCHAR(200) NOT NULL,
    Cantidad       DECIMAL(10,2) NOT NULL,
    PrecioUnitario DECIMAL(12,2) NOT NULL,                                                  -- con IVA incluido (RN-14)
    -- Columna calculada: SQL Server la calcula sola, nunca queda desactualizada
    Subtotal       AS CAST(ROUND(Cantidad * PrecioUnitario, 2) AS DECIMAL(12,2)) PERSISTED,

    CONSTRAINT PK_DetalleFactura PRIMARY KEY (DetalleId),
    -- Composicion: las lineas pertenecen a su factura
    CONSTRAINT FK_DetalleFactura_Facturas FOREIGN KEY (FacturaId) REFERENCES dbo.Facturas (FacturaId) ON DELETE CASCADE,
    CONSTRAINT CK_DetalleFactura_Cantidad CHECK (Cantidad > 0),                             -- RN-12
    CONSTRAINT CK_DetalleFactura_Precio   CHECK (PrecioUnitario > 0)                        -- RN-12
);
GO

/* ==================================================================
   5. MOVIMIENTOS DE PAGOS
   ================================================================== */

CREATE TABLE dbo.Pagos (
    PagoId             INT IDENTITY(1,1) NOT NULL,
    FacturaId          INT           NOT NULL,
    FormaPagoId        INT           NOT NULL,
    BancoId            INT           NULL,                                                  -- NULL en efectivo (RN-23)
    FechaPago          DATE          NOT NULL,
    Monto              DECIMAL(12,2) NOT NULL,
    NumeroReferencia   NVARCHAR(50)  NULL,
    Anulado            BIT           NOT NULL CONSTRAINT DF_Pagos_Anulado       DEFAULT (0),
    UsuarioId          INT           NOT NULL,
    FechaRegistro      DATETIME2(0)  NOT NULL CONSTRAINT DF_Pagos_FechaRegistro DEFAULT (SYSDATETIME()),
    MotivoAnulacion    NVARCHAR(250) NULL,
    UsuarioAnulacionId INT           NULL,
    FechaAnulacion     DATETIME2(0)  NULL,

    CONSTRAINT PK_Pagos PRIMARY KEY (PagoId),
    CONSTRAINT FK_Pagos_Facturas         FOREIGN KEY (FacturaId)          REFERENCES dbo.Facturas (FacturaId),
    CONSTRAINT FK_Pagos_FormasPago       FOREIGN KEY (FormaPagoId)        REFERENCES dbo.FormasPago (FormaPagoId),
    CONSTRAINT FK_Pagos_Bancos           FOREIGN KEY (BancoId)            REFERENCES dbo.Bancos (BancoId),
    CONSTRAINT FK_Pagos_Usuarios         FOREIGN KEY (UsuarioId)          REFERENCES dbo.Usuarios (UsuarioId),
    CONSTRAINT FK_Pagos_UsuarioAnulacion FOREIGN KEY (UsuarioAnulacionId) REFERENCES dbo.Usuarios (UsuarioId),

    -- RN-20: el monto debe ser mayor a cero
    CONSTRAINT CK_Pagos_Monto CHECK (Monto > 0),

    -- RN-23: banco y referencia van juntos (ambos o ninguno, como en efectivo)
    CONSTRAINT CK_Pagos_BancoReferencia CHECK (
           (BancoId IS NULL     AND NumeroReferencia IS NULL)
        OR (BancoId IS NOT NULL AND NumeroReferencia IS NOT NULL)
    ),

    -- RN-04 / RN-26: un pago anulado guarda motivo, usuario y fecha
    CONSTRAINT CK_Pagos_Anulacion CHECK (
           (Anulado = 1 AND MotivoAnulacion IS NOT NULL AND UsuarioAnulacionId IS NOT NULL AND FechaAnulacion IS NOT NULL)
        OR (Anulado = 0 AND MotivoAnulacion IS NULL     AND UsuarioAnulacionId IS NULL     AND FechaAnulacion IS NULL)
    )
);
GO

/* ==================================================================
   6. INDICES
   ================================================================== */

-- RN-24: el mismo cheque o transferencia no se registra dos veces.
-- Indice unico FILTRADO: solo considera pagos no anulados, asi un
-- cheque anulado por error puede registrarse de nuevo.
CREATE UNIQUE INDEX UX_Pagos_Referencia
    ON dbo.Pagos (FormaPagoId, BancoId, NumeroReferencia)
    WHERE Anulado = 0 AND NumeroReferencia IS NOT NULL;
GO

-- Indices para acelerar las consultas mas frecuentes
CREATE INDEX IX_Facturas_Estado_Vencimiento ON dbo.Facturas (Estado, FechaVencimiento);  -- panel de inicio y vencidas
CREATE INDEX IX_Facturas_FechaEmision       ON dbo.Facturas (FechaEmision);              -- estado de cuenta
CREATE INDEX IX_DetalleFactura_FacturaId    ON dbo.DetalleFactura (FacturaId);
CREATE INDEX IX_Pagos_FacturaId             ON dbo.Pagos (FacturaId);
CREATE INDEX IX_Pagos_FechaPago             ON dbo.Pagos (FechaPago);                    -- pagos por periodo
GO

/* ==================================================================
   7. DATOS INICIALES
   ================================================================== */

INSERT INTO dbo.ConfiguracionEmpresa (Nombre, Nit, Direccion, Telefono, PorcentajeIva, DiasCreditoDefault)
VALUES (N'Ferretería Los Esclavos', N'9876543-2', N'Aldea Los Esclavos, Cuilapa, Santa Rosa', N'7886-0000', 12.00, 30);

INSERT INTO dbo.FormasPago (Nombre, RequiereReferencia) VALUES
    (N'Efectivo',      0),
    (N'Cheque',        1),
    (N'Transferencia', 1),
    (N'Depósito',      1);

INSERT INTO dbo.Bancos (Nombre) VALUES
    (N'Banrural'),
    (N'Banco Industrial'),
    (N'BAM'),
    (N'G&T Continental'),
    (N'BAC Credomatic'),
    (N'Banco Promerica');

-- Proveedores de demostracion (nombres y datos ficticios)
INSERT INTO dbo.Proveedores (Nit, Nombre, Contacto, Telefono, Direccion, DiasCredito) VALUES
    (N'4521367-1', N'Distribuidora Cementos del Sur',       N'Carlos Méndez',  N'7880-1101', N'Barberena, Santa Rosa',        30),
    (N'3698524-7', N'Hierros y Aceros Santa Rosa',          N'Ana Lucía Pérez', N'7880-2202', N'Cuilapa, Santa Rosa',          60),
    (N'7412589-3', N'Eléctrica Industrial Barberena',       N'Luis Hernández', N'7880-3303', N'Barberena, Santa Rosa',        30),
    (N'8523697-4', N'Tuberías y Plomería de Escuintla',     N'Marta Aguilar',  N'7880-4404', N'Escuintla, Escuintla',         15),
    (N'9632587-0', N'Pinturas y Recubrimientos de Oriente', N'Jorge Castillo', N'7880-5505', N'Jutiapa, Jutiapa',             30),
    (N'1597534-6', N'Herramientas El Constructor',          N'Sofía Ramírez',  N'7880-6606', N'Chiquimulilla, Santa Rosa',     0);
GO

/* ==================================================================
   8. VERIFICACION
   ================================================================== */

-- Tablas creadas y numero de registros
SELECT t.name AS Tabla,
       SUM(p.rows) AS Registros
FROM sys.tables AS t
JOIN sys.partitions AS p ON p.object_id = t.object_id AND p.index_id IN (0, 1)
GROUP BY t.name
ORDER BY t.name;

-- Resumen de restricciones por tipo
SELECT CASE type
           WHEN 'PK' THEN 'Llave primaria'
           WHEN 'F'  THEN 'Llave foranea'
           WHEN 'UQ' THEN 'Valor unico'
           WHEN 'C'  THEN 'Restriccion CHECK'
           WHEN 'D'  THEN 'Valor por defecto'
       END AS TipoRestriccion,
       COUNT(*) AS Cantidad
FROM sys.objects
WHERE type IN ('PK', 'F', 'UQ', 'C', 'D')
GROUP BY type
ORDER BY TipoRestriccion;
GO
