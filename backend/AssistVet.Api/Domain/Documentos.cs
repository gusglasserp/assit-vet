namespace AssistVet.Api.Domain;

/// <summary>Validação de CPF e CNPJ pelos dígitos verificadores.</summary>
public static class Documentos
{
    public static string SoDigitos(string? s) => new((s ?? "").Where(char.IsDigit).ToArray());

    /// <summary>Física para CPF válido, Jurídica para CNPJ válido, nulo se inválido. Espera só dígitos.</summary>
    public static TipoPessoa? Tipo(string documento) => documento.Length switch
    {
        11 when CpfValido(documento) => TipoPessoa.Fisica,
        14 when CnpjValido(documento) => TipoPessoa.Juridica,
        _ => null,
    };

    public static bool CpfValido(string cpf)
    {
        if (cpf.Length != 11 || cpf.Distinct().Count() == 1) return false;
        int Digito(int tamanho)
        {
            var soma = 0;
            for (var i = 0; i < tamanho; i++) soma += (cpf[i] - '0') * (tamanho + 1 - i);
            var resto = soma % 11;
            return resto < 2 ? 0 : 11 - resto;
        }
        return Digito(9) == cpf[9] - '0' && Digito(10) == cpf[10] - '0';
    }

    public static bool CnpjValido(string cnpj)
    {
        if (cnpj.Length != 14 || cnpj.Distinct().Count() == 1) return false;
        int Digito(int tamanho)
        {
            // Pesos: 5,4,3,2,9,8,7,6,5,4,3,2 para o 1º dígito; 6,5,4,3,2,9,... para o 2º.
            var soma = 0;
            for (var i = 0; i < tamanho; i++) soma += (cnpj[i] - '0') * ((tamanho - i - 1) % 8 + 2);
            var resto = soma % 11;
            return resto < 2 ? 0 : 11 - resto;
        }
        return Digito(12) == cnpj[12] - '0' && Digito(13) == cnpj[13] - '0';
    }
}
