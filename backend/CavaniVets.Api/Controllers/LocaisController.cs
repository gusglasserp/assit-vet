using CavaniVets.Api.Data;
using CavaniVets.Api.Integracoes.Mapas;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CavaniVets.Api.Controllers;

[ApiController]
[Route("api/locais")]
public class LocaisController(CavaniDbContext db, GoogleMapsClient mapas) : ControllerBase
{
    /// <summary>Todos os locais cadastrados. A lista é pequena; a página filtra enquanto o veterinário digita.</summary>
    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken ct) =>
        Ok(await db.Locais.AsNoTracking()
            .OrderBy(x => x.Nome)
            .Select(x => new { x.Id, x.Nome, x.Tipo, x.Cidade, x.Uf, x.Bairro })
            .ToListAsync(ct));

    /// <summary>
    /// Busca o local no Google Maps enquanto o veterinário digita. Só para quem tem convite válido (a chave do
    /// Google é paga por uso). A sessão agrupa buscas e o detalhe escolhido numa cobrança só.
    /// </summary>
    [HttpGet("mapa")]
    public async Task<IActionResult> BuscarNoMapa(string convite, string? q, string? sessao, CancellationToken ct)
    {
        if (!mapas.Configurado) return Ok(new { disponivel = false, sugestoes = Array.Empty<SugestaoLocal>() });
        if (!await db.Convites.AnyAsync(x => x.Token == convite, ct)) return StatusCode(StatusCodes.Status403Forbidden, "Link inválido.");
        if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 3) return Ok(new { disponivel = true, sugestoes = Array.Empty<SugestaoLocal>() });
        return Ok(new { disponivel = true, sugestoes = await mapas.Sugestoes(q.Trim(), sessao ?? "", ct) });
    }

    /// <summary>Nome, endereço e ponto do local escolhido na busca do Google Maps.</summary>
    [HttpGet("mapa/{placeId}")]
    public async Task<IActionResult> DetalheDoMapa(string placeId, string convite, string sessao, CancellationToken ct)
    {
        if (!mapas.Configurado) return NotFound();
        if (!await db.Convites.AnyAsync(x => x.Token == convite, ct)) return StatusCode(StatusCodes.Status403Forbidden, "Link inválido.");
        var local = await mapas.Detalhes(placeId, sessao, ct);
        return local is null ? NotFound("Local não encontrado no Google Maps.") : Ok(local);
    }
}
