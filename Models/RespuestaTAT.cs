using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Metarsis.Api.Models
{
    [Table("RespuestaTAT", Schema = "MET")]
    public class RespuestaTAT
    {
        [Key]
        public int RespuestaTATID { get; set; } // ID de la respuesta de la prueba TAT 
        public int AsignacionID { get; set; } // ID de la asignación de la prueba TAT correspondiente
        public int LaminaID { get; set; } // ID de la lámina la cual fue contestada

        [Required]
        public string HistoriaTexto { get; set; } = string.Empty; // Texto de la historia redactada por el evaluado para la lámina

        public DateTime FechaRespuesta { get; set; } = DateTime.UtcNow; // Fecha y hora en que se registro la respuesta

        // RELACIONES
        public AsignacionTAT? Asignacion { get; set; } // Enlace hacia los datos de la asignación de la prueba
        public LaminaTAT? Lamina { get; set; } // Enlace hacia los datos de la lámina evaluada
    }
}