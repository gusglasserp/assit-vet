using CavaniVets.Api.Data;
using CavaniVets.Api.Servicos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CavaniVets.Api.Controllers;

/// <summary>Painel interno da clínica (index.html): lista das solicitações com os links de cada etapa.</summary>
[ApiController]
[Route("api/painel")]
[SenhaClinica]
public class PainelController(CavaniDbContext db) : ControllerBase
{
    /// <summary>Confere a senha (a página pede a senha uma vez e testa aqui).</summary>
    [HttpGet("entrar")]
    public IActionResult Entrar() => NoContent();

    /// <summary>Últimas solicitações, mais recentes primeiro.</summary>
    [HttpGet("solicitacoes")]
    public async Task<IActionResult> Solicitacoes(int quantidade = 50, CancellationToken ct = default)
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
                TemOrcamento = s.ContaAzulOrcamentoId != null,
            })
            .ToListAsync(ct);

        var baseUrl = Links.Base(Request);
        return Ok(lista.Select(s => new
        {
            s.Protocolo, s.CriadoEm, s.PreenchidoPor, s.Status, s.Prioridade, s.Veterinario, s.Animal, s.Especie,
            s.Local, s.Tutor, s.TutorCelular, s.AutorizadoEm,
            LinkTutor = $"{baseUrl}/autorizacao.html?t={s.TokenTutor}",
            TermoPdf = s.AutorizadoEm is null ? null : $"{baseUrl}/api/autorizacoes/{s.TokenTutor}/termo.pdf",
            OrcamentoPdf = s.TemOrcamento ? $"{baseUrl}/api/autorizacoes/{s.TokenTutor}/orcamento.pdf" : null,
        }));
    }
}
