namespace Metarsis.Api.Models
{
    public class ProgresoEmpleadoViewModel
    {
        public string Telefono { get; set; } = string.Empty; // Número de WhatsApp del empleado
        public DateTime UltimaInteraccion { get; set; } // Fecha y hora del último mensaje o respuesta registrado
        public int PreguntasRespondidas { get; set; } // Cantidad de preguntas respondidas hasta el momento
        public int TotalPreguntas { get; set; } // Total de preguntas configuradas en la encuesta

        // Porcentaje de avance calculado de forma dinámica
        public int PorcentajeAvance => TotalPreguntas > 0 ? (PreguntasRespondidas * 100) / TotalPreguntas : 0;

        // Etiqueta descriptiva del estado actual del empleado
        public string Estatus => PorcentajeAvance >= 100 ? "Completado" : "Respondiendo..."; 

        // Lista con el desglose de preguntas y respuestas del empleado
        public List<RespuestaDetalleViewModel> Respuestas { get; set; } = new List<RespuestaDetalleViewModel>(); 
    }

    // Estructura para almacenar individualmente cada pregunta con su respuesta
    public class RespuestaDetalleViewModel
    {
        public string Pregunta { get; set; } = string.Empty;
        public string Respuesta { get; set; } = string.Empty;
    }
}