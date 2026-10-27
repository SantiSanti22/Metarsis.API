using Metarsis.Api.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Soporte para controladores API REST y controladores MVC con vistas Razor
builder.Services.AddControllersWithViews();

// Inyectar el DbContext conectando con SQL Server
builder.Services.AddDbContext<MetarsisDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("CadenaSql")));

// Configuración del esquema de autenticación basado en Cookies para administradores y consultores[cite: 20]
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Usuario/Login";
        options.AccessDeniedPath = "/Usuario/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

// Habilitar la Interfaz Visual de Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configuración de la interfaz interactiva de Swagger UI
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.InjectStylesheet("/css/swagger-avenzo.css");
        c.InjectJavascript("/js/swagger-traductor.js");
        c.DocumentTitle = "Avenzo - Panel de Administración";
    });
}

app.UseHttpsRedirection();
app.UseStaticFiles();


app.UseRouting();

app.UseAuthentication(); // 1. Verifica quién es el usuario
app.UseAuthorization();  // 2. Verifica qué permisos tiene

// Ruta  que redirige hacia la pantalla de autenticación
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Usuario}/{action=Login}/{id?}");


app.MapControllers();

app.Run();