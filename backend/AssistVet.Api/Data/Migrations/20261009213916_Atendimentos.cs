using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssistVet.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Atendimentos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Atendimentos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SolicitacaoId = table.Column<int>(type: "int", nullable: false),
                    Numero = table.Column<int>(type: "int", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MarcadoPara = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ContaAzulOrcamentoId = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    ContaAzulOrcamentoNumero = table.Column<long>(type: "bigint", nullable: true),
                    ContaAzulVendaId = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    ContaAzulVendaNumero = table.Column<long>(type: "bigint", nullable: true),
                    ConcluidoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ValorFinal = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    PagamentoVencimento = table.Column<DateOnly>(type: "date", nullable: true),
                    PagamentoForma = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    CriadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Atendimentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Atendimentos_Solicitacoes_SolicitacaoId",
                        column: x => x.SolicitacaoId,
                        principalTable: "Solicitacoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "ItensPreco",
                keyColumn: "Id",
                keyValue: 5,
                column: "ContaAzulServicoId",
                value: "c009a3ea-951c-4aa6-bcc3-c532c4bf73a0");

            migrationBuilder.UpdateData(
                table: "ItensPreco",
                keyColumn: "Id",
                keyValue: 8,
                column: "ContaAzulServicoId",
                value: "420f0ea1-48fb-49ce-a32f-a237aa9bfb60");

            migrationBuilder.UpdateData(
                table: "ItensPreco",
                keyColumn: "Id",
                keyValue: 9,
                column: "ContaAzulServicoId",
                value: "c479be16-5a0c-496a-bf31-dedc89829723");

            migrationBuilder.UpdateData(
                table: "ItensPreco",
                keyColumn: "Id",
                keyValue: 10,
                column: "ContaAzulServicoId",
                value: "dec4b351-ddc8-491f-bd3d-0234067e1799");

            migrationBuilder.UpdateData(
                table: "ItensPreco",
                keyColumn: "Id",
                keyValue: 11,
                column: "ContaAzulServicoId",
                value: "4eeb1f4a-d258-4035-88fa-dea602b45c27");

            migrationBuilder.CreateIndex(
                name: "IX_Atendimentos_SolicitacaoId_Numero",
                table: "Atendimentos",
                columns: new[] { "SolicitacaoId", "Numero" },
                unique: true);

            // Cada solicitação com orçamento, data marcada ou venda vira o atendimento nº 1 (a consulta),
            // levando esses dados. Só depois as colunas antigas são apagadas.
            migrationBuilder.Sql("""
                INSERT INTO Atendimentos (SolicitacaoId, Numero, Tipo, MarcadoPara, ContaAzulOrcamentoId, ContaAzulOrcamentoNumero,
                    ContaAzulVendaId, ContaAzulVendaNumero, ConcluidoEm, ValorFinal, PagamentoVencimento, PagamentoForma, CriadoEm)
                SELECT Id, 1, 'Consulta', AtendimentoMarcadoPara, ContaAzulOrcamentoId, ContaAzulOrcamentoNumero,
                    ContaAzulVendaId, ContaAzulVendaNumero, AtendidoEm, ValorFinal, PagamentoVencimento, PagamentoForma,
                    COALESCE(AtendidoEm, SYSDATETIMEOFFSET())
                FROM Solicitacoes
                WHERE ContaAzulOrcamentoId IS NOT NULL OR ContaAzulVendaId IS NOT NULL OR AtendimentoMarcadoPara IS NOT NULL
                """);

            migrationBuilder.DropColumn(
                name: "AtendidoEm",
                table: "Solicitacoes");

            migrationBuilder.DropColumn(
                name: "AtendimentoMarcadoPara",
                table: "Solicitacoes");

            migrationBuilder.DropColumn(
                name: "ContaAzulOrcamentoId",
                table: "Solicitacoes");

            migrationBuilder.DropColumn(
                name: "ContaAzulOrcamentoNumero",
                table: "Solicitacoes");

            migrationBuilder.DropColumn(
                name: "ContaAzulVendaId",
                table: "Solicitacoes");

            migrationBuilder.DropColumn(
                name: "ContaAzulVendaNumero",
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Atendimentos");

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
                name: "ContaAzulOrcamentoId",
                table: "Solicitacoes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ContaAzulOrcamentoNumero",
                table: "Solicitacoes",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContaAzulVendaId",
                table: "Solicitacoes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ContaAzulVendaNumero",
                table: "Solicitacoes",
                type: "bigint",
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

            migrationBuilder.UpdateData(
                table: "ItensPreco",
                keyColumn: "Id",
                keyValue: 5,
                column: "ContaAzulServicoId",
                value: null);

            migrationBuilder.UpdateData(
                table: "ItensPreco",
                keyColumn: "Id",
                keyValue: 8,
                column: "ContaAzulServicoId",
                value: null);

            migrationBuilder.UpdateData(
                table: "ItensPreco",
                keyColumn: "Id",
                keyValue: 9,
                column: "ContaAzulServicoId",
                value: null);

            migrationBuilder.UpdateData(
                table: "ItensPreco",
                keyColumn: "Id",
                keyValue: 10,
                column: "ContaAzulServicoId",
                value: null);

            migrationBuilder.UpdateData(
                table: "ItensPreco",
                keyColumn: "Id",
                keyValue: 11,
                column: "ContaAzulServicoId",
                value: null);
        }
    }
}
