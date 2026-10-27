using Metarsis.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Metarsis.Api.Data
{
    public class MetarsisDbContext : DbContext
    {
        public MetarsisDbContext(DbContextOptions<MetarsisDbContext> options) : base(options)
        {
        }

        // 1. GESTIÓN DE CLIENTES, EMPLEADOS Y SESIONES DEL BOT
        public DbSet<EmpresaCliente> EmpresasClientes { get; set; }
        public DbSet<SesionBotWhatsApp> SesionesBotWhatsApp { get; set; }
        public DbSet<Empleado> Empleados { get; set; }
        public DbSet<SolicitudEncuesta> SolicitudesEncuestas { get; set; }

        // 2. GESTIÓN DE ENCUESTAS DIRIGIDAS PARA LAS EMPRESAS
        public DbSet<Encuesta> Encuestas { get; set; }
        public DbSet<PreguntaEncuesta> PreguntasEncuestas { get; set; }
        public DbSet<RespuestaEncuesta> RespuestasEncuestas { get; set; }

        // 3. GESTIÓN DE EVALUACIONES DE TEST TAT 
        public DbSet<LaminaTAT> LaminasTAT { get; set; }
        public DbSet<AsignacionTAT> AsignacionesTAT { get; set; }
        public DbSet<RespuestaTAT> RespuestasTAT { get; set; }

        // 4. GESTIÓN DE REPORTES
        public DbSet<ReporteFinal> ReportesFinales { get; set; }

        // 5. GESTIÓN DE SEGURIDAD Y CONTROL DE ACCESO
        public DbSet<Usuario> Usuarios { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}