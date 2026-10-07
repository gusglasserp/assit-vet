using CavaniVets.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CavaniVets.Api.Controllers;

[ApiController]
[Route("api/locais")]
public class LocaisController(CavaniDbContext db) : ControllerBase
{
    /// <summary>Todos os locais cadastrados. A lista é pequena; a página filtra enquanto o veterinário digita.</summary>
    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken ct) =>
        Ok(await db.Locais.AsNoTracking()
            .OrderBy(x => x.Nome)
            .Select(x => new { x.Id, x.Nome, x.Tipo, x.Cidade })
            .ToListAsync(ct));
}
