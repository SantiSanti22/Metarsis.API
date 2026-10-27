using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Metarsis.API.Migrations
{
    /// <inheritdoc />
    public partial class AgregarTablaReporteFinal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReporteFinal",
                schema: "MET",
                columns: table => new
                {
                    ReporteID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EncuestaID = table.Column<int>(type: "int", nullable: false),
                    BorradorJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RutaArchivoPdf = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FechaGeneracion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReporteFinal", x => x.ReporteID);
                    table.ForeignKey(
                        name: "FK_ReporteFinal_Encuesta_EncuestaID",
                        column: x => x.EncuestaID,
                        principalSchema: "MET",
                        principalTable: "Encuesta",
                        principalColumn: "EncuestaID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReporteFinal_EncuestaID",
                schema: "MET",
                table: "ReporteFinal",
                column: "EncuestaID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReporteFinal",
                schema: "MET");
        }
    }
}
