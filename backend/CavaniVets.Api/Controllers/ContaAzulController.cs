using CavaniVets.Api.Data;
using CavaniVets.Api.Domain;
using CavaniVets.Api.Integracoes.ContaAzul;
using CavaniVets.Api.Servicos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace CavaniVets.Api.Controllers;

[ApiController]
[Route("api/conta-azul")]
public class ContaAzulController(ContaAzulClient contaAzul, CavaniDbContext db, IMemoryCache cache, ILogger<ContaAzulController> log) : ControllerBase
{
    /// <summary>Abre o login do Conta Azul. Depois de autorizar, o Conta Azul redireciona com ?code=...&state=...</summary>
    [HttpGet("conectar")]
    [SenhaClinica(SomenteLocal = true)]
    public IActionResult Conectar() => Redirect(NovaUrlDeLogin());

    /// <summary>
    /// Mesmo que "conectar", para o botão do painel (exige a senha da clínica): devolve o endereço do login
    /// e a página vai até ele. O retorno (callback) é protegido pelo state, que só sai daqui.
    /// </summary>
    [HttpGet("conectar-url")]
    [SenhaClinica]
    public IActionResult ConectarUrl() => Ok(new { url = NovaUrlDeLogin() });

    string NovaUrlDeLogin()
    {
        var state = Guid.NewGuid().ToString("N");
        cache.Set(ChaveState(state), true, TimeSpan.FromMinutes(15));
        return contaAzul.MontarUrlLogin(state);
    }

    /// <summary>
    /// Recebe o código do login e troca pelos tokens. No app de desenvolvimento o Conta Azul
    /// redireciona para https://www.contaazul.com; troque esse endereço por este endpoint na barra do navegador.
    /// </summary>
    [HttpGet("callback")]
    // Sem senha: o state só é emitido por conectar/conectar-url (protegidos) e vale uma vez, por 15 minutos.
    public async Task<IActionResult> Callback(string? code, string? state, CancellationToken ct)
    {
        log.LogInformation("Callback Conta Azul recebido (code: {TemCode}, state: {State})", !string.IsNullOrEmpty(code), state);
        if (string.IsNullOrEmpty(code))
            return BadRequest("O endereço não tem o parâmetro code. Cole o endereço completo que o Conta Azul abriu, com ?code=...&state=...");
        if (state is null || !cache.TryGetValue(ChaveState(state), out _))
            return BadRequest("Link de conexão expirado ou inválido. Acesse /api/conta-azul/conectar de novo.");
        cache.Remove(ChaveState(state));

        await contaAzul.Conectar(code, ct);
        log.LogInformation("Conta Azul conectado");
        return Ok("Conta Azul conectado.");
    }

    [HttpGet("status")]
    [SenhaClinica]
    public async Task<IActionResult> Status(CancellationToken ct)
    {
        var conexao = await contaAzul.ObterConexao(ct);
        return Ok(new { conectado = conexao is not null, atualizadoEm = conexao?.AtualizadoEm });
    }

    /// <summary>Serviços cadastrados no Conta Azul, para ligar cada item da tabela de valores ao serviço certo.</summary>
    [HttpGet("servicos")]
    [SenhaClinica]
    public async Task<IActionResult> Servicos(CancellationToken ct) => Ok(await contaAzul.ListarServicos(ct));

    /// <summary>Liga um item da tabela de valores (ex.: "consulta") a um serviço do Conta Azul.</summary>
    [HttpPut("precos/{codigo}")]
    [SenhaClinica]
    public async Task<IActionResult> LigarServico(string codigo, LigarServicoRequest req, CancellationToken ct)
    {
        var item = await db.ItensPreco.SingleOrDefaultAsync(x => x.Codigo == codigo, ct);
        if (item is null) return NotFound($"Item de preço '{codigo}' não existe.");
        item.ContaAzulServicoId = req.ServicoId;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    /// <summary>
    /// Cria um orçamento no Conta Azul para o tutor, com itens da tabela de valores.
    /// O valor de cada item vem da tabela, a não ser que seja informado (obrigatório para itens sem valor fixo).
    /// </summary>
    [HttpPost("orcamentos")]
    [SenhaClinica]
    public async Task<IActionResult> CriarOrcamento(OrcamentoRequest req, CancellationToken ct)
    {
        var documento = Documentos.SoDigitos(req.Documento);
        var tutor = await db.Tutores.SingleOrDefaultAsync(x => x.Documento == documento, ct);
        if (tutor?.ContaAzulId is null)
            return BadRequest("Tutor ainda não cadastrado no Conta Azul. Use POST /api/clientes antes.");
        if (req.Itens.Count == 0) return BadRequest("Informe ao menos um item.");

        var codigos = req.Itens.Select(i => i.Codigo).ToList();
        var precos = await db.ItensPreco.Where(x => codigos.Contains(x.Codigo)).ToDictionaryAsync(x => x.Codigo, ct);

        var itens = new List<OrcamentoItem>();
        foreach (var i in req.Itens)
        {
            if (!precos.TryGetValue(i.Codigo, out var preco)) return BadRequest($"Item de preço '{i.Codigo}' não existe.");
            if (preco.ContaAzulServicoId is null)
                return BadRequest($"Item '{i.Codigo}' não está ligado a um serviço do Conta Azul. Use PUT /api/conta-azul/precos/{i.Codigo}.");
            var valor = i.Valor ?? preco.Valor;
            if (valor is null or <= 0) return BadRequest($"Item '{i.Codigo}' não tem valor fixo; informe o valor.");
            if (i.Quantidade <= 0) return BadRequest($"Quantidade do item '{i.Codigo}' deve ser maior que zero.");
            itens.Add(new OrcamentoItem(preco.ContaAzulServicoId, i.Quantidade, valor.Value));
        }

        var hoje = DateOnly.FromDateTime(DateTime.Now);
        var id = await contaAzul.CriarOrcamento(new OrcamentoCriar(
            IdCliente: tutor.ContaAzulId,
            DataOrcamento: hoje.ToString("yyyy-MM-dd"),
            DataValidade: hoje.AddDays(req.ValidadeDias).ToString("yyyy-MM-dd"),
            Itens: itens,
            Descricao: req.Descricao,
            Observacoes: req.Observacoes,
            ObservacoesPagamento: req.ObservacoesPagamento), ct);

        return Ok(new { contaAzulOrcamentoId = id });
    }

    static string ChaveState(string state) => $"conta-azul-state:{state}";
}

public record LigarServicoRequest(string ServicoId);

public record OrcamentoItemRequest(string Codigo, decimal Quantidade = 1, decimal? Valor = null);

public record OrcamentoRequest(
    string Documento,
    List<OrcamentoItemRequest> Itens,
    int ValidadeDias = 7,
    string? Descricao = null,
    string? Observacoes = null,
    string? ObservacoesPagamento = null);
