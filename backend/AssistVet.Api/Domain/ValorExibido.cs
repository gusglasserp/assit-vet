namespace AssistVet.Api.Domain;

/// <summary>Item da tabela de valores como foi mostrado ao tutor (também gravado no aceite, como prova).</summary>
public record ValorExibido(string Codigo, string Grupo, string Descricao, decimal? Valor, string? Observacao);
