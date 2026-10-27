using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Metarsis.Api.Models
{
    [Table("EmpresaCliente", Schema = "MET")]
    public class EmpresaCliente
    {
        [Key]
        public int EmpresaID { get; set; } // ID de la empresa cliente
        [Required]
        [MaxLength(150)]
        public string NombreEmpresa { get; set; } // Nombre o razon social de la empresa
        [Required]
        [MaxLength(100)]
        public string NombreContacto { get; set; } // Nombre del representante o contacto directo de la empresa
        [Required]
        [MaxLength(20)]
        public string TelefonoContacto { get; set; } // Teléfono de contacto registrado vía WhatsApp
        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow; // Fecha y hora en que se registró la empresa en el sistema
    }
}