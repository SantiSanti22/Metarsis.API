using Metarsis.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using System.Web;

namespace Metarsis.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Tags("Gestión de Encuestas (Admin)")]
    public class EncuestasController : ControllerBase
    {
        private readonly MetarsisDbContext _context;
        private readonly IConfiguration _configuration;

        public EncuestasController(MetarsisDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // PUBLICAR O REENVIAR ENCUESTA Y NOTIFICAR AL CLIENTE
        [HttpPost("{encuestaId}/publicar")]
        public async Task<IActionResult> PublicarEncuesta(int encuestaId)
        {
            var encuesta = await _context.Encuestas
                .Include(e => e.Empresa)
                .FirstOrDefaultAsync(e => e.EncuestaID == encuestaId); // Consulta la encuesta con los datos de su empresa vinculada

            if (encuesta == null)
                return NotFound("Encuesta no encontrada."); // Valida la existencia de la encuesta solicitada

            var solicitud = await _context.SolicitudesEncuestas
                .Where(s => s.EmpresaID == encuesta.EmpresaID)
                .OrderByDescending(s => s.FechaSolicitud)
                .FirstOrDefaultAsync(); // Obtiene la solicitud más reciente vinculada a la empresa cliente

            // 1. REGLAS DE NEGOCIO, VIGENCIA Y ENLACE ÚNICO
            if (solicitud != null)
            {
                encuesta.LimiteEmpleados = solicitud.NumeroEmpleados > 0 ? solicitud.NumeroEmpleados : encuesta.LimiteEmpleados;
                int dias = solicitud.DiasEstimados > 0 ? solicitud.DiasEstimados : 7;
                DateTime fechaCalculada = DateTime.Now.AddDays(dias);
                encuesta.FechaCierre = new DateTime(fechaCalculada.Year, fechaCalculada.Month, fechaCalculada.Day, 23, 59, 59);
            }
            else if (encuesta.FechaCierre == DateTime.MinValue || encuesta.FechaCierre.Year <= 1)
            {
                DateTime fechaCalculada = DateTime.Now.AddDays(7);
                encuesta.FechaCierre = new DateTime(fechaCalculada.Year, fechaCalculada.Month, fechaCalculada.Day, 23, 59, 59);
            }

            if (encuesta.LinkID == Guid.Empty)
            {
                encuesta.LinkID = Guid.NewGuid(); // Genera un identificador único si la encuesta aún no tenía enlace asignado
            }

            encuesta.EstaActiva = true; // Habilita la encuesta para comenzar a recibir respuestas de colaboradores
            await _context.SaveChangesAsync();

            // 2. CONSTRUCCIÓN DEL ENLACE DE ACCESO Y CÓDIGO QR
            string twilioNumber = _configuration["Twilio:WhatsAppNumber"] ?? "";
            string numeroLimpio = twilioNumber.Replace("+", "").Replace("whatsapp:", "").Trim();

            string linkWhatsApp = $"https://wa.me/{numeroLimpio}?text=Comenzar_Prueba_{encuesta.LinkID}"; // Enlace directo para iniciar el bot de WhatsApp
            string linkCodificado = HttpUtility.UrlEncode(linkWhatsApp);
            string urlCodigoQr = $"https://api.qrserver.com/v1/create-qr-code/?size=300x300&data={linkCodificado}"; // URL del servicio que genera la imagen QR

            // 3. NOTIFICACIÓN VÍA TWILIO AL CONTACTO DE LA EMPRESA
            if (!string.IsNullOrEmpty(encuesta.Empresa?.TelefonoContacto))
            {
                try
                {
                    string mensajeAviso = $"✅ ¡Tu encuesta está lista para contestarse!\n\n" +
                                          $"🏢 *Empresa:* {encuesta.Empresa.NombreEmpresa}\n" +
                                          $"👥 *Límite de accesos:* {encuesta.LimiteEmpleados} empleados\n" +
                                          $"📅 *Fecha de cierre:* {encuesta.FechaCierre:dd/MM/yyyy hh:mm tt}\n\n" +
                                          $"Comparte el siguiente enlace o imprime el Código QR para que tu personal participe en la encuesta:\n\n{linkWhatsApp}";

                    // Mensaje 1: Texto con Link
                    EnviarMensajeWhatsApp(encuesta.Empresa.TelefonoContacto, mensajeAviso);
                    // Mensaje 2: Imagen QR
                    EnviarMensajeWhatsApp(encuesta.Empresa.TelefonoContacto, null, urlCodigoQr);
                }
                catch (Exception ex)
                {
                    // Si el proveedor de mensajería falla, se confirma la activación y se entrega el enlace al administrador
                    return Ok(new
                    {
                        Mensaje = "La encuesta fue activada, pero Twilio no pudo entregar el WhatsApp: " + ex.Message,
                        LinkGenerado = linkWhatsApp
                    });
                }
            }

            return Ok(new
            {
                Mensaje = "Encuesta publicada y cliente notificado con éxito.",
                Configuracion = new
                {
                    Empresa = encuesta.Empresa?.NombreEmpresa,
                    FechaCierre = encuesta.FechaCierre,
                    LinkGenerado = linkWhatsApp,
                    QrGenerado = urlCodigoQr
                }
            });
        }

        // MÉTODO AUXILIAR: ENVÍO DE MENSAJES WHATSAPP VÍA TWILIO
        private void EnviarMensajeWhatsApp(string numeroDestino, string? mensajeTexto = null, string? mediaUrl = null)
        {
            string accountSid = _configuration["Twilio:AccountSid"] ?? "";
            string authToken = _configuration["Twilio:AuthToken"] ?? "";
            string twilioNumber = _configuration["Twilio:WhatsAppNumber"] ?? "";

            if (string.IsNullOrEmpty(accountSid) || string.IsNullOrEmpty(authToken) || string.IsNullOrEmpty(twilioNumber))
                return;

            string telLimpio = numeroDestino.Replace("whatsapp:", "").Trim();
            if (!telLimpio.StartsWith("+")) telLimpio = "+" + telLimpio;

            string twilioFormateado = twilioNumber.Replace("whatsapp:", "").Trim();
            if (!twilioFormateado.StartsWith("+")) twilioFormateado = "+" + twilioFormateado;

            TwilioClient.Init(accountSid, authToken);

            var opcionesMensaje = new CreateMessageOptions(new Twilio.Types.PhoneNumber("whatsapp:" + telLimpio))
            {
                From = new Twilio.Types.PhoneNumber("whatsapp:" + twilioFormateado)
            };

            if (!string.IsNullOrEmpty(mensajeTexto))
            {
                opcionesMensaje.Body = mensajeTexto;
            }

            if (!string.IsNullOrEmpty(mediaUrl))
            {
                opcionesMensaje.MediaUrl = new List<Uri> { new Uri(mediaUrl) };
            }

            MessageResource.Create(opcionesMensaje);
        }
    }
}