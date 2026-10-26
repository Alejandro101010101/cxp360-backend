using System.Text;
using CxP360.Api.Configuracion;
using CxP360.Api.Data;
using CxP360.Api.Infraestructura;
using CxP360.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ======================================================================
// SERVICIOS
// ======================================================================

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        // RF-32: errores de validación con título en español
        options.InvalidModelStateResponseFactory = context =>
            new BadRequestObjectResult(new ValidationProblemDetails(context.ModelState)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Los datos enviados no son válidos."
            });
    });

builder.Services.AddOpenApi();

// Manejo centralizado de errores (reglas de negocio, no encontrado, duplicados)
builder.Services.AddExceptionHandler<ManejadorExcepciones>();
builder.Services.AddProblemDetails();

// Conexión a SQL Server con Entity Framework Core
builder.Services.AddDbContext<CxP360DbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("CxP360")));

// CORS: dominios desde los que el frontend (React) puede llamar a la API
var origenesPermitidos = builder.Configuration.GetSection("Cors:Origenes").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options =>
    options.AddPolicy("Frontend", policy => policy
        .WithOrigins(origenesPermitidos)
        .AllowAnyHeader()
        .AllowAnyMethod()));

// Configuración del token JWT
var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOpciones>() ?? new JwtOpciones();
if (string.IsNullOrWhiteSpace(jwt.Clave) || jwt.Clave.Length < 32)
{
    throw new InvalidOperationException(
        "Falta la clave del token (Jwt:Clave) o es muy corta. " +
        "En desarrollo, configúrala con: dotnet user-secrets set \"Jwt:Clave\" \"...\"");
}

builder.Services.Configure<JwtOpciones>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddSingleton<TokenService>();

// Autenticación: la API acepta tokens JWT firmados con nuestra clave
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;                  // conserva los nombres cortos: sub, name, role
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Clave)),
            ValidateLifetime = true,                       // rechaza tokens vencidos
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = "name",
            RoleClaimType = "role"
        };
    });

// Autorización: RN-02, toda la API exige haber iniciado sesión,
// salvo los endpoints marcados con [AllowAnonymous] (login y salud)
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build());

var app = builder.Build();

// Crea el usuario Administrador la primera vez que arranca la API
await InicializadorDatos.CrearAdministradorInicialAsync(app);

// ======================================================================
// RECORRIDO DE CADA PETICIÓN (el orden importa)
// ======================================================================

app.UseExceptionHandler();   // 0. Atrapa cualquier error y responde con un mensaje claro

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
}

app.UseHttpsRedirection();

app.UseCors("Frontend");     // 1. Permite peticiones desde el frontend
app.UseAuthentication();     // 2. ¿Quién es? (lee y valida el token)
app.UseAuthorization();      // 3. ¿Tiene permiso?

app.MapControllers();

app.Run();
