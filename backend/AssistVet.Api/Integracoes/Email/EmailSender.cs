using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace AssistVet.Api.Integracoes.Email;

public class EmailSender(IOptions<EmailOptions> opcoes, ILogger<EmailSender> log)
{
    readonly EmailOptions _op = opcoes.Value;

    public bool Configurado => !string.IsNullOrWhiteSpace(_op.Remetente) && !string.IsNullOrWhiteSpace(_op.SenhaApp);

    /// <summary>Envia um e-mail em HTML (com versão em texto simples para leitores que não mostram HTML).</summary>
    public async Task Enviar(string para, string assunto, string html, string texto, CancellationToken ct = default,
        IReadOnlyList<(string Nome, byte[] Dados)>? anexos = null)
    {
        if (!Configurado) throw new InvalidOperationException("E-mail não configurado (Email:Remetente e Email:SenhaApp).");

        var msg = new MimeMessage();
        msg.From.Add(new MailboxAddress(_op.NomeRemetente, _op.Remetente));
        msg.To.Add(MailboxAddress.Parse(para));
        msg.Subject = assunto;
        var corpo = new BodyBuilder { HtmlBody = html, TextBody = texto };
        foreach (var (nome, dados) in anexos ?? [])
            corpo.Attachments.Add(nome, dados);
        msg.Body = corpo.ToMessageBody();

        // O Gmail às vezes recusa por instantes ("4.3.0 Temporary System Problem") ou derruba a conexão no meio
        // do envio, sobretudo com vários e-mails seguidos. Nesses casos tenta de novo, com uma espera crescente.
        for (var tentativa = 1; ; tentativa++)
        {
            try
            {
                using var smtp = new SmtpClient { Timeout = 30_000 };
                await smtp.ConnectAsync(_op.Host, _op.Porta, SecureSocketOptions.StartTls, ct);
                await smtp.AuthenticateAsync(_op.Remetente, _op.SenhaApp.Replace(" ", ""), ct);
                await smtp.SendAsync(msg, ct);
                await smtp.DisconnectAsync(true, ct);
                log.LogInformation("E-mail \"{Assunto}\" enviado", assunto);
                return;
            }
            catch (Exception e) when (tentativa < 3 && Temporaria(e) && !ct.IsCancellationRequested)
            {
                log.LogWarning(e, "Falha temporária ao enviar \"{Assunto}\" (tentativa {Tentativa}); tentando de novo", assunto, tentativa);
                await Task.Delay(TimeSpan.FromSeconds(3 * tentativa), ct);
            }
        }
    }

    /// <summary>Erros que costumam passar sozinhos: respostas 4xx do servidor, conexão caída, tempo esgotado.</summary>
    static bool Temporaria(Exception e) => e switch
    {
        SmtpCommandException c => (int)c.StatusCode is >= 400 and < 500,
        SmtpProtocolException or IOException or TimeoutException or System.Net.Sockets.SocketException => true,
        OperationCanceledException => false,
        _ => false,
    };
}
