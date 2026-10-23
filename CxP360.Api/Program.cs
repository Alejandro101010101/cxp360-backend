using CxP360.Api.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ---------- Servicios ----------
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Conexión a SQL Server con Entity Framework Core
builder.Services.AddDbContext<CxP360DbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("CxP360")));

var app = builder.Build();

// ---------- Recorrido de cada petición ----------
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();