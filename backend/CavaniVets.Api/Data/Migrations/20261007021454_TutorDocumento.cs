using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CavaniVets.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class TutorDocumento : Migration
    {
        // Ajustada à mão: o EF gerou drop + add da coluna, o que apagaria os CPFs já cadastrados.

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Cpf",
                table: "Tutores",
                newName: "Documento");

            migrationBuilder.RenameIndex(
                name: "IX_Tutores_Cpf",
                table: "Tutores",
                newName: "IX_Tutores_Documento");

            migrationBuilder.AlterColumn<string>(
                name: "Documento",
                table: "Tutores",
                type: "character varying(14)",
                maxLength: 14,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(11)",
                oldMaxLength: 11);

            // Todos os cadastros anteriores eram por CPF.
            migrationBuilder.AddColumn<string>(
                name: "TipoPessoa",
                table: "Tutores",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Fisica");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TipoPessoa",
                table: "Tutores");

            migrationBuilder.AlterColumn<string>(
                name: "Documento",
                table: "Tutores",
                type: "character varying(11)",
                maxLength: 11,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(14)",
                oldMaxLength: 14);

            migrationBuilder.RenameIndex(
                name: "IX_Tutores_Documento",
                table: "Tutores",
                newName: "IX_Tutores_Cpf");

            migrationBuilder.RenameColumn(
                name: "Documento",
                table: "Tutores",
                newName: "Cpf");
        }
    }
}
