using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssistVet.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class RelatorioDoAtendimento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AtendimentoId",
                table: "Relatorios",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Relatorios_AtendimentoId",
                table: "Relatorios",
                column: "AtendimentoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Relatorios_Atendimentos_AtendimentoId",
                table: "Relatorios",
                column: "AtendimentoId",
                principalTable: "Atendimentos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Relatorios_Atendimentos_AtendimentoId",
                table: "Relatorios");

            migrationBuilder.DropIndex(
                name: "IX_Relatorios_AtendimentoId",
                table: "Relatorios");

            migrationBuilder.DropColumn(
                name: "AtendimentoId",
                table: "Relatorios");
        }
    }
}
