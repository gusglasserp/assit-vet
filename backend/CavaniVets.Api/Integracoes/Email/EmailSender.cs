using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace CavaniVets.Api.Integracoes.Email;

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

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(_op.Host, _op.Porta, SecureSocketOptions.StartTls, ct);
        await smtp.AuthenticateAsync(_op.Remetente, _op.SenhaApp.Replace(" ", ""), ct);
        await smtp.SendAsync(msg, ct);
        await smtp.DisconnectAsync(true, ct);
        log.LogInformation("E-mail \"{Assunto}\" enviado", assunto);
    }
}
