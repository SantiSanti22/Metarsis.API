using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Metarsis.Api.Models
{
    [Table("AsignacionTAT", Schema = "MET")]
    public class AsignacionTAT
    {
        [Key]
        public int AsignacionID { get; set; } // ID de la asignación de la prueba TAT
        public int EncuestaID { get; set; } // Encuesta diagnóstica de la empresa de donde proviene esta prueba
        [ForeignKey("EncuestaID")]
        public virtual Encuesta? Encuesta { get; set; } // Navegación hacia los datos de la encuesta

        // DATOS RELACIONADOS AL EMPLEADO
        [MaxLength(20)]
        public string NumeroTelefono { get; set; } = string.Empty; // Teléfono del empleado evaluado
        public Guid LinkID { get; set; } = Guid.NewGuid(); // GUID único para el enlace de WhatsApp
        public bool Completada { get; set; } = false; // Bandera que indica si el empleado terminó las 31 láminas (evita reintentos)
        public DateTime FechaAsignacion { get; set; } = DateTime.UtcNow; // Fecha y hora en la que se creó la asignación

        // DATOS DEL ADMINISTRADOR / CONSULTOR RESPONSABLE DE EVALUAR
        public int? UsuarioID { get; set; } // ID del evaluador responsable en la tabla MET.Usuarios
        [ForeignKey("UsuarioID")]
        public virtual Usuario? Evaluador { get; set; } // Navegación hacia los datos del usuario evaluador
        [MaxLength(20)]
        public string? TelefonoAdministrador { get; set; } // WhatsApp del evaluador para enviar las notificaciones
        [MaxLength(150)]
        public string? NombreAdministrador { get; set; } // Nombre del evaluador para mostrarlo en el panel de control
    }
}