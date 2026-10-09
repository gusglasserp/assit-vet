namespace AssistVet.Api.Domain;

public class Veterinario
{
    public int Id { get; set; }
    /// <summary>Só dígitos, com DDD. Chave de identificação do veterinário.</summary>
    public required string Celular { get; set; }
    public required string Nome { get; set; }
    public required string Crmv { get; set; }
    public required string Uf { get; set; }
    public DateTimeOffset CriadoEm { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Link de solicitação enviado a um veterinário (solicitacao.html?c=TOKEN). O celular identifica o veterinário
/// sem ele digitar nada. Hoje a clínica gera o convite à mão; depois, a integração com o WhatsApp gera sozinha.
/// Reutilizável: o veterinário pode abrir várias solicitações pelo mesmo link.
/// </summary>
public class Convite
{
    public int Id { get; set; }
    public required string Token { get; set; }
    /// <summary>Só dígitos, com DDD.</summary>
    public required string Celular { get; set; }
    /// <summary>Clinica quando a assistente vai preencher no lugar do veterinário.</summary>
    public PreenchidoPor PreenchidoPor { get; set; }
    public DateTimeOffset CriadoEm { get; set; } = DateTimeOffset.UtcNow;
}

public class Local
{
    public int Id { get; set; }
    public required string Nome { get; set; }
    public TipoLocal Tipo { get; set; }

    // Endereço, para a Dra. saber onde é e calcular o deslocamento. Haras costumam não ter número.
    public string? Cep { get; set; }
    public string? Rua { get; set; }
    public string? Numero { get; set; }
    public string? Complemento { get; set; }
    public string? Bairro { get; set; }
    public string? Cidade { get; set; }
    public string? Uf { get; set; }
    /// <summary>Como chegar: portaria, porteira, ponto de referência.</summary>
    public string? Referencia { get; set; }

    // Quando o local foi escolhido no Google Maps: ponto exato (melhor que o CEP para haras em área rural).
    public string? GooglePlaceId { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    /// <summary>Distância de carro (só ida) a partir da origem do deslocamento, calculada pelo Google Maps.</summary>
    public decimal? DistanciaKmIda { get; set; }
    /// <summary>Pedágio estimado (só ida), quando o Google Maps informa.</summary>
    public decimal? PedagioIda { get; set; }
    public DateTimeOffset? DistanciaCalculadaEm { get; set; }

    public DateTimeOffset CriadoEm { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Endereço em uma linha, para mapas e e-mails. Nulo se não houver rua nem cidade.</summary>
    public string? EnderecoCompleto()
    {
        if (string.IsNullOrWhiteSpace(Rua) && string.IsNullOrWhiteSpace(Cidade)) return null;
        var rua = string.Join(", ", new[] { Rua, Numero }.Where(x => !string.IsNullOrWhiteSpace(x)));
        var cidade = string.Join(" - ", new[] { Cidade, Uf }.Where(x => !string.IsNullOrWhiteSpace(x)));
        var cep = Cep is { Length: 8 } ? $"{Cep[..5]}-{Cep[5..]}" : Cep;
        return string.Join(", ", new[] { rua, Complemento, Bairro, cidade, cep }.Where(x => !string.IsNullOrWhiteSpace(x)));
    }
}

public class Solicitacao
{
    public int Id { get; set; }
    public required string Protocolo { get; set; }
    /// <summary>Token aleatório do link enviado ao tutor. Substitui os dados que o protótipo passava na URL.</summary>
    public required string TokenTutor { get; set; }
    public StatusSolicitacao Status { get; set; } = StatusSolicitacao.Recebida;
    public PreenchidoPor PreenchidoPor { get; set; }

    public int VeterinarioId { get; set; }
    public Veterinario Veterinario { get; set; } = null!;
    public int? LocalId { get; set; }
    public Local? Local { get; set; }

    // Caso clínico (tudo opcional; o áudio pode substituir os campos)
    public string? AudioArquivo { get; set; }
    public int? AudioDuracaoSegundos { get; set; }
    public string? PetNome { get; set; }
    public Especie? Especie { get; set; }
    public string? Raca { get; set; }
    public string? Idade { get; set; }
    public Olho? Olho { get; set; }
    public Prioridade? Prioridade { get; set; }
    public string? QueixaHistorico { get; set; }
    public string? Medicacoes { get; set; }

    // Equinos / haras / hípica
    public string? TratadorNome { get; set; }
    public string? TratadorCelular { get; set; }

    // Tutor informado pelo veterinário
    public string? TutorNome { get; set; }
    public string? TutorCelular { get; set; }
    public bool? TutorCienteCusto { get; set; }

    public bool VeterinarioRecebeRelatorios { get; set; }

    // Preenchidos pelo tutor no link de autorização (etapas 1 e 2).
    public int? TutorId { get; set; }
    public Tutor? Tutor { get; set; }
    public int? AnimalId { get; set; }
    public Animal? Animal { get; set; }

    public DateTimeOffset CriadoEm { get; set; } = DateTimeOffset.UtcNow;

    public Autorizacao? Autorizacao { get; set; }
    public List<Relatorio> Relatorios { get; set; } = [];
    /// <summary>A consulta (nº 1) e os acompanhamentos seguintes, cada um com data, orçamento e venda.</summary>
    public List<Atendimento> Atendimentos { get; set; } = [];
}

/// <summary>
/// Uma visita da Dra.: a consulta (criada no aceite do tutor) ou um acompanhamento agendado depois.
/// Ciclo: orçamento no Conta Azul → data marcada (tutor avisado) → concluído: a venda é criada a partir do
/// orçamento, com data = data marcada, e o orçamento é excluído (a API não converte orçamento em venda).
/// </summary>
public class Atendimento
{
    public int Id { get; set; }
    public int SolicitacaoId { get; set; }
    public Solicitacao Solicitacao { get; set; } = null!;
    /// <summary>Ordem dentro da solicitação: 1 = consulta, 2 em diante = acompanhamentos.</summary>
    public int Numero { get; set; }
    public TipoAtendimento Tipo { get; set; }

    /// <summary>Data e hora combinadas com o tutor; vira a data da venda. O tutor é avisado ao marcar ou remarcar.</summary>
    public DateTimeOffset? MarcadoPara { get; set; }

    public string? ContaAzulOrcamentoId { get; set; }
    /// <summary>Número do orçamento como aparece no Conta Azul (para a clínica achar e acompanhar).</summary>
    public long? ContaAzulOrcamentoNumero { get; set; }
    public string? ContaAzulVendaId { get; set; }
    public long? ContaAzulVendaNumero { get; set; }

    /// <summary>Quando a clínica concluiu (a venda foi criada).</summary>
    public DateTimeOffset? ConcluidoEm { get; set; }
    /// <summary>Total da venda (consulta ou acompanhamento, km rodado, pedágio e procedimentos feitos).</summary>
    public decimal? ValorFinal { get; set; }
    public DateOnly? PagamentoVencimento { get; set; }
    /// <summary>Forma de pagamento da venda, no formato do Conta Azul (ex.: PIX_PAGAMENTO_INSTANTANEO).</summary>
    public string? PagamentoForma { get; set; }

    public DateTimeOffset CriadoEm { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Relatório clínico desta visita, enviado ao tutor junto com a venda.</summary>
    public List<Relatorio> Relatorios { get; set; } = [];

    /// <summary>Código do item da tabela de valores cobrado como base: "consulta" ou "acompanhamento".</summary>
    public string CodigoBase => Tipo == TipoAtendimento.Consulta ? "consulta" : "acompanhamento";

    /// <summary>"Consulta" ou "Acompanhamento 2" (o número conta a partir da consulta).</summary>
    public string Nome => Tipo == TipoAtendimento.Consulta ? "Consulta" : $"Acompanhamento {Numero - 1}";
}

/// <summary>
/// Relatório clínico em PDF de um atendimento (consulta ou acompanhamento), anexado pela clínica ao concluir,
/// junto com a venda. O tutor recebe no mesmo e-mail e vê na área dele; o Token gera o link direto enviado
/// pelo WhatsApp (ao tutor e, se ele pediu, ao veterinário solicitante).
/// </summary>
public class Relatorio
{
    public int Id { get; set; }
    public int SolicitacaoId { get; set; }
    public Solicitacao Solicitacao { get; set; } = null!;
    /// <summary>Atendimento a que o relatório se refere (vai junto com a venda). Nulo nos enviados antes disso.</summary>
    public int? AtendimentoId { get; set; }
    public Atendimento? Atendimento { get; set; }
    public required string Token { get; set; }
    public required string Titulo { get; set; }
    /// <summary>Nome do arquivo em dados/relatorios (fora da pasta pública).</summary>
    public required string Arquivo { get; set; }
    public long TamanhoBytes { get; set; }
    public DateTimeOffset CriadoEm { get; set; } = DateTimeOffset.UtcNow;
}

public class Tutor
{
    public int Id { get; set; }
    public required string Nome { get; set; }
    /// <summary>CPF (11) ou CNPJ (14), só dígitos. Haras e hípicas podem ser o responsável financeiro.</summary>
    public required string Documento { get; set; }
    public TipoPessoa TipoPessoa { get; set; }
    public required string Celular { get; set; }
    public required string Email { get; set; }
    public required string Cep { get; set; }
    public required string Uf { get; set; }
    public required string Cidade { get; set; }
    public required string Rua { get; set; }
    public required string Bairro { get; set; }
    public required string Numero { get; set; }
    public string? Complemento { get; set; }
    /// <summary>Id (UUID) da pessoa no Conta Azul, preenchido após a sincronização.</summary>
    public string? ContaAzulId { get; set; }
    public DateTimeOffset CriadoEm { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset AtualizadoEm { get; set; } = DateTimeOffset.UtcNow;

    public List<Animal> Animais { get; set; } = [];
}

public class Animal
{
    public int Id { get; set; }
    public int TutorId { get; set; }
    public Tutor Tutor { get; set; } = null!;
    public required string Nome { get; set; }
    public Especie Especie { get; set; }
    public Sexo Sexo { get; set; }
    public string? Raca { get; set; }
    public string? Idade { get; set; }
    public string? Peso { get; set; }
}

/// <summary>Texto do termo. Cada alteração vira uma nova versão; versões antigas nunca são editadas.</summary>
public class VersaoTermo
{
    public int Id { get; set; }
    public required string Versao { get; set; }
    public required string Texto { get; set; }
    public DateTimeOffset VigenteDesde { get; set; }
    public bool Ativa { get; set; }
}

/// <summary>Item da tabela de valores, editável pela clínica.</summary>
public class ItemPreco
{
    public int Id { get; set; }
    /// <summary>Identificador estável usado pelo código, ex.: "consulta", "km".</summary>
    public required string Codigo { get; set; }
    public required string Grupo { get; set; }
    public required string Descricao { get; set; }
    /// <summary>Nulo quando o item é "à parte" ou "sob orçamento".</summary>
    public decimal? Valor { get; set; }
    /// <summary>Texto mostrado no lugar do valor ou abaixo dele, ex.: "por visita", "sob orçamento".</summary>
    public string? Observacao { get; set; }
    public int Ordem { get; set; }
    public bool Ativo { get; set; } = true;
    /// <summary>Id (UUID) do serviço correspondente no Conta Azul. Necessário para usar o item em orçamentos.</summary>
    public string? ContaAzulServicoId { get; set; }
}

/// <summary>Tokens OAuth da conta Conta Azul da clínica. Uma linha só (Id = 1).</summary>
public class ContaAzulConexao
{
    public int Id { get; set; }
    public required string AccessToken { get; set; }
    /// <summary>Muda a cada renovação; o anterior deixa de valer.</summary>
    public required string RefreshToken { get; set; }
    public DateTimeOffset AccessTokenExpiraEm { get; set; }
    public DateTimeOffset AtualizadoEm { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>Aceite do tutor. Guarda cópia dos valores e do termo vigentes no momento, para valer como prova.</summary>
public class Autorizacao
{
    public int Id { get; set; }
    public int SolicitacaoId { get; set; }
    public Solicitacao Solicitacao { get; set; } = null!;
    public int TutorId { get; set; }
    public Tutor Tutor { get; set; } = null!;
    public int AnimalId { get; set; }
    public Animal Animal { get; set; } = null!;
    public int VersaoTermoId { get; set; }
    public VersaoTermo VersaoTermo { get; set; } = null!;

    public required string TextoTermoAceito { get; set; }
    /// <summary>JSON com a tabela de valores exibida ao tutor.</summary>
    public required string ValoresExibidosJson { get; set; }

    public bool AceitouTermo { get; set; }
    public bool AceitouResponsabilidadeFinanceira { get; set; }
    public bool AceitouLgpd { get; set; }
    public required string NomeAssinado { get; set; }

    public required string Ip { get; set; }
    public required string UserAgent { get; set; }
    public DateTimeOffset AceitoEm { get; set; } = DateTimeOffset.UtcNow;
}
