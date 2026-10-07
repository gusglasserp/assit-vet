using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CavaniVets.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class SolicitacaoTutorAnimal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AnimalId",
                table: "Solicitacoes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TutorId",
                table: "Solicitacoes",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Solicitacoes_AnimalId",
                table: "Solicitacoes",
                column: "AnimalId");

            migrationBuilder.CreateIndex(
                name: "IX_Solicitacoes_TutorId",
                table: "Solicitacoes",
                column: "TutorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Solicitacoes_Animais_AnimalId",
                table: "Solicitacoes",
                column: "AnimalId",
                principalTable: "Animais",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Solicitacoes_Tutores_TutorId",
                table: "Solicitacoes",
                column: "TutorId",
                principalTable: "Tutores",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Solicitacoes_Animais_AnimalId",
                table: "Solicitacoes");

            migrationBuilder.DropForeignKey(
                name: "FK_Solicitacoes_Tutores_TutorId",
                table: "Solicitacoes");

            migrationBuilder.DropIndex(
                name: "IX_Solicitacoes_AnimalId",
                table: "Solicitacoes");

            migrationBuilder.DropIndex(
                name: "IX_Solicitacoes_TutorId",
                table: "Solicitacoes");

            migrationBuilder.DropColumn(
                name: "AnimalId",
                table: "Solicitacoes");

            migrationBuilder.DropColumn(
                name: "TutorId",
                table: "Solicitacoes");
        }
    }
}
