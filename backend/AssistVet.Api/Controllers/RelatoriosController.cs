using AssistVet.Api.Data;
using AssistVet.Api.Domain;
using AssistVet.Api.Servicos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AssistVet.Api.Controllers;

/// <summary>
/// Link direto do relatório (/api/relatorios/TOKEN), enviado pelo WhatsApp ao tutor e ao veterinário
/// solicitante. Quem tem o link abre o PDF sem login, como o termo assinado.
/// </summary>
[ApiController]
[Route("api/relatorios")]
public class RelatoriosController(AssistVetDbContext db) : ControllerBase
{
    [HttpGet("{token}")]
    public async Task<IActionResult> PorToken(string token, CancellationToken ct)
    {
        var r = await db.Relatorios.AsNoTracking().Include(x => x.Solicitacao).SingleOrDefaultAsync(x => x.Token == token, ct);
        return r is null ? NotFound("Link inválido ou expirado.") : Arquivo(this, r);
    }

    /// <summary>Abre o PDF no navegador (inline), com nome legível para quem salvar.</summary>
    internal static IActionResult Arquivo(ControllerBase c, Relatorio r)
    {
        var caminho = Path.Combine(Armazenamento.Pasta(c.HttpContext.RequestServices, "relatorios"), r.Arquivo);
        if (!System.IO.File.Exists(caminho)) return c.NotFound("Arquivo do relatório não encontrado.");
        c.Response.Headers.ContentDisposition = $"inline; filename=\"relatorio-{r.Solicitacao.Protocolo}-{r.Id}.pdf\"";
        return c.PhysicalFile(caminho, "application/pdf");
    }
}
