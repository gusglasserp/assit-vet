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

/// <summary>GET /v1/venda/{id}: só o que a edição exige (número, versão, cliente) e a situação atual.</summary>
public record VendaPorId(VendaNegociacao Venda);

public record VendaNegociacao(string Id, long Numero, int Versao, string IdCliente, VendaSituacao? Situacao);

/// <summary>ORCAMENTO, ORCAMENTO_ACEITO, APROVADO, FATURADO, CANCELADO...</summary>
public record VendaSituacao(string? Nome);

/// <summary>
/// POST /v1/venda. A API não converte orçamento em venda (a edição recusa a mudança de situação de
/// orçamento para aprovado), então a venda é criada à parte, citando o número do orçamento.
/// </summary>
public record VendaCriacao(
    string IdCliente,
    long Numero,
    string Situacao,
    string DataVenda,
    List<VendaItem> Itens,
    VendaCondicaoPagamento CondicaoPagamento,
    string? Observacoes,
    string? ObservacoesPagamento);

/// <summary>Id = serviço do Conta Azul. Descrição opcional substitui o nome do serviço na linha.</summary>
public record VendaItem(string Id, decimal Quantidade, decimal Valor, string? Descricao = null);

/// <summary>TipoPagamento: PIX_PAGAMENTO_INSTANTANEO, BOLETO_BANCARIO, TRANSFERENCIA_BANCARIA, CARTAO_CREDITO, DINHEIRO...</summary>
public record VendaCondicaoPagamento(string TipoPagamento, string OpcaoCondicaoPagamento, List<VendaParcela> Parcelas);

public record ExclusaoLote(List<string> Ids);

public record ExclusaoResposta(int Atualizados, int Ignorados);

public record VendaParcela(string DataVencimento, decimal Valor, string? Descricao = null);

public record Servico(string Id, string? Codigo, string? Descricao, decimal? Preco, string? Status);

public record ServicosPorFiltro(List<Servico>? Itens);

public class ContaAzulException(string mensagem, int? statusCode = null) : Exception(mensagem)
{
    public int? StatusCode { get; } = statusCode;
}
