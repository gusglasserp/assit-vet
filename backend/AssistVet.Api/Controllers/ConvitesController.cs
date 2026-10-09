using AssistVet.Api.Data;
using AssistVet.Api.Domain;
using AssistVet.Api.Servicos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AssistVet.Api.Controllers;

/// <summary>
/// Gera o link de solicitação para um veterinário a partir do celular dele.
/// Hoje é chamado pela clínica; depois, pela integração com o WhatsApp.
/// Criar convite exige a senha da clínica (revela o nome do veterinário pelo celular); ler o convite é público.
/// </summary>
[ApiController]
[Route("api/convites")]
public class ConvitesController(AssistVetDbContext db) : ControllerBase
{
    [HttpPost]
    [SenhaClinica]
    public async Task<IActionResult> Criar(ConviteRequest req, CancellationToken ct)
    {
        var celular = Documentos.SoDigitos(req.Celular);
        if (celular.Length is < 10 or > 11) return BadRequest("Informe o celular com DDD.");

        // Reaproveita o convite do mesmo celular e modo, para o veterinário ter sempre o mesmo link.
        var convite = await db.Convites.FirstOrDefaultAsync(x => x.Celular == celular && x.PreenchidoPor == req.PreenchidoPor, ct);
        if (convite is null)
        {
            convite = new Convite { Token = Codigos.Token(), Celular = celular, PreenchidoPor = req.PreenchidoPor };
            db.Convites.Add(convite);
            await db.SaveChangesAsync(ct);
        }

        var vet = await db.Veterinarios.AsNoTracking().SingleOrDefaultAsync(x => x.Celular == celular, ct);
        return Ok(new
        {
            convite.Token,
            Link = $"{Links.Base(Request)}/solicitacao.html?c={convite.Token}",
            Veterinario = vet is null ? null : new { vet.Nome, vet.Crmv, vet.Uf },
        });
    }

    /// <summary>Usado pela página do veterinário: quem é e se já tem cadastro.</summary>
    [HttpGet("{token}")]
    public async Task<IActionResult> Obter(string token, CancellationToken ct)
    {
        var convite = await db.Convites.AsNoTracking().SingleOrDefaultAsync(x => x.Token == token, ct);
        if (convite is null) return NotFound("Link inválido.");
        var vet = await db.Veterinarios.AsNoTracking().SingleOrDefaultAsync(x => x.Celular == convite.Celular, ct);
        return Ok(new
        {
            convite.Celular,
            convite.PreenchidoPor,
            Veterinario = vet is null ? null : new { vet.Nome, vet.Crmv, vet.Uf },
        });
    }
}

public record ConviteRequest(string Celular, PreenchidoPor PreenchidoPor = PreenchidoPor.Veterinario);
