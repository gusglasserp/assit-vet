namespace AssistVet.Api.Servicos;

/// <summary>
/// Endereço base dos links enviados por WhatsApp e e-mail (convite, página do tutor, PDFs).
/// Usa Site:UrlPublica quando configurado (túnel de teste ou site publicado); senão, o endereço do próprio pedido.
/// Sem isso, um convite gerado no painel em localhost sairia com um link que não abre no celular.
/// </summary>
public static class Links
{
    public static string Base(HttpRequest req)
    {
        var publica = req.HttpContext.RequestServices.GetRequiredService<IConfiguration>()["Site:UrlPublica"];
        return string.IsNullOrWhiteSpace(publica) ? $"{req.Scheme}://{req.Host}" : publica.TrimEnd('/');
    }

    /// <summary>Área do tutor (login por CPF + código no e-mail).</summary>
    public static string AreaTutor(HttpRequest req) => $"{Base(req)}/minha-area.html";

    /// <summary>Link direto do PDF de um relatório, para enviar pelo WhatsApp.</summary>
    public static string Relatorio(HttpRequest req, string token) => $"{Base(req)}/api/relatorios/{token}";
}
