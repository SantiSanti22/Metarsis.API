using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Metarsis.API.Migrations
{
    /// <inheritdoc />
    public partial class CreaTablaRespuestasEncuesta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RespuestaEncuesta",
                schema: "MET",
                columns: table => new
                {
                    RespuestaID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PreguntaID = table.Column<int>(type: "int", nullable: false),
                    NumeroTelefono = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TextoRespuesta = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FechaRespuesta = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RespuestaEncuesta", x => x.RespuestaID);
                    table.ForeignKey(
                        name: "FK_RespuestaEncuesta_PreguntasEncuestas_PreguntaID",
                        column: x => x.PreguntaID,
                        principalTable: "PreguntasEncuestas",
                        principalColumn: "PreguntaID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RespuestaEncuesta_PreguntaID",
                schema: "MET",
                table: "RespuestaEncuesta",
                column: "PreguntaID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RespuestaEncuesta",
                schema: "MET");
        }
    }
}
