using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Metarsis.Api.Models
{
    [Table("RespuestaEncuesta", Schema = "MET")]
    public class RespuestaEncuesta
    {
        [Key]
        public int RespuestaID { get; set; } // ID de la respuesta
        public int PreguntaID { get; set; } // ID de la pregunta a la que corresponde la respuesta

        [MaxLength(20)]
        public string NumeroTelefono { get; set; } = string.Empty; // Número de WhatsApp del empleado que contesto la pregunta
        public string TextoRespuesta { get; set; } = string.Empty; // Texto o contenido de la respuesta enviada
        public DateTime FechaRespuesta { get; set; } = DateTime.UtcNow; // Fecha y hora en que se registro la respuesta

        // RELACION
        public PreguntaEncuesta? Pregunta { get; set; } // Enlace directo hacia la entidad de la pregunta respondida
    }
}