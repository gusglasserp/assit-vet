using AssistVet.Api.Data;
using AssistVet.Api.Domain;
using AssistVet.Api.Integracoes.Mapas;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AssistVet.Api.Servicos;

public class DeslocamentoOptions
{
    public const string Secao = "Deslocamento";
    /// <summary>De onde o km é contado.</summary>
    public string Origem { get; set; } = "";
}

/// <summary>
/// Estimativa do deslocamento até o local do atendimento: km de ida e volta × valor do km da tabela.
/// A distância de cada local é calculada uma vez (Google Maps) e guardada no próprio local.
/// </summary>
public class Deslocamentos(AssistVetDbContext db, GoogleMapsClient mapas, IOptions<DeslocamentoOptions> opcoes, ILogger<Deslocamentos> log)
{
    /// <summary>Calcula e grava a distância do local, se ainda não tiver e houver endereço e chave do Google.</summary>
    public async Task GarantirDistancia(Local local, CancellationToken ct)
    {
        if (local.DistanciaKmIda is not null || !mapas.Configurado || string.IsNullOrWhiteSpace(opcoes.Value.Origem)) return;
        var endereco = local.EnderecoCompleto();
        if (local.GooglePlaceId is null && endereco is null) return;
        try
        {
            var rota = await mapas.Calcular(opcoes.Value.Origem, local.GooglePlaceId, endereco, ct);
            if (rota is null) return;
            local.DistanciaKmIda = rota.Km;
            local.PedagioIda = rota.Pedagio;
            local.DistanciaCalculadaEm = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
        {
            // A estimativa é um extra: se o Google falhar ou demorar, a solicitação segue sem ela
            // (será calculada no próximo uso do local).
            log.LogWarning(e, "Não foi possível calcular a distância até o local {Id}", local.Id);
        }
    }

    /// <summary>
    /// Estimativa do deslocamento até o local (uso interno: orçamento no Conta Azul e aviso à clínica; não é
    /// mostrada ao tutor). Nulo se não houver local com distância conhecida.
    /// </summary>
    public async Task<Estimativa?> Estimar(int? localId, CancellationToken ct)
    {
        if (localId is null) return null;
        var local = await db.Locais.FindAsync([localId.Value], ct);
        if (local is null) return null;
        await GarantirDistancia(local, ct);
        if (local.DistanciaKmIda is not { } ida) return null;

        var valorKm = await db.ItensPreco.Where(x => x.Codigo == "km" && x.Ativo).Select(x => x.Valor).FirstOrDefaultAsync(ct);
        if (valorKm is null) return null;

        var idaVolta = Math.Round(ida * 2);
        return new Estimativa(idaVolta, valorKm.Value, Math.Round(idaVolta * valorKm.Value, 2),
            local.PedagioIda is { } p && p > 0 ? p * 2 : null);
    }
}

/// <summary>Km de ida e volta (arredondado), valor do km, total do deslocamento e pedágio estimado (ida e volta).</summary>
public record Estimativa(decimal KmIdaVolta, decimal ValorKm, decimal Valor, decimal? Pedagio);
