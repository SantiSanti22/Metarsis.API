using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Metarsis.Api.Models
{
    [Table("Encuesta", Schema = "MET")]
    public class Encuesta
    {
        [Key]
        public int EncuestaID { get; set; } // ID de la encuesta
        public int EmpresaID { get; set; } // ID de la empresa cliente a la que pertenece la encuesta
        public DateTime FechaCreacion { get; set; }  // Fecha y hora en que se creó la encuesta
        public string Titulo { get; set; } = string.Empty; // Problematica registrada para el diagnóstico
        public Guid LinkID { get; set; } // GUID único utilizado para el enlace de acceso por WhatsApp
        public int LimiteEmpleados { get; set; } // Cantidad máxima de empleados permitidos para responder
        public DateTime FechaCierre { get; set; } // Fecha y hora límite para contestar la encuesta
        public bool EstaActiva { get; set; } = false; // Estado de la encuesta (se activa al publicar)

        // DATOS DEL REPORTE 
        public string? DiagnosticoEjecutivo { get; set; } // Diagnostico ejecutivo derivado de los resultados
        public string? RecomendacionesClave { get; set; } // Sugerencias y acciones estratégicas para la empresa

        // CONSULTOR / EVALUADOR RESPONSABLE QUE APROBÓ LA ENCUESTA
        public int? UsuarioID { get; set; } // ID del consultor responsable en la tabla MET.Usuarios
        [ForeignKey("UsuarioID")]
        public Usuario? Consultor { get; set; } // Navegación hacia los datos del consultor
        public EmpresaCliente? Empresa { get; set; } // Navegación hacia los datos de la empresa cliente
    }
}