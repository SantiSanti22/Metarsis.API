using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Metarsis.API.Migrations
{
    /// <inheritdoc />
    public partial class AgregaTablasEncuesta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Encuestas",
                columns: table => new
                {
                    EncuestaID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmpresaID = table.Column<int>(type: "int", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Titulo = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Encuestas", x => x.EncuestaID);
                    table.ForeignKey(
                        name: "FK_Encuestas_EmpresaCliente_EmpresaID",
                        column: x => x.EmpresaID,
                        principalSchema: "MET",
                        principalTable: "EmpresaCliente",
                        principalColumn: "EmpresaID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PreguntasEncuestas",
                columns: table => new
                {
                    PreguntaID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EncuestaID = table.Column<int>(type: "int", nullable: false),
                    TextoPregunta = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreguntasEncuestas", x => x.PreguntaID);
                    table.ForeignKey(
                        name: "FK_PreguntasEncuestas_Encuestas_EncuestaID",
                        column: x => x.EncuestaID,
                        principalTable: "Encuestas",
                        principalColumn: "EncuestaID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Encuestas_EmpresaID",
                table: "Encuestas",
                column: "EmpresaID");

            migrationBuilder.CreateIndex(
                name: "IX_PreguntasEncuestas_EncuestaID",
                table: "PreguntasEncuestas",
                column: "EncuestaID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PreguntasEncuestas");

            migrationBuilder.DropTable(
                name: "Encuestas");
        }
    }
}
