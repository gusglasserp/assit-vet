using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssistVet.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AreaTutorAtendimento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AtendidoEm",
                table: "Solicitacoes",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AtendimentoMarcadoPara",
                table: "Solicitacoes",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PagamentoForma",
                table: "Solicitacoes",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "PagamentoVencimento",
                table: "Solicitacoes",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorFinal",
                table: "Solicitacoes",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Relatorios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SolicitacaoId = table.Column<int>(type: "int", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false, collation: "Latin1_General_100_BIN2"),
                    Titulo = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Arquivo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TamanhoBytes = table.Column<long>(type: "bigint", nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Relatorios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Relatorios_Solicitacoes_SolicitacaoId",
                        column: x => x.SolicitacaoId,
                        principalTable: "Solicitacoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Relatorios_SolicitacaoId",
                table: "Relatorios",
                column: "SolicitacaoId");

            migrationBuilder.CreateIndex(
                name: "IX_Relatorios_Token",
                table: "Relatorios",
                column: "Token",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Relatorios");

            migrationBuilder.DropColumn(
                name: "AtendidoEm",
                table: "Solicitacoes");

            migrationBuilder.DropColumn(
                name: "AtendimentoMarcadoPara",
                table: "Solicitacoes");

            migrationBuilder.DropColumn(
                name: "PagamentoForma",
                table: "Solicitacoes");

            migrationBuilder.DropColumn(
                name: "PagamentoVencimento",
                table: "Solicitacoes");

            migrationBuilder.DropColumn(
                name: "ValorFinal",
                table: "Solicitacoes");
        }
    }
}
