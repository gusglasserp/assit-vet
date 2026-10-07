using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CavaniVets.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class ContaAzul : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContaAzulId",
                table: "Tutores",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContaAzulServicoId",
                table: "ItensPreco",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ContaAzulConexoes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    AccessToken = table.Column<string>(type: "text", nullable: false),
                    RefreshToken = table.Column<string>(type: "text", nullable: false),
                    AccessTokenExpiraEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContaAzulConexoes", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "ItensPreco",
                keyColumn: "Id",
                keyValue: 1,
                column: "ContaAzulServicoId",
                value: null);

            migrationBuilder.UpdateData(
                table: "ItensPreco",
                keyColumn: "Id",
                keyValue: 2,
                column: "ContaAzulServicoId",
                value: null);

            migrationBuilder.UpdateData(
                table: "ItensPreco",
                keyColumn: "Id",
                keyValue: 3,
                column: "ContaAzulServicoId",
                value: null);

            migrationBuilder.UpdateData(
                table: "ItensPreco",
                keyColumn: "Id",
                keyValue: 4,
                column: "ContaAzulServicoId",
                value: null);

            migrationBuilder.UpdateData(
                table: "ItensPreco",
                keyColumn: "Id",
                keyValue: 5,
                column: "ContaAzulServicoId",
                value: null);

            migrationBuilder.UpdateData(
                table: "ItensPreco",
                keyColumn: "Id",
                keyValue: 6,
                column: "ContaAzulServicoId",
                value: null);

            migrationBuilder.UpdateData(
                table: "ItensPreco",
                keyColumn: "Id",
                keyValue: 7,
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContaAzulConexoes");

            migrationBuilder.DropColumn(
                name: "ContaAzulId",
                table: "Tutores");

            migrationBuilder.DropColumn(
                name: "ContaAzulServicoId",
                table: "ItensPreco");
        }
    }
}
