using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Metarsis.Api.Models
{
    [Table("ReporteFinal", Schema = "MET")]
    public class ReporteFinal
    {
        [Key]
        public int ReporteID { get; set; } // ID del reporte final
        public int EncuestaID { get; set; } // ID de la encuesta vinculada al reporte
        public string BorradorJson { get; set; } = string.Empty; // Estructura JSON generada por la IA para previsualización y edición

        [MaxLength(255)]
        public string RutaArchivoPdf { get; set; } = string.Empty; // Ruta del archivo PDF generado 
        public DateTime FechaGeneracion { get; set; } = DateTime.Now; // Fecha y hora en que se generó el reporte

        [ForeignKey("EncuestaID")]
        public Encuesta? Encuesta { get; set; } // Navegación hacia los datos de la encuesta asociada
    }
}