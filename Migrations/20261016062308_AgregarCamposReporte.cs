using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Metarsis.API.Migrations
{
    /// <inheritdoc />
    public partial class AgregarCamposReporte : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DiagnosticoEjecutivo",
                schema: "MET",
                table: "Encuesta",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecomendacionesClave",
                schema: "MET",
                table: "Encuesta",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiagnosticoEjecutivo",
                schema: "MET",
                table: "Encuesta");

            migrationBuilder.DropColumn(
                name: "RecomendacionesClave",
                schema: "MET",
                table: "Encuesta");
        }
    }
}
