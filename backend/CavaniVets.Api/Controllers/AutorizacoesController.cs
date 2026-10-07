using System.Globalization;
using System.Text;
using CavaniVets.Api.Data;
using CavaniVets.Api.Domain;
using CavaniVets.Api.Servicos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CavaniVets.Api.Controllers;

/// <summary>
/// Página do tutor (autorizacao.html?t=TOKEN). O token é o TokenTutor da solicitação,
/// enviado pela clínica no WhatsApp.
/// </summary>
[ApiController]
[Route("api/autorizacoes/{token}")]
public class AutorizacoesController(CavaniDbContext db, CadastroClientes cadastro, ConfirmacaoPorEmail confirmacao) : ControllerBase
{
    /// <summary>Dados do caso para montar a página: animal, veterinário e local informados na solicitação.</summary>
    [HttpGet]
    public async Task<IActionResult> Caso(string token, CancellationToken ct)
    {
        var s = await db.Solicitacoes.AsNoTracking()
            .Include(x => x.Veterinario).Include(x => x.Local).Include(x => x.Animal)
            .Include(x => x.Autorizacao).ThenInclude(x => x!.Tutor)
            .SingleOrDefaultAsync(x => x.TokenTutor == token, ct);
        if (s is null) return NotFound("Link inválido ou expirado.");

        var termo = await TermoVigente(ct);
        var valores = await TabelaDeValores(ct);

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
            // Primeiro nome de quem já tem cadastro com o celular informado pelo veterinário (só para cumprimentar).
            Reconhecido = s.Autorizacao is null ? await cadastro.ReconhecerPorCelular(s.TutorCelular, ct) : null,
            // Etapa 3: tabela de valores configurável. Etapa 4: termo vigente ({animal} = nome do animal).
            Valores = valores,
            Termo = termo is null ? null : new { termo.Versao, termo.Texto },
            // Link já usado: a página mostra o comprovante em vez das etapas.
            Autorizacao = s.Autorizacao is null ? null : new
            {
                s.Autorizacao.AceitoEm,
                Tutor = s.Autorizacao.Tutor.Nome,
                Documento = s.Autorizacao.Tutor.Documento,
                Orcamento = s.ContaAzulOrcamentoId is not null,
            },
        });
    }

    /// <summary>
    /// Etapa 4: registra o aceite. Confere tudo de novo no servidor e guarda, como prova, a cópia do termo e dos
    /// valores exibidos, data e hora, IP e navegador. Depois avisa o tutor (com a cópia do termo) e a clínica.
    /// </summary>
    [HttpPost("aceite")]
    public async Task<IActionResult> Aceite(string token, AceiteRequest req, [FromServices] Avisos avisos, [FromServices] OrcamentosContaAzul orcamentos, CancellationToken ct)
    {
        var s = await db.Solicitacoes
            .Include(x => x.Tutor).Include(x => x.Animal).Include(x => x.Autorizacao)
            .SingleOrDefaultAsync(x => x.TokenTutor == token, ct);
        if (s is null) return NotFound("Link inválido ou expirado.");
        if (s.Autorizacao is not null) return Conflict("Esta consulta já foi autorizada.");
        if (s.Tutor is null || s.Animal is null) return BadRequest("Complete seus dados e os do animal antes de autorizar.");
        if (!req.AceitouTermo || !req.AceitouResponsabilidadeFinanceira || !req.AceitouLgpd)
            return BadRequest("Marque as três confirmações para autorizar.");
        if (Normalizar(req.NomeAssinado) != Normalizar(s.Tutor.Nome))
            return BadRequest("O nome digitado precisa ser igual ao informado no cadastro.");

        var termo = await TermoVigente(ct);
        if (termo is null) return Problem("Nenhum termo vigente cadastrado.");
        // O termo pode ter mudado enquanto a pessoa lia: ela só pode aceitar a versão que viu.
        if (termo.Versao != req.VersaoTermo) return Conflict("O termo foi atualizado. Recarregue a página e leia a nova versão.");

        var valores = await TabelaDeValores(ct);

        var autorizacao = new Autorizacao
        {
            Solicitacao = s,
            Tutor = s.Tutor,
            Animal = s.Animal,
            VersaoTermo = termo,
            TextoTermoAceito = termo.Texto.Replace("{animal}", s.Animal.Nome),
            ValoresExibidosJson = System.Text.Json.JsonSerializer.Serialize(valores),
            AceitouTermo = true,
            AceitouResponsabilidadeFinanceira = true,
            AceitouLgpd = true,
            NomeAssinado = req.NomeAssinado.Trim(),
            Ip = IpDoCliente(),
            UserAgent = Request.Headers.UserAgent.ToString(),
            AceitoEm = DateTimeOffset.UtcNow,
        };
        db.Autorizacoes.Add(autorizacao);
        s.Status = StatusSolicitacao.Autorizada;
        await db.SaveChangesAsync(ct);

        // Orçamento no Conta Azul (consulta + km estimado) para a clínica enviar ao cliente; falha não desfaz o aceite.
        var orcamento = await orcamentos.CriarParaSolicitacao(s.Id, ct);
        await avisos.ConsultaAutorizada(s.Id, orcamento, ct);

        return Ok(new { s.Protocolo, autorizacao.AceitoEm, Orcamento = orcamento.OrcamentoId is not null });
    }

    /// <summary>PDF do termo assinado (botão "Baixar termo" no comprovante). Só existe depois do aceite.</summary>
    [HttpGet("termo.pdf")]
    public async Task<IActionResult> Pdf(string token, [FromServices] TermoPdf termoPdf, CancellationToken ct)
    {
        var id = await db.Solicitacoes.Where(x => x.TokenTutor == token).Select(x => (int?)x.Id).SingleOrDefaultAsync(ct);
        if (id is null) return NotFound("Link inválido ou expirado.");
        var pdf = await termoPdf.Gerar(id.Value, ct);
        return pdf is { } p ? File(p.Pdf, "application/pdf", p.NomeArquivo) : NotFound("A consulta ainda não foi autorizada.");
    }

    /// <summary>Tabela de valores ativa, como mostrada ao tutor. A estimativa de km fica só para o orçamento interno.</summary>
    Task<List<ValorExibido>> TabelaDeValores(CancellationToken ct) =>
        db.ItensPreco.AsNoTracking().Where(x => x.Ativo).OrderBy(x => x.Ordem)
            .Select(x => new ValorExibido(x.Codigo, x.Grupo, x.Descricao, x.Valor, x.Observacao))
            .ToListAsync(ct);

    /// <summary>PDF do orçamento no Conta Azul (botão "Ver orçamento" no comprovante). Gerado na hora pelo Conta Azul.</summary>
    [HttpGet("orcamento.pdf")]
    public async Task<IActionResult> OrcamentoPdf(string token, [FromServices] Integracoes.ContaAzul.ContaAzulClient contaAzul, CancellationToken ct)
    {
        var s = await db.Solicitacoes.AsNoTracking().Where(x => x.TokenTutor == token)
            .Select(x => new { x.Protocolo, x.ContaAzulOrcamentoId }).SingleOrDefaultAsync(ct);
        if (s is null) return NotFound("Link inválido ou expirado.");
        if (s.ContaAzulOrcamentoId is null) return NotFound("O orçamento ainda não está disponível.");
        var pdf = await contaAzul.ImprimirOrcamento(s.ContaAzulOrcamentoId, ct);
        return pdf is null
            ? StatusCode(StatusCodes.Status502BadGateway, "Não foi possível obter o orçamento agora. Tente de novo em instantes.")
            : File(pdf, "application/pdf", $"orcamento-{s.Protocolo}.pdf");
    }

    Task<VersaoTermo?> TermoVigente(CancellationToken ct) =>
        db.VersoesTermo.Where(x => x.Ativa).OrderByDescending(x => x.VigenteDesde).FirstOrDefaultAsync(ct);

    /// <summary>
    /// IP de quem aceitou. Atrás de proxy (ex.: Netlify repassando /api), o IP real vem em X-Forwarded-For;
    /// guarda os dois para não perder a informação.
    /// TODO: ao publicar, configurar ForwardedHeaders confiando só no proxy, e gravar só o IP real.
    /// </summary>
    string IpDoCliente()
    {
        var remoto = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "desconhecido";
        var repassado = Request.Headers["X-Forwarded-For"].ToString();
        return string.IsNullOrWhiteSpace(repassado) ? remoto : $"{repassado} (via {remoto})";
    }

    /// <summary>
    /// Etapa 1: busca o cadastro pelo CPF/CNPJ. Os dados só voltam preenchidos se o cadastro tiver o mesmo
    /// celular que o veterinário informou para este link (ou se já foi confirmado neste link). Assim quem
    /// recebe o link por engano, ou digita o CPF de outra pessoa, não vê os dados dela.
    /// </summary>
    [HttpGet("cliente/{*documento}")]
    public async Task<IActionResult> BuscarCliente(string token, string documento, CancellationToken ct)
    {
        var s = await db.Solicitacoes.AsNoTracking().SingleOrDefaultAsync(x => x.TokenTutor == token, ct);
        if (s is null) return NotFound("Link inválido ou expirado.");
        var doc = Documentos.SoDigitos(documento);
        if (Documentos.Tipo(doc) is not { } tipo) return BadRequest("CPF ou CNPJ inválido.");

        var achado = await cadastro.Buscar(doc, tipo, ct);
        if (achado is null) return NotFound(new { documento = doc, tipoPessoa = tipo, existe = false });

        var liberado = (achado.TutorId is not null && achado.TutorId == s.TutorId)
                       || achado.Telefones.Any(t => Telefones.Mesmo(t, s.TutorCelular))
                       || confirmacao.Confirmado(token, doc);
        if (liberado) return Ok(achado.Dados);

        // Existe, mas o celular do link não confirma: a página oferece o código por e-mail (se houver e-mail).
        return NotFound(new
        {
            documento = doc,
            tipoPessoa = tipo,
            existe = true,
            emailMascarado = string.IsNullOrWhiteSpace(achado.Dados.Email) ? null : ConfirmacaoPorEmail.Mascarar(achado.Dados.Email),
        });
    }

    /// <summary>Envia o código de confirmação ao e-mail do cadastro desse CPF/CNPJ.</summary>
    [HttpPost("cliente/codigo")]
    public async Task<IActionResult> EnviarCodigo(string token, CodigoRequest req, CancellationToken ct)
    {
        if (!await db.Solicitacoes.AnyAsync(x => x.TokenTutor == token, ct)) return NotFound("Link inválido ou expirado.");
        var doc = Documentos.SoDigitos(req.Documento);
        if (Documentos.Tipo(doc) is not { } tipo) return BadRequest("CPF ou CNPJ inválido.");
        var achado = await cadastro.Buscar(doc, tipo, ct);
        if (achado is null || string.IsNullOrWhiteSpace(achado.Dados.Email)) return BadRequest("Este cadastro não tem e-mail. Preencha seus dados.");

        var erro = await confirmacao.Enviar(token, doc, achado.Dados.Email, achado.Dados.Nome, ct);
        return erro is null
            ? Ok(new { emailMascarado = ConfirmacaoPorEmail.Mascarar(achado.Dados.Email) })
            : StatusCode(StatusCodes.Status429TooManyRequests, erro);
    }

    /// <summary>Confere o código; se certo, devolve os dados do cadastro para a pessoa conferir e alterar.</summary>
    [HttpPost("cliente/verificar")]
    public async Task<IActionResult> VerificarCodigo(string token, CodigoRequest req, CancellationToken ct)
    {
        if (!await db.Solicitacoes.AnyAsync(x => x.TokenTutor == token, ct)) return NotFound("Link inválido ou expirado.");
        var doc = Documentos.SoDigitos(req.Documento);
        if (Documentos.Tipo(doc) is not { } tipo) return BadRequest("CPF ou CNPJ inválido.");

        if (confirmacao.Verificar(token, doc, req.Codigo ?? "") is { } erro) return BadRequest(erro);
        var achado = await cadastro.Buscar(doc, tipo, ct);
        return achado is null ? NotFound("Cadastro não encontrado.") : Ok(achado.Dados);
    }

    /// <summary>Etapa 1: salva o cadastro (sistema e Conta Azul) e liga o tutor a esta solicitação.</summary>
    [HttpPost("cliente")]
    public async Task<IActionResult> SalvarCliente(string token, ClienteDto req, CancellationToken ct)
    {
        var s = await db.Solicitacoes.SingleOrDefaultAsync(x => x.TokenTutor == token, ct);
        if (s is null) return NotFound("Link inválido ou expirado.");
        try
        {
            var salvo = await cadastro.Salvar(req, ct);
            s.TutorId = salvo.TutorId;
            await db.SaveChangesAsync(ct);
            return Ok(salvo);
        }
        catch (ArgumentException e)
        {
            return BadRequest(e.Message);
        }
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

public record AceiteRequest(
    bool AceitouTermo,
    bool AceitouResponsabilidadeFinanceira,
    bool AceitouLgpd,
    string NomeAssinado,
    string VersaoTermo);


public record CodigoRequest(string Documento, string? Codigo = null);
