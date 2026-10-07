using System.Globalization;
using System.Text;
using CavaniVets.Api.Data;
using CavaniVets.Api.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CavaniVets.Api.Controllers;

/// <summary>
/// Página do tutor (autorizacao.html?t=TOKEN). O token é o TokenTutor da solicitação,
/// enviado pela clínica no WhatsApp.
/// </summary>
[ApiController]
[Route("api/autorizacoes/{token}")]
public class AutorizacoesController(CavaniDbContext db) : ControllerBase
{
    /// <summary>Dados do caso para montar a página: animal, veterinário e local informados na solicitação.</summary>
    [HttpGet]
    public async Task<IActionResult> Caso(string token, CancellationToken ct)
    {
        var s = await db.Solicitacoes.AsNoTracking()
            .Include(x => x.Veterinario).Include(x => x.Local).Include(x => x.Animal)
            .SingleOrDefaultAsync(x => x.TokenTutor == token, ct);
        if (s is null) return NotFound("Link inválido ou expirado.");

        // Se o tutor já confirmou o animal neste link, vale o que ele confirmou.
        var a = s.Animal;
        return Ok(new
        {
            s.Protocolo,
            Animal = new
            {
                Nome = a?.Nome ?? s.PetNome,
                Especie = a?.Especie ?? s.Especie,
                Sexo = a?.Sexo,
                Raca = a?.Raca ?? s.Raca,
                Idade = a?.Idade ?? s.Idade,
                Peso = a?.Peso,
            },
            Veterinario = new { s.Veterinario.Nome },
            Local = s.Local is null ? null : new { s.Local.Nome, s.Local.Tipo, s.Local.Cidade },
            Tutor = new { Nome = s.TutorNome, Celular = s.TutorCelular },
        });
    }

    /// <summary>
    /// Etapa 2: confirma o animal. Reaproveita o cadastro se o tutor já tiver um animal com o mesmo nome.
    /// </summary>
    [HttpPost("animal")]
    public async Task<IActionResult> SalvarAnimal(string token, AnimalRequest req, CancellationToken ct)
    {
        var s = await db.Solicitacoes.SingleOrDefaultAsync(x => x.TokenTutor == token, ct);
        if (s is null) return NotFound("Link inválido ou expirado.");
        if (string.IsNullOrWhiteSpace(req.Nome)) return BadRequest("Informe o nome do animal.");

        var tutor = await db.Tutores.Include(x => x.Animais)
            .SingleOrDefaultAsync(x => x.Documento == Documentos.SoDigitos(req.Documento), ct);
        if (tutor is null) return BadRequest("Cadastro do tutor não encontrado. Volte à etapa anterior.");

        var nome = req.Nome.Trim();
        var animal = tutor.Animais.FirstOrDefault(x => x.Id == s.AnimalId)
                     ?? tutor.Animais.FirstOrDefault(x => Normalizar(x.Nome) == Normalizar(nome));
        if (animal is null)
        {
            animal = new Animal { Nome = nome };
            tutor.Animais.Add(animal);
        }
        animal.Nome = nome;
        animal.Especie = req.Especie;
        animal.Sexo = req.Sexo;
        animal.Raca = Limpar(req.Raca);
        animal.Idade = Limpar(req.Idade);
        animal.Peso = Limpar(req.Peso);
        await db.SaveChangesAsync(ct);

        s.TutorId = tutor.Id;
        s.AnimalId = animal.Id;
        await db.SaveChangesAsync(ct);

        return Ok(new { animalId = animal.Id });
    }

    static string? Limpar(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    /// <summary>Compara nomes ignorando acentos, maiúsculas e espaços extras ("Tôbi" = "tobi").</summary>
    static string Normalizar(string s)
    {
        var semAcento = new string(s.Normalize(NormalizationForm.FormD)
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray());
        return string.Join(' ', semAcento.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
}

public record AnimalRequest(
    string Documento,
    string Nome,
    Especie Especie,
    Sexo Sexo,
    string? Raca,
    string? Idade,
    string? Peso);
