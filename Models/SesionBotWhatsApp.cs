using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Metarsis.Api.Models
{
    [Table("SesionBotWhatsApp", Schema = "MET")]
    public class SesionBotWhatsApp
    {
        [Key]
        [MaxLength(20)]
        public string NumeroTelefono { get; set; } // Número de WhatsApp que identifica la sesión activa (Representante/Empleado)

        [Required]
        [MaxLength(50)]
        public string TipoFlujo { get; set; } // Proceso activo del bot en el que interactúa el usuario (ejem. Solicitud, Encuesta, PruebaTAT)
        public int PasoActual { get; set; } // Número de paso o pregunta en la que se encuentra la conversación

        [MaxLength(50)]
        public string? ContextoID { get; set; } // ID de la encuesta o prueba específica que el usuario está respondiendo
        public int ErroresConsecutivos { get; set; } = 0; // Contador de respuestas no válidas consecutivas para control de fallos
        public DateTime UltimaInteraccion { get; set; } = DateTime.UtcNow; // Fecha y hora del último mensaje registrado en la sesión
    }
}