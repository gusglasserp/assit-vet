namespace AssistVet.Api.Integracoes.ContaAzul;

/// <summary>
/// Credenciais do app criado no Portal do Desenvolvedor (developers-portal.contaazul.com).
/// ClientId e ClientSecret ficam em user-secrets no desenvolvimento, nunca no appsettings.
/// </summary>
public class ContaAzulOptions
{
    public const string Secao = "ContaAzul";

    public string ClientId { get; set; } = "";
    public string ClientSecret { get; set; } = "";
    /// <summary>Tem que ser idêntica à cadastrada no app. No app de desenvolvimento é https://www.contaazul.com.</summary>
    public string RedirectUri { get; set; } = "";

    public string UrlLogin { get; set; } = "https://login.contaazul.com/#/oauth/authorize";
    public string UrlApi { get; set; } = "https://api-v2.contaazul.com";
}
