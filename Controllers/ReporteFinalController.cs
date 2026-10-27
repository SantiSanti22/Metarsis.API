using Metarsis.Api.Data;
using Metarsis.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenAI.Chat;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Metarsis.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Tags("Reportes y Diagnósticos")]
    public class ReporteFinalController : ControllerBase
    {
        private readonly MetarsisDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;

        public ReporteFinalController(MetarsisDbContext context, IConfiguration configuration, IWebHostEnvironment env)
        {
            _context = context;
            _configuration = configuration;
            _env = env;
        }

        // 1. RECEPCIÓN Y ALMACENAMIENTO DEL REPORTE PDF EN EL SERVIDOR
        [HttpPost("subir-pdf-servidor/{encuestaId}")]
        public async Task<IActionResult> SubirPdfServidor(int encuestaId, IFormFile archivoPdf)
        {
            try
            {
                if (archivoPdf == null || archivoPdf.Length == 0)
                    return BadRequest("No se recibió ningún archivo PDF.");

                var encuesta = await _context.Encuestas
                    .Include(e => e.Empresa)
                    .FirstOrDefaultAsync(e => e.EncuestaID == encuestaId);

                if (encuesta == null) return NotFound("La encuesta no existe.");

                string textoNormalizado = (encuesta.Empresa?.NombreEmpresa ?? "Empresa").Normalize(NormalizationForm.FormD);
                StringBuilder sbSinAcentos = new StringBuilder();
                foreach (char c in textoNormalizado)
                {
                    if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                        sbSinAcentos.Append(c);
                }
                string nombreEmpresaLimpio = Regex.Replace(sbSinAcentos.ToString(), @"[^a-zA-Z0-9]+", "_").Trim('_');
                string fileName = $"Reporte_{nombreEmpresaLimpio}_{DateTime.Now:yyyyMMddHHmmss}.pdf";

                string savePath = Path.Combine(_env.WebRootPath, "reportes");
                if (!Directory.Exists(savePath)) Directory.CreateDirectory(savePath);

                string rutaCompleta = Path.Combine(savePath, fileName);
                using (var stream = new FileStream(rutaCompleta, FileMode.Create))
                {
                    await archivoPdf.CopyToAsync(stream);
                }

                var reporteBD = await _context.ReportesFinales.FirstOrDefaultAsync(r => r.EncuestaID == encuestaId);
                if (reporteBD == null)
                {
                    reporteBD = new ReporteFinal { EncuestaID = encuestaId, RutaArchivoPdf = fileName, FechaGeneracion = DateTime.Now };
                    _context.ReportesFinales.Add(reporteBD);
                }
                else
                {
                    reporteBD.RutaArchivoPdf = fileName;
                    reporteBD.FechaGeneracion = DateTime.Now;
                }
                await _context.SaveChangesAsync();

                return Ok(new { success = true, archivo = fileName });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al guardar el PDF: {ex.Message}");
            }
        }

        // 2. DESCARGA DEL REPORTE PDF
        [HttpGet("archivo-pdf/{nombreArchivo}")]
        [AllowAnonymous]
        [ApiExplorerSettings(IgnoreApi = true)]
        public IActionResult DescargarArchivoPdfPublico(string nombreArchivo)
        {
            string nombreDecodificado = Uri.UnescapeDataString(nombreArchivo);
            string rutaFisica = Path.Combine(_env.WebRootPath, "reportes", nombreDecodificado);

            if (!System.IO.File.Exists(rutaFisica))
                return NotFound("Archivo no encontrado.");

            byte[] fileBytes = System.IO.File.ReadAllBytes(rutaFisica);
            return File(fileBytes, "application/pdf", nombreDecodificado);
        }

        // 3. ENVÍO DEL ENLACE DE DESCARGA POR WHATSAPP MEDIANTE TWILIO
        [HttpPost("enviar-whatsapp/{encuestaId}")]
        public async Task<IActionResult> EnviarReportePorWhatsApp(int encuestaId)
        {
            try
            {
                var reporteBD = await _context.ReportesFinales
                    .Include(r => r.Encuesta)
                    .ThenInclude(e => e.Empresa)
                    .FirstOrDefaultAsync(r => r.EncuestaID == encuestaId);

                if (reporteBD == null || string.IsNullOrEmpty(reporteBD.RutaArchivoPdf))
                    return BadRequest("El reporte PDF aún no ha sido generado.");

                string numeroTelefono = await _context.Encuestas
                    .Where(e => e.EncuestaID == encuestaId)
                    .Include(e => e.Empresa)
                    .Select(e => e.Empresa.TelefonoContacto)
                    .FirstOrDefaultAsync();

                if (string.IsNullOrEmpty(numeroTelefono))
                    return BadRequest("No se encontró el teléfono de contacto de la empresa asociada a esta encuesta.");

                string nombreArchivo = reporteBD.RutaArchivoPdf;
                string rutaFisica = Path.Combine(_env.WebRootPath, "reportes", nombreArchivo);

                if (!System.IO.File.Exists(rutaFisica))
                    return NotFound("No se encontró el archivo físico del PDF en el servidor.");

                string forwardedHost = Request.Headers["X-Forwarded-Host"].FirstOrDefault();
                string baseUrl = !string.IsNullOrEmpty(forwardedHost)
                    ? $"https://{forwardedHost}"
                    : (_configuration["AppSettings:BaseUrl"]?.TrimEnd('/') ?? $"{Request.Scheme}://{Request.Host}");

                if (baseUrl.Contains("localhost") || baseUrl.Contains("p1cvjzr0"))
                {
                    baseUrl = "https://fcgmbx1v-7011.usw3.devtunnels.ms";
                }

                string nombreArchivoCodificado = Uri.EscapeDataString(nombreArchivo);
                string urlPublicaPdf = $"{baseUrl}/api/ReporteFinal/archivo-pdf/{nombreArchivoCodificado}";

                string accountSid = _configuration["Twilio:AccountSid"];
                string authToken = _configuration["Twilio:AuthToken"];
                string twilioWhatsAppNumber = _configuration["Twilio:WhatsAppNumber"];

                string telLimpio = numeroTelefono.Replace("whatsapp:", "").Trim();
                if (!telLimpio.StartsWith("+")) telLimpio = "+" + telLimpio;

                string formatoDestino = $"whatsapp:{telLimpio}";
                string formatoOrigen = twilioWhatsAppNumber.StartsWith("whatsapp:")
                    ? twilioWhatsAppNumber
                    : $"whatsapp:{twilioWhatsAppNumber}";

                using var client = new HttpClient();
                var byteArray = Encoding.ASCII.GetBytes($"{accountSid}:{authToken}");
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

                string nombreEmpresa = reporteBD.Encuesta.Empresa?.NombreEmpresa ?? "Avenzo";

                string cuerpoMensaje = $"¡Hola! \n✅El *Reporte Diagnotisco* con sus resultados esta listo.\n\n📋Puedes descargar el archivo PDF haciendo clic en el siguiente enlace:\n{urlPublicaPdf}";

                var keyValues = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("To", formatoDestino),
                    new KeyValuePair<string, string>("From", formatoOrigen),
                    new KeyValuePair<string, string>("Body", cuerpoMensaje)
                };

                var encodedContent = new FormUrlEncodedContent(keyValues);
                string urlTwilioApi = $"https://api.twilio.com/2010-04-01/Accounts/{accountSid}/Messages.json";

                var response = await client.PostAsync(urlTwilioApi, encodedContent);
                if (response.IsSuccessStatusCode)
                {
                    if (reporteBD.Encuesta != null)
                    {
                        reporteBD.Encuesta.EstaActiva = false;
                    }

                    var solicitud = await _context.Set<SolicitudEncuesta>()
                        .FirstOrDefaultAsync(s => s.EmpresaID == reporteBD.Encuesta.EmpresaID && s.Estatus != "Finalizada");

                    if (solicitud != null)
                    {
                        solicitud.Estatus = "Finalizada";
                    }

                    string numeroDestino = "+" + telLimpio.Replace("+", "");
                    await _context.Database.ExecuteSqlRawAsync(
                        "DELETE FROM MET.SesionBotWhatsApp WHERE NumeroTelefono = {0}",
                        numeroDestino
                    );

                    await _context.SaveChangesAsync();

                    return Ok(new
                    {
                        mensaje = $"Enlace del reporte enviado exitosamente por WhatsApp. La sesión del bot se reinició.",
                        urlEnviada = urlPublicaPdf
                    });
                }
                else
                {
                    string errorDetalle = await response.Content.ReadAsStringAsync();
                    return StatusCode(500, $"Error al enviar por Twilio: {errorDetalle}");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Excepción: {ex.Message}");
            }
        }
    }
}