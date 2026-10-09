using System.Text.Json;
using AssistVet.Api.Data;
using AssistVet.Api.Domain;
using AssistVet.Api.Servicos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AssistVet.Api.Controllers;

/// <summary>
/// Painel interno da clínica (clinica.html): lista das solicitações com os links de cada etapa e os
/// atendimentos de cada uma (consulta e acompanhamentos): data marcada, conclusão (venda no Conta Azul)
/// e envio dos relatórios.
/// </summary>
[ApiController]
[Route("api/painel")]
[SenhaClinica]
public class PainelController(AssistVetDbContext db, Avisos avisos) : ControllerBase
{
    const long TamanhoMaximoRelatorio = 20 * 1024 * 1024;

    /// <summary>Confere a senha (a página pede a senha uma vez e testa aqui).</summary>
    [HttpGet("entrar")]
    public IActionResult Entrar() => NoContent();

    /// <summary>Últimas solicitações, mais recentes primeiro.</summary>
    [HttpGet("solicitacoes")]
    public async Task<IActionResult> Solicitacoes([FromServices] IOptions<PagamentoOptions> pagamento, int quantidade = 50, CancellationToken ct = default)
    {
        var lista = await db.Solicitacoes.AsNoTracking()
            .OrderByDescending(x => x.Id)
            .Take(Math.Clamp(quantidade, 1, 200))
            .Select(s => new
            {
                s.Protocolo,
                s.CriadoEm,
                s.PreenchidoPor,
                s.Status,
                s.Prioridade,
                Veterinario = new { s.Veterinario.Nome, s.Veterinario.Celular },
                s.VeterinarioRecebeRelatorios,
                Animal = s.Animal != null ? s.Animal.Nome : s.PetNome,
                s.Especie,
                Local = s.Local == null ? null : new
                {
                    s.Local.Nome,
                    s.Local.Cidade,
                    KmIdaVolta = s.Local.DistanciaKmIda == null ? (decimal?)null : Math.Round(s.Local.DistanciaKmIda.Value * 2),
                },
                Tutor = s.Tutor != null ? s.Tutor.Nome : s.TutorNome,
                TutorCelular = s.Tutor != null ? s.Tutor.Celular : s.TutorCelular,
                s.TokenTutor,
                AutorizadoEm = s.Autorizacao == null ? (DateTimeOffset?)null : s.Autorizacao.AceitoEm,
                Atendimentos = s.Atendimentos.OrderBy(a => a.Numero).ToList(),
                Relatorios = s.Relatorios.OrderBy(r => r.Id).Select(r => new { r.Titulo, r.CriadoEm, r.Token, r.AtendimentoId }).ToList(),
            })
            .ToListAsync(ct);

        var baseUrl = Links.Base(Request);
        return Ok(new
        {
            LinkAreaTutor = Links.AreaTutor(Request),
            PagamentoInstrucoes = pagamento.Value.Instrucoes,
            pagamento.Value.PrazoDias,
            FormasPagamento = VendasContaAzul.Formas.Select(f => new { Codigo = f.Key, Nome = f.Value }),
            Solicitacoes = lista.Select(s => new
            {
                s.Protocolo, s.CriadoEm, s.PreenchidoPor, s.Status, s.Prioridade, s.Veterinario, s.VeterinarioRecebeRelatorios,
                s.Animal, s.Especie, s.Local, s.Tutor, s.TutorCelular, s.AutorizadoEm,
                LinkTutor = $"{baseUrl}/autorizacao.html?t={s.TokenTutor}",
                TermoPdf = s.AutorizadoEm is null ? null : $"{baseUrl}/api/autorizacoes/{s.TokenTutor}/termo.pdf",
                Atendimentos = s.Atendimentos.Select(a => Resumo(a, $"{baseUrl}/api/autorizacoes/{s.TokenTutor}",
                    s.Relatorios.Where(r => r.AtendimentoId == a.Id).Select(r => new { r.Titulo, r.CriadoEm, Link = Links.Relatorio(Request, r.Token) }))),
                // Relatórios enviados antes de irem junto com a venda (sem atendimento).
                Relatorios = s.Relatorios.Where(r => r.AtendimentoId == null).Select(r => new { r.Titulo, r.CriadoEm, Link = Links.Relatorio(Request, r.Token) }),
            }),
        });
    }

    /// <summary>Um atendimento como o painel e a área do tutor mostram. O PDF do orçamento some depois da venda (ele é excluído).</summary>
    internal static object Resumo(Atendimento a, string baseAutorizacao, IEnumerable<object> relatorios) => new
    {
        a.Numero,
        a.Tipo,
        a.Nome,
        a.MarcadoPara,
        a.ConcluidoEm,
        a.ValorFinal,
        a.PagamentoVencimento,
        a.PagamentoForma,
        FormaPagamentoNome = a.PagamentoForma is { } f && VendasContaAzul.Formas.TryGetValue(f, out var nome) ? nome : null,
        OrcamentoNumero = a.ContaAzulOrcamentoNumero,
        OrcamentoPdf = a.ContaAzulOrcamentoId is not null && a.ContaAzulVendaId is null ? $"{baseAutorizacao}/atendimentos/{a.Numero}/orcamento.pdf" : null,
        VendaNumero = a.ContaAzulVendaNumero,
        VendaPdf = a.ContaAzulVendaId is not null ? $"{baseAutorizacao}/atendimentos/{a.Numero}/venda.pdf" : null,
        Relatorios = relatorios.ToList(),
    };

    /// <summary>
    /// Marca (ou remarca) a data do atendimento em aberto e avisa o tutor por e-mail. Essa data é a data da venda.
    /// Sem atendimento ainda (orçamento não criado no aceite), cria o da consulta.
    /// </summary>
    [HttpPost("solicitacoes/{protocolo}/agendar")]
    public async Task<IActionResult> Agendar(string protocolo, AgendarRequest req, [FromServices] OrcamentosContaAzul orcamentos, CancellationToken ct)
    {
        var s = await db.Solicitacoes.Include(x => x.Atendimentos).SingleOrDefaultAsync(x => x.Protocolo == protocolo, ct);
        if (s is null) return NotFound("Solicitação não encontrada.");
        if (req.Quando < DateTimeOffset.UtcNow.AddDays(-30)) return BadRequest("Confira a data: ela já passou há mais de um mês.");

        var a = s.Atendimentos.Where(x => x.ConcluidoEm is null).OrderBy(x => x.Numero).FirstOrDefault();
        if (a is null)
        {
            if (s.Atendimentos.Count > 0) return Conflict("Os atendimentos desta solicitação já foram concluídos. Agende um acompanhamento.");
            a = new Atendimento { Numero = 1, Tipo = TipoAtendimento.Consulta };
            s.Atendimentos.Add(a);
        }
        a.MarcadoPara = req.Quando;
        if (s.Status is StatusSolicitacao.Recebida or StatusSolicitacao.AguardandoTutor or StatusSolicitacao.Autorizada)
            s.Status = StatusSolicitacao.Agendada;
        await db.SaveChangesAsync(ct);
        if (a.ContaAzulOrcamentoId is null && s.TutorId is not null) await orcamentos.CriarParaAtendimento(a.Id, ct);
        await avisos.AtendimentoMarcado(a.Id, enviarOrcamento: false, Links.AreaTutor(Request), ct);
        return NoContent();
    }

    /// <summary>
    /// Agenda um acompanhamento: cria o atendimento com a data, gera o orçamento no Conta Azul (acompanhamento
    /// + km estimado) e envia ao tutor o aviso com o orçamento anexo. Depois segue igual à consulta: concluir gera a venda.
    /// </summary>
    [HttpPost("solicitacoes/{protocolo}/acompanhamentos")]
    public async Task<IActionResult> NovoAcompanhamento(string protocolo, AgendarRequest req, [FromServices] OrcamentosContaAzul orcamentos, CancellationToken ct)
    {
        var s = await db.Solicitacoes.Include(x => x.Atendimentos).Include(x => x.Autorizacao)
            .SingleOrDefaultAsync(x => x.Protocolo == protocolo, ct);
        if (s is null) return NotFound("Solicitação não encontrada.");
        if (s.Autorizacao is null) return BadRequest("O tutor ainda não autorizou a consulta.");
        if (s.Atendimentos.Any(x => x.ConcluidoEm is null)) return Conflict("Conclua o atendimento em aberto antes de agendar um acompanhamento.");
        if (req.Quando < DateTimeOffset.UtcNow.AddDays(-1)) return BadRequest("Confira a data: ela já passou.");

        var a = new Atendimento
        {
            Numero = s.Atendimentos.Select(x => x.Numero).DefaultIfEmpty(0).Max() + 1,
            Tipo = TipoAtendimento.Acompanhamento,
            MarcadoPara = req.Quando,
        };
        s.Atendimentos.Add(a);
        s.Status = StatusSolicitacao.Agendada;
        await db.SaveChangesAsync(ct);

        var orcamento = await orcamentos.CriarParaAtendimento(a.Id, ct);
        await avisos.AtendimentoMarcado(a.Id, enviarOrcamento: true, Links.AreaTutor(Request), ct);
        return Ok(new
        {
            aviso = orcamento.OrcamentoId is null
                ? $"Acompanhamento agendado e tutor avisado, mas o orçamento não foi criado: {orcamento.Motivo}. Ele será criado ao concluir."
                : null,
        });
    }

    /// <summary>Sugestões para concluir o atendimento em aberto: km e pedágio estimados e itens da tabela que podem entrar na venda.</summary>
    [HttpGet("solicitacoes/{protocolo}/conclusao")]
    public async Task<IActionResult> Conclusao(string protocolo, [FromServices] Deslocamentos deslocamentos, CancellationToken ct)
    {
        var s = await db.Solicitacoes.AsNoTracking().Include(x => x.Atendimentos).SingleOrDefaultAsync(x => x.Protocolo == protocolo, ct);
        if (s is null) return NotFound("Solicitação não encontrada.");
        var a = s.Atendimentos.Where(x => x.ConcluidoEm is null).OrderBy(x => x.Numero).FirstOrDefault();
        if (a is null) return Conflict("Não há atendimento em aberto.");

        var estimativa = await deslocamentos.Estimar(s.LocalId, ct);
        var extras = await db.ItensPreco.AsNoTracking()
            .Where(x => x.Ativo && x.Valor != null && x.Codigo != "consulta" && x.Codigo != "km" && x.Codigo != a.CodigoBase)
            .OrderBy(x => x.Ordem)
            .Select(x => new { x.Codigo, x.Grupo, x.Descricao, x.Valor, Ligado = x.ContaAzulServicoId != null })
            .ToListAsync(ct);
        var precoBase = await db.ItensPreco.AsNoTracking().Where(x => x.Codigo == a.CodigoBase).Select(x => new { x.Descricao, x.Valor }).SingleOrDefaultAsync(ct);
        var km = await db.ItensPreco.AsNoTracking().Where(x => x.Codigo == "km").Select(x => x.Valor).SingleOrDefaultAsync(ct);
        return Ok(new
        {
            Atendimento = a.Nome,
            a.MarcadoPara,
            Base = precoBase?.Descricao,
            ValorBase = precoBase?.Valor,
            ValorKm = km,
            KmIdaVolta = estimativa?.KmIdaVolta,
            Pedagio = estimativa?.Pedagio,
            Extras = extras,
        });
    }

    /// <summary>
    /// Conclui o atendimento em aberto: cria a venda no Conta Azul a partir do orçamento (data = data marcada;
    /// km rodado, pedágio e procedimentos), exclui o orçamento, guarda o relatório do atendimento e envia ao
    /// tutor um e-mail só, com o relatório e a venda anexos e os dados de pagamento.
    /// Formulário: "dados" (JSON de ConcluirRequest), "relatorio" (PDF, obrigatório) e "tituloRelatorio".
    /// </summary>
    [HttpPost("solicitacoes/{protocolo}/concluir")]
    [RequestSizeLimit(TamanhoMaximoRelatorio + 1024 * 1024)]
    public async Task<IActionResult> Concluir(string protocolo, [FromForm] string dados, IFormFile? relatorio, [FromForm] string? tituloRelatorio,
        [FromServices] VendasContaAzul vendas, [FromServices] IOptions<PagamentoOptions> pagamento, [FromServices] IOptions<JsonOptions> json,
        CancellationToken ct)
    {
        ConcluirRequest? req;
        try { req = JsonSerializer.Deserialize<ConcluirRequest>(dados, json.Value.JsonSerializerOptions); }
        catch (JsonException) { return BadRequest("Dados inválidos."); }
        if (req is null) return BadRequest("Dados inválidos.");

        var s = await db.Solicitacoes.Include(x => x.Autorizacao).Include(x => x.Atendimentos).SingleOrDefaultAsync(x => x.Protocolo == protocolo, ct);
        if (s is null) return NotFound("Solicitação não encontrada.");
        if (s.Autorizacao is null) return BadRequest("O tutor ainda não autorizou a consulta.");
        var a = s.Atendimentos.Where(x => x.ConcluidoEm is null).OrderBy(x => x.Numero).FirstOrDefault();
        if (a is null) return Conflict("Não há atendimento em aberto.");

        // O relatório é conferido antes da venda: se o arquivo estiver errado, nada é criado no Conta Azul.
        if (relatorio is null) return BadRequest("Anexe o relatório do atendimento (PDF).");
        var (pdf, erroPdf) = await LerPdf(relatorio, ct);
        if (pdf is null) return BadRequest(erroPdf);

        var (total, erro, aviso) = await vendas.Concluir(a.Id, new VendasContaAzul.Conclusao(
            req.KmIdaVolta, req.Pedagio, req.Extras ?? [], req.FormaPagamento, req.Vencimento), ct);
        if (erro is not null) return BadRequest(erro);

        var r = await GuardarRelatorio(s.Id, s.Protocolo, a, pdf, tituloRelatorio, ct);
        await avisos.AtendimentoConcluido(a.Id, pagamento.Value.Instrucoes, Links.AreaTutor(Request), (r.Titulo, pdf), ct);
        return Ok(new { total, aviso });
    }

    /// <summary>
    /// Anexa o relatório de um atendimento já concluído (ex.: concluído antes de o relatório ir junto com a venda)
    /// e envia ao tutor por e-mail. Os links para o WhatsApp (tutor e veterinário) aparecem na lista.
    /// </summary>
    [HttpPost("solicitacoes/{protocolo}/atendimentos/{numero:int}/relatorio")]
    [RequestSizeLimit(TamanhoMaximoRelatorio + 1024 * 1024)]
    public async Task<IActionResult> EnviarRelatorio(string protocolo, int numero, IFormFile arquivo, [FromForm] string? titulo, CancellationToken ct)
    {
        var a = await db.Atendimentos.Include(x => x.Solicitacao)
            .SingleOrDefaultAsync(x => x.Solicitacao.Protocolo == protocolo && x.Numero == numero, ct);
        if (a is null) return NotFound("Atendimento não encontrado.");
        var (pdf, erroPdf) = await LerPdf(arquivo, ct);
        if (pdf is null) return BadRequest(erroPdf);

        var r = await GuardarRelatorio(a.SolicitacaoId, a.Solicitacao.Protocolo, a, pdf, titulo, ct);
        await avisos.RelatorioDisponivel(r.Id, pdf, Links.AreaTutor(Request), ct);
        return Ok(new { link = Links.Relatorio(Request, r.Token) });
    }

    static async Task<(byte[]? Pdf, string? Erro)> LerPdf(IFormFile arquivo, CancellationToken ct)
    {
        if (arquivo.Length == 0 || arquivo.Length > TamanhoMaximoRelatorio) return (null, "Envie o relatório em PDF de até 20 MB.");
        using var memoria = new MemoryStream();
        await arquivo.CopyToAsync(memoria, ct);
        var pdf = memoria.ToArray();
        return pdf.Length >= 5 && pdf.AsSpan(0, 5).SequenceEqual("%PDF-"u8) ? (pdf, null) : (null, "O relatório precisa ser um arquivo PDF.");
    }

    /// <summary>Grava o PDF em dados/relatorios (fora da pasta pública) e o registro ligado ao atendimento.</summary>
    async Task<Relatorio> GuardarRelatorio(int solicitacaoId, string protocolo, Atendimento a, byte[] pdf, string? titulo, CancellationToken ct)
    {
        titulo = titulo?.Trim();
        var r = new Relatorio
        {
            SolicitacaoId = solicitacaoId,
            AtendimentoId = a.Id,
            Token = Codigos.Token(),
            Titulo = string.IsNullOrEmpty(titulo)
                ? (a.Tipo == TipoAtendimento.Consulta ? "Relatório da consulta" : $"Relatório do {a.Nome.ToLowerInvariant()}")
                : titulo[..Math.Min(titulo.Length, 120)],
            Arquivo = "",
            TamanhoBytes = pdf.Length,
        };
        r.Arquivo = $"{protocolo}-{r.Token}.pdf";
        await System.IO.File.WriteAllBytesAsync(Path.Combine(Armazenamento.Pasta(HttpContext.RequestServices, "relatorios"), r.Arquivo), pdf, ct);
        db.Relatorios.Add(r);
        await db.SaveChangesAsync(ct);
        return r;
    }
}

public record AgendarRequest(DateTimeOffset Quando);

public record ConcluirRequest(
    decimal KmIdaVolta,
    decimal Pedagio,
    List<VendasContaAzul.Extra>? Extras,
    string FormaPagamento,
    DateOnly Vencimento);
