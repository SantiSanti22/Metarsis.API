using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Metarsis.API.Migrations
{
    /// <inheritdoc />
    public partial class AgregaSolicitudes_V3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SolicitudEncuesta",
                schema: "MET",
                columns: table => new
                {
                    SolicitudID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmpresaID = table.Column<int>(type: "int", nullable: false),
                    NumeroEmpleados = table.Column<int>(type: "int", nullable: false),
                    DiasEstimados = table.Column<int>(type: "int", nullable: false),
                    Problematica = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Estatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FechaSolicitud = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolicitudEncuesta", x => x.SolicitudID);
                    table.ForeignKey(
                        name: "FK_SolicitudEncuesta_EmpresaCliente_EmpresaID",
                        column: x => x.EmpresaID,
                        principalSchema: "MET",
                        principalTable: "EmpresaCliente",
                        principalColumn: "EmpresaID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudEncuesta_EmpresaID",
                schema: "MET",
                table: "SolicitudEncuesta",
                column: "EmpresaID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SolicitudEncuesta",
                schema: "MET");
        }
    }
}
