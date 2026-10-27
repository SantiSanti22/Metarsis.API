using Metarsis.Api.Data;
using Metarsis.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenAI.Chat;
using System.Security.Claims; 
using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace Metarsis.Api.Controllers
{
    // DTO para la captura del motivo en el rechazo de solicitudes
    public class RechazoDto
    {
        public string? Motivo { get; set; }
    }

    [Authorize] 
    [ApiController]
    [Route("api/[controller]")]
    [Tags("Gestión de Solicitudes (Admin)")]
    public class SolicitudesController : ControllerBase
    {
        private readonly MetarsisDbContext _context;
        private readonly IConfiguration _configuration;

        public SolicitudesController(MetarsisDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // 1. CONSULTA DE SOLICITUDES EN ESTADO PENDIENTE
        [HttpGet("pendientes")]
        public async Task<IActionResult> ObtenerPendientes()
        {
            var pendientes = await _context.SolicitudesEncuestas
                .Include(s => s.Empresa)
                .Where(s => s.Estatus == "Pendiente")
                .Select(s => new {
                    s.SolicitudID,
                    s.Empresa.NombreEmpresa,
                    s.Empresa.NombreContacto,
                    s.Empresa.TelefonoContacto,
                    s.NumeroEmpleados,
                    s.DiasEstimados,
                    s.Problematica,
                    s.FechaSolicitud
                })
                .ToListAsync();

            if (!pendientes.Any())
                return Ok(new { Mensaje = "No hay solicitudes pendientes por revisar." });

            return Ok(pendientes);
        }

        // 2. RECHAZO DE SOLICITUD Y NOTIFICACIÓN AL CLIENTE
        [HttpPost("{id}/rechazar")]
        public async Task<IActionResult> RechazarSolicitud(int id, [FromBody] RechazoDto? dto)
        {
            var solicitud = await _context.SolicitudesEncuestas
                .Include(s => s.Empresa)
                .FirstOrDefaultAsync(s => s.SolicitudID == id);

            if (solicitud == null) return NotFound("Solicitud no encontrada.");
            if (solicitud.Estatus != "Pendiente") return BadRequest($"La solicitud ya fue {solicitud.Estatus}.");

            solicitud.Estatus = "Rechazada";

            if (!string.IsNullOrEmpty(solicitud.Empresa?.TelefonoContacto))
            {
                string telLimpio = solicitud.Empresa.TelefonoContacto.Replace("whatsapp:", "").Trim();
                var sesionBot = await _context.SesionesBotWhatsApp.FirstOrDefaultAsync(s => s.NumeroTelefono == telLimpio);
                if (sesionBot != null)
                {
                    sesionBot.TipoFlujo = "RegistroInicial";
                    sesionBot.PasoActual = 0;
                    sesionBot.ContextoID = null;
                    sesionBot.ErroresConsecutivos = 0;
                    sesionBot.UltimaInteraccion = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();

            string razon = !string.IsNullOrWhiteSpace(dto?.Motivo)
                ? dto.Motivo.Trim()
                : "Los datos proporcionados no cumplen con los requerimientos necesarios para el diagnóstico.";

            string mensajeAviso = $"Hola, te informamos que tras revisar los datos proporcionados, tu solicitud de diagnóstico para *{solicitud.Empresa?.NombreEmpresa}* no ha podido ser procesada en esta ocasión.\n\n" +
                                  $"📋 *Motivo:*\n_{razon}_\n\n" +
                                  $"Si deseas enviar una nueva solicitud corrigiendo este punto, escribe la palabra: *Iniciar*.";

            EnviarMensajeWhatsApp(solicitud.Empresa.TelefonoContacto, mensajeAviso);

            return Ok(new { Mensaje = $"Solicitud {id} RECHAZADA. Cliente notificado con el motivo y sesión liberada." });
        }

        // 3. APROBACIÓN DE SOLICITUD, ASIGNACIÓN DE CONSULTOR Y GENERACIÓN DE PREGUNTAS CON IA
        [HttpPost("{id}/aprobar")]
        public async Task<IActionResult> AprobarSolicitud(int id)
        {
            var solicitud = await _context.SolicitudesEncuestas
                .Include(s => s.Empresa)
                .FirstOrDefaultAsync(s => s.SolicitudID == id);

            if (solicitud == null) return NotFound("Solicitud no encontrada.");
            if (solicitud.Estatus != "Pendiente") return BadRequest($"La solicitud ya fue {solicitud.Estatus}.");

            string claimId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0";
            int.TryParse(claimId, out int usuarioSesionId);

            solicitud.Estatus = "Aprobada";
            await _context.SaveChangesAsync();

            try
            {
                string apiKey = _configuration["OpenAI:ApiKey"] ?? "";
                ChatClient client = new(model: "gpt-4o-mini", apiKey: apiKey);

                string prompt = $"Actúa como un experto en diagnóstico organizacional. La empresa '{solicitud.Empresa.NombreEmpresa}' tiene la siguiente problemática: '{solicitud.Problematica}'. " +
                                $"Genera un mínimo de 8 preguntas profesionales para evaluar esta situación en su personal. " +
                                $"REGLAS ESTRICTAS:\n" +
                                $"1. Debes usar una combinación de 3 tipos de preguntas: Abiertas, Opción Múltiple y Escala de Likert. NO uses un solo tipo, altérnalas dependiendo del contexto de la pregunta.\n" +
                                $"2. Devuelve cada pregunta en UNA SOLA LÍNEA. Si la pregunta incluye opciones de respuesta (Múltiple o Likert), incluye las opciones dentro de esa misma línea (ejemplo: ¿Qué tan satisfecho estás? [1. Nada, 2. Poco, 3. Mucho]).\n" +
                                $"3. No incluyas numeración al inicio de las preguntas.\n" +
                                $"4. No agregues introducciones, títulos ni conclusiones. Devuelve ÚNICAMENTE las líneas con las preguntas.";

                ChatCompletion completion = await client.CompleteChatAsync(prompt);
                string respuestaIa = completion.Content[0].Text;

                var lineasPreguntas = respuestaIa.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                var nuevaEncuesta = new Encuesta
                {
                    EmpresaID = solicitud.EmpresaID,
                    LinkID = Guid.NewGuid(),
                    EstaActiva = true,
                    FechaCreacion = DateTime.Now,
                    FechaCierre = DateTime.Now.AddDays(solicitud.DiasEstimados > 0 ? solicitud.DiasEstimados : 7),
                    LimiteEmpleados = solicitud.NumeroEmpleados > 0 ? solicitud.NumeroEmpleados : 30,
                    Titulo = $"Diagnóstico para {solicitud.Empresa.NombreEmpresa}",
                    UsuarioID = usuarioSesionId > 0 ? usuarioSesionId : null
                };

                _context.Encuestas.Add(nuevaEncuesta);
                await _context.SaveChangesAsync();

                foreach (var textoPregunta in lineasPreguntas)
                {
                    var preguntaDb = new PreguntaEncuesta
                    {
                        EncuestaID = nuevaEncuesta.EncuestaID,
                        TextoPregunta = textoPregunta.Trim()
                    };
                    _context.PreguntasEncuestas.Add(preguntaDb);
                }
                await _context.SaveChangesAsync();

                string mensajeAviso = "✅ ¡Excelentes noticias! Tu solicitud ha sido aprobada y ahora se encuentra en proceso de desarrollo. Nos comunicaremos tan pronto tengamos la encuesta lista.";
                EnviarMensajeWhatsApp(solicitud.Empresa.TelefonoContacto, mensajeAviso);

                return Ok(new
                {
                    Mensaje = $"Solicitud {id} APROBADA y Encuesta generada exitosamente.",
                    ConsultorAsignadoID = nuevaEncuesta.UsuarioID,
                    PreguntasGeneradas = lineasPreguntas
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Mensaje = "La solicitud fue aprobada, pero ocurrió un error al generar las preguntas con OpenAI.", Error = ex.Message });
            }
        }

        // MÉTODO AUXILIAR: ENVÍO DE MENSAJES WHATSAPP VÍA TWILIO
        private void EnviarMensajeWhatsApp(string numeroDestino, string mensajeTexto)
        {
            string accountSid = _configuration["Twilio:AccountSid"] ?? "";
            string authToken = _configuration["Twilio:AuthToken"] ?? "";
            string twilioNumber = _configuration["Twilio:WhatsAppNumber"] ?? "";

            if (string.IsNullOrEmpty(accountSid) || string.IsNullOrEmpty(authToken) || string.IsNullOrEmpty(twilioNumber))
                return;

            string tel = numeroDestino.Replace("whatsapp:", "").Trim();
            if (!tel.StartsWith("+")) tel = "+" + tel;

            string twilioFormateado = twilioNumber.Replace("whatsapp:", "").Trim();
            if (!twilioFormateado.StartsWith("+")) twilioFormateado = "+" + twilioFormateado;

            TwilioClient.Init(accountSid, authToken);

            MessageResource.Create(
                body: mensajeTexto,
                from: new Twilio.Types.PhoneNumber("whatsapp:" + twilioFormateado),
                to: new Twilio.Types.PhoneNumber("whatsapp:" + tel)
            );
        }
    }
}