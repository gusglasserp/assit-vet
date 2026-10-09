using System.Net;
using System.Security.Cryptography;
using System.Text;
using AssistVet.Api.Integracoes.Email;
using Microsoft.Extensions.Caching.Memory;

namespace AssistVet.Api.Servicos;

/// <summary>
/// Código de 6 números enviado ao e-mail do cadastro para liberar os dados na página do tutor, quando o
/// celular do link não confirma que é a mesma pessoa. Vale 10 minutos, aceita 5 tentativas, reenvio após
/// 1 minuto e no máximo 3 envios por hora. Guarda só o hash do código, em memória.
/// </summary>
public class ConfirmacaoPorEmail(IMemoryCache cache, EmailSender email, ILogger<ConfirmacaoPorEmail> log)
{
    static readonly TimeSpan Validade = TimeSpan.FromMinutes(10);
    static readonly TimeSpan IntervaloReenvio = TimeSpan.FromMinutes(1);
    const int MaxTentativas = 5, MaxEnviosPorHora = 3;

    sealed class Pendente
    {
        public required byte[] Hash { get; init; }
        public required DateTimeOffset ExpiraEm { get; init; }
        public int Tentativas { get; set; }
    }

    sealed class Envios
    {
        public List<DateTimeOffset> Horarios { get; } = [];
    }

    static string Chave(string tipo, string token, string documento) => $"confirmacao:{tipo}:{token}:{documento}";

    /// <summary>Código já confirmado neste link para este documento (vale 30 minutos).</summary>
    public bool Confirmado(string token, string documento) => cache.TryGetValue(Chave("ok", token, documento), out _);

    /// <summary>Envia um código novo. Devolve a mensagem de erro, ou nulo se enviou.</summary>
    /// <param name="finalidade">Completa "Seu código para ...", ex.: "confirmar o cadastro", "entrar na sua área".</param>
    public async Task<string?> Enviar(string token, string documento, string emailDestino, string nome, string finalidade, CancellationToken ct)
    {
        var agora = DateTimeOffset.UtcNow;
        var envios = cache.GetOrCreate(Chave("envios", token, documento), e =>
        {
            e.SlidingExpiration = TimeSpan.FromHours(1);
            return new Envios();
        })!;
        lock (envios)
        {
            envios.Horarios.RemoveAll(h => agora - h > TimeSpan.FromHours(1));
            if (envios.Horarios.Count >= MaxEnviosPorHora) return "Muitos envios de código. Tente de novo mais tarde ou preencha seus dados.";
            if (envios.Horarios.Count > 0 && agora - envios.Horarios[^1] < IntervaloReenvio) return "Aguarde um minuto para pedir outro código.";
            envios.Horarios.Add(agora);
        }

        var codigo = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        cache.Set(Chave("pendente", token, documento),
            new Pendente { Hash = Hash(token, documento, codigo), ExpiraEm = agora + Validade }, Validade);

        var primeiro = nome.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
        var html = $"""
            <div style="font-family:Arial,Helvetica,sans-serif;font-size:15px;line-height:1.55;color:#14262F;max-width:520px">
              <p style="font-size:18px;font-weight:bold;color:#0E3A53;margin:0 0 16px">Clínica Pimentel Vets</p>
              <p>Olá, {WebUtility.HtmlEncode(primeiro)}!</p>
              <p>Seu código para {WebUtility.HtmlEncode(finalidade)} é:</p>
              <p style="font-size:30px;font-weight:bold;letter-spacing:6px;color:#0E3A53;margin:8px 0 16px">{codigo}</p>
              <p style="color:#56707C;font-size:14px">Ele vale 10 minutos. Se você não pediu este código, pode ignorar este e-mail.</p>
            </div>
            """;
        try
        {
            // Código fora do assunto: o assunto vai para o log.
            await email.Enviar(emailDestino, "Código de confirmação · Clínica Pimentel Vets", html,
                $"Seu código para {finalidade} na Clínica Pimentel Vets é {codigo}. Ele vale 10 minutos.", ct);
            return null;
        }
        catch (Exception e)
        {
            log.LogError(e, "Falha ao enviar código de confirmação");
            return "Não foi possível enviar o código agora. Tente de novo ou preencha seus dados.";
        }
    }

    /// <summary>Confere o código. Devolve a mensagem de erro, ou nulo se está certo.</summary>
    public string? Verificar(string token, string documento, string codigo)
    {
        if (!cache.TryGetValue(Chave("pendente", token, documento), out Pendente? p) || p is null || p.ExpiraEm < DateTimeOffset.UtcNow)
            return "O código expirou. Peça um novo.";
        lock (p)
        {
            if (p.Tentativas >= MaxTentativas) return "Muitas tentativas. Peça um novo código.";
            p.Tentativas++;
        }
        var digitos = new string((codigo ?? "").Where(char.IsDigit).ToArray());
        if (!CryptographicOperations.FixedTimeEquals(p.Hash, Hash(token, documento, digitos)))
            return "Código incorreto. Confira o e-mail e tente de novo.";

        cache.Remove(Chave("pendente", token, documento));
        cache.Set(Chave("ok", token, documento), true, TimeSpan.FromMinutes(30));
        return null;
    }

    /// <summary>"joana.silva@gmail.com" → "j****@gmail.com".</summary>
    public static string Mascarar(string email)
    {
        var i = email.IndexOf('@');
        return i <= 0 ? "****" : email[0] + "****" + email[i..];
    }

    static byte[] Hash(string token, string documento, string codigo) =>
        SHA256.HashData(Encoding.UTF8.GetBytes($"{token}|{documento}|{codigo}"));
}
