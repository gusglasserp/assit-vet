namespace AssistVet.Api.Servicos;

/// <summary>
/// Pastas dos arquivos enviados (áudios, relatórios). Armazenamento:Pasta fica fora da pasta publicada
/// (no Azure Linux, /home/dados), senão os arquivos somem a cada publicação. Sem configuração, usa dados/
/// dentro do projeto (desenvolvimento).
/// </summary>
public static class Armazenamento
{
    public static string Pasta(IServiceProvider servicos, string subpasta)
    {
        var config = servicos.GetRequiredService<IConfiguration>();
        var env = servicos.GetRequiredService<IWebHostEnvironment>();
        var raiz = config["Armazenamento:Pasta"] is { Length: > 0 } p ? p : Path.Combine(env.ContentRootPath, "dados");
        var pasta = Path.Combine(raiz, subpasta);
        Directory.CreateDirectory(pasta);
        return pasta;
    }
}
