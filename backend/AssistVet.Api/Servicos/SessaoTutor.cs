using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace AssistVet.Api.Servicos;

/// <summary>
/// Login do tutor na área dele (minha-area.html): CPF/CNPJ + código enviado ao e-mail do cadastro.
/// Depois do código, um cookie (HttpOnly, assinado com as chaves do Data Protection) mantém a pessoa
/// conectada por 30 dias, renovados a cada acesso. Sem senha para esquecer.
/// A clínica continua com a senha própria (SenhaClinicaAttribute); são acessos separados.
/// </summary>
public static class SessaoTutor
{
    public const string Esquema = "Tutor";

    public static void Configurar(CookieAuthenticationOptions o)
    {
        o.Cookie.Name = "assistvet_tutor";
        o.Cookie.HttpOnly = true;
        o.Cookie.SameSite = SameSiteMode.Lax;
        o.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        o.ExpireTimeSpan = TimeSpan.FromDays(30);
        o.SlidingExpiration = true;
        // É uma API: sem sessão responde 401 (a página mostra o login) em vez de redirecionar.
        o.Events.OnRedirectToLogin = c => { c.Response.StatusCode = StatusCodes.Status401Unauthorized; return Task.CompletedTask; };
        o.Events.OnRedirectToAccessDenied = c => { c.Response.StatusCode = StatusCodes.Status403Forbidden; return Task.CompletedTask; };
    }

    public static Task Entrar(HttpContext http, int tutorId, string nome)
    {
        var identidade = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, tutorId.ToString()), new Claim(ClaimTypes.Name, nome)], Esquema);
        return http.SignInAsync(Esquema, new ClaimsPrincipal(identidade), new AuthenticationProperties { IsPersistent = true });
    }

    public static Task Sair(HttpContext http) => http.SignOutAsync(Esquema);

    public static int? TutorId(ClaimsPrincipal usuario) =>
        int.TryParse(usuario.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
}
