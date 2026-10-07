using System.Text.Json;
using CavaniVets.Api.Data;
using CavaniVets.Api.Domain;
using CavaniVets.Api.Servicos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CavaniVets.Api.Controllers;

/// <summary>Página do veterinário (solicitacao.html?c=TOKEN): envia o caso, com áudio opcional.</summary>
[ApiController]
[Route("api/solicitacoes")]
public class SolicitacoesController(CavaniDbContext db, IWebHostEnvironment env, IOptions<JsonOptions> json, Avisos avisos, Deslocamentos deslocamentos) : ControllerBase
{
    const long TamanhoMaximoAudio = 30 * 1024 * 1024;

    /// <summary>
    /// multipart/form-data com "dados" (JSON de <see cref="SolicitacaoRequest"/>) e "audio" (arquivo, opcional).
    /// Cadastra ou corrige o veterinário, cria o local se for novo e devolve o link do tutor.
    /// </summary>
    [HttpPost]
    [RequestSizeLimit(TamanhoMaximoAudio + 1024 * 1024)]
    public async Task<IActionResult> Criar([FromForm] string dados, IFormFile? audio, CancellationToken ct)
    {
        SolicitacaoRequest? req;
        try { req = JsonSerializer.Deserialize<SolicitacaoRequest>(dados, json.Value.JsonSerializerOptions); }
        catch (JsonException) { return BadRequest("Dados inválidos."); }
        if (req is null) return BadRequest("Dados inválidos.");

        var convite = await db.Convites.SingleOrDefaultAsync(x => x.Token == req.Convite, ct);
        if (convite is null) return BadRequest("Link inválido. Peça um novo link à clínica.");

        // Veterinário: identificado pelo celular do convite. Cadastra na primeira vez; atualiza se ele corrigiu os dados.
        var vet = await db.Veterinarios.SingleOrDefaultAsync(x => x.Celular == convite.Celular, ct);
        if (req.Veterinario is { } v)
        {
            if (string.IsNullOrWhiteSpace(v.Nome) || string.IsNullOrWhiteSpace(v.Crmv) || v.Uf?.Length != 2)
                return BadRequest("Informe nome, CRMV e UF.");
            vet ??= db.Veterinarios.Add(new Veterinario { Celular = convite.Celular, Nome = "", Crmv = "", Uf = "" }).Entity;
            vet.Nome = v.Nome.Trim();
            vet.Crmv = v.Crmv.Trim();
            vet.Uf = v.Uf.ToUpperInvariant();
        }
        if (vet is null) return BadRequest("Informe nome, CRMV e UF.");

        // Local: existente pelo id, ou novo.
        Local? local = null;
        if (req.LocalId is int localId)
        {
            local = await db.Locais.FindAsync([localId], ct);
            if (local is null) return BadRequest("Local não encontrado.");
        }
        else if (req.NovoLocal is { } nl && !string.IsNullOrWhiteSpace(nl.Nome))
        {
            // A Dra. precisa saber onde é: sem rua (ou ponto no Google Maps), cidade e UF, não dá para chegar.
            if (nl.GooglePlaceId is null && (string.IsNullOrWhiteSpace(nl.Rua) || string.IsNullOrWhiteSpace(nl.Cidade) || nl.Uf?.Length != 2))
                return BadRequest("Informe o endereço do local (rua, cidade e UF), ou escolha o local no Google Maps.");

            // Mesmo lugar do Google Maps já cadastrado por outro veterinário: reaproveita.
            local = nl.GooglePlaceId is null ? null : await db.Locais.FirstOrDefaultAsync(x => x.GooglePlaceId == nl.GooglePlaceId, ct);
            local ??= db.Locais.Add(new Local
            {
                Nome = nl.Nome.Trim(),
                Tipo = nl.Tipo,
                Cep = NuloSeVazio(Documentos.SoDigitos(nl.Cep)),
                Rua = Limpar(nl.Rua),
                Numero = Limpar(nl.Numero),
                Complemento = Limpar(nl.Complemento),
                Bairro = Limpar(nl.Bairro),
                Cidade = Limpar(nl.Cidade),
                Uf = Limpar(nl.Uf)?.ToUpperInvariant(),
                Referencia = Limpar(nl.Referencia),
                GooglePlaceId = Limpar(nl.GooglePlaceId),
                Latitude = nl.Latitude,
                Longitude = nl.Longitude,
            }).Entity;
        }

        var s = new Solicitacao
        {
            Protocolo = Codigos.Protocolo(),
            TokenTutor = Codigos.Token(),
            Status = StatusSolicitacao.Recebida,
            PreenchidoPor = convite.PreenchidoPor,
            Veterinario = vet,
            Local = local,
            PetNome = Limpar(req.PetNome),
            Especie = req.Especie,
            Raca = Limpar(req.Raca),
            Idade = Limpar(req.Idade),
            Olho = req.Olho,
            Prioridade = req.Prioridade,
            QueixaHistorico = Limpar(req.QueixaHistorico),
            Medicacoes = Limpar(req.Medicacoes),
            TratadorNome = Limpar(req.TratadorNome),
            TratadorCelular = NuloSeVazio(Documentos.SoDigitos(req.TratadorCelular)),
            TutorNome = Limpar(req.TutorNome),
            TutorCelular = NuloSeVazio(Documentos.SoDigitos(req.TutorCelular)),
            TutorCienteCusto = req.TutorCienteCusto,
            VeterinarioRecebeRelatorios = req.VeterinarioRecebeRelatorios,
        };

        if (audio is { Length: > 0 })
        {
            if (audio.Length > TamanhoMaximoAudio) return BadRequest("O áudio passa de 30 MB.");
            if (!audio.ContentType.StartsWith("audio/") && audio.ContentType != "video/webm")
                return BadRequest("O arquivo enviado não é um áudio.");
            s.AudioArquivo = await GuardarAudio(audio, s.Protocolo, ct);
            s.AudioDuracaoSegundos = req.AudioDuracaoSegundos is > 0 ? (int)Math.Round(req.AudioDuracaoSegundos.Value) : null;
        }

        db.Solicitacoes.Add(s);
        await db.SaveChangesAsync(ct);

        // Distância até o local (para o orçamento e o aviso), calculada uma vez por local.
        if (local is not null) await deslocamentos.GarantirDistancia(local, ct);

        var linkTutor = $"{Links.Base(Request)}/autorizacao.html?t={s.TokenTutor}";
        await avisos.NovaSolicitacao(s.Id, linkTutor, ct);

        return Ok(new
        {
            s.Protocolo,
            Veterinario = new { vet.Nome, vet.Crmv, vet.Uf },
            LinkTutor = linkTutor,
        });
    }

    /// <summary>Grava em dados/audios (fora da pasta pública). Nome = protocolo + extensão.</summary>
    async Task<string> GuardarAudio(IFormFile audio, string protocolo, CancellationToken ct)
    {
        var pasta = Path.Combine(env.ContentRootPath, "dados", "audios");
        Directory.CreateDirectory(pasta);
        var nome = protocolo + Extensao(audio);
        await using var arquivo = System.IO.File.Create(Path.Combine(pasta, nome));
        await audio.CopyToAsync(arquivo, ct);
        return nome;
    }

    static string Extensao(IFormFile audio)
    {
        var tipo = audio.ContentType.Split(';')[0].Trim();
        return tipo switch
        {
            "audio/webm" or "video/webm" => ".webm",
            "audio/ogg" => ".ogg",
            "audio/mp4" or "audio/x-m4a" or "audio/m4a" => ".m4a",
            "audio/mpeg" or "audio/mp3" => ".mp3",
            "audio/wav" or "audio/x-wav" => ".wav",
            "audio/aac" => ".aac",
            _ => Path.GetExtension(audio.FileName) is { Length: > 1 and <= 5 } e ? e.ToLowerInvariant() : ".audio",
        };
    }

    static string? Limpar(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
    static string? NuloSeVazio(string s) => s.Length == 0 ? null : s;
}

public record SolicitacaoRequest(
    string Convite,
    // Nulo quando o veterinário já cadastrado só confirmou ("Sou eu").
    VeterinarioDados? Veterinario,
    double? AudioDuracaoSegundos,
    string? PetNome,
    Especie? Especie,
    string? Raca,
    string? Idade,
    Olho? Olho,
    Prioridade? Prioridade,
    string? QueixaHistorico,
    string? Medicacoes,
    int? LocalId,
    NovoLocalDados? NovoLocal,
    string? TratadorNome,
    string? TratadorCelular,
    string? TutorNome,
    string? TutorCelular,
    bool? TutorCienteCusto,
    bool VeterinarioRecebeRelatorios);

public record VeterinarioDados(string Nome, string Crmv, string Uf);

public record NovoLocalDados(
    string Nome, TipoLocal Tipo,
    string? Cep, string? Rua, string? Numero, string? Complemento, string? Bairro, string? Cidade, string? Uf,
    string? Referencia,
    // Preenchidos quando o local foi escolhido no Google Maps.
    string? GooglePlaceId, double? Latitude, double? Longitude);
