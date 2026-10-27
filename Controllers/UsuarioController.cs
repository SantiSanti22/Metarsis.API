using System.Security.Claims;
using Metarsis.Api.Data;
using Metarsis.Api.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Metarsis.Api.Controllers
{
    public class UsuarioController : Controller
    {
        private readonly MetarsisDbContext _context;

        public UsuarioController(MetarsisDbContext context)
        {
            _context = context;
        }

        // 1. VISTA DE INICIO DE SESIÓN 
        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Panel");
            }

            return View();
        }

        // 2. PROCESAMIENTO Y AUTENTICACIÓN DE CREDENCIALES 
        [HttpPost]
        public async Task<IActionResult> Login(string correo, string password)
        {
            if (string.IsNullOrWhiteSpace(correo) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Por favor, ingresa tu correo y contraseña.";
                return View();
            }

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Correo == correo.Trim() && u.Activo);

            if (usuario == null || usuario.PasswordHash != password)
            {
                ViewBag.Error = "Correo o contraseña incorrectos, o la cuenta está inactiva.";
                return View();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.UsuarioID.ToString()),
                new Claim(ClaimTypes.Name, usuario.NombreCompleto),
                new Claim(ClaimTypes.Email, usuario.Correo),
                new Claim(ClaimTypes.Role, usuario.Rol ?? "Admin"),
                new Claim("TelefonoWhatsApp", usuario.TelefonoWhatsApp ?? string.Empty)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true, 
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            return RedirectToAction("Index", "Panel");
        }

        // 3. CIERRE DE SESIÓN
        [HttpGet]
        public async Task<IActionResult> Salir()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Usuario");
        }
    }
}