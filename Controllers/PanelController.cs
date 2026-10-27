using Metarsis.Api.Data;
using Metarsis.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OpenAI.Chat;
using System.Security.Claims;
using System.Text.Json;

namespace Metarsis.Api.Controllers
{
    [Authorize] // Protege todas las acciones de este controlador (obliga a pasar por el Login)
    public class PanelController : Controller
    {
        private readonly MetarsisDbContext _context;
        private readonly IConfiguration _configuration;

        public PanelController(MetarsisDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // 1. DASHBOARD PRINCIPAL 
        public async Task<IActionResult> Index()
        {
            bool esAdmin = User.IsInRole("Admin"); 
            string claimId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0";
            int.TryParse(claimId, out int usuarioId);

            var encuestasQuery = _context.Set<Encuesta>().AsNoTracking().AsQueryable();
            var solicitudesQuery = _context.Set<SolicitudEncuesta>().AsNoTracking().AsQueryable();
            var respuestasQuery = _context.Set<RespuestaEncuesta>().AsNoTracking().AsQueryable();

            if (!esAdmin)
            {
                encuestasQuery = encuestasQuery.Where(e => e.UsuarioID == usuarioId);
                var misEmpresaIds = await encuestasQuery.Select(e => e.EmpresaID).Distinct().ToListAsync();

                ViewBag.TotalEmpresas = misEmpresaIds.Count;
                ViewBag.EncuestasActivas = await encuestasQuery.CountAsync(e => e.EstaActiva);
                ViewBag.EncuestasFinalizadas = await encuestasQuery.CountAsync(e => !e.EstaActiva);

                var misEncuestaIds = await encuestasQuery.Select(e => e.EncuestaID).ToListAsync();
                var misPreguntaIds = await _context.Set<PreguntaEncuesta>().AsNoTracking()
                    .Where(p => misEncuestaIds.Contains(p.EncuestaID))
                    .Select(p => p.PreguntaID)
                    .ToListAsync(); 

                ViewBag.TotalRespuestas = await respuestasQuery.CountAsync(r => misPreguntaIds.Contains(r.PreguntaID));
                ViewBag.SolicitudesPendientesCount = await solicitudesQuery
                    .CountAsync(s => s.Estatus != "Finalizada" && s.Estatus != "Aprobada" && s.Estatus != "Rechazada"); 
            }
            else
            {
                ViewBag.TotalEmpresas = await _context.Set<EmpresaCliente>().AsNoTracking().CountAsync();
                ViewBag.EncuestasActivas = await encuestasQuery.CountAsync(e => e.EstaActiva);
                ViewBag.EncuestasFinalizadas = await encuestasQuery.CountAsync(e => !e.EstaActiva);
                ViewBag.TotalRespuestas = await respuestasQuery.CountAsync();
                ViewBag.SolicitudesPendientesCount = await solicitudesQuery
                    .CountAsync(s => s.Estatus != "Finalizada" && s.Estatus != "Aprobada" && s.Estatus != "Rechazada");
            }

            var hace7Dias = DateTime.Now.Date.AddDays(-6);
            var respuestasQuerySemana = _context.Set<RespuestaEncuesta>()
                .AsNoTracking()
                .Where(r => r.FechaRespuesta >= hace7Dias);

            if (!esAdmin)
            {
                var misEncuestaIds = await _context.Set<Encuesta>()
                    .AsNoTracking()
                    .Where(e => e.UsuarioID == usuarioId)
                    .Select(e => e.EncuestaID)
                    .ToListAsync();

                var misPreguntaIds = await _context.Set<PreguntaEncuesta>()
                    .AsNoTracking()
                    .Where(p => misEncuestaIds.Contains(p.EncuestaID))
                    .Select(p => p.PreguntaID)
                    .ToListAsync();

                respuestasQuerySemana = respuestasQuerySemana.Where(r => misPreguntaIds.Contains(r.PreguntaID));
            }

            var respuestasUltimaSemana = await respuestasQuerySemana
                .GroupBy(r => r.FechaRespuesta.Date)
                .Select(g => new { Fecha = g.Key, Cantidad = g.Count() })
                .ToListAsync();

            var diasSemana = new List<string>();
            var conteosSemana = new List<int>();

            for (int i = 0; i < 7; i++)
            {
                var dia = hace7Dias.AddDays(i);
                diasSemana.Add(dia.ToString("dd MMM"));
                var registro = respuestasUltimaSemana.FirstOrDefault(r => r.Fecha == dia);
                conteosSemana.Add(registro?.Cantidad ?? 0);
            }

            ViewBag.GraficaDiasLabels = diasSemana;
            ViewBag.GraficaDiasData = conteosSemana;

            var encuestasRecientes = await encuestasQuery
                .Include(e => e.Empresa)
                .OrderByDescending(e => e.FechaCreacion)
                .Take(5)
                .ToListAsync();

            return View(encuestasRecientes);
        }

        // 2. BANDEJA DE SOLICITUDES PENDIENTES Y ENCUESTAS
        public async Task<IActionResult> Solicitudes()
        {
            var solicitudesPendientes = await _context.Set<SolicitudEncuesta>()
                .AsNoTracking()
                .Include(s => s.Empresa)
                .Where(s => s.Estatus != "Finalizada" && s.Estatus != "Aprobada" && s.Estatus != "Rechazada")
                .OrderByDescending(s => s.SolicitudID)
                .ToListAsync(); 

            return View(solicitudesPendientes);
        }

        public async Task<IActionResult> Encuestas()
        {
            bool esAdmin = User.IsInRole("Admin");
            string claimId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0";
            int.TryParse(claimId, out int usuarioId);

            var solicitudesQuery = _context.Set<SolicitudEncuesta>()
                .AsNoTracking()
                .Include(s => s.Empresa)
                .Where(s => s.Estatus == "Aprobada" || s.Estatus == "Finalizada")
                .AsQueryable(); 

            if (!esAdmin)
            {
                var misEmpresasIds = await _context.Set<Encuesta>()
                    .AsNoTracking()
                    .Where(e => e.UsuarioID == usuarioId)
                    .Select(e => e.EmpresaID)
                    .Distinct()
                    .ToListAsync(); 

                solicitudesQuery = solicitudesQuery.Where(s => misEmpresasIds.Contains(s.EmpresaID));
            }

            var encuestasEnCurso = await solicitudesQuery
                .OrderByDescending(s => s.SolicitudID)
                .ToListAsync();

            return View(encuestasEnCurso);
        }

        [HttpGet]
        public async Task<IActionResult> RevisarEncuesta(int empresaId)
        {
            var encuesta = await _context.Set<Encuesta>()
                .AsNoTracking()
                .Include(e => e.Empresa)
                .FirstOrDefaultAsync(e => e.EmpresaID == empresaId); // Busca la encuesta vinculada a la empresa cliente

            if (encuesta == null)
                return NotFound("No se encontró la encuesta para esta empresa.");

            ViewBag.Preguntas = await _context.Set<PreguntaEncuesta>()
                .AsNoTracking()
                .Where(p => p.EncuestaID == encuesta.EncuestaID)
                .ToListAsync();

            var respuestas = await (from r in _context.Set<RespuestaEncuesta>().AsNoTracking()
                                    join p in _context.Set<PreguntaEncuesta>().AsNoTracking() on r.PreguntaID equals p.PreguntaID
                                    where p.EncuestaID == encuesta.EncuestaID
                                    select new
                                    {
                                        r.NumeroTelefono,
                                        p.TextoPregunta,
                                        r.TextoRespuesta,
                                        r.FechaRespuesta
                                    }).ToListAsync(); // Cruce de respuestas con sus respectivas preguntas y teléfonos

            ViewBag.RespuestasEmpleados = respuestas.GroupBy(x => x.NumeroTelefono).ToList();

            return View(encuesta);
        }

        public async Task<IActionResult> MonitorearEncuesta(int empresaId)
        {
            var encuesta = await _context.Set<Encuesta>()
                .AsNoTracking()
                .Include(e => e.Empresa)
                .FirstOrDefaultAsync(e => e.EmpresaID == empresaId);

            if (encuesta == null) return Content("Error: No se encontró la encuesta para esta empresa.");

            var totalPreguntas = await _context.Set<PreguntaEncuesta>().AsNoTracking()
                .Where(p => p.EncuestaID == encuesta.EncuestaID).CountAsync();

            var respuestasBase = await (from r in _context.Set<RespuestaEncuesta>().AsNoTracking()
                                        join p in _context.Set<PreguntaEncuesta>().AsNoTracking() on r.PreguntaID equals p.PreguntaID
                                        where p.EncuestaID == encuesta.EncuestaID
                                        select new { r.NumeroTelefono, r.FechaRespuesta, r.PreguntaID, p.TextoPregunta, r.TextoRespuesta })
                                        .ToListAsync();

            var progresoEmpleados = respuestasBase
                .GroupBy(r => r.NumeroTelefono)
                .Select(g => new ProgresoEmpleadoViewModel
                {
                    Telefono = g.Key,
                    UltimaInteraccion = g.Max(r => r.FechaRespuesta),
                    PreguntasRespondidas = g.Select(r => r.PreguntaID).Distinct().Count(),
                    TotalPreguntas = totalPreguntas,
                    Respuestas = g.Select(r => new RespuestaDetalleViewModel
                    {
                        Pregunta = r.TextoPregunta,
                        Respuesta = r.TextoRespuesta
                    }).ToList()
                })
                .OrderByDescending(p => p.UltimaInteraccion)
                .ToList();

            int totalRespondidas = progresoEmpleados.Count;
            int respuestasIndividualesRecibidas = progresoEmpleados.Sum(p => p.PreguntasRespondidas);
            int metaRespuestasTotales = encuesta.LimiteEmpleados * totalPreguntas;

            string twilioNumber = _configuration["Twilio:WhatsAppNumber"] ?? "";
            string numeroLimpio = twilioNumber.Replace("+", "");

            ViewBag.TotalPreguntas = totalPreguntas;
            ViewBag.TotalRespondidas = totalRespondidas;
            ViewBag.RespuestasIndividuales = respuestasIndividualesRecibidas;
            ViewBag.MetaRespuestas = metaRespuestasTotales;
            ViewBag.LinkWhatsApp = $"https://wa.me/{numeroLimpio}?text=Comenzar_Prueba_{encuesta.LinkID}";
            ViewBag.ProgresoEmpleados = progresoEmpleados;

            return View(encuesta);
        }

        [HttpPost]
        public async Task<IActionResult> FinalizarEncuestaManual(int encuestaId)
        {
            var encuesta = await _context.Set<Encuesta>().FirstOrDefaultAsync(e => e.EncuestaID == encuestaId);

            if (encuesta != null)
            {
                encuesta.EstaActiva = false;
                encuesta.FechaCierre = DateTime.Now;

                var (diagnosticoIA, recomendacionesIA) = await GenerarAnalisisConIA(encuestaId);
                encuesta.DiagnosticoEjecutivo = diagnosticoIA;
                encuesta.RecomendacionesClave = recomendacionesIA;

                var solicitud = await _context.Set<SolicitudEncuesta>()
                    .FirstOrDefaultAsync(s => s.EmpresaID == encuesta.EmpresaID);

                if (solicitud != null)
                {
                    solicitud.Estatus = "Finalizada";
                }

                await _context.SaveChangesAsync();
            }

            return RedirectToAction("MonitorearEncuesta", new { empresaId = encuesta?.EmpresaID });
        }

        public async Task<IActionResult> PrevisualizarReporte(int empresaId)
        {
            var encuesta = await _context.Set<Encuesta>()
                .Include(e => e.Empresa)
                .FirstOrDefaultAsync(e => e.EmpresaID == empresaId);

            if (encuesta == null) return NotFound("Encuesta no encontrada");

            if (string.IsNullOrWhiteSpace(encuesta.DiagnosticoEjecutivo) || encuesta.DiagnosticoEjecutivo.Contains("Esto es una prueba"))
            {
                var (diag, rec) = await GenerarAnalisisConIA(encuesta.EncuestaID);
                encuesta.DiagnosticoEjecutivo = diag;
                encuesta.RecomendacionesClave = rec;
                await _context.SaveChangesAsync();
            }

            var totalPreguntas = await _context.Set<PreguntaEncuesta>().AsNoTracking()
                .Where(p => p.EncuestaID == encuesta.EncuestaID).CountAsync();

            var respuestasBase = await (from r in _context.Set<RespuestaEncuesta>().AsNoTracking()
                                        join p in _context.Set<PreguntaEncuesta>().AsNoTracking() on r.PreguntaID equals p.PreguntaID
                                        where p.EncuestaID == encuesta.EncuestaID
                                        select new { r.NumeroTelefono, r.PreguntaID, p.TextoPregunta, r.TextoRespuesta })
                                        .ToListAsync();

            ViewBag.TotalRespondidas = respuestasBase.Select(r => r.NumeroTelefono).Distinct().Count();
            ViewBag.RespuestasIndividuales = respuestasBase.Count;

            var preguntasCerradas = respuestasBase
                .Where(r => r.TextoPregunta.Contains("[") && r.TextoPregunta.Contains("]"))
                .GroupBy(r => new { r.PreguntaID, r.TextoPregunta })
                .Select(gPregunta => {
                    var totalRespuestasPregunta = gPregunta.Count();
                    var opciones = gPregunta
                        .GroupBy(r => r.TextoRespuesta)
                        .Select(gOpcion => new {
                            Nombre = gOpcion.Key,
                            Cantidad = gOpcion.Count(),
                            Porcentaje = totalRespuestasPregunta > 0 ? (int)Math.Round((double)(gOpcion.Count() * 100) / totalRespuestasPregunta) : 0
                        })
                        .OrderByDescending(o => o.Cantidad)
                        .ToList();

                    string textoLimpio = gPregunta.Key.TextoPregunta;
                    int startIndex = textoLimpio.IndexOf('[');
                    if (startIndex > 0)
                    {
                        textoLimpio = textoLimpio.Substring(0, startIndex).Trim();
                    }

                    return new
                    {
                        PreguntaTexto = textoLimpio,
                        Promedio = "1.5",
                        Opciones = opciones
                    };
                }).ToList<dynamic>();

            var preguntasAbiertas = respuestasBase
                .Where(r => !r.TextoPregunta.Contains("[") || !r.TextoPregunta.Contains("]"))
                .GroupBy(r => new { r.PreguntaID, r.TextoPregunta })
                .Select(gPregunta => new {
                    PreguntaTexto = gPregunta.Key.TextoPregunta,
                    Respuestas = gPregunta.Select(r => r.TextoRespuesta).Take(5).ToList(),
                    ResumenGeneral = "Se detecta un consenso generalizado sobre la necesidad de actualizar las herramientas de trabajo actuales y optimizar los flujos de comunicación interna."
                }).ToList<dynamic>();

            ViewBag.MetricasPreguntas = preguntasCerradas;
            ViewBag.PreguntasAbiertas = preguntasAbiertas;

            return View("ReporteDiagnostico", encuesta);
        }

        private async Task<(string Diagnostico, string Recomendaciones)> GenerarAnalisisConIA(int encuestaId)
        {
            try
            {
                string apiKey = _configuration["OpenAI:ApiKey"] ?? _configuration["OpenAIKey"] ?? "";
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    return ("No se encontró configurada la clave de OpenAI en appsettings.json.", "<ul><li>Verificar la clave de API en la configuración del servidor.</li></ul>");
                }

                var respuestas = await (from r in _context.Set<RespuestaEncuesta>().AsNoTracking()
                                        join p in _context.Set<PreguntaEncuesta>().AsNoTracking() on r.PreguntaID equals p.PreguntaID
                                        where p.EncuestaID == encuestaId
                                        select new { p.TextoPregunta, r.TextoRespuesta })
                                        .ToListAsync();

                if (!respuestas.Any())
                {
                    return ("Aún no hay respuestas registradas para generar un diagnóstico.", "<ul><li>Esperar a la participación de los colaboradores.</li></ul>");
                }

                var resumenDatos = respuestas
                    .GroupBy(x => x.TextoPregunta)
                    .Select(g => new
                    {
                        Pregunta = g.Key,
                        Respuestas = g.GroupBy(r => r.TextoRespuesta).Select(r => $"{r.Key} ({r.Count()} menciones)").Take(10).ToList()
                    });

                string datosJson = JsonSerializer.Serialize(resumenDatos);

                string modelo = _configuration["OpenAI:Model"] ?? "gpt-4o-mini";
                ChatClient client = new ChatClient(modelo, apiKey);

                string systemPrompt = @"Eres un consultor de Recursos Humanos y Diagnóstico de Clima Organizacional en Avenzo.
                                        Analiza los resultados de la encuesta proporcionados y redacta un diagnóstico profesional.
                                        Debes responder ÚNICAMENTE un objeto JSON plano con la siguiente estructura exacta:
                                        {
                                          ""diagnostico"": ""Párrafo ejecutivo conciso (3 a 5 oraciones) explicando el estado del clima laboral, áreas críticas detectadas y la causa raíz."",
                                          ""recomendaciones"": ""<ul><li>Primera acción recomendada concreta.</li><li>Segunda recomendación de mejora.</li><li>Tercera recomendación accionable.</li></ul>""
                                        }";

                string userPrompt = $"Analiza el siguiente consolidado de respuestas y genera el diagnóstico ejecutivo y recomendaciones:\n{datosJson}";

                ChatCompletion completion = await client.CompleteChatAsync(new List<ChatMessage>
                {
                    new SystemChatMessage(systemPrompt),
                    new UserChatMessage(userPrompt)
                });

                string respuestaTexto = completion.Content[0].Text.Trim();

                if (respuestaTexto.StartsWith("```json"))
                    respuestaTexto = respuestaTexto.Substring(7);
                if (respuestaTexto.StartsWith("```"))
                    respuestaTexto = respuestaTexto.Substring(3);
                if (respuestaTexto.EndsWith("```"))
                    respuestaTexto = respuestaTexto.Substring(0, respuestaTexto.Length - 3);

                using var doc = JsonDocument.Parse(respuestaTexto.Trim());
                var root = doc.RootElement;

                string diagnostico = root.GetProperty("diagnostico").GetString() ?? "";
                string recomendaciones = root.GetProperty("recomendaciones").GetString() ?? "";

                return (diagnostico, recomendaciones);
            }
            catch (Exception ex)
            {
                return ($"Error al generar análisis con IA: {ex.Message}", "<ul><li>No se pudieron generar las recomendaciones automáticamente.</li></ul>");
            }
        }

        [HttpPost]
        public async Task<IActionResult> GuardarTextosReporte([FromBody] GuardarTextosRequest request)
        {
            var encuesta = await _context.Set<Encuesta>().FindAsync(request.EncuestaId);
            if (encuesta != null)
            {
                encuesta.DiagnosticoEjecutivo = request.Diagnostico;
                encuesta.RecomendacionesClave = request.Recomendaciones;

                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false, message = "Encuesta no encontrada" });
        }

        [HttpGet]
        public async Task<IActionResult> EmpresasClientes()
        {
            var empresas = await _context.Set<EmpresaCliente>()
                .AsNoTracking()
                .OrderByDescending(e => e.EmpresaID)
                .ToListAsync();

            return View(empresas);
        }

        // 3. GESTIÓN DE PRUEBA TAT (ASIGNACIONES Y LÁMINAS)
        [HttpGet]
        public async Task<IActionResult> GestionTAT()
        {
            bool esAdmin = User.IsInRole("Admin");
            string claimId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0";
            int.TryParse(claimId, out int usuarioId);

            var laminas = await _context.Set<LaminaTAT>().AsNoTracking().OrderBy(l => l.LaminaID).ToListAsync();
            int totalActivas = laminas.Count(l => l.Activa);

            var asignacionesQuery = _context.Set<AsignacionTAT>().AsNoTracking().AsQueryable();

            
            if (!esAdmin)
            {
                asignacionesQuery = asignacionesQuery.Where(a => a.UsuarioID == usuarioId);
            }

            var asignaciones = await asignacionesQuery
                .OrderByDescending(a => a.FechaAsignacion)
                .ToListAsync();

            var respuestas = await _context.Set<RespuestaTAT>().AsNoTracking().Include(r => r.Lamina).ToListAsync();

            string twilioNumber = (_configuration["Twilio:WhatsAppNumber"] ?? "").Replace("+", "");

            var progresoEmpleados = asignaciones.Select(a => {
                var respuestasEmpleado = respuestas.Where(r => r.AsignacionID == a.AsignacionID).ToList();
                int porcentaje = totalActivas > 0 ? (int)Math.Round((double)respuestasEmpleado.Count / totalActivas * 100) : 0;

                return new
                {
                    AsignacionID = a.AsignacionID,
                    Telefono = a.NumeroTelefono,
                    Fecha = a.FechaAsignacion,
                    Estatus = a.Completada ? "Finalizado" : "En Progreso",
                    Respuestas = respuestasEmpleado,
                    Porcentaje = porcentaje > 100 ? 100 : porcentaje,
                    Evaluador = a.NombreAdministrador ?? "Sin asignar",
                    TelefonoAdmin = a.TelefonoAdministrador ?? "",
                    GuidLink = a.LinkID,
                    EnlaceWhatsApp = $"[https://wa.me/](https://wa.me/){twilioNumber}?text=Comenzar_TAT_{a.LinkID}"
                };
            }).ToList();

            ViewBag.Laminas = laminas;
            ViewBag.ProgresoEmpleados = progresoEmpleados;
            ViewBag.TotalActivas = totalActivas;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AsignarTAT([FromBody] AsignarTATRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.NumeroTelefono))
            {
                return Json(new { exito = false, mensaje = "El número de teléfono del evaluado es obligatorio." });
            }

            string claimId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0";
            int.TryParse(claimId, out int adminId);
            string nombreAdmin = User.Identity?.Name ?? "Evaluador";
            string telefonoAdmin = User.FindFirst("TelefonoWhatsApp")?.Value ?? "";

            string telLimpio = request.NumeroTelefono.Trim().Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
            if (!telLimpio.StartsWith("+") && telLimpio.Length == 10)
            {
                telLimpio = "+52" + telLimpio;
            }

            var nuevaAsignacion = new AsignacionTAT
            {
                EncuestaID = request.EncuestaID,
                NumeroTelefono = telLimpio,
                LinkID = Guid.NewGuid(),
                Completada = false,
                FechaAsignacion = DateTime.Now,
                UsuarioID = adminId > 0 ? adminId : null,
                NombreAdministrador = nombreAdmin,
                TelefonoAdministrador = telefonoAdmin
            };

            _context.Set<AsignacionTAT>().Add(nuevaAsignacion);
            await _context.SaveChangesAsync();

            string twilioNumber = (_configuration["Twilio:WhatsAppNumber"] ?? "").Replace("+", "");
            string enlaceWhatsApp = $"[https://wa.me/](https://wa.me/){twilioNumber}?text=Comenzar_TAT_{nuevaAsignacion.LinkID}";

            return Json(new
            {
                exito = true,
                mensaje = "Asignación creada correctamente.",
                asignacionId = nuevaAsignacion.AsignacionID,
                guid = nuevaAsignacion.LinkID,
                enlace = enlaceWhatsApp
            });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditarEmpresaCliente([FromBody] Metarsis.Api.Models.EmpresaCliente datos)
        {
            var empresa = await _context.EmpresasClientes.FindAsync(datos.EmpresaID);
            if (empresa == null)
                return NotFound(new { exito = false, mensaje = "La empresa no existe." });

            empresa.NombreEmpresa = datos.NombreEmpresa?.Trim() ?? "";
            empresa.NombreContacto = datos.NombreContacto?.Trim();
            empresa.TelefonoContacto = datos.TelefonoContacto?.Trim();

            await _context.SaveChangesAsync();
            return Ok(new { exito = true, mensaje = "Empresa actualizada exitosamente." });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EliminarEmpresaCliente(int id)
        {
            var empresa = await _context.EmpresasClientes.FindAsync(id);
            if (empresa == null)
                return NotFound(new { exito = false, mensaje = "La empresa no existe." });

            _context.EmpresasClientes.Remove(empresa);
            await _context.SaveChangesAsync();
            return Ok(new { exito = true, mensaje = "Empresa eliminada correctamente." });
        }

        // 4. GESTIÓN DE EQUIPO Y USUARIOS (SOLO ADMINISTRADOR)
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Usuarios()
        {
            var usuarios = await _context.Usuarios
                .AsNoTracking()
                .OrderByDescending(u => u.FechaRegistro)
                .ToListAsync();

            return View(usuarios);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CrearUsuario([FromBody] Metarsis.Api.Models.Usuario nuevoUsuario)
        {
            if (string.IsNullOrWhiteSpace(nuevoUsuario.NombreCompleto) ||
                string.IsNullOrWhiteSpace(nuevoUsuario.Correo) ||
                string.IsNullOrWhiteSpace(nuevoUsuario.PasswordHash))
            {
                return Json(new { exito = false, mensaje = "Nombre, correo y contraseña son obligatorios." });
            }

            bool existe = await _context.Usuarios.AnyAsync(u => u.Correo == nuevoUsuario.Correo.Trim());
            if (existe)
            {
                return Json(new { exito = false, mensaje = "El correo ya está registrado en el sistema." });
            }

            string tel = (nuevoUsuario.TelefonoWhatsApp ?? "").Trim().Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
            if (!string.IsNullOrWhiteSpace(tel) && !tel.StartsWith("+") && tel.Length == 10)
            {
                tel = "+52" + tel;
            }

            nuevoUsuario.NombreCompleto = nuevoUsuario.NombreCompleto.Trim();
            nuevoUsuario.Correo = nuevoUsuario.Correo.Trim();
            nuevoUsuario.TelefonoWhatsApp = tel;
            nuevoUsuario.Rol = string.IsNullOrWhiteSpace(nuevoUsuario.Rol) ? "Consultor" : nuevoUsuario.Rol.Trim();
            nuevoUsuario.Activo = true;
            nuevoUsuario.FechaRegistro = DateTime.Now;

            _context.Usuarios.Add(nuevoUsuario);
            await _context.SaveChangesAsync();

            return Json(new { exito = true, mensaje = "Usuario creado exitosamente." });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ToggleEstadoUsuario(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null)
            {
                return Json(new { exito = false, mensaje = "Usuario no encontrado." });
            }

            usuario.Activo = !usuario.Activo;
            await _context.SaveChangesAsync();

            return Json(new { exito = true, activo = usuario.Activo });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditarUsuario([FromBody] Metarsis.Api.Models.Usuario usuarioModificado)
        {
            var usuario = await _context.Usuarios.FindAsync(usuarioModificado.UsuarioID);
            if (usuario == null)
            {
                return Json(new { exito = false, mensaje = "El usuario no existe." });
            }

            if (string.IsNullOrWhiteSpace(usuarioModificado.NombreCompleto) || string.IsNullOrWhiteSpace(usuarioModificado.Correo))
            {
                return Json(new { exito = false, mensaje = "Nombre y correo son campos obligatorios." });
            }

            string tel = (usuarioModificado.TelefonoWhatsApp ?? "").Trim().Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");

            usuario.NombreCompleto = usuarioModificado.NombreCompleto.Trim();
            usuario.Correo = usuarioModificado.Correo.Trim();
            usuario.TelefonoWhatsApp = tel;
            usuario.Rol = string.IsNullOrWhiteSpace(usuarioModificado.Rol) ? "Consultor" : usuarioModificado.Rol.Trim();

            if (!string.IsNullOrWhiteSpace(usuarioModificado.PasswordHash))
            {
                usuario.PasswordHash = usuarioModificado.PasswordHash.Trim();
            }

            await _context.SaveChangesAsync();
            return Json(new { exito = true, mensaje = "Usuario actualizado exitosamente." });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EliminarUsuario(int id)
        {
            string miIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0";
            if (int.TryParse(miIdClaim, out int miId) && miId == id)
            {
                return Json(new { exito = false, mensaje = "No puedes eliminar tu propia cuenta de administrador en sesión." });
            }

            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null)
            {
                return Json(new { exito = false, mensaje = "El usuario no existe." });
            }

            _context.Usuarios.Remove(usuario);
            await _context.SaveChangesAsync();

            return Json(new { exito = true, mensaje = "Usuario eliminado permanentemente." });
        }

        // DTOs Y CLASES AUXILIARES PARA PETICIONES JSON
        public class GuardarTextosRequest
        {
            public int EncuestaId { get; set; }
            public string Diagnostico { get; set; } = string.Empty;
            public string Recomendaciones { get; set; } = string.Empty;
        }

        public class AsignarTATRequest
        {
            public int EncuestaID { get; set; }
            public string NumeroTelefono { get; set; } = string.Empty;
        }
    }
}