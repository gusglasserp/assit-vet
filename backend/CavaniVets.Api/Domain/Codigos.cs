using System.Security.Cryptography;

namespace CavaniVets.Api.Domain;

/// <summary>Códigos gerados pelo sistema.</summary>
public static class Codigos
{
    /// <summary>Token aleatório para links (24 caracteres, seguro para URL).</summary>
    public static string Token() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(18)).Replace('+', '-').Replace('/', '_');

    /// <summary>Protocolo legível da solicitação, ex.: CV-202610-3F9A2.</summary>
    public static string Protocolo() => $"CV-{DateTime.Now:yyyyMM}-{RandomNumberGenerator.GetHexString(5)}";
}
