using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Metarsis.Api.Models
{
    [Table("PreguntaEncuesta", Schema = "MET")]
    public class PreguntaEncuesta
    {
        [Key] // <--- ¡Esta es la etiqueta que faltaba!
        public int PreguntaID { get; set; } // ID de la pregunta

        public int EncuestaID { get; set; } // ID de la encuesta a la que pertenece la pregunta
        public string TextoPregunta { get; set; } = string.Empty; // Pregunta realizada (abierta/opciones)
        public Encuesta? Encuesta { get; set; } // Navegación hacia los datos de la encuesta
    }
}