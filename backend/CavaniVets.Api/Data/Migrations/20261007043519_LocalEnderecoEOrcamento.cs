using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CavaniVets.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class LocalEnderecoEOrcamento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContaAzulOrcamentoId",
                table: "Solicitacoes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Bairro",
                table: "Locais",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Cep",
                table: "Locais",
                type: "character varying(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Complemento",
                table: "Locais",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DistanciaCalculadaEm",
                table: "Locais",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DistanciaKmIda",
                table: "Locais",
                type: "numeric(8,1)",
                precision: 8,
                scale: 1,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GooglePlaceId",
                table: "Locais",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Locais",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Locais",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Numero",
                table: "Locais",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PedagioIda",
                table: "Locais",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Referencia",
                table: "Locais",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Rua",
                table: "Locais",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Uf",
                table: "Locais",
                type: "character varying(2)",
                maxLength: 2,
                nullable: true);

            // Endereço das hípicas cadastradas na migração anterior (CEPs conferidos no ViaCEP).
            migrationBuilder.Sql("""
                UPDATE "Locais" SET "Rua" = 'Rua Quintana', "Numero" = '206', "Bairro" = 'Cidade Monções', "Cidade" = 'São Paulo', "Uf" = 'SP', "Cep" = '04569010'
                WHERE "Nome" = 'Sociedade Hípica Paulista' AND "Rua" IS NULL;
                UPDATE "Locais" SET "Rua" = 'Rua Visconde de Taunay', "Numero" = '508', "Bairro" = 'Vila Cruzeiro', "Cidade" = 'São Paulo', "Uf" = 'SP', "Cep" = '04726010'
                WHERE "Nome" = 'Clube Hípico de Santo Amaro' AND "Rua" IS NULL;
                UPDATE "Locais" SET "Rua" = 'Rua Buriti', "Bairro" = 'Bairro das Palmeiras', "Cidade" = 'Campinas', "Uf" = 'SP', "Cep" = '13092566'
                WHERE "Nome" = 'Sociedade Hípica de Campinas' AND "Rua" IS NULL;
                """);

            // Serviços do Conta Azul da clínica usados no orçamento automático:
            // "CONSULTA OFTALMOLÓGICA" e "DESPESAS COM KM". Não sobrescreve se já tiver sido ligado.
            migrationBuilder.Sql("""
                UPDATE "ItensPreco" SET "ContaAzulServicoId" = 'fac27e62-a5ef-4c50-bee3-74f5ba9b0137'
                WHERE "Codigo" = 'consulta' AND "ContaAzulServicoId" IS NULL;
                UPDATE "ItensPreco" SET "ContaAzulServicoId" = '29b9050a-700c-43bf-bbb7-c13cd5f374eb'
                WHERE "Codigo" = 'km' AND "ContaAzulServicoId" IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContaAzulOrcamentoId",
                table: "Solicitacoes");

            migrationBuilder.DropColumn(
                name: "Bairro",
                table: "Locais");

            migrationBuilder.DropColumn(
                name: "Cep",
                table: "Locais");

            migrationBuilder.DropColumn(
                name: "Complemento",
                table: "Locais");

            migrationBuilder.DropColumn(
                name: "DistanciaCalculadaEm",
                table: "Locais");

            migrationBuilder.DropColumn(
                name: "DistanciaKmIda",
                table: "Locais");

            migrationBuilder.DropColumn(
                name: "GooglePlaceId",
                table: "Locais");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Locais");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Locais");

            migrationBuilder.DropColumn(
                name: "Numero",
                table: "Locais");

            migrationBuilder.DropColumn(
                name: "PedagioIda",
                table: "Locais");

            migrationBuilder.DropColumn(
                name: "Referencia",
                table: "Locais");

            migrationBuilder.DropColumn(
                name: "Rua",
                table: "Locais");

            migrationBuilder.DropColumn(
                name: "Uf",
                table: "Locais");
        }
    }
}
