using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Metarsis.Api.Models
{
    [Table("SolicitudEncuesta", Schema = "MET")]
    public class SolicitudEncuesta
    {
        [Key]
        public int SolicitudID { get; set; } // ID de la solicitud de encuesta

        // RELACION CON TABLA EMPRESA_CLIENTE
        [Required]
        public int EmpresaID { get; set; } // ID de la empresa cliente que solicita la encuesta
        [ForeignKey("EmpresaID")]
        public EmpresaCliente Empresa { get; set; } // Enlace directo hacia los datos de la empresa cliente

        public int NumeroEmpleados { get; set; } // Cantidad de colaboradores que participarán en la evaluación
        public int DiasEstimados { get; set; } // Días estimados previstos para aplicar la encuesta
        public string Problematica { get; set; } // Motivo o situación por la que se solicita el diagnóstico

        [MaxLength(20)]
        public string Estatus { get; set; } = "Pendiente"; // Estado de la solicitud (ejem. Pendiente, Aprobada, Rechazada)
        public DateTime FechaSolicitud { get; set; } = DateTime.UtcNow; // Fecha y hora en que se registró la solicitud en el sistema
    }
}