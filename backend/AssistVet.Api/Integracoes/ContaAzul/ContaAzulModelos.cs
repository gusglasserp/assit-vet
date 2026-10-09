namespace AssistVet.Api.Integracoes.ContaAzul;

// Contratos da API v2 do Conta Azul (https://developers.contaazul.com).
// Serializados em snake_case; só os campos que o sistema usa.

public record TokenResposta(string AccessToken, string RefreshToken, int ExpiresIn);

public record PessoaCriar(
    string Nome,
    string TipoPessoa,
    string? Cpf,
    string? Cnpj,
    string? Email,
    string? TelefoneCelular,
    List<EnderecoPessoa>? Enderecos,
    List<PerfilPessoa> Perfis);

public record EnderecoPessoa(
    string? Logradouro,
    string? Numero,
    string? Complemento,
    string? Bairro,
    string? Cidade,
    string? Estado,
    string? Cep,
    string Pais = "Brasil",
    /// <summary>Vem no GET; na atualização indica qual endereço alterar. Nulo na criação.</summary>
    string? Id = null);

public record PerfilPessoa(string TipoPerfil);

/// <summary>PATCH /v1/pessoas/{id}: só os campos enviados são alterados.</summary>
public record PessoaAtualizar(
    string? Nome = null,
    string? Email = null,
    string? TelefoneCelular = null,
    List<EnderecoPessoa>? Enderecos = null);

public record PessoaResumo(string Id, string? Nome, string? Documento);

public record PessoasPorFiltro(List<PessoaResumo>? Items);

/// <summary>Cadastro completo (GET /v1/pessoas/{id}).</summary>
public record Pessoa(
    string Id,
    string? Nome,
    string? Documento,
    string? TipoPessoa,
    string? Email,
    string? TelefoneCelular,
    string? TelefoneComercial,
    List<EnderecoPessoa>? Enderecos);

public record IdResposta(string Id);

public record OrcamentoCriar(
    string IdCliente,
    string DataOrcamento,
    string DataValidade,
    List<OrcamentoItem> Itens,
    string? Descricao,
    string? Observacoes,
    string? ObservacoesPagamento);

public record OrcamentoItem(string Id, decimal Quantidade, decimal Valor);

public record Servico(string Id, string? Codigo, string? Descricao, decimal? Preco, string? Status);

public record ServicosPorFiltro(List<Servico>? Itens);

public class ContaAzulException(string mensagem, int? statusCode = null) : Exception(mensagem)
{
    public int? StatusCode { get; } = statusCode;
}
