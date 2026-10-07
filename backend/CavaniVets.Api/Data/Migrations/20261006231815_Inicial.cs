using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CavaniVets.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ItensPreco",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<string>(type: "text", nullable: false),
                    Grupo = table.Column<string>(type: "text", nullable: false),
                    Descricao = table.Column<string>(type: "text", nullable: false),
                    Valor = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    Observacao = table.Column<string>(type: "text", nullable: true),
                    Ordem = table.Column<int>(type: "integer", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItensPreco", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Locais",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    Tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Cidade = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Locais", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tutores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    Cpf = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: false),
                    Celular = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Cep = table.Column<string>(type: "text", nullable: false),
                    Uf = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    Cidade = table.Column<string>(type: "text", nullable: false),
                    Rua = table.Column<string>(type: "text", nullable: false),
                    Bairro = table.Column<string>(type: "text", nullable: false),
                    Numero = table.Column<string>(type: "text", nullable: false),
                    Complemento = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tutores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VersoesTermo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Versao = table.Column<string>(type: "text", nullable: false),
                    Texto = table.Column<string>(type: "text", nullable: false),
                    VigenteDesde = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Ativa = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VersoesTermo", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Veterinarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Celular = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: false),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    Crmv = table.Column<string>(type: "text", nullable: false),
                    Uf = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Veterinarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Animais",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TutorId = table.Column<int>(type: "integer", nullable: false),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    Especie = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Sexo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Raca = table.Column<string>(type: "text", nullable: true),
                    Idade = table.Column<string>(type: "text", nullable: true),
                    Peso = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Animais", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Animais_Tutores_TutorId",
                        column: x => x.TutorId,
                        principalTable: "Tutores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Solicitacoes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Protocolo = table.Column<string>(type: "text", nullable: false),
                    TokenTutor = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PreenchidoPor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    VeterinarioId = table.Column<int>(type: "integer", nullable: false),
                    LocalId = table.Column<int>(type: "integer", nullable: true),
                    AudioArquivo = table.Column<string>(type: "text", nullable: true),
                    AudioDuracaoSegundos = table.Column<int>(type: "integer", nullable: true),
                    PetNome = table.Column<string>(type: "text", nullable: true),
                    Especie = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Raca = table.Column<string>(type: "text", nullable: true),
                    Idade = table.Column<string>(type: "text", nullable: true),
                    Olho = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Prioridade = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    QueixaHistorico = table.Column<string>(type: "text", nullable: true),
                    Medicacoes = table.Column<string>(type: "text", nullable: true),
                    TratadorNome = table.Column<string>(type: "text", nullable: true),
                    TratadorCelular = table.Column<string>(type: "text", nullable: true),
                    TutorNome = table.Column<string>(type: "text", nullable: true),
                    TutorCelular = table.Column<string>(type: "text", nullable: true),
                    TutorCienteCusto = table.Column<bool>(type: "boolean", nullable: true),
                    VeterinarioRecebeRelatorios = table.Column<bool>(type: "boolean", nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Solicitacoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Solicitacoes_Locais_LocalId",
                        column: x => x.LocalId,
                        principalTable: "Locais",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Solicitacoes_Veterinarios_VeterinarioId",
                        column: x => x.VeterinarioId,
                        principalTable: "Veterinarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Autorizacoes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SolicitacaoId = table.Column<int>(type: "integer", nullable: false),
                    TutorId = table.Column<int>(type: "integer", nullable: false),
                    AnimalId = table.Column<int>(type: "integer", nullable: false),
                    VersaoTermoId = table.Column<int>(type: "integer", nullable: false),
                    TextoTermoAceito = table.Column<string>(type: "text", nullable: false),
                    ValoresExibidosJson = table.Column<string>(type: "text", nullable: false),
                    AceitouTermo = table.Column<bool>(type: "boolean", nullable: false),
                    AceitouResponsabilidadeFinanceira = table.Column<bool>(type: "boolean", nullable: false),
                    AceitouLgpd = table.Column<bool>(type: "boolean", nullable: false),
                    NomeAssinado = table.Column<string>(type: "text", nullable: false),
                    Ip = table.Column<string>(type: "text", nullable: false),
                    UserAgent = table.Column<string>(type: "text", nullable: false),
                    AceitoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Autorizacoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Autorizacoes_Animais_AnimalId",
                        column: x => x.AnimalId,
                        principalTable: "Animais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Autorizacoes_Solicitacoes_SolicitacaoId",
                        column: x => x.SolicitacaoId,
                        principalTable: "Solicitacoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Autorizacoes_Tutores_TutorId",
                        column: x => x.TutorId,
                        principalTable: "Tutores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Autorizacoes_VersoesTermo_VersaoTermoId",
                        column: x => x.VersaoTermoId,
                        principalTable: "VersoesTermo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "ItensPreco",
                columns: new[] { "Id", "Ativo", "Codigo", "Descricao", "Grupo", "Observacao", "Ordem", "Valor" },
                values: new object[,]
                {
                    { 1, true, "consulta", "Consulta oftalmológica inicial", "Agora", "Medicamentos para diagnóstico incluídos", 1, 800m },
                    { 2, true, "km", "Deslocamento", "Agora", "por km rodado + pedágio", 2, 2.50m },
                    { 3, true, "materiais-diagnostico", "Materiais estéreis para diagnóstico", "Agora", "cobrados à parte, se usados", 3, null },
                    { 4, true, "ultrassom", "Exame ultrassonográfico oftalmológico", "Indicados", null, 10, 550m },
                    { 5, true, "acompanhamento", "Acompanhamento oftálmico, até a alta clínica", "Indicados", "por visita", 11, 300m },
                    { 6, true, "medicamentos-tratamento", "Medicamentos e materiais para tratamento", "Indicados", "à parte", 12, null },
                    { 7, true, "cirurgia", "Procedimentos cirúrgicos", "Indicados", "sob orçamento", 13, null },
                    { 8, true, "inf-subconjuntival", "Subconjuntival", "Infiltrações", null, 20, 350m },
                    { 9, true, "inf-retrobulbar", "Retrobulbar", "Infiltrações", null, 21, 450m },
                    { 10, true, "inf-intravitrea", "Intravítrea", "Infiltrações", null, 22, 700m },
                    { 11, true, "inf-intralesional", "Intralesional", "Infiltrações", null, 23, 750m }
                });

            migrationBuilder.InsertData(
                table: "VersoesTermo",
                columns: new[] { "Id", "Ativa", "Texto", "Versao", "VigenteDesde" },
                values: new object[] { 1, true, "1. Autorizo a M.V. Juliane Cavani Pimentel (CRMV-SP 11.064), da Clínica Cavani Vets, a realizar consulta oftalmológica no animal {animal}, a pedido do médico-veterinário solicitante.\n2. Declaro estar ciente de que a consulta inicial custa R$ 800,00, com medicamentos para diagnóstico incluídos, acrescida das despesas de deslocamento (R$ 2,50 por km rodado + pedágio) e dos materiais estéreis para diagnóstico, se utilizados.\n3. Estou ciente da tabela de valores de exames, infiltrações e acompanhamento apresentada nesta página, e de que procedimentos adicionais, medicamentos de tratamento e cirurgias serão informados antes de sua realização.\n4. Assumo a responsabilidade pelo pagamento dos valores referentes ao atendimento do animal, nas condições informadas pela clínica.\n5. Estou ciente de que a medicina veterinária não é uma ciência exata e de que a resposta ao diagnóstico e ao tratamento varia de acordo com cada paciente.\n6. Autorizo o uso dos meus dados pessoais para cadastro, faturamento, emissão de documentos fiscais e comunicação sobre o atendimento, incluindo o compartilhamento com o veterinário solicitante e, quando necessário, com hospitais parceiros, conforme a Lei Geral de Proteção de Dados (Lei 13.709/2018).\n7. Esta autorização é registrada eletronicamente com data, hora e dados do dispositivo utilizado, e vale como minha assinatura.", "2026.1-rascunho", new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.CreateIndex(
                name: "IX_Animais_TutorId",
                table: "Animais",
                column: "TutorId");

            migrationBuilder.CreateIndex(
                name: "IX_Autorizacoes_AnimalId",
                table: "Autorizacoes",
                column: "AnimalId");

            migrationBuilder.CreateIndex(
                name: "IX_Autorizacoes_SolicitacaoId",
                table: "Autorizacoes",
                column: "SolicitacaoId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Autorizacoes_TutorId",
                table: "Autorizacoes",
                column: "TutorId");

            migrationBuilder.CreateIndex(
                name: "IX_Autorizacoes_VersaoTermoId",
                table: "Autorizacoes",
                column: "VersaoTermoId");

            migrationBuilder.CreateIndex(
                name: "IX_ItensPreco_Codigo",
                table: "ItensPreco",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Solicitacoes_LocalId",
                table: "Solicitacoes",
                column: "LocalId");

            migrationBuilder.CreateIndex(
                name: "IX_Solicitacoes_Protocolo",
                table: "Solicitacoes",
                column: "Protocolo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Solicitacoes_TokenTutor",
                table: "Solicitacoes",
                column: "TokenTutor",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Solicitacoes_VeterinarioId",
                table: "Solicitacoes",
                column: "VeterinarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Tutores_Cpf",
                table: "Tutores",
                column: "Cpf",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VersoesTermo_Versao",
                table: "VersoesTermo",
                column: "Versao",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Veterinarios_Celular",
                table: "Veterinarios",
                column: "Celular",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Autorizacoes");

            migrationBuilder.DropTable(
                name: "ItensPreco");

            migrationBuilder.DropTable(
                name: "Animais");

            migrationBuilder.DropTable(
                name: "Solicitacoes");

            migrationBuilder.DropTable(
                name: "VersoesTermo");

            migrationBuilder.DropTable(
                name: "Tutores");

            migrationBuilder.DropTable(
                name: "Locais");

            migrationBuilder.DropTable(
                name: "Veterinarios");
        }
    }
}
