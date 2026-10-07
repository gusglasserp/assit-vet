using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace CavaniVets.Api.Integracoes.Mapas;

/// <summary>
/// Chave do Google Maps (Routes API e Places API (New)). Fica em user-secrets (GoogleMaps:ChaveApi) no
/// desenvolvimento e em variável de ambiente (GoogleMaps__ChaveApi) no servidor; nunca vai para as páginas.
/// Sem chave, o sistema funciona sem a busca no Maps e sem a estimativa de km.
/// </summary>
public class GoogleMapsOptions
{
    public const string Secao = "GoogleMaps";
    public string ChaveApi { get; set; } = "";
}

public record Rota(decimal Km, decimal? Pedagio);

public record SugestaoLocal(string PlaceId, string Nome, string? Detalhe);

public record LocalDoMapa(
    string PlaceId, string Nome, string? Cep, string? Rua, string? Numero, string? Bairro,
    string? Cidade, string? Uf, double? Latitude, double? Longitude, string? EnderecoFormatado);

public class GoogleMapsClient(HttpClient http, IOptions<GoogleMapsOptions> opcoes, ILogger<GoogleMapsClient> log)
{
    public bool Configurado => !string.IsNullOrWhiteSpace(opcoes.Value.ChaveApi);

    /// <summary>
    /// Distância de carro até o destino (pelo ponto do Google, se houver; senão pelo endereço).
    /// Rota sem trânsito: para cobrança, o horário não importa. Nulo se não achou rota.
    /// </summary>
    public async Task<Rota?> Calcular(string origem, string? destinoPlaceId, string? destinoEndereco, CancellationToken ct)
    {
        object destino = destinoPlaceId is not null ? new { placeId = destinoPlaceId } : new { address = destinoEndereco };
        using var req = Requisicao(HttpMethod.Post, "https://routes.googleapis.com/directions/v2:computeRoutes",
            "routes.distanceMeters,routes.travelAdvisory.tollInfo", new
            {
                origin = new { address = origem },
                destination = destino,
                travelMode = "DRIVE",
                routingPreference = "TRAFFIC_UNAWARE",
                languageCode = "pt-BR",
                regionCode = "BR",
                extraComputations = new[] { "TOLLS" },
            });
        var rota = (await Enviar<RespostaRotas>(req, ct))?.Routes?.FirstOrDefault();
        if (rota is null) return null;

        var preco = rota.TravelAdvisory?.TollInfo?.EstimatedPrice?.FirstOrDefault(p => p.CurrencyCode == "BRL");
        decimal? pedagio = preco is null ? null : decimal.Parse(preco.Units ?? "0") + preco.Nanos / 1_000_000_000m;
        return new Rota(Math.Round(rota.DistanceMeters / 1000m, 1), pedagio);
    }

    /// <summary>
    /// Sugestões enquanto o veterinário digita o nome do local (Places Autocomplete), com preferência
    /// pela região de São Paulo. A sessão agrupa as buscas e o detalhe escolhido numa cobrança só.
    /// </summary>
    public async Task<List<SugestaoLocal>> Sugestoes(string texto, string sessao, CancellationToken ct)
    {
        using var req = Requisicao(HttpMethod.Post, "https://places.googleapis.com/v1/places:autocomplete", null, new
        {
            input = texto,
            languageCode = "pt-BR",
            includedRegionCodes = new[] { "br" },
            sessionToken = sessao,
            locationBias = new { circle = new { center = new { latitude = -23.55, longitude = -46.63 }, radius = 50000.0 } },
        });
        var r = await Enviar<RespostaSugestoes>(req, ct);
        return (r?.Suggestions ?? [])
            .Select(s => s.PlacePrediction)
            .OfType<Previsao>()
            .Select(p => new SugestaoLocal(p.PlaceId, p.StructuredFormat?.MainText?.Text ?? p.Text?.Text ?? "", p.StructuredFormat?.SecondaryText?.Text))
            .ToList();
    }

    /// <summary>Nome, endereço e ponto do local escolhido (Place Details).</summary>
    public async Task<LocalDoMapa?> Detalhes(string placeId, string sessao, CancellationToken ct)
    {
        using var req = Requisicao(HttpMethod.Get,
            $"https://places.googleapis.com/v1/places/{Uri.EscapeDataString(placeId)}?languageCode=pt-BR&regionCode=br&sessionToken={Uri.EscapeDataString(sessao)}",
            "id,displayName,formattedAddress,addressComponents,location", null);
        var p = await Enviar<Lugar>(req, ct);
        if (p is null) return null;

        string? Parte(string tipo, bool curto = false) =>
            p.AddressComponents?.FirstOrDefault(c => c.Types?.Contains(tipo) == true) is { } c ? (curto ? c.ShortText : c.LongText) : null;
        return new LocalDoMapa(
            p.Id ?? placeId,
            p.DisplayName?.Text ?? "",
            Parte("postal_code")?.Replace("-", ""),
            Parte("route"),
            Parte("street_number"),
            Parte("sublocality_level_1") ?? Parte("sublocality"),
            Parte("administrative_area_level_2") ?? Parte("locality"),
            Parte("administrative_area_level_1", curto: true),
            p.Location?.Latitude, p.Location?.Longitude,
            p.FormattedAddress);
    }

    HttpRequestMessage Requisicao(HttpMethod metodo, string url, string? campos, object? corpo)
    {
        var req = new HttpRequestMessage(metodo, url);
        if (corpo is not null) req.Content = JsonContent.Create(corpo);
        req.Headers.Add("X-Goog-Api-Key", opcoes.Value.ChaveApi);
        if (campos is not null) req.Headers.Add("X-Goog-FieldMask", campos);
        return req;
    }

    async Task<T?> Enviar<T>(HttpRequestMessage req, CancellationToken ct)
    {
        using var resp = await http.SendAsync(req, ct);
        if (resp.IsSuccessStatusCode) return await resp.Content.ReadFromJsonAsync<T>(ct);
        log.LogWarning("Google Maps respondeu {Status} em {Url}: {Corpo}",
            (int)resp.StatusCode, req.RequestUri?.GetLeftPart(UriPartial.Path), await resp.Content.ReadAsStringAsync(ct));
        return default;
    }

    // Contratos das respostas do Google (só os campos usados).
    record RespostaRotas(List<RotaGoogle>? Routes);
    record RotaGoogle(int DistanceMeters, Aviso? TravelAdvisory);
    record Aviso(InfoPedagio? TollInfo);
    record InfoPedagio(List<Dinheiro>? EstimatedPrice);
    record Dinheiro(string? CurrencyCode, string? Units, int Nanos);

    record RespostaSugestoes(List<Sugestao>? Suggestions);
    record Sugestao(Previsao? PlacePrediction);
    record Previsao(string PlaceId, Texto? Text, Formato? StructuredFormat);
    record Formato(Texto? MainText, Texto? SecondaryText);
    record Texto(string? Text);

    record Lugar(string? Id, Texto? DisplayName, string? FormattedAddress, List<Componente>? AddressComponents, Ponto? Location);
    record Componente(string? LongText, string? ShortText, List<string>? Types);
    record Ponto(double Latitude, double Longitude);
}
