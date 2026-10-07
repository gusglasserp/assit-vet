using CavaniVets.Api.Data;
using CavaniVets.Api.Domain;
using CavaniVets.Api.Servicos;
using CavaniVets.Api.Integracoes.Email;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CavaniVets.Api.Controllers;

/// <summary>
/// Atalhos só para desenvolvimento, enquanto a tela interna da clínica não existe.
/// </summary>
[ApiController]
[Route("api/dev")]
[SenhaClinica]
[ApiExplorerSettings(IgnoreApi = true)]
public class DevController(CavaniDbContext db, IWebHostEnvironment env) : ControllerBase
{
    /// <summary>
    /// Abre a página do veterinário com um convite para o celular informado.
    /// Ex.: /api/dev/convite?cel=11999990000 (veterinário) ou &amp;clinica=true (assistente preenchendo).
    /// </summary>
    [HttpGet("convite")]
    public async Task<IActionResult> Convite(string cel = "11999990000", bool clinica = false, CancellationToken ct = default)
    {
        if (!env.IsDevelopment()) return NotFound();
        var celular = Documentos.SoDigitos(cel);
        var modo = clinica ? PreenchidoPor.Clinica : PreenchidoPor.Veterinario;
        var convite = await db.Convites.FirstOrDefaultAsync(x => x.Celular == celular && x.PreenchidoPor == modo, ct);
        if (convite is null)
        {
            convite = db.Convites.Add(new Convite { Token = Codigos.Token(), Celular = celular, PreenchidoPor = modo }).Entity;
            await db.SaveChangesAsync(ct);
        }
        return Redirect($"/solicitacao.html?c={convite.Token}");
    }

    /// <summary>Manda um e-mail de teste para a caixa de avisos da clínica: /api/dev/email-teste</summary>
    [HttpGet("email-teste")]
    public async Task<IActionResult> EmailTeste([FromServices] EmailSender email, [FromServices] IOptions<EmailOptions> op, CancellationToken ct)
    {
        if (!env.IsDevelopment()) return NotFound();
        if (!email.Configurado) return BadRequest("E-mail não configurado: falta Email:SenhaApp nos user-secrets.");
        await email.Enviar(op.Value.AvisosPara, "Teste do sistema Cavani Vets",
            "<p>Este é um e-mail de teste do sistema da <b>Clínica Cavani Vets</b>.</p><p>Se chegou, o envio está funcionando.</p>",
            "Este é um e-mail de teste do sistema da Clínica Cavani Vets. Se chegou, o envio está funcionando.", ct);
        return Ok($"E-mail de teste enviado para {op.Value.AvisosPara}.");
    }

    /// <summary>Abra no navegador: http://localhost:5273/api/dev/solicitacao-exemplo</summary>
    [HttpGet("solicitacao-exemplo")]
    public async Task<IActionResult> SolicitacaoExemplo(CancellationToken ct)
    {
        if (!env.IsDevelopment()) return NotFound();

        var vet = await db.Veterinarios.SingleOrDefaultAsync(x => x.Celular == "11999990000", ct)
                  ?? db.Veterinarios.Add(new Veterinario { Celular = "11999990000", Nome = "Dr. Marcos Almeida", Crmv = "12345", Uf = "SP" }).Entity;
        var local = await db.Locais.FirstOrDefaultAsync(x => x.Nome == "Haras Santa Fé", ct)
                    ?? db.Locais.Add(new Local { Nome = "Haras Santa Fé", Tipo = TipoLocal.Haras, Cidade = "Itu" }).Entity;

        var s = new Solicitacao
        {
            Protocolo = Codigos.Protocolo(),
            TokenTutor = Codigos.Token(),
            Status = StatusSolicitacao.AguardandoTutor,
            PreenchidoPor = PreenchidoPor.Veterinario,
            Veterinario = vet,
            Local = local,
            PetNome = "Trovão",
            Especie = Especie.Equino,
            Raca = "Quarto de milha",
            Idade = "8 anos",
            Olho = Olho.OD,
            Prioridade = Prioridade.Ate48h,
            QueixaHistorico = "Lacrimejamento e blefaroespasmo no olho direito há 3 dias.",
            TutorNome = "Maria da Silva",
            TutorCelular = "11988887777",
            VeterinarioRecebeRelatorios = true,
        };
        db.Solicitacoes.Add(s);
        await db.SaveChangesAsync(ct);

        return Redirect($"/autorizacao.html?t={s.TokenTutor}");
    }
}
