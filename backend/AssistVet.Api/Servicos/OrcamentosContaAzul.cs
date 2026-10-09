using System.Globalization;
using AssistVet.Api.Data;
using AssistVet.Api.Domain;
using AssistVet.Api.Integracoes.ContaAzul;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AssistVet.Api.Servicos;

/// <summary>
/// Cria no Conta Azul o orçamento de um atendimento: 1 consulta (ou 1 acompanhamento) + km de ida e volta
/// estimado. O orçamento do Conta Azul não tem descrição por item, então o local, a conta do km e o pedágio
/// vão nas observações. Usa os serviços ligados aos itens da tabela de valores.
/// </summary>
public class OrcamentosContaAzul(AssistVetDbContext db, ContaAzulClient contaAzul, Deslocamentos deslocamentos,
    IOptions<DeslocamentoOptions> deslocamento, ILogger<OrcamentosContaAzul> log)
{
    static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    /// <summary>Resultado para o aviso à clínica: o número do orçamento, ou por que não foi criado.</summary>
    public record Resultado(string? OrcamentoId, string? Motivo);

    /// <summary>No aceite: cria o atendimento da consulta (nº 1), se ainda não existe, e o orçamento dele.</summary>
    public async Task<Resultado> CriarParaConsulta(int solicitacaoId, CancellationToken ct)
    {
        var consulta = await db.Atendimentos.SingleOrDefaultAsync(x => x.SolicitacaoId == solicitacaoId && x.Numero == 1, ct);
        if (consulta is null)
        {
            consulta = new Atendimento { SolicitacaoId = solicitacaoId, Numero = 1, Tipo = TipoAtendimento.Consulta };
            db.Atendimentos.Add(consulta);
            await db.SaveChangesAsync(ct);
        }
        return await CriarParaAtendimento(consulta.Id, ct);
    }

    public async Task<Resultado> CriarParaAtendimento(int atendimentoId, CancellationToken ct)
    {
        var a = await db.Atendimentos
            .Include(x => x.Solicitacao).ThenInclude(x => x.Tutor)
            .Include(x => x.Solicitacao).ThenInclude(x => x.Animal)
            .Include(x => x.Solicitacao).ThenInclude(x => x.Local)
            .SingleAsync(x => x.Id == atendimentoId, ct);
        var s = a.Solicitacao;
        if (a.ContaAzulOrcamentoId is not null) return new(a.ContaAzulOrcamentoId, null);
        if (s.Tutor?.ContaAzulId is null) return new(null, "o tutor não está cadastrado no Conta Azul");

        var precos = await db.ItensPreco.Where(x => x.Codigo == a.CodigoBase || x.Codigo == "km").ToDictionaryAsync(x => x.Codigo, ct);
        if (!precos.TryGetValue(a.CodigoBase, out var basePreco) || basePreco.ContaAzulServicoId is null || basePreco.Valor is null)
            return new(null, $"o item \"{a.CodigoBase}\" da tabela não está ligado a um serviço do Conta Azul");

        var itens = new List<OrcamentoItem> { new(basePreco.ContaAzulServicoId, 1, basePreco.Valor.Value) };
        var estimativa = await deslocamentos.Estimar(s.LocalId, ct);
        if (estimativa is not null && precos.TryGetValue("km", out var km) && km.ContaAzulServicoId is not null)
            itens.Add(new(km.ContaAzulServicoId, estimativa.KmIdaVolta, estimativa.ValorKm));

        var observacoes = new List<string> { $"Protocolo {s.Protocolo} · {a.Nome}." };
        if (s.Local is not null) observacoes.Add($"Local: {s.Local.Nome}{(s.Local.EnderecoCompleto() is { } e ? $" ({e})" : "")}.");
        observacoes.Add(estimativa is null
            ? "Deslocamento: a calcular, R$ 2,50 por km rodado + pedágio."
            : $"Deslocamento estimado: {estimativa.KmIdaVolta:0} km ida e volta saindo de {deslocamento.Value.Origem}, " +
              $"× {estimativa.ValorKm.ToString("C", PtBr)} por km. O valor final segue o km rodado.");
        observacoes.Add(estimativa?.Pedagio is { } p
            ? $"Pedágio à parte (estimado em {p.ToString("C", PtBr)} ida e volta)."
            : "Pedágio à parte, se houver.");
        observacoes.Add("Se a rota for compartilhada com outros atendimentos, o deslocamento pode ser dividido.");

        var hoje = DateOnly.FromDateTime(DateTime.Now);
        var tipo = a.Tipo == TipoAtendimento.Consulta ? "Consulta oftalmológica" : "Acompanhamento oftálmico";
        try
        {
            a.ContaAzulOrcamentoId = await contaAzul.CriarOrcamento(new OrcamentoCriar(
                IdCliente: s.Tutor.ContaAzulId,
                DataOrcamento: hoje.ToString("yyyy-MM-dd"),
                DataValidade: hoje.AddDays(7).ToString("yyyy-MM-dd"),
                Itens: itens,
                Descricao: $"{tipo} · {s.Animal?.Nome ?? s.PetNome ?? "animal"}",
                Observacoes: string.Join("\n", observacoes),
                ObservacoesPagamento: null), ct);
            // A criação só devolve o id; o número (o que a clínica vê no Conta Azul) vem da leitura.
            try { a.ContaAzulOrcamentoNumero = (await contaAzul.ObterVenda(a.ContaAzulOrcamentoId, ct))?.Numero; }
            catch (ContaAzulException e) { log.LogWarning(e, "Número do orçamento {Id} não lido", a.ContaAzulOrcamentoId); }
            await db.SaveChangesAsync(ct);
            return new(a.ContaAzulOrcamentoId, null);
        }
        catch (ContaAzulException e)
        {
            log.LogError(e, "Falha ao criar orçamento no Conta Azul para a solicitação {Protocolo}", s.Protocolo);
            return new(null, "o Conta Azul recusou o orçamento (detalhes no log do sistema)");
        }
    }
}
