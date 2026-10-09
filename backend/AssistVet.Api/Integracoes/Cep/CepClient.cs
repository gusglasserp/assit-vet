using System.Net.Http.Json;

namespace AssistVet.Api.Integracoes.Cep;

public record Endereco(string Cep, string Uf, string Cidade, string Bairro, string Rua);

/// <summary>
/// Consulta de CEP pela base dos Correios via ViaCEP, com BrasilAPI como reserva.
/// A API oficial dos Correios exige contrato; estas duas são gratuitas e sem cadastro.
/// </summary>
public class CepClient(HttpClient http, ILogger<CepClient> log)
{
    // "erro" vem como "true" (texto) quando o CEP não existe; object aceita texto ou booleano.
    record ViaCep(string? Cep, string? Uf, string? Localidade, string? Bairro, string? Logradouro, object? Erro);
    record BrasilApi(string? Cep, string? State, string? City, string? Neighborhood, string? Street);

    /// <summary>Nulo quando o CEP não existe. Espera 8 dígitos.</summary>
    public async Task<Endereco?> Buscar(string cep, CancellationToken ct = default)
    {
        try
        {
            var r = await http.GetFromJsonAsync<ViaCep>($"https://viacep.com.br/ws/{cep}/json/", ct);
            return r is null || r.Erro is not null
                ? null
                : new Endereco(cep, r.Uf ?? "", r.Localidade ?? "", r.Bairro ?? "", r.Logradouro ?? "");
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
        {
            log.LogWarning(e, "ViaCEP falhou para {Cep}; tentando BrasilAPI", cep);
        }

        using var resp = await http.GetAsync($"https://brasilapi.com.br/api/cep/v1/{cep}", ct);
        if (resp.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        resp.EnsureSuccessStatusCode();
        var b = await resp.Content.ReadFromJsonAsync<BrasilApi>(ct);
        return b is null ? null : new Endereco(cep, b.State ?? "", b.City ?? "", b.Neighborhood ?? "", b.Street ?? "");
    }
}
