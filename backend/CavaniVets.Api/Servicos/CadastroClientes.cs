using CavaniVets.Api.Data;
using CavaniVets.Api.Domain;
using CavaniVets.Api.Integracoes.ContaAzul;
using Microsoft.EntityFrameworkCore;

namespace CavaniVets.Api.Servicos;

/// <summary>
/// Cadastro do responsável financeiro (tutor), no sistema e no Conta Azul.
/// Usado pela página do tutor (com as verificações do link) e pela futura tela interna.
/// </summary>
public class CadastroClientes(CavaniDbContext db, ContaAzulClient contaAzul, ILogger<CadastroClientes> log)
{
    /// <summary>Cadastro encontrado e os telefones dele, para conferir com o celular do link.</summary>
    public record Encontrado(ClienteDto Dados, IReadOnlyList<string> Telefones, int? TutorId);

    public record Salvo(int TutorId, string ContaAzulId, bool CriadoNoContaAzul);

    /// <summary>Procura pelo CPF/CNPJ (só dígitos) no sistema e, se não achar, no Conta Azul.</summary>
    public async Task<Encontrado?> Buscar(string documento, TipoPessoa tipo, CancellationToken ct)
    {
        var tutor = await db.Tutores.AsNoTracking().SingleOrDefaultAsync(x => x.Documento == documento, ct);
        if (tutor is not null) return new(ClienteDto.De(tutor), [tutor.Celular], tutor.Id);

        var resumo = await contaAzul.BuscarPessoaPorDocumento(documento, ct);
        if (resumo is not null && await contaAzul.ObterPessoa(resumo.Id, ct) is { } p)
            return new(ClienteDto.De(p, documento, tipo),
                new[] { p.TelefoneCelular, p.TelefoneComercial }.OfType<string>().ToList(), null);

        return null;
    }

    /// <summary>
    /// Primeiro nome de quem tem este celular, quando há uma única pessoa com ele (sistema ou Conta Azul).
    /// Só serve para cumprimentar; os dados continuam exigindo o CPF/CNPJ.
    /// </summary>
    public async Task<string?> ReconhecerPorCelular(string? celular, CancellationToken ct)
    {
        var variantes = Telefones.Variantes(celular);
        if (variantes.Count == 0) return null;

        var locais = await db.Tutores.AsNoTracking().Where(x => variantes.Contains(x.Celular)).Select(x => x.Nome).ToListAsync(ct);
        if (locais.Count > 0) return locais.Count == 1 ? PrimeiroNome(locais[0]) : null;

        try
        {
            var pessoas = new Dictionary<string, string?>();
            foreach (var v in variantes)
                foreach (var p in await contaAzul.BuscarPessoasPorTelefone(v, ct))
                    pessoas[p.Id] = p.Nome;
            return pessoas.Count == 1 ? PrimeiroNome(pessoas.Values.First()) : null;
        }
        catch (ContaAzulException e)
        {
            // Cumprimentar pelo nome é um extra: sem Conta Azul, a página segue normal.
            log.LogWarning(e, "Não foi possível buscar o celular no Conta Azul");
            return null;
        }
    }

    /// <summary>Salva no sistema e no Conta Azul: cria se não existir lá, senão atualiza nome, contato e endereço.</summary>
    /// <exception cref="ArgumentException">Dados inválidos; a mensagem vai para o usuário.</exception>
    public async Task<Salvo> Salvar(ClienteDto req, CancellationToken ct)
    {
        var doc = Documentos.SoDigitos(req.Documento);
        var tipo = Documentos.Tipo(doc) ?? throw new ArgumentException("CPF ou CNPJ inválido.");
        if (string.IsNullOrWhiteSpace(req.Nome)) throw new ArgumentException("Informe o nome.");

        var tutor = await db.Tutores.SingleOrDefaultAsync(x => x.Documento == doc, ct);
        if (tutor is null)
        {
            tutor = new Tutor { Documento = doc, Nome = "", Celular = "", Email = "", Cep = "", Uf = "", Cidade = "", Rua = "", Bairro = "", Numero = "" };
            db.Tutores.Add(tutor);
        }
        tutor.TipoPessoa = tipo;
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
        tutor.AtualizadoEm = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        // Salvo localmente antes: se o Conta Azul falhar, basta reenviar.
        var criado = false;
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
            criado = true;
        }
        await db.SaveChangesAsync(ct);

        return new(tutor.Id, tutor.ContaAzulId, criado);
    }

    static string? NuloSeVazio(string s) => string.IsNullOrWhiteSpace(s) ? null : s;

    static string? PrimeiroNome(string? nome) =>
        nome?.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() is { } n
            ? char.ToUpper(n[0]) + n[1..].ToLowerInvariant()
            : null;
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
    TipoPessoa? TipoPessoa = null,
    // "sistema" ou "contaAzul": onde o cadastro foi encontrado.
    string? Origem = null)
{
    public static ClienteDto De(Tutor t) => new(
        t.Documento, t.Nome, t.Celular, t.Email, t.Cep, t.Uf, t.Cidade, t.Rua, t.Bairro, t.Numero, t.Complemento,
        t.TipoPessoa, "sistema");

    public static ClienteDto De(Pessoa p, string documento, TipoPessoa tipo)
    {
        var e = p.Enderecos?.FirstOrDefault();
        return new(
            documento, p.Nome ?? "", Documentos.SoDigitos(p.TelefoneCelular ?? p.TelefoneComercial), p.Email,
            Documentos.SoDigitos(e?.Cep), e?.Estado, e?.Cidade, e?.Logradouro, e?.Bairro, e?.Numero, e?.Complemento,
            tipo, "contaAzul");
    }
}
