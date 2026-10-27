using Metarsis.Api.Data;
using Metarsis.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace Metarsis.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Tags("Gestión de Prueba TAT")]
    public class PruebaTATController : ControllerBase
    {
        private readonly MetarsisDbContext _context;
        private readonly IConfiguration _configuration;

        public PruebaTATController(MetarsisDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // 1. ASIGNACIÓN INDIVIDUAL DE PRUEBA TAT DESDE EL PANEL
        [HttpPost("asignar")]
        public async Task<IActionResult> AsignarPruebaTATIndividual([FromBody] AsignarTatIndividualDto peticion)
        {
            if (string.IsNullOrWhiteSpace(peticion.Telefono))
                return BadRequest("El número de teléfono es obligatorio.");

            var encuesta = await _context.Encuestas
                .Include(e => e.Empresa)
                .FirstOrDefaultAsync(e => e.EncuestaID == peticion.EncuestaId);

            if (encuesta == null)
                return NotFound("La encuesta especificada no existe.");

            int? evaluadorId = peticion.UsuarioId;

            if (!evaluadorId.HasValue || evaluadorId <= 0)
            {
                var claimId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(claimId, out int idSesion))
                {
                    evaluadorId = idSesion;
                }
            }

            Usuario? evaluador = null;
            if (evaluadorId.HasValue && evaluadorId > 0)
            {
                evaluador = await _context.Set<Usuario>().FindAsync(evaluadorId.Value);
            }

            string twilioNumber = _configuration["Twilio:WhatsAppNumber"] ?? "";
            string numeroLimpio = twilioNumber.Replace("+", "");
            string numeroDestino = peticion.Telefono.Trim();

            bool yaAsignado = await _context.AsignacionesTAT
                .AnyAsync(a => a.EncuestaID == encuesta.EncuestaID && a.NumeroTelefono == numeroDestino);

            if (yaAsignado)
                return BadRequest("Este empleado ya tiene la Prueba TAT asignada previamente.");

            var nuevaAsignacion = new AsignacionTAT
            {
                EncuestaID = encuesta.EncuestaID,
                NumeroTelefono = numeroDestino,
                LinkID = Guid.NewGuid(),
                Completada = false,
                FechaAsignacion = DateTime.Now,

                UsuarioID = evaluador?.UsuarioID,
                NombreAdministrador = evaluador?.NombreCompleto ?? "Sin asignar",
                TelefonoAdministrador = evaluador?.TelefonoWhatsApp
            };

            _context.AsignacionesTAT.Add(nuevaAsignacion);
            await _context.SaveChangesAsync();

            string linkTAT = $"https://wa.me/{numeroLimpio}?text=Comenzar_TAT_{nuevaAsignacion.LinkID}";

            string mensajeInvitacion = $"¡Buen día! Derivado de la encuesta que realizaste con *{encuesta.Empresa?.NombreEmpresa}*, has sido seleccionado para participar en la *Prueba TAT*, una dinámica complementaria.\n\nHaz clic en el siguiente enlace y presiona enviar para comenzar la prueba:\n\n{linkTAT}";

            EnviarMensajeWhatsApp(numeroDestino, mensajeInvitacion);

            return Ok(new
            {
                mensaje = "Prueba asignada y enviada correctamente.",
                asignacionId = nuevaAsignacion.AsignacionID,
                evaluador = nuevaAsignacion.NombreAdministrador
            });
        }

        // 2. ASIGNACIÓN MASIVA DE PRUEBAS TAT
        [HttpPost("asignar-invitaciones")]
        public async Task<IActionResult> AsignarPruebaTATMasiva([FromBody] AsignarTatDto peticion)
        {
            if (peticion.NumerosSeleccionados == null || !peticion.NumerosSeleccionados.Any())
                return BadRequest("Debes ingresar al menos un número de teléfono.");

            var encuesta = await _context.Encuestas
                .Include(e => e.Empresa)
                .FirstOrDefaultAsync(e => e.EncuestaID == peticion.EncuestaID);

            if (encuesta == null)
                return NotFound("La encuesta especificada no existe.");

            var claimId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            Usuario? evaluador = null;
            if (int.TryParse(claimId, out int idSesion))
            {
                evaluador = await _context.Set<Usuario>().FindAsync(idSesion);
            }

            string twilioNumber = _configuration["Twilio:WhatsAppNumber"] ?? "";
            string numeroLimpio = twilioNumber.Replace("+", "");

            var resultadosEnvio = new List<object>();

            foreach (var numero in peticion.NumerosSeleccionados)
            {
                string numeroDestino = numero.Trim().Replace("+", "");

                bool yaAsignado = await _context.AsignacionesTAT
                    .AnyAsync(a => a.EncuestaID == encuesta.EncuestaID && a.NumeroTelefono == numeroDestino);

                if (yaAsignado)
                {
                    resultadosEnvio.Add(new { Numero = numeroDestino, Estatus = "Omitido (Ya tenía asignación previa)" });
                    continue;
                }

                var nuevaAsignacion = new AsignacionTAT
                {
                    EncuestaID = encuesta.EncuestaID,
                    NumeroTelefono = numeroDestino,
                    LinkID = Guid.NewGuid(),
                    Completada = false,
                    FechaAsignacion = DateTime.Now,
                    UsuarioID = evaluador?.UsuarioID,
                    NombreAdministrador = evaluador?.NombreCompleto ?? "Sin asignar",
                    TelefonoAdministrador = evaluador?.TelefonoWhatsApp
                };

                _context.AsignacionesTAT.Add(nuevaAsignacion);
                await _context.SaveChangesAsync();

                string linkTAT = $"https://wa.me/{numeroLimpio}?text=Comenzar_TAT_{nuevaAsignacion.LinkID}";

                string mensajeInvitacion = $"Hola. Por parte de la encuesta de la empresa *{encuesta.Empresa.NombreEmpresa}*, has sido seleccionado para participar en la *Prueba TAT*, una dinámica de seguimiento confidencial.\n\n" +
                                           $"Haz clic en el siguiente enlace y presiona enviar para comenzar la prueba:\n\n{linkTAT}";

                EnviarMensajeWhatsApp(numeroDestino, mensajeInvitacion);

                resultadosEnvio.Add(new { Numero = numeroDestino, Estatus = "Enviado con éxito", LinkGenerado = linkTAT });
            }

            return Ok(new
            {
                Mensaje = "Proceso de asignación TAT finalizado.",
                Detalles = resultadosEnvio
            });
        }

        // MÉTODO AUXILIAR: ENVÍO DE MENSAJES WHATSAPP VÍA TWILIO
        private void EnviarMensajeWhatsApp(string numeroDestino, string mensajeTexto)
        {
            string accountSid = _configuration["Twilio:AccountSid"] ?? "";
            string authToken = _configuration["Twilio:AuthToken"] ?? "";
            string twilioNumber = _configuration["Twilio:WhatsAppNumber"] ?? "";

            TwilioClient.Init(accountSid, authToken);

            string numeroE164 = numeroDestino.StartsWith("+") ? numeroDestino : "+" + numeroDestino;

            MessageResource.Create(
                body: mensajeTexto,
                from: new Twilio.Types.PhoneNumber("whatsapp:" + twilioNumber),
                to: new Twilio.Types.PhoneNumber("whatsapp:" + numeroE164)
            );
        }

        [HttpPost("toggle-lamina/{id}")]
        public async Task<IActionResult> ToggleLamina(int id)
        {
            var lamina = await _context.Set<LaminaTAT>().FindAsync(id);
            if (lamina == null)
                return NotFound();

            lamina.Activa = !lamina.Activa;
            await _context.SaveChangesAsync();

            return Ok(new { estado = lamina.Activa });
        }
    }

    // DTOs Y MODELOS DE TRANSFERENCIA DE DATOS
    public class AsignarTatDto
    {
        public int EncuestaID { get; set; }
        public List<string> NumerosSeleccionados { get; set; } = new List<string>();
    }
    public class AsignarTatIndividualDto
    {
        public string Telefono { get; set; } = string.Empty;
        public int EncuestaId { get; set; }
        public int? UsuarioId { get; set; } // Campo opcional para inyectar evaluador directamente
    }
}