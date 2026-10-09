using AssistVet.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace AssistVet.Api.Data;

public class AssistVetDbContext(DbContextOptions<AssistVetDbContext> options) : DbContext(options)
{
    public DbSet<Veterinario> Veterinarios => Set<Veterinario>();
    public DbSet<Local> Locais => Set<Local>();
    public DbSet<Solicitacao> Solicitacoes => Set<Solicitacao>();
    public DbSet<Tutor> Tutores => Set<Tutor>();
    public DbSet<Animal> Animais => Set<Animal>();
    public DbSet<VersaoTermo> VersoesTermo => Set<VersaoTermo>();
    public DbSet<ItemPreco> ItensPreco => Set<ItemPreco>();
    public DbSet<Autorizacao> Autorizacoes => Set<Autorizacao>();
    public DbSet<ContaAzulConexao> ContaAzulConexoes => Set<ContaAzulConexao>();
    public DbSet<Convite> Convites => Set<Convite>();

    protected override void ConfigureConventions(ModelConfigurationBuilder b)
    {
        // Enums gravados como texto: o banco fica legível e não quebra se a ordem mudar.
        b.Properties<Enum>().HaveConversion<string>().HaveMaxLength(20);
    }

    protected override void OnModelCreating(ModelBuilder m)
    {
        m.Entity<Veterinario>(e =>
        {
            e.HasIndex(x => x.Celular).IsUnique();
            e.Property(x => x.Celular).HasMaxLength(11);
            e.Property(x => x.Uf).HasMaxLength(2);
        });

        m.Entity<Solicitacao>(e =>
        {
            e.HasIndex(x => x.Protocolo).IsUnique();
            e.Property(x => x.Protocolo).HasMaxLength(20);
            e.HasIndex(x => x.TokenTutor).IsUnique();
            e.Property(x => x.TokenTutor).HasMaxLength(40).UseCollation(CollationExata);
            e.HasOne(x => x.Autorizacao).WithOne(x => x.Solicitacao).HasForeignKey<Autorizacao>(x => x.SolicitacaoId);
        });

        m.Entity<Tutor>(e =>
        {
            e.HasIndex(x => x.Documento).IsUnique();
            e.Property(x => x.Documento).HasMaxLength(14);
            e.Property(x => x.Uf).HasMaxLength(2);
        });

        m.Entity<ItemPreco>(e =>
        {
            e.HasIndex(x => x.Codigo).IsUnique();
            e.Property(x => x.Codigo).HasMaxLength(40);
            e.Property(x => x.Valor).HasPrecision(10, 2);
        });

        m.Entity<VersaoTermo>(e =>
        {
            e.HasIndex(x => x.Versao).IsUnique();
            e.Property(x => x.Versao).HasMaxLength(40);
        });

        m.Entity<ContaAzulConexao>().Property(x => x.Id).ValueGeneratedNever();

        m.Entity<Local>(e =>
        {
            e.Property(x => x.Cep).HasMaxLength(8);
            e.Property(x => x.Uf).HasMaxLength(2);
            e.Property(x => x.DistanciaKmIda).HasPrecision(8, 1);
            e.Property(x => x.PedagioIda).HasPrecision(10, 2);
        });

        m.Entity<Convite>(e =>
        {
            e.HasIndex(x => x.Token).IsUnique();
            e.Property(x => x.Token).HasMaxLength(40).UseCollation(CollationExata);
            e.Property(x => x.Celular).HasMaxLength(11);
        });

        // Nada é apagado em cascata: autorizações, termos e solicitações são prova e ficam guardados.
        // (O SQL Server também recusa os caminhos de cascata cruzados, ex.: Tutor → Animal → Autorização.)
        foreach (var fk in m.Model.GetEntityTypes().SelectMany(t => t.GetForeignKeys()))
            fk.DeleteBehavior = DeleteBehavior.Restrict;

        Seed(m);
    }

    /// <summary>
    /// Comparação exata (diferencia maiúsculas) para os códigos dos links: o padrão do SQL Server
    /// ignora maiúsculas, e os tokens usam as duas.
    /// </summary>
    const string CollationExata = "Latin1_General_100_BIN2";

    // Serviços do Conta Azul da clínica usados no orçamento automático: "CONSULTA OFTALMOLÓGICA" e "DESPESAS COM KM".
    static void Seed(ModelBuilder m)
    {
        m.Entity<ItemPreco>().HasData(
            new ItemPreco { Id = 1, Codigo = "consulta", Grupo = "Agora", Descricao = "Consulta oftalmológica inicial", Valor = 800m, Observacao = "Medicamentos para diagnóstico incluídos", Ordem = 1, ContaAzulServicoId = "fac27e62-a5ef-4c50-bee3-74f5ba9b0137" },
            new ItemPreco { Id = 2, Codigo = "km", Grupo = "Agora", Descricao = "Deslocamento", Valor = 2.50m, Observacao = "por km rodado, saindo da Rua Arandu, 885 (Brooklin Paulista), + pedágio. Se a rota for compartilhada com outros atendimentos, o deslocamento pode ser dividido.", Ordem = 2, ContaAzulServicoId = "29b9050a-700c-43bf-bbb7-c13cd5f374eb" },
            new ItemPreco { Id = 3, Codigo = "materiais-diagnostico", Grupo = "Agora", Descricao = "Materiais estéreis para diagnóstico", Observacao = "cobrados à parte, se usados", Ordem = 3 },
            new ItemPreco { Id = 4, Codigo = "ultrassom", Grupo = "Indicados", Descricao = "Exame ultrassonográfico oftalmológico", Valor = 550m, Ordem = 10 },
            new ItemPreco { Id = 5, Codigo = "acompanhamento", Grupo = "Indicados", Descricao = "Acompanhamento oftálmico, até a alta clínica", Valor = 300m, Observacao = "por visita", Ordem = 11 },
            new ItemPreco { Id = 6, Codigo = "medicamentos-tratamento", Grupo = "Indicados", Descricao = "Medicamentos e materiais para tratamento", Observacao = "à parte", Ordem = 12 },
            new ItemPreco { Id = 7, Codigo = "cirurgia", Grupo = "Indicados", Descricao = "Procedimentos cirúrgicos", Observacao = "sob orçamento", Ordem = 13 },
            new ItemPreco { Id = 8, Codigo = "inf-subconjuntival", Grupo = "Infiltrações", Descricao = "Subconjuntival", Valor = 350m, Ordem = 20 },
            new ItemPreco { Id = 9, Codigo = "inf-retrobulbar", Grupo = "Infiltrações", Descricao = "Retrobulbar", Valor = 450m, Ordem = 21 },
            new ItemPreco { Id = 10, Codigo = "inf-intravitrea", Grupo = "Infiltrações", Descricao = "Intravítrea", Valor = 700m, Ordem = 22 },
            new ItemPreco { Id = 11, Codigo = "inf-intralesional", Grupo = "Infiltrações", Descricao = "Intralesional", Valor = 750m, Ordem = 23 }
        );

        // Versões do termo nunca são editadas: cada mudança é uma nova versão, e as autorizações antigas
        // continuam apontando para o texto que foi aceito.
        m.Entity<VersaoTermo>().HasData(new VersaoTermo
        {
            Id = 1,
            Versao = "2026.1-rascunho",
            VigenteDesde = new DateTimeOffset(2026, 10, 6, 0, 0, 0, TimeSpan.Zero),
            Ativa = false,
            // {animal} é substituído pelo nome do animal na exibição e na cópia gravada no aceite.
            Texto = """
                1. Autorizo a M.V. Juliane Cavani Pimentel (CRMV-SP 11.064), da Clínica Cavani Vets, a realizar consulta oftalmológica no animal {animal}, a pedido do médico-veterinário solicitante.
                2. Declaro estar ciente de que a consulta inicial custa R$ 800,00, com medicamentos para diagnóstico incluídos, acrescida das despesas de deslocamento (R$ 2,50 por km rodado + pedágio) e dos materiais estéreis para diagnóstico, se utilizados.
                3. Estou ciente da tabela de valores de exames, infiltrações e acompanhamento apresentada nesta página, e de que procedimentos adicionais, medicamentos de tratamento e cirurgias serão informados antes de sua realização.
                4. Assumo a responsabilidade pelo pagamento dos valores referentes ao atendimento do animal, nas condições informadas pela clínica.
                5. Estou ciente de que a medicina veterinária não é uma ciência exata e de que a resposta ao diagnóstico e ao tratamento varia de acordo com cada paciente.
                6. Autorizo o uso dos meus dados pessoais para cadastro, faturamento, emissão de documentos fiscais e comunicação sobre o atendimento, incluindo o compartilhamento com o veterinário solicitante e, quando necessário, com hospitais parceiros, conforme a Lei Geral de Proteção de Dados (Lei 13.709/2018).
                7. Esta autorização é registrada eletronicamente com data, hora e dados do dispositivo utilizado, e vale como minha assinatura.
                """
        }, new VersaoTermo
        {
            // 2026.2: deslocamento contado a partir da Rua Arandu, 885, e possibilidade de dividir a rota.
            Id = 2,
            Versao = "2026.2-rascunho",
            VigenteDesde = new DateTimeOffset(2026, 10, 7, 0, 0, 0, TimeSpan.Zero),
            Ativa = false,
            Texto = """
                1. Autorizo a M.V. Juliane Cavani Pimentel (CRMV-SP 11.064), da Clínica Cavani Vets, a realizar consulta oftalmológica no animal {animal}, a pedido do médico-veterinário solicitante.
                2. Declaro estar ciente de que a consulta inicial custa R$ 800,00, com medicamentos para diagnóstico incluídos, acrescida das despesas de deslocamento e dos materiais estéreis para diagnóstico, se utilizados. O deslocamento custa R$ 2,50 por km rodado, contado a partir da Rua Arandu, 885, Brooklin Paulista, São Paulo, mais pedágio; quando a rota for compartilhada com outros atendimentos, o deslocamento poderá ser dividido entre eles.
                3. Estou ciente da tabela de valores de exames, infiltrações e acompanhamento apresentada nesta página, e de que procedimentos adicionais, medicamentos de tratamento e cirurgias serão informados antes de sua realização.
                4. Assumo a responsabilidade pelo pagamento dos valores referentes ao atendimento do animal, nas condições informadas pela clínica.
                5. Estou ciente de que a medicina veterinária não é uma ciência exata e de que a resposta ao diagnóstico e ao tratamento varia de acordo com cada paciente.
                6. Autorizo o uso dos meus dados pessoais para cadastro, faturamento, emissão de documentos fiscais e comunicação sobre o atendimento, incluindo o compartilhamento com o veterinário solicitante e, quando necessário, com hospitais parceiros, conforme a Lei Geral de Proteção de Dados (Lei 13.709/2018).
                7. Esta autorização é registrada eletronicamente com data, hora e dados do dispositivo utilizado, e vale como minha assinatura.
                """
        }, new VersaoTermo
        {
            // 2026.3: nome da clínica corrigido para Clínica Pimentel Vets (Assist-vet é a plataforma).
            Id = 3,
            Versao = "2026.3-rascunho",
            VigenteDesde = new DateTimeOffset(2026, 10, 9, 0, 0, 0, TimeSpan.Zero),
            Ativa = true,
            Texto = """
                1. Autorizo a M.V. Juliane Cavani Pimentel (CRMV-SP 11.064), da Clínica Pimentel Vets, a realizar consulta oftalmológica no animal {animal}, a pedido do médico-veterinário solicitante.
                2. Declaro estar ciente de que a consulta inicial custa R$ 800,00, com medicamentos para diagnóstico incluídos, acrescida das despesas de deslocamento e dos materiais estéreis para diagnóstico, se utilizados. O deslocamento custa R$ 2,50 por km rodado, contado a partir da Rua Arandu, 885, Brooklin Paulista, São Paulo, mais pedágio; quando a rota for compartilhada com outros atendimentos, o deslocamento poderá ser dividido entre eles.
                3. Estou ciente da tabela de valores de exames, infiltrações e acompanhamento apresentada nesta página, e de que procedimentos adicionais, medicamentos de tratamento e cirurgias serão informados antes de sua realização.
                4. Assumo a responsabilidade pelo pagamento dos valores referentes ao atendimento do animal, nas condições informadas pela clínica.
                5. Estou ciente de que a medicina veterinária não é uma ciência exata e de que a resposta ao diagnóstico e ao tratamento varia de acordo com cada paciente.
                6. Autorizo o uso dos meus dados pessoais para cadastro, faturamento, emissão de documentos fiscais e comunicação sobre o atendimento, incluindo o compartilhamento com o veterinário solicitante e, quando necessário, com hospitais parceiros, conforme a Lei Geral de Proteção de Dados (Lei 13.709/2018).
                7. Esta autorização é registrada eletronicamente com data, hora e dados do dispositivo utilizado, e vale como minha assinatura.
                """
        });
    }
}
