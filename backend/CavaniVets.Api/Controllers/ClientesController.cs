using CavaniVets.Api.Data;
using CavaniVets.Api.Domain;
using CavaniVets.Api.Integracoes.ContaAzul;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CavaniVets.Api.Controllers;

/// <summary>
/// Cadastro do responsável financeiro (tutor). O formulário começa pelo CPF/CNPJ:
/// se já existe no sistema ou no Conta Azul, os dados vêm preenchidos; senão, é cadastro novo.
/// </summary>
[ApiController]
[Route("api/clientes")]
public class ClientesController(CavaniDbContext db, ContaAzulClient contaAzul) : ControllerBase
{
    // Catch-all para aceitar CNPJ formatado, que tem barra (11.222.333/0001-81).
    [HttpGet("{*documento}")]
    public async Task<IActionResult> Buscar(string documento, CancellationToken ct)
    {
        var doc = Documentos.SoDigitos(documento);
        var tipo = Documentos.Tipo(doc);
        if (tipo is null) return BadRequest("CPF ou CNPJ inválido.");

        var tutor = await db.Tutores.AsNoTracking().SingleOrDefaultAsync(x => x.Documento == doc, ct);
        if (tutor is not null) return Ok(ClienteDto.De(tutor));

        var resumo = await contaAzul.BuscarPessoaPorDocumento(doc, ct);
        if (resumo is not null && await contaAzul.ObterPessoa(resumo.Id, ct) is { } pessoa)
            return Ok(ClienteDto.De(pessoa, doc, tipo.Value));

        return NotFound(new { documento = doc, tipoPessoa = tipo, mensagem = "Cadastro novo." });
    }

    /// <summary>Salva no sistema e no Conta Azul: cria se não existir lá, senão atualiza nome, contato e endereço.</summary>
    [HttpPost]
    public async Task<IActionResult> Salvar(ClienteDto req, CancellationToken ct)
    {
        var doc = Documentos.SoDigitos(req.Documento);
        var tipo = Documentos.Tipo(doc);
        if (tipo is null) return BadRequest("CPF ou CNPJ inválido.");
        if (string.IsNullOrWhiteSpace(req.Nome)) return BadRequest("Informe o nome.");

        var tutor = await db.Tutores.SingleOrDefaultAsync(x => x.Documento == doc, ct);
        if (tutor is null)
        {
            tutor = new Tutor { Documento = doc, Nome = "", Celular = "", Email = "", Cep = "", Uf = "", Cidade = "", Rua = "", Bairro = "", Numero = "" };
            db.Tutores.Add(tutor);
        }
        tutor.TipoPessoa = tipo.Value;
        tutor.Nome = req.Nome.Trim();
        tutor.Celular = Documentos.SoDigitos(req.Celular);
        tutor.Email = (req.Email ?? "").Trim();
        tutor.Cep = Documentos.SoDigitos(req.Cep);
        tutor.Uf = (req.Uf ?? "").Trim().ToUpperInvariant();
        tutor.Cidade = (req.Cidade ?? "").Trim();
        tutor.Rua = (req.Rua ?? "").Trim();
        tutor.Bairro = (req.Bairro ?? "").Trim();
        tutor.Numero = (req.Numero ?? "").Trim();
        tutor.Complemento = string.IsNullOrWhiteSpace(req.Complemento) ? null : req.Complemento.Trim();
        tutor.ContaAzulId ??= req.ContaAzulId;
        tutor.AtualizadoEm = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        // Salvo localmente antes: se o Conta Azul falhar, basta reenviar.
        var criadoNoContaAzul = false;
        tutor.ContaAzulId ??= (await contaAzul.BuscarPessoaPorDocumento(doc, ct))?.Id;
        if (tutor.ContaAzulId is not null)
        {
            // Já existe lá: envia o que pode ter sido corrigido no formulário.
            var atual = await contaAzul.ObterPessoa(tutor.ContaAzulId, ct);
            var idEndereco = atual?.Enderecos?.FirstOrDefault()?.Id;
            await contaAzul.AtualizarPessoa(tutor.ContaAzulId, new PessoaAtualizar(
                Nome: tutor.Nome,
                Email: NuloSeVazio(tutor.Email),
                TelefoneCelular: NuloSeVazio(tutor.Celular),
                Enderecos: [new EnderecoPessoa(tutor.Rua, tutor.Numero, tutor.Complemento, tutor.Bairro, tutor.Cidade, tutor.Uf, tutor.Cep, Id: idEndereco)]), ct);
            await db.SaveChangesAsync(ct);
        }
        else
        {
            tutor.ContaAzulId = await contaAzul.CriarPessoa(new PessoaCriar(
                Nome: tutor.Nome,
                TipoPessoa: tipo == TipoPessoa.Fisica ? "Física" : "Jurídica",
                Cpf: tipo == TipoPessoa.Fisica ? doc : null,
                Cnpj: tipo == TipoPessoa.Juridica ? doc : null,
                Email: NuloSeVazio(tutor.Email),
                TelefoneCelular: NuloSeVazio(tutor.Celular),
                Enderecos: [new EnderecoPessoa(tutor.Rua, tutor.Numero, tutor.Complemento, tutor.Bairro, tutor.Cidade, tutor.Uf, tutor.Cep)],
                Perfis: [new PerfilPessoa("Cliente")]), ct);
            criadoNoContaAzul = true;
            await db.SaveChangesAsync(ct);
        }

        return Ok(new { tutorId = tutor.Id, contaAzulId = tutor.ContaAzulId, criadoNoContaAzul, atualizadoNoContaAzul = !criadoNoContaAzul });
    }

    static string? NuloSeVazio(string s) => string.IsNullOrWhiteSpace(s) ? null : s;
}

/// <summary>Dados do cliente para preencher e enviar o formulário.</summary>
public record ClienteDto(
    string Documento,
    string Nome,
    string? Celular,
    string? Email,
    string? Cep,
    string? Uf,
    string? Cidade,
    string? Rua,
    string? Bairro,
    string? Numero,
    string? Complemento,
    string? ContaAzulId = null,
    TipoPessoa? TipoPessoa = null,
    /// <summary>"sistema" ou "contaAzul": onde o cadastro foi encontrado.</summary>
    string? Origem = null)
{
    public static ClienteDto De(Tutor t) => new(
        t.Documento, t.Nome, t.Celular, t.Email, t.Cep, t.Uf, t.Cidade, t.Rua, t.Bairro, t.Numero, t.Complemento,
        t.ContaAzulId, t.TipoPessoa, "sistema");

    public static ClienteDto De(Pessoa p, string documento, TipoPessoa tipo)
    {
        var e = p.Enderecos?.FirstOrDefault();
        return new(
            documento, p.Nome ?? "", Documentos.SoDigitos(p.TelefoneCelular ?? p.TelefoneComercial), p.Email,
            Documentos.SoDigitos(e?.Cep), e?.Estado, e?.Cidade, e?.Logradouro, e?.Bairro, e?.Numero, e?.Complemento,
            p.Id, tipo, "contaAzul");
    }
}
