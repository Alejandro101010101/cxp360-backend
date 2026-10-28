# CxP360 – Backend (API REST)

API REST del **Sistema de Cuentas por Pagar de Ferretería Los Esclavos** (aldea Los Esclavos, Cuilapa, Santa Rosa).
Controla proveedores, facturas de compra, pagos totales y parciales, comprobantes adjuntos, estados de cuenta y reportes de saldos.

> Frontend (React): [cxp360-frontend](https://github.com/Alejandro101010101/cxp360-frontend)

---

## Tecnologías

| Capa | Tecnología |
|---|---|
| Lenguaje y framework | C# · ASP.NET Core 9 Web API (controladores) |
| Acceso a datos | Entity Framework Core 9 (SQL Server) |
| Base de datos | Microsoft SQL Server 2025 |
| Seguridad | JWT Bearer · contraseñas cifradas con `PasswordHasher` · roles Administrador / Operador |
| Errores | `ProblemDetails` (RFC 7807) con mensajes en español |
| Pruebas | Archivos `.http` (REST Client de VS Code) y script SQL de restricciones |

## Estructura del repositorio

```
cxp360-backend/
├── CxP360.Api/
│   ├── Configuracion/       Opciones del JWT
│   ├── Controllers/         Endpoints de la API
│   ├── Data/                DbContext y creación del administrador inicial
│   ├── Dtos/                Datos que entran y salen de la API
│   ├── Extensiones/         Lectura del usuario del token
│   ├── Infraestructura/     Manejador global de errores
│   ├── Models/              Entidades y reglas de negocio
│   ├── Services/            Token, reloj de Guatemala (UTC-6) y validación de comprobantes
│   ├── *.http               Pruebas de la API
│   └── Program.cs
└── database/
    ├── 01_crear_base_datos.sql          Crea la BD, tablas, restricciones y datos iniciales (local)
    ├── 02_pruebas_restricciones.sql     22 pruebas de las restricciones (no deja datos)
    ├── 03_crear_tablas_servidor.sql     Crea las tablas en una BD ya existente (hosting)
    ├── 04_comprobantes_y_controles.sql  Tabla Adjuntos y límite de pago en efectivo
    └── 05_limpiar_movimientos.sql       Borra facturas, pagos y comprobantes de prueba
```

## Módulos y endpoints

Todas las rutas requieren token, excepto `POST /api/auth/login` y `GET /api/salud`.

| Módulo | Endpoints principales |
|---|---|
| Autenticación | `POST /api/auth/login` · `GET /api/auth/perfil` |
| Menú principal | `GET /api/dashboard` |
| Proveedores | `GET/POST /api/proveedores` · `GET/PUT /api/proveedores/{id}` · `PUT /api/proveedores/{id}/estado` |
| Ingresos por compra | `GET/POST /api/facturas` · `GET /api/facturas/{id}` · `PUT /api/facturas/{id}/anular` · `POST /api/facturas/{id}/comprobantes` |
| Movimientos de pagos | `GET/POST /api/pagos` · `GET /api/pagos/{id}` · `PUT /api/pagos/{id}/anular` · `POST /api/pagos/{id}/comprobantes` |
| Comprobantes | `GET /api/adjuntos/{id}` (devuelve la imagen o el PDF) |
| Consultas y reportes | `GET /api/reportes/estado-cuenta` · `/pendientes` · `/antiguedad-saldos` · `/pagos` |
| Parámetros (Administrador) | `/api/configuracion` · `/api/formas-pago` · `/api/bancos` · `/api/usuarios` |
| Monitoreo | `GET /api/salud` |

## Reglas de negocio destacadas

- Facturas con **IVA incluido**: el sistema calcula base e IVA y el vencimiento según los días de crédito del proveedor.
- El **total según el documento** debe coincidir con la suma de las líneas.
- Toda factura lleva su **foto o PDF**; los pagos con cheque, transferencia o depósito exigen su **comprobante**.
- Los pagos en **efectivo** tienen un límite configurable (Q5,000 por defecto) y generan un recibo para firma.
- Un pago **no puede superar el saldo**; se detectan pagos duplicados y cheques repetidos.
- Las facturas y los pagos **no se eliminan**: se anulan con motivo, usuario y fecha.
- Cada movimiento guarda **quién lo registró**.

## Ejecutar en local (on premise)

**Requisitos:** .NET SDK 9, SQL Server (local, autenticación de Windows) y SSMS.

1. **Base de datos:** en SSMS ejecute `database/01_crear_base_datos.sql` y luego `database/04_comprobantes_y_controles.sql` (con la base `CxP360` seleccionada).
2. **Clave del token** (no se guarda en el repositorio), desde la carpeta `CxP360.Api`:
   ```
   dotnet user-secrets set "Jwt:Clave" "una-clave-secreta-de-al-menos-32-caracteres"
   ```
3. **Ejecutar la API:**
   ```
   dotnet run --project CxP360.Api
   ```
   Queda disponible en `http://localhost:5288`. Al arrancar con la base vacía se crea el usuario **admin** con la contraseña de `AdminInicial:Password` (en `appsettings.Development.json`, solo para desarrollo).
4. **Probar:** abra los archivos `.http` en VS Code (extensión REST Client) y ejecútelos en este orden:
   `CxP360.Api.http` → `Pruebas-Facturas-Pagos.http` → `Pruebas-Reportes.http` → `Pruebas-Comprobantes.http`.

## Publicación en la nube

En el servidor la base se crea con `03_crear_tablas_servidor.sql` y `04_comprobantes_y_controles.sql`.
La cadena de conexión, la clave del JWT, la contraseña del administrador y los orígenes de CORS se configuran en
`appsettings.Production.json`, que **no se sube al repositorio**.
