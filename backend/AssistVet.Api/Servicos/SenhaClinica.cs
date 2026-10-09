using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AssistVet.Api.Servicos;

/// <summary>
/// Protege rotas internas da clínica (painel, cadastro de clientes, Conta Azul, convites, atalhos de teste)
/// com a senha da clínica, enviada no cabeçalho X-Senha-Clinica. A senha fica em user-secrets
/// (Clinica:Senha) ou variável de ambiente (Clinica__Senha).
/// Sem senha configurada: liberado só para quem acessa da própria máquina (localhost); de fora, bloqueado.
/// TODO: trocar por login individual (Dra. e assistente) quando a tela interna crescer.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class SenhaClinicaAttribute : Attribute, IAsyncAuthorizationFilter
{
    public const string Cabecalho = "X-Senha-Clinica";

    /// <summary>Só da própria máquina, com ou sem senha (ex.: login do Conta Azul, aberto pelo navegador).</summary>
    public bool SomenteLocal { get; set; }

    public Task OnAuthorizationAsync(AuthorizationFilterContext ctx)
    {
        var http = ctx.HttpContext;
        var senha = http.RequestServices.GetRequiredService<IConfiguration>()["Clinica:Senha"];
        // Pelo túnel o acesso também chega como localhost, mas com cabeçalhos de proxy; nesse caso é de fora.
        var local = http.Connection.RemoteIpAddress is { } ip && System.Net.IPAddress.IsLoopback(ip)
                    && !http.Request.Headers.ContainsKey("X-Forwarded-For")
                    && !http.Request.Headers.ContainsKey("Cf-Connecting-Ip");

        if (SomenteLocal || string.IsNullOrEmpty(senha))
        {
            if (!local)
                ctx.Result = new ObjectResult(SomenteLocal
                    ? "Disponível só no computador da clínica."
                    : "Configure a senha da clínica (Clinica:Senha) para acessar de fora.") { StatusCode = 403 };
            return Task.CompletedTask;
        }

        var enviada = http.Request.Headers[Cabecalho].ToString();
        if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(enviada), Encoding.UTF8.GetBytes(senha)))
            ctx.Result = new ObjectResult("Senha da clínica inválida.") { StatusCode = 401 };
        return Task.CompletedTask;
    }
}
