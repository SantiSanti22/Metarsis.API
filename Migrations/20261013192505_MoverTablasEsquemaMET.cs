using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Metarsis.API.Migrations
{
    /// <inheritdoc />
    public partial class MoverTablasEsquemaMET : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Encuestas_EmpresaCliente_EmpresaID",
                table: "Encuestas");

            migrationBuilder.DropForeignKey(
                name: "FK_PreguntasEncuestas_Encuestas_EncuestaID",
                table: "PreguntasEncuestas");

            migrationBuilder.DropForeignKey(
                name: "FK_RespuestaEncuesta_PreguntasEncuestas_PreguntaID",
                schema: "MET",
                table: "RespuestaEncuesta");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PreguntasEncuestas",
                table: "PreguntasEncuestas");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Encuestas",
                table: "Encuestas");

            migrationBuilder.RenameTable(
                name: "PreguntasEncuestas",
                newName: "PreguntaEncuesta",
                newSchema: "MET");

            migrationBuilder.RenameTable(
                name: "Encuestas",
                newName: "Encuesta",
                newSchema: "MET");

            migrationBuilder.RenameIndex(
                name: "IX_PreguntasEncuestas_EncuestaID",
                schema: "MET",
                table: "PreguntaEncuesta",
                newName: "IX_PreguntaEncuesta_EncuestaID");

            migrationBuilder.RenameIndex(
                name: "IX_Encuestas_EmpresaID",
                schema: "MET",
                table: "Encuesta",
                newName: "IX_Encuesta_EmpresaID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PreguntaEncuesta",
                schema: "MET",
                table: "PreguntaEncuesta",
                column: "PreguntaID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Encuesta",
                schema: "MET",
                table: "Encuesta",
                column: "EncuestaID");

            migrationBuilder.AddForeignKey(
                name: "FK_Encuesta_EmpresaCliente_EmpresaID",
                schema: "MET",
                table: "Encuesta",
                column: "EmpresaID",
                principalSchema: "MET",
                principalTable: "EmpresaCliente",
                principalColumn: "EmpresaID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PreguntaEncuesta_Encuesta_EncuestaID",
                schema: "MET",
                table: "PreguntaEncuesta",
                column: "EncuestaID",
                principalSchema: "MET",
                principalTable: "Encuesta",
                principalColumn: "EncuestaID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RespuestaEncuesta_PreguntaEncuesta_PreguntaID",
                schema: "MET",
                table: "RespuestaEncuesta",
                column: "PreguntaID",
                principalSchema: "MET",
                principalTable: "PreguntaEncuesta",
                principalColumn: "PreguntaID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Encuesta_EmpresaCliente_EmpresaID",
                schema: "MET",
                table: "Encuesta");

            migrationBuilder.DropForeignKey(
                name: "FK_PreguntaEncuesta_Encuesta_EncuestaID",
                schema: "MET",
                table: "PreguntaEncuesta");

            migrationBuilder.DropForeignKey(
                name: "FK_RespuestaEncuesta_PreguntaEncuesta_PreguntaID",
                schema: "MET",
                table: "RespuestaEncuesta");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PreguntaEncuesta",
                schema: "MET",
                table: "PreguntaEncuesta");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Encuesta",
                schema: "MET",
                table: "Encuesta");

            migrationBuilder.RenameTable(
                name: "PreguntaEncuesta",
                schema: "MET",
                newName: "PreguntasEncuestas");

            migrationBuilder.RenameTable(
                name: "Encuesta",
                schema: "MET",
                newName: "Encuestas");

            migrationBuilder.RenameIndex(
                name: "IX_PreguntaEncuesta_EncuestaID",
                table: "PreguntasEncuestas",
                newName: "IX_PreguntasEncuestas_EncuestaID");

            migrationBuilder.RenameIndex(
                name: "IX_Encuesta_EmpresaID",
                table: "Encuestas",
                newName: "IX_Encuestas_EmpresaID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PreguntasEncuestas",
                table: "PreguntasEncuestas",
                column: "PreguntaID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Encuestas",
                table: "Encuestas",
                column: "EncuestaID");

            migrationBuilder.AddForeignKey(
                name: "FK_Encuestas_EmpresaCliente_EmpresaID",
                table: "Encuestas",
                column: "EmpresaID",
                principalSchema: "MET",
                principalTable: "EmpresaCliente",
                principalColumn: "EmpresaID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PreguntasEncuestas_Encuestas_EncuestaID",
                table: "PreguntasEncuestas",
                column: "EncuestaID",
                principalTable: "Encuestas",
                principalColumn: "EncuestaID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RespuestaEncuesta_PreguntasEncuestas_PreguntaID",
                schema: "MET",
                table: "RespuestaEncuesta",
                column: "PreguntaID",
                principalTable: "PreguntasEncuestas",
                principalColumn: "PreguntaID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
