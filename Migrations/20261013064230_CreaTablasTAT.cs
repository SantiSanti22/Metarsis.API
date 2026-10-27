using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Metarsis.API.Migrations
{
    /// <inheritdoc />
    public partial class CreaTablasTAT : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AsignacionTAT",
                schema: "MET",
                columns: table => new
                {
                    AsignacionID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EncuestaID = table.Column<int>(type: "int", nullable: false),
                    NumeroTelefono = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    LinkID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Completada = table.Column<bool>(type: "bit", nullable: false),
                    FechaAsignacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AsignacionTAT", x => x.AsignacionID);
                });

            migrationBuilder.CreateTable(
                name: "LaminaTAT",
                schema: "MET",
                columns: table => new
                {
                    LaminaID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NombreLamina = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UrlImagen = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LaminaTAT", x => x.LaminaID);
                });

            migrationBuilder.CreateTable(
                name: "RespuestaTAT",
                schema: "MET",
                columns: table => new
                {
                    RespuestaTATID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AsignacionID = table.Column<int>(type: "int", nullable: false),
                    LaminaID = table.Column<int>(type: "int", nullable: false),
                    HistoriaTexto = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FechaRespuesta = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RespuestaTAT", x => x.RespuestaTATID);
                    table.ForeignKey(
                        name: "FK_RespuestaTAT_AsignacionTAT_AsignacionID",
                        column: x => x.AsignacionID,
                        principalSchema: "MET",
                        principalTable: "AsignacionTAT",
                        principalColumn: "AsignacionID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RespuestaTAT_LaminaTAT_LaminaID",
                        column: x => x.LaminaID,
                        principalSchema: "MET",
                        principalTable: "LaminaTAT",
                        principalColumn: "LaminaID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RespuestaTAT_AsignacionID",
                schema: "MET",
                table: "RespuestaTAT",
                column: "AsignacionID");

            migrationBuilder.CreateIndex(
                name: "IX_RespuestaTAT_LaminaID",
                schema: "MET",
                table: "RespuestaTAT",
                column: "LaminaID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RespuestaTAT",
                schema: "MET");

            migrationBuilder.DropTable(
                name: "AsignacionTAT",
                schema: "MET");

            migrationBuilder.DropTable(
                name: "LaminaTAT",
                schema: "MET");
        }
    }
}
