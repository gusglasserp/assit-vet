using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CavaniVets.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class DeslocamentoEHipicas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "ItensPreco",
                keyColumn: "Id",
                keyValue: 2,
                column: "Observacao",
                value: "por km rodado, saindo da Rua Arandu, 885 (Brooklin Paulista), + pedágio. Se a rota for compartilhada com outros atendimentos, o deslocamento pode ser dividido.");

            migrationBuilder.UpdateData(
                table: "VersoesTermo",
                keyColumn: "Id",
                keyValue: 1,
                column: "Ativa",
                value: false);

            migrationBuilder.InsertData(
                table: "VersoesTermo",
                columns: new[] { "Id", "Ativa", "Texto", "Versao", "VigenteDesde" },
                values: new object[] { 2, true, "1. Autorizo a M.V. Juliane Cavani Pimentel (CRMV-SP 11.064), da Clínica Cavani Vets, a realizar consulta oftalmológica no animal {animal}, a pedido do médico-veterinário solicitante.\n2. Declaro estar ciente de que a consulta inicial custa R$ 800,00, com medicamentos para diagnóstico incluídos, acrescida das despesas de deslocamento e dos materiais estéreis para diagnóstico, se utilizados. O deslocamento custa R$ 2,50 por km rodado, contado a partir da Rua Arandu, 885, Brooklin Paulista, São Paulo, mais pedágio; quando a rota for compartilhada com outros atendimentos, o deslocamento poderá ser dividido entre eles.\n3. Estou ciente da tabela de valores de exames, infiltrações e acompanhamento apresentada nesta página, e de que procedimentos adicionais, medicamentos de tratamento e cirurgias serão informados antes de sua realização.\n4. Assumo a responsabilidade pelo pagamento dos valores referentes ao atendimento do animal, nas condições informadas pela clínica.\n5. Estou ciente de que a medicina veterinária não é uma ciência exata e de que a resposta ao diagnóstico e ao tratamento varia de acordo com cada paciente.\n6. Autorizo o uso dos meus dados pessoais para cadastro, faturamento, emissão de documentos fiscais e comunicação sobre o atendimento, incluindo o compartilhamento com o veterinário solicitante e, quando necessário, com hospitais parceiros, conforme a Lei Geral de Proteção de Dados (Lei 13.709/2018).\n7. Esta autorização é registrada eletronicamente com data, hora e dados do dispositivo utilizado, e vale como minha assinatura.", "2026.2-rascunho", new DateTimeOffset(new DateTime(2026, 10, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) });

            // Cadastro inicial das hípicas de São Paulo e Campinas (escrito à mão; locais não são seed do modelo,
            // a clínica e os veterinários cadastram outros pela página). Não duplica se já existirem.
            foreach (var (nome, cidade) in Hipicas)
                migrationBuilder.Sql($"""
                    INSERT INTO "Locais" ("Nome", "Tipo", "Cidade", "CriadoEm")
                    SELECT '{nome}', 'Hipica', '{cidade}', now()
                    WHERE NOT EXISTS (SELECT 1 FROM "Locais" WHERE "Nome" = '{nome}');
                    """);
        }

        static readonly (string Nome, string Cidade)[] Hipicas =
        [
            ("Sociedade Hípica Paulista", "São Paulo"),
            ("Clube Hípico de Santo Amaro", "São Paulo"),
            ("Sociedade Hípica de Campinas", "Campinas"),
        ];

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Só remove as hípicas que nenhuma solicitação usa.
            foreach (var (nome, _) in Hipicas)
                migrationBuilder.Sql($"""
                    DELETE FROM "Locais" l WHERE l."Nome" = '{nome}'
                      AND NOT EXISTS (SELECT 1 FROM "Solicitacoes" s WHERE s."LocalId" = l."Id");
                    """);

            migrationBuilder.DeleteData(
                table: "VersoesTermo",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.UpdateData(
                table: "ItensPreco",
                keyColumn: "Id",
                keyValue: 2,
                column: "Observacao",
                value: "por km rodado + pedágio");

            migrationBuilder.UpdateData(
                table: "VersoesTermo",
                keyColumn: "Id",
                keyValue: 1,
                column: "Ativa",
                value: true);
        }
    }
}
