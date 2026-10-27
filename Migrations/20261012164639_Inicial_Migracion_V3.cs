using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Metarsis.API.Migrations
{
    /// <inheritdoc />
    public partial class Inicial_Migracion_V3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "MET");

            migrationBuilder.CreateTable(
                name: "Empleado",
                schema: "MET",
                columns: table => new
                {
                    EmpleadoID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TelefonoWhatsApp = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Empleado", x => x.EmpleadoID);
                });

            migrationBuilder.CreateTable(
                name: "EmpresaCliente",
                schema: "MET",
                columns: table => new
                {
                    EmpresaID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NombreEmpresa = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    NombreContacto = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TelefonoContacto = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmpresaCliente", x => x.EmpresaID);
                });

            migrationBuilder.CreateTable(
                name: "SesionBotWhatsApp",
                schema: "MET",
                columns: table => new
                {
                    NumeroTelefono = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TipoFlujo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PasoActual = table.Column<int>(type: "int", nullable: false),
                    ContextoID = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UltimaInteraccion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SesionBotWhatsApp", x => x.NumeroTelefono);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Empleado",
                schema: "MET");

            migrationBuilder.DropTable(
                name: "EmpresaCliente",
                schema: "MET");

            migrationBuilder.DropTable(
                name: "SesionBotWhatsApp",
                schema: "MET");
        }
    }
}
