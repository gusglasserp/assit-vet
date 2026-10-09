using System.Globalization;
using AssistVet.Api.Data;
using AssistVet.Api.Integracoes.ContaAzul;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AssistVet.Api.Servicos;

/// <summary>
/// Cria no Conta Azul o orçamento da consulta autorizada: 1 consulta + km de ida e volta estimado.
/// O orçamento do Conta Azul não tem descrição por item, então o local, a conta do km e o pedágio
/// vão nas observações. Usa os serviços ligados aos itens "consulta" e "km" da tabela de valores.
/// </summary>
public class OrcamentosContaAzul(AssistVetDbContext db, ContaAzulClient contaAzul, Deslocamentos deslocamentos,
    IOptions<DeslocamentoOptions> deslocamento, ILogger<OrcamentosContaAzul> log)
{
    static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    /// <summary>Resultado para o aviso à clínica: o número do orçamento, ou por que não foi criado.</summary>
    public record Resultado(string? OrcamentoId, string? Motivo);

    public async Task<Resultado> CriarParaSolicitacao(int solicitacaoId, CancellationToken ct)
    {
        var s = await db.Solicitacoes.Include(x => x.Tutor).Include(x => x.Animal).Include(x => x.Local)
            .SingleAsync(x => x.Id == solicitacaoId, ct);
        if (s.ContaAzulOrcamentoId is not null) return new(s.ContaAzulOrcamentoId, null);
        if (s.Tutor?.ContaAzulId is null) return new(null, "o tutor não está cadastrado no Conta Azul");

        var precos = await db.ItensPreco.Where(x => x.Codigo == "consulta" || x.Codigo == "km").ToDictionaryAsync(x => x.Codigo, ct);
        if (!precos.TryGetValue("consulta", out var consulta) || consulta.ContaAzulServicoId is null || consulta.Valor is null)
            return new(null, "o item \"consulta\" da tabela não está ligado a um serviço do Conta Azul");

        var itens = new List<OrcamentoItem> { new(consulta.ContaAzulServicoId, 1, consulta.Valor.Value) };
        var estimativa = await deslocamentos.Estimar(s.LocalId, ct);
        if (estimativa is not null && precos.TryGetValue("km", out var km) && km.ContaAzulServicoId is not null)
            itens.Add(new(km.ContaAzulServicoId, estimativa.KmIdaVolta, estimativa.ValorKm));

        var observacoes = new List<string> { $"Protocolo {s.Protocolo}." };
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
        try
        {
            s.ContaAzulOrcamentoId = await contaAzul.CriarOrcamento(new OrcamentoCriar(
                IdCliente: s.Tutor.ContaAzulId,
                DataOrcamento: hoje.ToString("yyyy-MM-dd"),
                DataValidade: hoje.AddDays(7).ToString("yyyy-MM-dd"),
                Itens: itens,
                Descricao: $"Consulta oftalmológica · {s.Animal?.Nome ?? s.PetNome ?? "animal"}",
                Observacoes: string.Join("\n", observacoes),
                ObservacoesPagamento: null), ct);
            await db.SaveChangesAsync(ct);
            return new(s.ContaAzulOrcamentoId, null);
        }
        catch (ContaAzulException e)
        {
            log.LogError(e, "Falha ao criar orçamento no Conta Azul para a solicitação {Protocolo}", s.Protocolo);
            return new(null, "o Conta Azul recusou o orçamento (detalhes no log do sistema)");
        }
    }
}
