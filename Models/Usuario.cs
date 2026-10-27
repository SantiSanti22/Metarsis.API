using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Metarsis.Api.Models
{
    [Table("Usuarios", Schema = "MET")] 
    public class Usuario
    {
        [Key]
        public int UsuarioID { get; set; } // ID del usuario del sistema

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [MaxLength(150)]
        public string NombreCompleto { get; set; } = string.Empty; // Nombre completo del administrador o consultor

        [Required(ErrorMessage = "El correo electrónico es obligatorio")]
        [EmailAddress(ErrorMessage = "Formato de correo inválido")]
        [MaxLength(150)]
        public string Correo { get; set; } = string.Empty; // Correo electrónico utilizado para el inicio de sesión

        [Required]
        public string PasswordHash { get; set; } = string.Empty; // Hash de seguridad de la contraseña cifrada

        [Required(ErrorMessage = "El teléfono de WhatsApp es obligatorio")]
        [MaxLength(20)]
        public string TelefonoWhatsApp { get; set; } = string.Empty; // Número de WhatsApp para alertas y notificacione

        [MaxLength(50)]
        public string Rol { get; set; } = "Admin"; // Rol o nivel de permisos asignado (ej. Admin, Consultor)
        public bool Activo { get; set; } = true; // Estado de la cuenta para permitir o restringir el acceso
        public DateTime FechaRegistro { get; set; } = DateTime.Now; // Fecha y hora en que se registró el usuario
    }
}