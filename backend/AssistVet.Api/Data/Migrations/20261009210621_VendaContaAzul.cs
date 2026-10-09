using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssistVet.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class VendaContaAzul : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContaAzulVendaId",
                table: "Solicitacoes",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContaAzulVendaId",
                table: "Solicitacoes");
        }
    }
}
