namespace CavaniVets.Api.Servicos;

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
}
