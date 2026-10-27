using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Metarsis.Api.Models
{
    [Table("Empleado", Schema = "MET")]
    public class Empleado
    {
        [Key]
        public int EmpleadoID { get; set; } // ID del empleado
        [Required]
        [MaxLength(20)]
        public string TelefonoWhatsApp { get; set; } // WhatsApp del empleado para contacto y pruebas
        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow; //Fecha y hora cuando se registro el empleado
    }
}