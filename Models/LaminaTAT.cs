using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Metarsis.Api.Models
{
    [Table("LaminaTAT", Schema = "MET")]
    public class LaminaTAT
    {
        [Key]
        public int LaminaID { get; set; } // ID de la lámina

        [Required]
        [MaxLength(100)]
        public string NombreLamina { get; set; } = string.Empty; // Nombre de la lámina (ej. Lámina 1, Lámina 2)

        [Required]
        public string UrlImagen { get; set; } = string.Empty; // URL pública de la imagen alojada para envío por WhatsApp
        public bool Activa { get; set; } = true; // Control de disponibilidad para habilitar o deshabilitar la lámina
    }
}