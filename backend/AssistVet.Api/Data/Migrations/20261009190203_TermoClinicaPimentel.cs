using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssistVet.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class TermoClinicaPimentel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "VersoesTermo",
                keyColumn: "Id",
                keyValue: 2,
                column: "Ativa",
                value: false);

            migrationBuilder.InsertData(
                table: "VersoesTermo",
                columns: new[] { "Id", "Ativa", "Texto", "Versao", "VigenteDesde" },
                values: new object[] { 3, true, "1. Autorizo a M.V. Juliane Cavani Pimentel (CRMV-SP 11.064), da Clínica Pimentel Vets, a realizar consulta oftalmológica no animal {animal}, a pedido do médico-veterinário solicitante.\n2. Declaro estar ciente de que a consulta inicial custa R$ 800,00, com medicamentos para diagnóstico incluídos, acrescida das despesas de deslocamento e dos materiais estéreis para diagnóstico, se utilizados. O deslocamento custa R$ 2,50 por km rodado, contado a partir da Rua Arandu, 885, Brooklin Paulista, São Paulo, mais pedágio; quando a rota for compartilhada com outros atendimentos, o deslocamento poderá ser dividido entre eles.\n3. Estou ciente da tabela de valores de exames, infiltrações e acompanhamento apresentada nesta página, e de que procedimentos adicionais, medicamentos de tratamento e cirurgias serão informados antes de sua realização.\n4. Assumo a responsabilidade pelo pagamento dos valores referentes ao atendimento do animal, nas condições informadas pela clínica.\n5. Estou ciente de que a medicina veterinária não é uma ciência exata e de que a resposta ao diagnóstico e ao tratamento varia de acordo com cada paciente.\n6. Autorizo o uso dos meus dados pessoais para cadastro, faturamento, emissão de documentos fiscais e comunicação sobre o atendimento, incluindo o compartilhamento com o veterinário solicitante e, quando necessário, com hospitais parceiros, conforme a Lei Geral de Proteção de Dados (Lei 13.709/2018).\n7. Esta autorização é registrada eletronicamente com data, hora e dados do dispositivo utilizado, e vale como minha assinatura.", "2026.3-rascunho", new DateTimeOffset(new DateTime(2026, 10, 9, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "VersoesTermo",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.UpdateData(
                table: "VersoesTermo",
                keyColumn: "Id",
                keyValue: 2,
                column: "Ativa",
                value: true);
        }
    }
}
