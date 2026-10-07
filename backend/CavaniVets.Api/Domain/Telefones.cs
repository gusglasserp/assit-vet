namespace CavaniVets.Api.Domain;

/// <summary>
/// Comparação de celulares brasileiros. O mesmo número pode aparecer com ou sem o 9º dígito
/// (cadastros antigos e alguns números vindos do WhatsApp), então "11 98765-4321" = "11 8765-4321".
/// </summary>
public static class Telefones
{
    /// <summary>O número (só dígitos, com DDD) e a forma com/sem o 9º dígito, quando for celular.</summary>
    public static IReadOnlyList<string> Variantes(string? telefone)
    {
        var d = Documentos.SoDigitos(telefone);
        if (d.StartsWith("55") && d.Length is 12 or 13) d = d[2..];
        return d.Length switch
        {
            11 when d[2] == '9' => [d, d[..2] + d[3..]],
            10 when d[2] is >= '6' and <= '9' => [d, d[..2] + "9" + d[2..]],
            10 or 11 => [d],
            _ => [],
        };
    }

    public static bool Mesmo(string? a, string? b)
    {
        var vb = Variantes(b);
        return Variantes(a).Any(vb.Contains);
    }
}
