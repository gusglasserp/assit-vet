using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CavaniVets.Api.Data;
using CavaniVets.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CavaniVets.Api.Integracoes.ContaAzul;

/// <summary>
/// Acesso à API v2 do Conta Azul. Guarda os tokens no banco e renova o access_token
/// (validade de 1 h) automaticamente usando o refresh_token.
/// </summary>
public class ContaAzulClient(HttpClient http, CavaniDbContext db, IOptions<ContaAzulOptions> opcoes)
{
    const int ConexaoId = 1;

    // O refresh_token muda a cada renovação: duas renovações simultâneas invalidariam uma à outra.
    static readonly SemaphoreSlim TravaRenovacao = new(1, 1);

    static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    readonly ContaAzulOptions _op = opcoes.Value;

    public string MontarUrlLogin(string state) =>
        $"{_op.UrlLogin}?response_type=code&client_id={Uri.EscapeDataString(_op.ClientId)}" +
        $"&redirect_uri={Uri.EscapeDataString(_op.RedirectUri)}&state={Uri.EscapeDataString(state)}" +
        "&scope=openid+profile+aws.cognito.signin.user.admin";

    public async Task<ContaAzulConexao?> ObterConexao(CancellationToken ct = default) =>
        await db.ContaAzulConexoes.FindAsync([ConexaoId], ct);

    /// <summary>Troca o código recebido no redirecionamento do login pelos tokens e salva.</summary>
    public async Task Conectar(string code, CancellationToken ct = default)
    {
        var tokens = await PedirToken(new()
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = _op.RedirectUri,
        }, ct);
        await SalvarTokens(tokens, ct);
    }

    /// <summary>Busca por CPF ou CNPJ (só dígitos).</summary>
    public async Task<PessoaResumo?> BuscarPessoaPorDocumento(string documento, CancellationToken ct = default)
    {
        var r = await Enviar<PessoasPorFiltro>(HttpMethod.Get, $"/v1/pessoas?documentos={documento}", null, ct);
        // O filtro deveria ser exato, mas confere de novo para não ligar o tutor à pessoa errada.
        return r?.Items?.FirstOrDefault(p => Documentos.SoDigitos(p.Documento) == documento);
    }

    public Task<Pessoa?> ObterPessoa(string id, CancellationToken ct = default) =>
        Enviar<Pessoa>(HttpMethod.Get, $"/v1/pessoas/{id}", null, ct);

    public async Task<string> CriarPessoa(PessoaCriar pessoa, CancellationToken ct = default) =>
        (await Enviar<IdResposta>(HttpMethod.Post, "/v1/pessoas", pessoa, ct))!.Id;

    public Task AtualizarPessoa(string id, PessoaAtualizar pessoa, CancellationToken ct = default) =>
        Enviar<object>(HttpMethod.Patch, $"/v1/pessoas/{id}", pessoa, ct);

    public async Task<string> CriarOrcamento(OrcamentoCriar orcamento, CancellationToken ct = default) =>
        (await Enviar<IdResposta>(HttpMethod.Post, "/v1/orcamentos", orcamento, ct))!.Id;

    public async Task<List<Servico>> ListarServicos(CancellationToken ct = default) =>
        (await Enviar<ServicosPorFiltro>(HttpMethod.Get, "/v1/servicos?pagina=1&tamanho_pagina=100", null, ct))?.Itens ?? [];

    async Task<T?> Enviar<T>(HttpMethod metodo, string caminho, object? corpo, CancellationToken ct)
    {
        var token = await AccessTokenValido(ct);
        using var req = new HttpRequestMessage(metodo, _op.UrlApi + caminho);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (corpo is not null)
            req.Content = JsonContent.Create(corpo, corpo.GetType(), options: Json);

        using var resp = await http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode)
        {
            var detalhe = await resp.Content.ReadAsStringAsync(ct);
            throw new ContaAzulException($"Conta Azul respondeu {(int)resp.StatusCode} em {metodo} {caminho}: {detalhe}", (int)resp.StatusCode);
        }
        // PATCH responde 204 sem corpo.
        if (resp.StatusCode == System.Net.HttpStatusCode.NoContent || resp.Content.Headers.ContentLength == 0)
            return default;
        return await resp.Content.ReadFromJsonAsync<T>(Json, ct);
    }

    async Task<string> AccessTokenValido(CancellationToken ct)
    {
        var conexao = await ObterConexao(ct)
            ?? throw new ContaAzulException("Conta Azul não conectado. Acesse /api/conta-azul/conectar.");
        if (conexao.AccessTokenExpiraEm > DateTimeOffset.UtcNow.AddMinutes(2))
            return conexao.AccessToken;

        await TravaRenovacao.WaitAsync(ct);
        try
        {
            // Outra requisição pode ter renovado enquanto esta esperava.
            await db.Entry(conexao).ReloadAsync(ct);
            if (conexao.AccessTokenExpiraEm > DateTimeOffset.UtcNow.AddMinutes(2))
                return conexao.AccessToken;

            var tokens = await PedirToken(new()
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = conexao.RefreshToken,
            }, ct);
            await SalvarTokens(tokens, ct);
            return tokens.AccessToken;
        }
        finally
        {
            TravaRenovacao.Release();
        }
    }

    async Task<TokenResposta> PedirToken(Dictionary<string, string> campos, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, _op.UrlApi + "/oauth/token")
        {
            Content = new FormUrlEncodedContent(campos),
        };
        var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_op.ClientId}:{_op.ClientSecret}"));
        req.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);

        using var resp = await http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode)
        {
            var detalhe = await resp.Content.ReadAsStringAsync(ct);
            throw new ContaAzulException($"Falha ao obter token do Conta Azul ({(int)resp.StatusCode}): {detalhe}", (int)resp.StatusCode);
        }
        return (await resp.Content.ReadFromJsonAsync<TokenResposta>(Json, ct))!;
    }

    async Task SalvarTokens(TokenResposta tokens, CancellationToken ct)
    {
        var conexao = await ObterConexao(ct);
        if (conexao is null)
        {
            conexao = new ContaAzulConexao { Id = ConexaoId, AccessToken = "", RefreshToken = "" };
            db.ContaAzulConexoes.Add(conexao);
        }
        conexao.AccessToken = tokens.AccessToken;
        conexao.RefreshToken = tokens.RefreshToken;
        conexao.AccessTokenExpiraEm = DateTimeOffset.UtcNow.AddSeconds(tokens.ExpiresIn);
        conexao.AtualizadoEm = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
    }
}
