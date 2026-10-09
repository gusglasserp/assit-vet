using System;
using Microsoft.EntityFrameworkCore.Migrations;

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
                name: "ContaAzulConexoes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    AccessToken = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RefreshToken = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AccessTokenExpiraEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContaAzulConexoes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Convites",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Token = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false, collation: "Latin1_General_100_BIN2"),
                    Celular = table.Column<string>(type: "nvarchar(11)", maxLength: 11, nullable: false),
                    PreenchidoPor = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Convites", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ItensPreco",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Codigo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Grupo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    Observacao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Ordem = table.Column<int>(type: "int", nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false),
                    ContaAzulServicoId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItensPreco", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Locais",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Cep = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    Rua = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Numero = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Complemento = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Bairro = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Cidade = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Uf = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                    Referencia = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GooglePlaceId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Latitude = table.Column<double>(type: "float", nullable: true),
                    Longitude = table.Column<double>(type: "float", nullable: true),
                    DistanciaKmIda = table.Column<decimal>(type: "decimal(8,1)", precision: 8, scale: 1, nullable: true),
                    PedagioIda = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    DistanciaCalculadaEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CriadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Locais", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tutores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Documento = table.Column<string>(type: "nvarchar(14)", maxLength: 14, nullable: false),
                    TipoPessoa = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Celular = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Cep = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Uf = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    Cidade = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Rua = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Bairro = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Numero = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Complemento = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContaAzulId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CriadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tutores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VersoesTermo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Versao = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Texto = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VigenteDesde = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Ativa = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VersoesTermo", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Veterinarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Celular = table.Column<string>(type: "nvarchar(11)", maxLength: 11, nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Crmv = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Uf = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Veterinarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Animais",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TutorId = table.Column<int>(type: "int", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Especie = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Sexo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Raca = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Idade = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Peso = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Animais", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Animais_Tutores_TutorId",
                        column: x => x.TutorId,
                        principalTable: "Tutores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Solicitacoes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Protocolo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TokenTutor = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false, collation: "Latin1_General_100_BIN2"),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PreenchidoPor = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    VeterinarioId = table.Column<int>(type: "int", nullable: false),
                    LocalId = table.Column<int>(type: "int", nullable: true),
                    AudioArquivo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AudioDuracaoSegundos = table.Column<int>(type: "int", nullable: true),
                    PetNome = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Especie = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Raca = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Idade = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Olho = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Prioridade = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    QueixaHistorico = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Medicacoes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TratadorNome = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TratadorCelular = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TutorNome = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TutorCelular = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TutorCienteCusto = table.Column<bool>(type: "bit", nullable: true),
                    VeterinarioRecebeRelatorios = table.Column<bool>(type: "bit", nullable: false),
                    ContaAzulOrcamentoId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TutorId = table.Column<int>(type: "int", nullable: true),
                    AnimalId = table.Column<int>(type: "int", nullable: true),
                    CriadoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Solicitacoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Solicitacoes_Animais_AnimalId",
                        column: x => x.AnimalId,
                        principalTable: "Animais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Solicitacoes_Locais_LocalId",
                        column: x => x.LocalId,
                        principalTable: "Locais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Solicitacoes_Tutores_TutorId",
                        column: x => x.TutorId,
                        principalTable: "Tutores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Solicitacoes_Veterinarios_VeterinarioId",
                        column: x => x.VeterinarioId,
                        principalTable: "Veterinarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Autorizacoes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SolicitacaoId = table.Column<int>(type: "int", nullable: false),
                    TutorId = table.Column<int>(type: "int", nullable: false),
                    AnimalId = table.Column<int>(type: "int", nullable: false),
                    VersaoTermoId = table.Column<int>(type: "int", nullable: false),
                    TextoTermoAceito = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ValoresExibidosJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AceitouTermo = table.Column<bool>(type: "bit", nullable: false),
                    AceitouResponsabilidadeFinanceira = table.Column<bool>(type: "bit", nullable: false),
                    AceitouLgpd = table.Column<bool>(type: "bit", nullable: false),
                    NomeAssinado = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Ip = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserAgent = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AceitoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Autorizacoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Autorizacoes_Animais_AnimalId",
                        column: x => x.AnimalId,
                        principalTable: "Animais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Autorizacoes_Solicitacoes_SolicitacaoId",
                        column: x => x.SolicitacaoId,
                        principalTable: "Solicitacoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Autorizacoes_Tutores_TutorId",
                        column: x => x.TutorId,
                        principalTable: "Tutores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Autorizacoes_VersoesTermo_VersaoTermoId",
                        column: x => x.VersaoTermoId,
                        principalTable: "VersoesTermo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "ItensPreco",
                columns: new[] { "Id", "Ativo", "Codigo", "ContaAzulServicoId", "Descricao", "Grupo", "Observacao", "Ordem", "Valor" },
                values: new object[,]
                {
                    { 1, true, "consulta", "fac27e62-a5ef-4c50-bee3-74f5ba9b0137", "Consulta oftalmológica inicial", "Agora", "Medicamentos para diagnóstico incluídos", 1, 800m },
                    { 2, true, "km", "29b9050a-700c-43bf-bbb7-c13cd5f374eb", "Deslocamento", "Agora", "por km rodado, saindo da Rua Arandu, 885 (Brooklin Paulista), + pedágio. Se a rota for compartilhada com outros atendimentos, o deslocamento pode ser dividido.", 2, 2.50m },
                    { 3, true, "materiais-diagnostico", null, "Materiais estéreis para diagnóstico", "Agora", "cobrados à parte, se usados", 3, null },
                    { 4, true, "ultrassom", null, "Exame ultrassonográfico oftalmológico", "Indicados", null, 10, 550m },
                    { 5, true, "acompanhamento", null, "Acompanhamento oftálmico, até a alta clínica", "Indicados", "por visita", 11, 300m },
                    { 6, true, "medicamentos-tratamento", null, "Medicamentos e materiais para tratamento", "Indicados", "à parte", 12, null },
                    { 7, true, "cirurgia", null, "Procedimentos cirúrgicos", "Indicados", "sob orçamento", 13, null },
                    { 8, true, "inf-subconjuntival", null, "Subconjuntival", "Infiltrações", null, 20, 350m },
                    { 9, true, "inf-retrobulbar", null, "Retrobulbar", "Infiltrações", null, 21, 450m },
                    { 10, true, "inf-intravitrea", null, "Intravítrea", "Infiltrações", null, 22, 700m },
                    { 11, true, "inf-intralesional", null, "Intralesional", "Infiltrações", null, 23, 750m }
                });

            migrationBuilder.InsertData(
                table: "VersoesTermo",
                columns: new[] { "Id", "Ativa", "Texto", "Versao", "VigenteDesde" },
                values: new object[,]
                {
                    { 1, false, "1. Autorizo a M.V. Juliane Cavani Pimentel (CRMV-SP 11.064), da Clínica Cavani Vets, a realizar consulta oftalmológica no animal {animal}, a pedido do médico-veterinário solicitante.\n2. Declaro estar ciente de que a consulta inicial custa R$ 800,00, com medicamentos para diagnóstico incluídos, acrescida das despesas de deslocamento (R$ 2,50 por km rodado + pedágio) e dos materiais estéreis para diagnóstico, se utilizados.\n3. Estou ciente da tabela de valores de exames, infiltrações e acompanhamento apresentada nesta página, e de que procedimentos adicionais, medicamentos de tratamento e cirurgias serão informados antes de sua realização.\n4. Assumo a responsabilidade pelo pagamento dos valores referentes ao atendimento do animal, nas condições informadas pela clínica.\n5. Estou ciente de que a medicina veterinária não é uma ciência exata e de que a resposta ao diagnóstico e ao tratamento varia de acordo com cada paciente.\n6. Autorizo o uso dos meus dados pessoais para cadastro, faturamento, emissão de documentos fiscais e comunicação sobre o atendimento, incluindo o compartilhamento com o veterinário solicitante e, quando necessário, com hospitais parceiros, conforme a Lei Geral de Proteção de Dados (Lei 13.709/2018).\n7. Esta autorização é registrada eletronicamente com data, hora e dados do dispositivo utilizado, e vale como minha assinatura.", "2026.1-rascunho", new DateTimeOffset(new DateTime(2026, 10, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 2, true, "1. Autorizo a M.V. Juliane Cavani Pimentel (CRMV-SP 11.064), da Clínica Cavani Vets, a realizar consulta oftalmológica no animal {animal}, a pedido do médico-veterinário solicitante.\n2. Declaro estar ciente de que a consulta inicial custa R$ 800,00, com medicamentos para diagnóstico incluídos, acrescida das despesas de deslocamento e dos materiais estéreis para diagnóstico, se utilizados. O deslocamento custa R$ 2,50 por km rodado, contado a partir da Rua Arandu, 885, Brooklin Paulista, São Paulo, mais pedágio; quando a rota for compartilhada com outros atendimentos, o deslocamento poderá ser dividido entre eles.\n3. Estou ciente da tabela de valores de exames, infiltrações e acompanhamento apresentada nesta página, e de que procedimentos adicionais, medicamentos de tratamento e cirurgias serão informados antes de sua realização.\n4. Assumo a responsabilidade pelo pagamento dos valores referentes ao atendimento do animal, nas condições informadas pela clínica.\n5. Estou ciente de que a medicina veterinária não é uma ciência exata e de que a resposta ao diagnóstico e ao tratamento varia de acordo com cada paciente.\n6. Autorizo o uso dos meus dados pessoais para cadastro, faturamento, emissão de documentos fiscais e comunicação sobre o atendimento, incluindo o compartilhamento com o veterinário solicitante e, quando necessário, com hospitais parceiros, conforme a Lei Geral de Proteção de Dados (Lei 13.709/2018).\n7. Esta autorização é registrada eletronicamente com data, hora e dados do dispositivo utilizado, e vale como minha assinatura.", "2026.2-rascunho", new DateTimeOffset(new DateTime(2026, 10, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

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
                name: "IX_Convites_Token",
                table: "Convites",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItensPreco_Codigo",
                table: "ItensPreco",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Solicitacoes_AnimalId",
                table: "Solicitacoes",
                column: "AnimalId");

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
                name: "IX_Solicitacoes_TutorId",
                table: "Solicitacoes",
                column: "TutorId");

            migrationBuilder.CreateIndex(
                name: "IX_Solicitacoes_VeterinarioId",
                table: "Solicitacoes",
                column: "VeterinarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Tutores_Documento",
                table: "Tutores",
                column: "Documento",
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

            // Cadastro inicial das hípicas de São Paulo e Campinas, com endereço (CEPs conferidos no ViaCEP).
            // Escrito à mão: locais não são seed do modelo (a clínica e os veterinários cadastram outros).
            migrationBuilder.Sql("""
                INSERT INTO [Locais] ([Nome], [Tipo], [Rua], [Numero], [Bairro], [Cidade], [Uf], [Cep], [CriadoEm]) VALUES
                (N'Sociedade Hípica Paulista', N'Hipica', N'Rua Quintana', N'206', N'Cidade Monções', N'São Paulo', N'SP', N'04569010', SYSDATETIMEOFFSET()),
                (N'Clube Hípico de Santo Amaro', N'Hipica', N'Rua Visconde de Taunay', N'508', N'Vila Cruzeiro', N'São Paulo', N'SP', N'04726010', SYSDATETIMEOFFSET()),
                (N'Sociedade Hípica de Campinas', N'Hipica', N'Rua Buriti', NULL, N'Bairro das Palmeiras', N'Campinas', N'SP', N'13092566', SYSDATETIMEOFFSET());
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Autorizacoes");

            migrationBuilder.DropTable(
                name: "ContaAzulConexoes");

            migrationBuilder.DropTable(
                name: "Convites");

            migrationBuilder.DropTable(
                name: "ItensPreco");

            migrationBuilder.DropTable(
                name: "Solicitacoes");

            migrationBuilder.DropTable(
                name: "VersoesTermo");

            migrationBuilder.DropTable(
                name: "Animais");

            migrationBuilder.DropTable(
                name: "Locais");

            migrationBuilder.DropTable(
                name: "Veterinarios");

            migrationBuilder.DropTable(
                name: "Tutores");
        }
    }
}
