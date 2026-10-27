using Metarsis.Api.Data;
using Metarsis.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using Twilio;
using Twilio.AspNet.Core;
using Twilio.Rest.Api.V2010.Account;
using Twilio.TwiML;

namespace Metarsis.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Tags("Canal WhatsApp")]
    public class WhatsAppCanalController : TwilioController
    {
        private readonly MetarsisDbContext _context;
        private readonly IConfiguration _configuration;

        public WhatsAppCanalController(MetarsisDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // 1. RECEPTOR PRINCIPAL DE WEBHOOKS ENTRANTES DE TWILIO WHATSAPP
        [HttpPost("webhook")]
        public async Task<TwiMLResult> RecibirMensaje([FromForm] string From, [FromForm] string Body)
        {
            var response = new MessagingResponse();
            var numeroUsuario = From.Replace("whatsapp:", "").Trim();
            var mensajeUsuario = (Body ?? "").Trim();
            string comandoLimpio = mensajeUsuario.ToLowerInvariant();

            // RUTA A: APERTURA DE ENCUESTA MEDIANTE LINK O CÓDIGO QR
            if (mensajeUsuario.StartsWith("Comenzar_Prueba_", StringComparison.OrdinalIgnoreCase))
            {
                string guidString = mensajeUsuario.Substring("Comenzar_Prueba_".Length).Trim();

                if (!Guid.TryParse(guidString, out Guid linkId))
                {
                    response.Message("❌ El enlace escaneado no es válido.");
                    return TwiML(response);
                }

                var encuesta = await _context.Encuestas
                    .Include(e => e.Empresa)
                    .FirstOrDefaultAsync(e => e.LinkID == linkId && e.EstaActiva);

                if (encuesta == null)
                {
                    response.Message("❌ Esta encuesta no existe o ha sido desactivada por el administrador.");
                    return TwiML(response);
                }

                if (DateTime.Now > encuesta.FechaCierre)
                {
                    response.Message("⏳ Lo sentimos, el tiempo límite para participar en esta encuesta ha finalizado.");
                    return TwiML(response);
                }

                bool esModoPrueba = false;
                string contextoAsignado = esModoPrueba ? $"TEST_{encuesta.EncuestaID}" : $"REAL_{encuesta.EncuestaID}";

                if (!esModoPrueba)
                {
                    int encuestasEnCurso = await _context.SesionesBotWhatsApp
                        .CountAsync(s => s.TipoFlujo == "Encuesta" && s.ContextoID == contextoAsignado);

                    if (encuestasEnCurso >= encuesta.LimiteEmpleados)
                    {
                        response.Message("🚫 Se ha alcanzado el límite máximo de participantes permitidos para esta encuesta.");
                        return TwiML(response);
                    }
                }

                var sesionEncuesta = await _context.SesionesBotWhatsApp.FindAsync(numeroUsuario);
                if (sesionEncuesta != null)
                {
                    if (sesionEncuesta.TipoFlujo == "Encuesta" && sesionEncuesta.ContextoID == contextoAsignado)
                    {
                        response.Message("⚠️ *Ya tienes esta encuesta en curso*. Por favor, lee el mensaje anterior y envíanos tu respuesta.");
                        return TwiML(response);
                    }
                    else
                    {
                        sesionEncuesta.TipoFlujo = "Encuesta";
                        sesionEncuesta.PasoActual = 0;
                        sesionEncuesta.ContextoID = contextoAsignado;
                        sesionEncuesta.ErroresConsecutivos = 0;
                        sesionEncuesta.UltimaInteraccion = DateTime.Now;
                    }
                }
                else
                {
                    sesionEncuesta = new SesionBotWhatsApp
                    {
                        NumeroTelefono = numeroUsuario,
                        TipoFlujo = "Encuesta",
                        PasoActual = 0,
                        ContextoID = contextoAsignado,
                        ErroresConsecutivos = 0,
                        UltimaInteraccion = DateTime.Now
                    };
                    _context.SesionesBotWhatsApp.Add(sesionEncuesta);
                }

                await _context.SaveChangesAsync();

                var primeraPregunta = await _context.PreguntasEncuestas
                    .Where(p => p.EncuestaID == encuesta.EncuestaID)
                    .OrderBy(p => p.PreguntaID)
                    .FirstOrDefaultAsync();

                if (primeraPregunta != null)
                {
                    string mensajeBienvenida = esModoPrueba
                        ? $"🛠️ *MODO PRUEBA ACTIVADO*\nHola {encuesta.Empresa?.NombreContacto}, estás probando la prueba diagnóstico de tu empresa. Tus respuestas no se guardarán ni restarán lugares a tus empleados.\n\n"
                        : $"👋 ¡Hola! Bienvenido a la encuesta realizada por *{encuesta.Empresa?.NombreEmpresa}*.\n\n";

                    string primerTextoFormateado = primeraPregunta.TextoPregunta;
                    if (primerTextoFormateado.Contains("[") && primerTextoFormateado.Contains("]"))
                    {
                        int start = primerTextoFormateado.IndexOf('[');
                        int end = primerTextoFormateado.IndexOf(']');
                        string parteInicial = primerTextoFormateado.Substring(0, start).Trim();
                        string bloqueOpciones = primerTextoFormateado.Substring(start + 1, end - start - 1);
                        string listaOpciones = bloqueOpciones.Replace(", ", "\n");
                        primerTextoFormateado = $"{parteInicial}\n\n*Opciones:*\n{listaOpciones}";
                    }

                    mensajeBienvenida += $"*Pregunta 1:*\n{primerTextoFormateado}";
                    response.Message(mensajeBienvenida);
                }

                return TwiML(response);
            }

            // RUTA B: APERTURA DE PRUEBA TAT MEDIANTE LINK DE INVITACIÓN
            if (mensajeUsuario.StartsWith("Comenzar_TAT_", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    string guidString = mensajeUsuario.Substring("Comenzar_TAT_".Length).Trim();

                    if (!Guid.TryParse(guidString, out Guid asignacionId))
                    {
                        response.Message("❌ El enlace de la prueba no es válido.");
                        return TwiML(response);
                    }

                    var asignacion = await _context.AsignacionesTAT.FirstOrDefaultAsync(a => a.LinkID == asignacionId);

                    if (asignacion == null || asignacion.Completada)
                    {
                        response.Message("❌ Esta prueba ya fue completada o el enlace ha caducado.");
                        return TwiML(response);
                    }

                    var sesionTAT = await _context.SesionesBotWhatsApp.FindAsync(numeroUsuario);
                    if (sesionTAT != null)
                    {
                        sesionTAT.TipoFlujo = "TAT";
                        sesionTAT.PasoActual = 0;
                        sesionTAT.ContextoID = asignacionId.ToString();
                        sesionTAT.UltimaInteraccion = DateTime.Now;
                    }
                    else
                    {
                        sesionTAT = new SesionBotWhatsApp
                        {
                            NumeroTelefono = numeroUsuario,
                            TipoFlujo = "TAT",
                            PasoActual = 0,
                            ContextoID = asignacionId.ToString(),
                            UltimaInteraccion = DateTime.Now
                        };
                        _context.SesionesBotWhatsApp.Add(sesionTAT);
                    }

                    await _context.SaveChangesAsync();

                    var primeraLamina = await _context.LaminasTAT.OrderBy(l => l.LaminaID).FirstOrDefaultAsync();

                    if (primeraLamina != null)
                    {
                        var msgInstrucciones = new Twilio.TwiML.Messaging.Message();
                        msgInstrucciones.Body("🧠 *Bienvenido a la Prueba TAT*.\nEsta se compone de un total de 31 láminas distintas, donde redactarás una historia para cada lámina.\n\n" +
                                             "Escribe una historia describiendo:\n" +
                                             "1. ¿Qué está sucediendo en la imagen?\n" +
                                             "2. ¿Qué ocurrió antes de este momento?\n" +
                                             "3. ¿Cómo terminará la historia?");
                        response.Append(msgInstrucciones);

                        var totalLaminas = await _context.LaminasTAT.CountAsync();

                        var msgLamina = new Twilio.TwiML.Messaging.Message();
                        msgLamina.Body($"*Lámina 1 de {totalLaminas}*\nObserva la imagen y redacta tu historia.");
                        msgLamina.Media(new Uri(primeraLamina.UrlImagen));
                        response.Append(msgLamina);
                    }
                    else
                    {
                        response.Message("⚠️ No se encontraron imágenes configuradas para la prueba TAT. Revisa que se encuentren en la base de datos.");
                    }

                    return TwiML(response);
                }
                catch (Exception ex)
                {
                    response.Message($"🚨 *Error Interno de C#:* {ex.Message}");
                    return TwiML(response);
                }
            }

            var sesion = await _context.SesionesBotWhatsApp.FirstOrDefaultAsync(s => s.NumeroTelefono == numeroUsuario);

            if (comandoLimpio == "reiniciar" || comandoLimpio == "reset")
            {
                if (sesion != null)
                {
                    sesion.TipoFlujo = "RegistroInicial";
                    sesion.PasoActual = 0;
                    sesion.ContextoID = null;
                    sesion.ErroresConsecutivos = 0;
                    sesion.UltimaInteraccion = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }

                response.Message("🔄 *Sesión restablecida.*\n\nPara iniciar una nueva solicitud, escribe la palabra: *Iniciar*");
                return TwiML(response);
            }

            if (sesion == null)
            {
                sesion = new SesionBotWhatsApp
                {
                    NumeroTelefono = numeroUsuario,
                    TipoFlujo = "RegistroInicial",
                    PasoActual = 0,
                    UltimaInteraccion = DateTime.UtcNow
                };

                _context.SesionesBotWhatsApp.Add(sesion);
                await _context.SaveChangesAsync();

                response.Message("🤖 ¡Hola! Soy el asistente personal de *Avenzo*.\n\nEstoy aquí para ayudarte a realizar el proceso de solicitud para una encuesta diagnóstico dirigida hacia tu empresa/negocio.\n\nPara iniciar, escribe la palabra: *Iniciar*");
                return TwiML(response);
            }

            // GESTIÓN DE FLUJOS ACTIVOS SEGÚN EL TIPO REGISTRADO
            sesion.UltimaInteraccion = DateTime.UtcNow;

            // FLUJO 1: CAPTURA Y VALIDACIÓN DE RESPUESTAS DE ENCUESTA 
            if (sesion.TipoFlujo == "Encuesta")
            {
                string[] contextoPartes = sesion.ContextoID!.Split('_');
                bool esModoPrueba = contextoPartes[0] == "TEST";
                int encuestaId = int.Parse(contextoPartes[1]);

                var preguntas = await _context.PreguntasEncuestas
                    .Where(p => p.EncuestaID == encuestaId)
                    .OrderBy(p => p.PreguntaID)
                    .ToListAsync();

                var preguntaActual = preguntas[sesion.PasoActual];
                string textoPregunta = preguntaActual.TextoPregunta;
                string respuestaParaGuardar = mensajeUsuario;

                if (textoPregunta.Contains("[") && textoPregunta.Contains("]"))
                {
                    bool opcionValida = false;
                    if (int.TryParse(mensajeUsuario, out int numeroOpcion))
                    {
                        int start = textoPregunta.IndexOf('[');
                        int end = textoPregunta.IndexOf(']');
                        string bloqueOpciones = textoPregunta.Substring(start + 1, end - start - 1);
                        string patronBuscar = $"{numeroOpcion}.";

                        if (bloqueOpciones.Contains(patronBuscar))
                        {
                            opcionValida = true;
                            string[] opcionesSeparadas = bloqueOpciones.Split(',');
                            foreach (var opcion in opcionesSeparadas)
                            {
                                string opcionLimpia = opcion.Trim();
                                if (opcionLimpia.StartsWith(patronBuscar))
                                {
                                    respuestaParaGuardar = opcionLimpia.Replace(patronBuscar, "").Trim();
                                    break;
                                }
                            }
                        }
                    }

                    if (!opcionValida)
                    {
                        sesion.ErroresConsecutivos++;

                        if (sesion.ErroresConsecutivos >= 5)
                        {
                            sesion.TipoFlujo = "Baneado";
                            sesion.ContextoID = null;
                            response.Message("⛔ *Encuesta Finalizada*\nHemos detectado múltiples respuestas inválidas consecutivas. Por políticas de calidad y honestidad en los resultados, tu sesión ha sido bloqueada en la encuesta y tus respuestas no serán contabilizadas.");
                            await _context.SaveChangesAsync();
                            return TwiML(response);
                        }
                        else
                        {
                            int intentosRestantes = 3 - sesion.ErroresConsecutivos;
                            response.Message($"⚠️ *Respuesta inválida.*\nPor favor, responde únicamente con el *número* de tu elección.\n\n_Tienes {intentosRestantes} intentos restantes antes de cancelar la encuesta._");
                            await _context.SaveChangesAsync();
                            return TwiML(response);
                        }
                    }
                }

                sesion.ErroresConsecutivos = 0;

                if (!esModoPrueba)
                {
                    var nuevaRespuesta = new RespuestaEncuesta
                    {
                        PreguntaID = preguntaActual.PreguntaID,
                        NumeroTelefono = numeroUsuario,
                        TextoRespuesta = respuestaParaGuardar,
                        FechaRespuesta = DateTime.Now
                    };
                    _context.RespuestasEncuestas.Add(nuevaRespuesta);
                }

                sesion.PasoActual++;

                if (sesion.PasoActual < preguntas.Count)
                {
                    var siguientePregunta = preguntas[sesion.PasoActual];

                    string textoFormateado = siguientePregunta.TextoPregunta;
                    if (textoFormateado.Contains("[") && textoFormateado.Contains("]"))
                    {
                        int start = textoFormateado.IndexOf('[');
                        int end = textoFormateado.IndexOf(']');
                        string parteInicial = textoFormateado.Substring(0, start).Trim();
                        string bloqueOpciones = textoFormateado.Substring(start + 1, end - start - 1);
                        string listaOpciones = bloqueOpciones.Replace(", ", "\n");
                        textoFormateado = $"{parteInicial}\n\n*Opciones:*\n{listaOpciones}";
                    }

                    response.Message($"*Pregunta {sesion.PasoActual + 1} de {preguntas.Count}:*\n{textoFormateado}");
                }
                else
                {
                    string mensajeDespedida = esModoPrueba
                        ? "✅ *¡Prueba Finalizada!*\nHas revisado todas las preguntas de la encuesta. Como estás en Modo Prueba, estas respuestas no se guardaron."
                        : "✅ *¡Has finalizado la encuesta!*\nAgradecemos tu participacion en esta actividad.";

                    response.Message(mensajeDespedida);

                    sesion.TipoFlujo = "Finalizado";
                    sesion.PasoActual = 0;
                    sesion.ContextoID = null;
                }
            }

            // FLUJO 2: RECEPCIÓN DE HISTORIAS PARA PRUEBA TAT
            else if (sesion.TipoFlujo == "TAT")
            {
                try
                {
                    var laminas = await _context.LaminasTAT.OrderBy(l => l.LaminaID).ToListAsync();
                    var laminaActual = laminas[sesion.PasoActual];
                    Guid asignacionId = Guid.Parse(sesion.ContextoID!);

                    var asignacion = await _context.AsignacionesTAT
                        .Include(a => a.Encuesta)
                            .ThenInclude(e => e.Empresa)
                        .FirstOrDefaultAsync(a => a.LinkID == asignacionId);

                    var nuevaRespuestaTAT = new RespuestaTAT
                    {
                        AsignacionID = asignacion!.AsignacionID,
                        LaminaID = laminaActual.LaminaID,
                        HistoriaTexto = mensajeUsuario,
                        FechaRespuesta = DateTime.Now
                    };
                    _context.RespuestasTAT.Add(nuevaRespuestaTAT);

                    sesion.PasoActual++;

                    if (sesion.PasoActual < laminas.Count)
                    {
                        var siguienteLamina = laminas[sesion.PasoActual];

                        var message = new Twilio.TwiML.Messaging.Message();
                        message.Body($"*Lámina {sesion.PasoActual + 1} de {laminas.Count}*\nObserva la imagen y redacta tu historia.");
                        message.Media(new Uri(siguienteLamina.UrlImagen));
                        response.Append(message);
                    }
                    else
                    {
                        asignacion.Completada = true;
                        sesion.TipoFlujo = "Finalizado";
                        sesion.PasoActual = 0;
                        sesion.ContextoID = null;

                        response.Message("*¡Has completado la prueba TAT!*\nGracias por tu participacion.");

                        if (!string.IsNullOrWhiteSpace(asignacion.TelefonoAdministrador))
                        {
                            await _context.SaveChangesAsync();

                            var todasLasRespuestas = await _context.RespuestasTAT
                                .Include(r => r.Lamina)
                                .Where(r => r.AsignacionID == asignacion.AsignacionID)
                                .OrderBy(r => r.LaminaID)
                                .ToListAsync();

                            string empresaNombre = asignacion.Encuesta?.Empresa?.NombreEmpresa ?? "la empresa evaluada";

                            string encabezadoPrincipal =
                                $"🔔 *Alerta: Prueba TAT Finalizada*\n" +
                                $"Candidato: *{numeroUsuario}*\n" +
                                $"Empresa: *{empresaNombre}*\n" +
                                $"Total láminas: *{todasLasRespuestas.Count}*\n" +
                                $"────────────────────\n\n";

                            var paquetesMensajes = new List<string>();
                            string mensajeBuffer = encabezadoPrincipal;
                            int numeroLamina = 1;

                            foreach (var respuesta in todasLasRespuestas)
                            {
                                string nombreLamina = !string.IsNullOrWhiteSpace(respuesta.Lamina?.NombreLamina)
                                    ? respuesta.Lamina.NombreLamina
                                    : $"Lámina {numeroLamina}";

                                string bloqueTexto = $"🖼️ *{nombreLamina}:*\n\"{respuesta.HistoriaTexto.Trim()}\"\n\n";

                                if ((mensajeBuffer.Length + bloqueTexto.Length) > 1400)
                                {
                                    paquetesMensajes.Add(mensajeBuffer.TrimEnd());
                                    mensajeBuffer = $"📄 *Continuación Prueba TAT (+{numeroUsuario}):*\n\n";
                                }

                                mensajeBuffer += bloqueTexto;
                                numeroLamina++;
                            }

                            if (!string.IsNullOrWhiteSpace(mensajeBuffer))
                            {
                                paquetesMensajes.Add(mensajeBuffer.TrimEnd());
                            }

                            for (int i = 0; i < paquetesMensajes.Count; i++)
                            {
                                string textoAEnviar = paquetesMensajes.Count > 1
                                    ? $"{paquetesMensajes[i]}\n\n_(Parte {i + 1} de {paquetesMensajes.Count})_"
                                    : paquetesMensajes[i];

                                await EnviarNotificacionWhatsApp(asignacion.TelefonoAdministrador, textoAEnviar);
                                await Task.Delay(300);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    response.Message($"🚨 *Error Interno de C# al continuar TAT:* {ex.Message}");
                    return TwiML(response);
                }
            }

            // FLUJO 3: REGISTRO CONVERSACIONAL DE NUEVA SOLICITUD DE CLIENTE
            else if (sesion.TipoFlujo == "RegistroInicial")
            {
                switch (sesion.PasoActual)
                {
                    case 0:
                        if (mensajeUsuario.Equals("iniciar", StringComparison.OrdinalIgnoreCase))
                        {
                            sesion.PasoActual = 1;
                            response.Message("👍 ¡Bienvenido! Empezaremos con el registro. Necesitaré los siguientes 5 datos:\n\n*1️*. *Nombre de la Empresa* que será evaluada.");
                        }
                        else
                        {
                            response.Message("Para iniciar, escribe la palabra: *Iniciar*");
                        }
                        break;

                    case 1:
                        var nuevaEmpresa = new EmpresaCliente
                        {
                            NombreEmpresa = mensajeUsuario,
                            NombreContacto = "Pendiente",
                            TelefonoContacto = numeroUsuario
                        };
                        _context.EmpresasClientes.Add(nuevaEmpresa);
                        await _context.SaveChangesAsync();

                        var nuevaSolicitud = new SolicitudEncuesta
                        {
                            EmpresaID = nuevaEmpresa.EmpresaID,
                            Problematica = "Pendiente"
                        };
                        _context.SolicitudesEncuestas.Add(nuevaSolicitud);
                        await _context.SaveChangesAsync();

                        sesion.ContextoID = nuevaSolicitud.SolicitudID.ToString();
                        sesion.PasoActual = 2;
                        response.Message($"✅ Respuesta registrada: _{mensajeUsuario}_.\n\n*2️*. *Nombre del representante* de la empresa (Tuyo).\n_(Comienza por el Nombre y despues Apellidos)_");
                        break;

                    case 2:
                        var solPaso2 = await _context.SolicitudesEncuestas.Include(s => s.Empresa)
                                                               .FirstOrDefaultAsync(s => s.SolicitudID == int.Parse(sesion.ContextoID!));
                        if (solPaso2 != null && solPaso2.Empresa != null) solPaso2.Empresa.NombreContacto = mensajeUsuario;

                        sesion.PasoActual = 3;
                        response.Message($"✅ Respuesta registrada: _{mensajeUsuario}_.\n\n*3️*. *Número de empleados* que contestarán la encuesta.\n_(Por favor, escribe únicamente el número)_");
                        break;

                    case 3:
                        if (int.TryParse(mensajeUsuario, out int numEmpleados))
                        {
                            var solPaso3 = await _context.SolicitudesEncuestas.FindAsync(int.Parse(sesion.ContextoID!));
                            if (solPaso3 != null) solPaso3.NumeroEmpleados = numEmpleados;

                            sesion.PasoActual = 4;
                            response.Message($"✅ Respuesta registrada: _{mensajeUsuario}_.\n\n*4️*. *Número de días* que se necesitan para aplicar la encuesta a los empleados.\n_(Por favor, escribe únicamente el número)_");
                        }
                        else
                        {
                            response.Message("⚠️ *Formato inválido.*\nPor favor, ingresa únicamente el *número* (ejemplo: 50).");
                        }
                        break;

                    case 4:
                        if (int.TryParse(mensajeUsuario, out int numDias))
                        {
                            var solPaso4 = await _context.SolicitudesEncuestas.FindAsync(int.Parse(sesion.ContextoID!));
                            if (solPaso4 != null) solPaso4.DiasEstimados = numDias;

                            sesion.PasoActual = 5;
                            response.Message($"✅ Respuesta registrada: _{mensajeUsuario}_.\n\n*5️*. Por último, describe brevemente la *problemática actual* que ocurre dentro de la empresa.");
                        }
                        else
                        {
                            response.Message("⚠️ *Formato inválido.*\nPor favor, ingresa únicamente el *número*(ejemplo: 5).");
                        }
                        break;

                    case 5:
                        var solPaso5 = await _context.SolicitudesEncuestas.FindAsync(int.Parse(sesion.ContextoID!));
                        if (solPaso5 != null) solPaso5.Problematica = mensajeUsuario;

                        sesion.PasoActual = 6;
                        response.Message("✅ *¡Solicitud registrada con éxito!*\nSerá revisada por nuestro equipo, te enviaremos un mensaje por este mismo chat cuando sea aprobada/rechazada.");
                        break;

                    default:
                        var ultimaSolicitud = await _context.SolicitudesEncuestas
                            .Include(s => s.Empresa)
                            .Where(s => s.Empresa.TelefonoContacto == numeroUsuario ||
                                        s.Empresa.TelefonoContacto == "+" + numeroUsuario ||
                                        s.Empresa.TelefonoContacto.Contains(numeroUsuario))
                            .OrderByDescending(s => s.FechaSolicitud)
                            .FirstOrDefaultAsync();

                        if (ultimaSolicitud != null && ultimaSolicitud.Estatus == "Pendiente")
                        {
                            response.Message("⏳ Tu solicitud se encuentra actualmente en *proceso de revisión* por nuestro equipo.\n\nPor favor, espera a que sea evaluada antes de enviar una nueva.");
                            break;
                        }

                        if (mensajeUsuario.Equals("iniciar", StringComparison.OrdinalIgnoreCase) ||
                            mensajeUsuario.Equals("comenzar", StringComparison.OrdinalIgnoreCase))
                        {
                            sesion.PasoActual = 1;
                            sesion.ContextoID = null;
                            response.Message("👍 ¡Bienvenido! Comenzaremos con el registro de tu nueva solicitud. Necesitaré los siguientes 5 datos:\n\n*1️*. *Nombre de la Empresa* que será evaluada.");
                        }
                        else
                        {
                            response.Message("Tu solicitud anterior fue atendida.\n\nPara iniciar una nueva solicitud, escribe la palabra: *Iniciar*");
                        }
                        break;
                }
            }

            // FLUJO 4: ATENCIÓN A SESIONES PREVIAMENTE FINALIZADAS
            else if (sesion.TipoFlujo == "Finalizado")
            {
                if (mensajeUsuario.Equals("iniciar", StringComparison.OrdinalIgnoreCase))
                {
                    sesion.TipoFlujo = "RegistroInicial";
                    sesion.PasoActual = 1;
                    sesion.ContextoID = null;
                    response.Message("👍 ¡Bienvenido! Empezaremos con el registro. Necesitaré los siguientes 5 datos:\n\n*1️*. *Nombre de la Empresa* que será evaluada.");
                }
            }

            await _context.SaveChangesAsync();
            return TwiML(response);
        }

        // 2. MÉTODO AUXILIAR: NOTIFICACIÓN DIRECTA AL EVALUADOR 
        private async Task<bool> EnviarNotificacionWhatsApp(string numeroDestino, string mensajeTexto)
        {
            try
            {
                string accountSid = _configuration["Twilio:AccountSid"] ?? "";
                string authToken = _configuration["Twilio:AuthToken"] ?? "";
                string twilioWhatsAppNumber = _configuration["Twilio:WhatsAppNumber"] ?? "";

                if (string.IsNullOrWhiteSpace(accountSid) || string.IsNullOrWhiteSpace(authToken) || string.IsNullOrWhiteSpace(twilioWhatsAppNumber))
                    return false;

                string telLimpio = numeroDestino.Replace("whatsapp:", "").Trim();
                if (!telLimpio.StartsWith("+")) telLimpio = "+" + telLimpio;

                string formatoDestino = $"whatsapp:{telLimpio}";
                string formatoOrigen = twilioWhatsAppNumber.StartsWith("whatsapp:")
                    ? twilioWhatsAppNumber
                    : $"whatsapp:{twilioWhatsAppNumber}";

                using var client = new HttpClient();
                var byteArray = Encoding.ASCII.GetBytes($"{accountSid}:{authToken}");
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

                var keyValues = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("To", formatoDestino),
                    new KeyValuePair<string, string>("From", formatoOrigen),
                    new KeyValuePair<string, string>("Body", mensajeTexto)
                };

                var encodedContent = new FormUrlEncodedContent(keyValues);
                string urlTwilioApi = $"https://api.twilio.com/2010-04-01/Accounts/{accountSid}/Messages.json";

                var response = await client.PostAsync(urlTwilioApi, encodedContent);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error al notificar al evaluador vía Twilio API]: {ex.Message}");
                return false;
            }
        }
    }
}