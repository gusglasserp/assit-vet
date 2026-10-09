using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssistVet.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class NumerosContaAzul : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "ContaAzulOrcamentoNumero",
                table: "Solicitacoes",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ContaAzulVendaNumero",
                table: "Solicitacoes",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContaAzulOrcamentoNumero",
                table: "Solicitacoes");

            migrationBuilder.DropColumn(
                name: "ContaAzulVendaNumero",
                table: "Solicitacoes");
        }
    }
}
