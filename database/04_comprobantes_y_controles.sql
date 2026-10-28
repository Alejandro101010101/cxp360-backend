/* ==================================================================
   CxP360 - Sistema de Cuentas por Pagar
   Script 04: comprobantes adjuntos y controles de montos

   Se ejecuta UNA VEZ, despues del script 01 (local) o del 03 (servidor).
   NO borra datos: solo agrega una tabla y una columna.

   Agrega:
     - Tabla Adjuntos: fotos o PDF de facturas y comprobantes de pago
       (RN-32, RN-33, RN-34, RN-37).
     - ConfiguracionEmpresa.LimitePagoEfectivo (RN-36).
   ================================================================== */

-- Proteccion: no ejecutar sobre las bases del sistema
IF DB_NAME() IN (N'master', N'model', N'msdb', N'tempdb')
BEGIN
    RAISERROR(N'Seleccione la base de datos de CxP360 antes de ejecutar este script.', 16, 1);
    SET NOEXEC ON;
END
GO

/* ------------------------------------------------------------------
   1. TABLA ADJUNTOS
   ------------------------------------------------------------------ */
IF OBJECT_ID(N'dbo.Adjuntos', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Adjuntos (
        AdjuntoId     INT IDENTITY(1,1) NOT NULL,
        FacturaId     INT            NULL,
        PagoId        INT            NULL,
        NombreArchivo NVARCHAR(200)  NOT NULL,
        TipoContenido NVARCHAR(100)  NOT NULL,
        TamanoBytes   INT            NOT NULL,
        Contenido     VARBINARY(MAX) NOT NULL,
        UsuarioId     INT            NOT NULL,
        FechaRegistro DATETIME2(0)   NOT NULL CONSTRAINT DF_Adjuntos_FechaRegistro DEFAULT (SYSDATETIME()),

        CONSTRAINT PK_Adjuntos PRIMARY KEY (AdjuntoId),
        CONSTRAINT FK_Adjuntos_Facturas FOREIGN KEY (FacturaId) REFERENCES dbo.Facturas (FacturaId),
        CONSTRAINT FK_Adjuntos_Pagos    FOREIGN KEY (PagoId)    REFERENCES dbo.Pagos (PagoId),
        CONSTRAINT FK_Adjuntos_Usuarios FOREIGN KEY (UsuarioId) REFERENCES dbo.Usuarios (UsuarioId),

        -- Cada comprobante pertenece a UNA factura o a UN pago, nunca a ambos
        CONSTRAINT CK_Adjuntos_Destino CHECK (
               (FacturaId IS NOT NULL AND PagoId IS NULL)
            OR (FacturaId IS NULL     AND PagoId IS NOT NULL)
        ),

        -- RN-34: solo imagenes JPG/PNG o PDF, maximo 2 MB
        CONSTRAINT CK_Adjuntos_Tipo CHECK (TipoContenido IN (N'image/jpeg', N'image/png', N'application/pdf')),
        CONSTRAINT CK_Adjuntos_Tamano CHECK (TamanoBytes > 0 AND TamanoBytes <= 2097152)
    );

    CREATE INDEX IX_Adjuntos_FacturaId ON dbo.Adjuntos (FacturaId) WHERE FacturaId IS NOT NULL;
    CREATE INDEX IX_Adjuntos_PagoId    ON dbo.Adjuntos (PagoId)    WHERE PagoId IS NOT NULL;

    PRINT N'Tabla Adjuntos creada.';
END
ELSE
    PRINT N'La tabla Adjuntos ya existia.';
GO

/* ------------------------------------------------------------------
   2. LIMITE DE PAGO EN EFECTIVO (RN-36)
      NULL = sin limite. Valor inicial: Q5,000.00
   ------------------------------------------------------------------ */
IF COL_LENGTH(N'dbo.ConfiguracionEmpresa', N'LimitePagoEfectivo') IS NULL
BEGIN
    ALTER TABLE dbo.ConfiguracionEmpresa
        ADD LimitePagoEfectivo DECIMAL(12,2) NULL
            CONSTRAINT DF_Config_LimitePagoEfectivo DEFAULT (5000.00) WITH VALUES;

    PRINT N'Columna LimitePagoEfectivo agregada.';
END
ELSE
    PRINT N'La columna LimitePagoEfectivo ya existia.';
GO

IF OBJECT_ID(N'dbo.CK_Config_LimitePagoEfectivo', N'C') IS NULL
    ALTER TABLE dbo.ConfiguracionEmpresa
        ADD CONSTRAINT CK_Config_LimitePagoEfectivo CHECK (LimitePagoEfectivo IS NULL OR LimitePagoEfectivo > 0);
GO

/* ------------------------------------------------------------------
   3. VERIFICACION
   ------------------------------------------------------------------ */
SELECT N'Adjuntos' AS Objeto,
       CASE WHEN OBJECT_ID(N'dbo.Adjuntos', N'U') IS NOT NULL THEN N'OK' ELSE N'FALTA' END AS Estado
UNION ALL
SELECT N'LimitePagoEfectivo',
       CASE WHEN COL_LENGTH(N'dbo.ConfiguracionEmpresa', N'LimitePagoEfectivo') IS NOT NULL THEN N'OK' ELSE N'FALTA' END;

SELECT Nombre, PorcentajeIva, DiasCreditoDefault, LimitePagoEfectivo
FROM dbo.ConfiguracionEmpresa;
GO

SET NOEXEC OFF;
GO
