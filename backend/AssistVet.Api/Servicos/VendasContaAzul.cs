using System.Globalization;
using AssistVet.Api.Data;
using AssistVet.Api.Domain;
using AssistVet.Api.Integracoes.ContaAzul;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AssistVet.Api.Servicos;

/// <summary>
/// Dados de pagamento enviados ao tutor junto com a venda (seção "Pagamento" da configuração).
/// Ex.: Pagamento__Instrucoes = "Pix (CNPJ): 00.000.000/0001-00 · Clínica Pimentel Vets".
/// </summary>
public class PagamentoOptions
{
    public const string Secao = "Pagamento";
    /// <summary>Chave Pix, banco e favorecido, como o tutor deve ver. Vai na venda e no e-mail.</summary>
    public string Instrucoes { get; set; } = "";
    /// <summary>Prazo padrão de vencimento, em dias depois do atendimento (sugestão no painel).</summary>
    public int PrazoDias { get; set; } = 3;
}

/// <summary>
/// Fim de um atendimento (consulta ou acompanhamento): a partir do orçamento dele, cria a venda aprovada no
/// Conta Azul, com o km rodado de verdade, o pedágio e os procedimentos feitos, e exclui o orçamento (a API
/// não converte orçamento em venda). A data da venda é a data marcada do atendimento.
/// </summary>
public class VendasContaAzul(AssistVetDbContext db, ContaAzulClient contaAzul, OrcamentosContaAzul orcamentos,
    IOptions<PagamentoOptions> pagamento, ILogger<VendasContaAzul> log)
{
    static readonly TimeZoneInfo Brasilia = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
    static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    /// <summary>Formas aceitas no painel → valor do Conta Azul.</summary>
    public static readonly IReadOnlyDictionary<string, string> Formas = new Dictionary<string, string>
    {
        ["PIX_PAGAMENTO_INSTANTANEO"] = "Pix",
        ["BOLETO_BANCARIO"] = "Boleto",
        ["TRANSFERENCIA_BANCARIA"] = "Transferência bancária",
        ["CARTAO_CREDITO"] = "Cartão de crédito",
        ["DINHEIRO"] = "Dinheiro",
    };

    public record Extra(string Codigo, decimal Quantidade);

    public record Conclusao(decimal KmIdaVolta, decimal Pedagio, List<Extra> Extras, string FormaPagamento, DateOnly Vencimento);

    /// <summary>Cria a venda. Devolve o total, a mensagem de erro (nada é gravado) ou um aviso (venda criada).</summary>
    public async Task<(decimal? Total, string? Erro, string? Aviso)> Concluir(int atendimentoId, Conclusao c, CancellationToken ct)
    {
        if (!Formas.ContainsKey(c.FormaPagamento)) return (null, "Forma de pagamento inválida.", null);
        if (c.KmIdaVolta < 0 || c.Pedagio < 0) return (null, "Km e pedágio não podem ser negativos.", null);

        var a = await db.Atendimentos
            .Include(x => x.Solicitacao).ThenInclude(x => x.Animal)
            .Include(x => x.Solicitacao).ThenInclude(x => x.Local)
            .SingleAsync(x => x.Id == atendimentoId, ct);
        var s = a.Solicitacao;
        if (a.ConcluidoEm is not null) return (null, "Este atendimento já foi concluído.", null);
        if (a.MarcadoPara is not { } marcado) return (null, "Marque a data do atendimento antes de concluir: ela é a data da venda.", null);
        var dataAtendimento = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(marcado, Brasilia).DateTime);

        if (a.ContaAzulOrcamentoId is null)
        {
            // Orçamento não criado antes (ex.: Conta Azul desconectado): tenta agora.
            var orc = await orcamentos.CriarParaAtendimento(a.Id, ct);
            if (orc.OrcamentoId is null) return (null, $"Sem orçamento no Conta Azul: {orc.Motivo}.", null);
        }

        var precos = await db.ItensPreco.AsNoTracking().ToDictionaryAsync(x => x.Codigo, ct);
        var itens = new List<VendaItem>();
        string? Adicionar(string codigo, decimal quantidade, decimal? valor = null, string? descricao = null)
        {
            if (!precos.TryGetValue(codigo, out var p)) return $"Item \"{codigo}\" não existe na tabela.";
            if (p.ContaAzulServicoId is null) return $"\"{p.Descricao}\" não está ligado a um serviço do Conta Azul.";
            if ((valor ?? p.Valor) is not { } v || v <= 0) return $"\"{p.Descricao}\" não tem valor na tabela.";
            if (quantidade <= 0) return $"Quantidade de \"{p.Descricao}\" deve ser maior que zero.";
            itens.Add(new VendaItem(p.ContaAzulServicoId, quantidade, v, descricao));
            return null;
        }

        var erro = Adicionar(a.CodigoBase, 1);
        if (erro is null && c.KmIdaVolta > 0) erro = Adicionar("km", c.KmIdaVolta, descricao: $"Deslocamento: {c.KmIdaVolta:0.#} km ida e volta");
        // Pedágio não tem serviço próprio: vai como linha do serviço de deslocamento, com descrição própria.
        if (erro is null && c.Pedagio > 0) erro = Adicionar("km", 1, c.Pedagio, "Pedágio");
        foreach (var e in c.Extras)
            erro ??= Adicionar(e.Codigo, e.Quantidade);
        if (erro is not null) return (null, erro, null);

        var total = itens.Sum(i => Math.Round(i.Quantidade * i.Valor, 2));
        var instrucoes = pagamento.Value.Instrucoes;
        if (string.IsNullOrWhiteSpace(instrucoes))
            log.LogWarning("Pagamento:Instrucoes não configurado; a venda {Protocolo} sai sem os dados de pagamento", s.Protocolo);

        try
        {
            var orcamento = await contaAzul.ObterVenda(a.ContaAzulOrcamentoId!, ct)
                            ?? throw new ContaAzulException("Orçamento não encontrado no Conta Azul.");
            var observacoes = string.Join("\n", new[]
            {
                $"Protocolo {s.Protocolo} · {a.Nome}. Referente ao orçamento nº {orcamento.Numero}.",
                $"{(a.Tipo == TipoAtendimento.Consulta ? "Consulta oftalmológica" : "Acompanhamento oftálmico")} · " +
                $"{s.Animal?.Nome ?? s.PetNome ?? "animal"}, atendimento em {dataAtendimento:dd/MM/yyyy}.",
                s.Local is null ? null : $"Local: {s.Local.Nome}{(s.Local.Cidade is null ? "" : $", {s.Local.Cidade}")}.",
            }.Where(x => x is not null));

            a.ContaAzulOrcamentoNumero = orcamento.Numero;
            var numero = await contaAzul.ProximoNumeroVenda(ct);
            a.ContaAzulVendaId = await contaAzul.CriarVenda(new VendaCriacao(
                IdCliente: orcamento.IdCliente,
                Numero: numero,
                Situacao: "APROVADO",
                DataVenda: dataAtendimento.ToString("yyyy-MM-dd"),
                Itens: itens,
                CondicaoPagamento: new VendaCondicaoPagamento(c.FormaPagamento, "À vista",
                    [new VendaParcela(c.Vencimento.ToString("yyyy-MM-dd"), total)]),
                Observacoes: observacoes,
                ObservacoesPagamento: string.IsNullOrWhiteSpace(instrucoes) ? null : instrucoes), ct);
            a.ContaAzulVendaNumero = numero;
        }
        catch (ContaAzulException e)
        {
            log.LogError(e, "Falha ao criar a venda da solicitação {Protocolo} ({Atendimento})", s.Protocolo, a.Nome);
            return (null, "O Conta Azul recusou a venda: " + e.Message, null);
        }

        // A venda substitui o orçamento: ele é excluído para não ficar "Em andamento" no Conta Azul.
        // Se a exclusão falhar, a venda já existe e vale; a clínica exclui o orçamento à mão.
        string? aviso = null;
        try
        {
            if (!await contaAzul.ExcluirVenda(a.ContaAzulOrcamentoId!, ct))
                aviso = $"A venda foi criada, mas o Conta Azul não excluiu o orçamento nº {a.ContaAzulOrcamentoNumero}. Exclua-o por lá.";
        }
        catch (ContaAzulException e)
        {
            log.LogError(e, "Falha ao excluir o orçamento da solicitação {Protocolo}", s.Protocolo);
            aviso = $"A venda foi criada, mas não foi possível excluir o orçamento nº {a.ContaAzulOrcamentoNumero}. Exclua-o no Conta Azul.";
        }

        a.ConcluidoEm = DateTimeOffset.UtcNow;
        a.ValorFinal = total;
        a.PagamentoVencimento = c.Vencimento;
        a.PagamentoForma = c.FormaPagamento;
        s.Status = StatusSolicitacao.Atendida;
        await db.SaveChangesAsync(ct);
        log.LogInformation("Venda da solicitação {Protocolo} ({Atendimento}) criada: {Total}", s.Protocolo, a.Nome, total.ToString("C", PtBr));
        return (total, null, aviso);
    }
}
