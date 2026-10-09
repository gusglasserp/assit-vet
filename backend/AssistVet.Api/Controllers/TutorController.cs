using AssistVet.Api.Data;
using AssistVet.Api.Domain;
using AssistVet.Api.Servicos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AssistVet.Api.Controllers;

/// <summary>
/// Área do tutor (minha-area.html): entra com CPF/CNPJ + código no e-mail do cadastro e acompanha as
/// solicitações, o termo, o orçamento e os relatórios. Só vale para quem já tem cadastro no sistema
/// (feito na página de autorização).
/// </summary>
[ApiController]
[Route("api/tutor")]
public class TutorController(AssistVetDbContext db, ConfirmacaoPorEmail confirmacao, ILogger<TutorController> log) : ControllerBase
{
    /// <summary>Chave dos códigos de login no ConfirmacaoPorEmail (separada dos códigos da página de autorização).</summary>
    const string ChaveLogin = "area-tutor";

    /// <summary>
    /// Envia o código ao e-mail do cadastro. A resposta é a mesma com ou sem cadastro, para a página não
    /// revelar quem é cliente da clínica a quem digita um CPF qualquer.
    /// </summary>
    [HttpPost("codigo")]
    public async Task<IActionResult> EnviarCodigo(LoginRequest req, CancellationToken ct)
    {
        var doc = Documentos.SoDigitos(req.Documento);
        if (Documentos.Tipo(doc) is null) return BadRequest("CPF ou CNPJ inválido.");

        var tutor = await db.Tutores.AsNoTracking().Where(x => x.Documento == doc)
            .Select(x => new { x.Nome, x.Email }).SingleOrDefaultAsync(ct);
        if (tutor is null || string.IsNullOrWhiteSpace(tutor.Email))
        {
            log.LogInformation("Login na área do tutor sem cadastro com e-mail");
            return NoContent();
        }

        var erro = await confirmacao.Enviar(ChaveLogin, doc, tutor.Email, tutor.Nome, "entrar na sua área", ct);
        return erro is null ? NoContent() : StatusCode(StatusCodes.Status429TooManyRequests, erro);
    }

    /// <summary>Confere o código e abre a sessão (cookie de 30 dias).</summary>
    [HttpPost("entrar")]
    public async Task<IActionResult> Entrar(LoginRequest req, CancellationToken ct)
    {
        var doc = Documentos.SoDigitos(req.Documento);
        if (Documentos.Tipo(doc) is null) return BadRequest("CPF ou CNPJ inválido.");
        if (confirmacao.Verificar(ChaveLogin, doc, req.Codigo ?? "") is { } erro) return BadRequest(erro);

        var tutor = await db.Tutores.AsNoTracking().SingleOrDefaultAsync(x => x.Documento == doc, ct);
        if (tutor is null) return BadRequest("Cadastro não encontrado.");
        await SessaoTutor.Entrar(HttpContext, tutor.Id, tutor.Nome);
        return NoContent();
    }

    [HttpPost("sair")]
    public async Task<IActionResult> Sair()
    {
        await SessaoTutor.Sair(HttpContext);
        return NoContent();
    }

    /// <summary>
    /// Solicitações do tutor, mais recentes primeiro. Inclui as que ele já preencheu e as que ainda esperam
    /// a autorização dele (o veterinário informou o mesmo celular do cadastro).
    /// </summary>
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> MinhaArea([FromServices] IOptions<PagamentoOptions> pagamento, CancellationToken ct)
    {
        var tutor = await db.Tutores.AsNoTracking().SingleOrDefaultAsync(x => x.Id == SessaoTutor.TutorId(User), ct);
        if (tutor is null)
        {
            await SessaoTutor.Sair(HttpContext);
            return Unauthorized();
        }

        var lista = await DoTutor(tutor)
            .OrderByDescending(s => s.Id)
            .Select(s => new
            {
                s.Protocolo,
                s.CriadoEm,
                s.Status,
                s.TokenTutor,
                Animal = s.Animal != null ? s.Animal.Nome : s.PetNome,
                Especie = s.Animal != null ? s.Animal.Especie : s.Especie,
                Veterinario = s.Veterinario.Nome,
                Local = s.Local == null ? null : s.Local.Nome + (s.Local.Cidade == null ? "" : ", " + s.Local.Cidade),
                AutorizadoEm = s.Autorizacao == null ? (DateTimeOffset?)null : s.Autorizacao.AceitoEm,
                Atendimentos = s.Atendimentos.OrderBy(a => a.Numero).ToList(),
                Relatorios = s.Relatorios.OrderBy(r => r.Id).Select(r => new { r.Id, r.Titulo, r.CriadoEm, r.AtendimentoId }).ToList(),
            })
            .ToListAsync(ct);

        return Ok(new
        {
            tutor.Nome,
            Solicitacoes = lista.Select(s =>
            {
                var aberto = s.Atendimentos.FirstOrDefault(a => a.ConcluidoEm is null);
                return new
                {
                    s.Protocolo, s.CriadoEm, s.Animal, s.Especie, s.Veterinario, s.Local, s.AutorizadoEm,
                    Situacao = s.AutorizadoEm is null ? "AguardandoAutorizacao"
                        : aberto?.MarcadoPara is not null ? (aberto.Tipo == TipoAtendimento.Consulta ? "Agendada" : "AcompanhamentoAgendado")
                        : aberto is null && s.Atendimentos.Count > 0 ? "Atendida" : "Autorizada",
                    ProximaData = aberto?.MarcadoPara,
                    // Instruções de pagamento quando há atendimento concluído (a área não sabe se já foi pago).
                    PagamentoInstrucoes = s.Atendimentos.Any(a => a.ConcluidoEm is not null) && !string.IsNullOrWhiteSpace(pagamento.Value.Instrucoes)
                        ? pagamento.Value.Instrucoes : null,
                    LinkAutorizacao = s.AutorizadoEm is null ? $"/autorizacao.html?t={s.TokenTutor}" : null,
                    TermoPdf = s.AutorizadoEm is null ? null : $"/api/autorizacoes/{s.TokenTutor}/termo.pdf",
                    Atendimentos = s.Atendimentos.Select(a => PainelController.Resumo(a, $"/api/autorizacoes/{s.TokenTutor}",
                        s.Relatorios.Where(r => r.AtendimentoId == a.Id).Select(r => new { r.Titulo, r.CriadoEm, Pdf = $"/api/tutor/relatorios/{r.Id}" }))),
                    Relatorios = s.Relatorios.Where(r => r.AtendimentoId == null).Select(r => new { r.Titulo, r.CriadoEm, Pdf = $"/api/tutor/relatorios/{r.Id}" }),
                };
            }),
        });
    }

    /// <summary>Solicitações que o tutor preencheu ou que esperam a autorização dele (mesmo celular do cadastro).</summary>
    IQueryable<Solicitacao> DoTutor(Tutor tutor)
    {
        var celulares = Telefones.Variantes(tutor.Celular).ToList();
        return db.Solicitacoes.AsNoTracking()
            .Where(s => s.Status != StatusSolicitacao.Cancelada
                        && (s.TutorId == tutor.Id || (s.TutorId == null && s.TutorCelular != null && celulares.Contains(s.TutorCelular))));
    }

    /// <summary>PDF de um relatório, só se a solicitação for deste tutor.</summary>
    [Authorize]
    [HttpGet("relatorios/{id:int}")]
    public async Task<IActionResult> Relatorio(int id, CancellationToken ct)
    {
        var tutor = await db.Tutores.AsNoTracking().SingleOrDefaultAsync(x => x.Id == SessaoTutor.TutorId(User), ct);
        if (tutor is null) return Unauthorized();
        var minhas = DoTutor(tutor).Select(s => s.Id);
        var r = await db.Relatorios.AsNoTracking().Include(x => x.Solicitacao)
            .SingleOrDefaultAsync(x => x.Id == id && minhas.Contains(x.SolicitacaoId), ct);
        return r is null ? NotFound("Relatório não encontrado.") : RelatoriosController.Arquivo(this, r);
    }
}

public record LoginRequest(string Documento, string? Codigo = null);
