using System.Text.Json.Serialization;
using AssistVet.Api.Data;
using AssistVet.Api.Integracoes.Cep;
using AssistVet.Api.Integracoes.ContaAzul;
using AssistVet.Api.Integracoes.Email;
using AssistVet.Api.Integracoes.Mapas;
using AssistVet.Api.Servicos;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// Azure SQL (oferta gratuita, serverless): o banco pausa sem uso e a primeira conexão depois da pausa pode
// falhar enquanto ele acorda. As novas tentativas e o tempo maior cobrem esse intervalo.
builder.Services.AddDbContext<AssistVetDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("AssistVet"), sql => sql
        .EnableRetryOnFailure(maxRetryCount: 6, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null)
        .CommandTimeout(60)));

builder.Services.AddMemoryCache();

// Chaves de assinatura (ex.: state do login do Conta Azul) na pasta persistente, para valerem entre
// reinícios e publicações do App Service. Sem Armazenamento:Pasta (desenvolvimento), usa o padrão do .NET.
var protecao = builder.Services.AddDataProtection().SetApplicationName("AssistVet");
if (builder.Configuration["Armazenamento:Pasta"] is { Length: > 0 } pastaDados)
    protecao.PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(pastaDados, "chaves")));
builder.Services.Configure<ContaAzulOptions>(builder.Configuration.GetSection(ContaAzulOptions.Secao));
builder.Services.AddHttpClient<ContaAzulClient>();
builder.Services.AddHttpClient<CepClient>(c => c.Timeout = TimeSpan.FromSeconds(5));
builder.Services.AddScoped<CadastroClientes>();
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection(EmailOptions.Secao));
builder.Services.AddSingleton<EmailSender>();
builder.Services.AddScoped<Avisos>();
builder.Services.AddScoped<TermoPdf>();
builder.Services.Configure<GoogleMapsOptions>(builder.Configuration.GetSection(GoogleMapsOptions.Secao));
builder.Services.AddHttpClient<GoogleMapsClient>(c => c.Timeout = TimeSpan.FromSeconds(20));
builder.Services.Configure<DeslocamentoOptions>(builder.Configuration.GetSection(DeslocamentoOptions.Secao));
builder.Services.AddScoped<Deslocamentos>();
builder.Services.AddScoped<OrcamentosContaAzul>();
builder.Services.AddSingleton<ConfirmacaoPorEmail>();

// No Azure (e no túnel de teste) as requisições chegam por um proxy: o IP real do tutor e o https vêm nos
// cabeçalhos X-Forwarded-*. Necessário para gravar o IP certo no aceite. Só o proxy alcança a aplicação,
// então os cabeçalhos são aceitos de qualquer origem.
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownIPNetworks.Clear();
    o.KnownProxies.Clear();
});

// Páginas e API ficam no mesmo endereço; CORS só para testes locais em outra porta.
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .SetIsOriginAllowed(origem => builder.Environment.IsDevelopment()
        && Uri.TryCreate(origem, UriKind.Absolute, out var u) && u.Host == "localhost")
    .AllowAnyHeader()
    .AllowAnyMethod()));

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseForwardedHeaders();

// Migrações ao iniciar só se Banco:MigrarAoIniciar = true. No App Service gratuito o site dorme e acorda
// várias vezes por dia (inclusive por robôs); migrar a cada início acordaria o Azure SQL gratuito à toa e
// gastaria a cota. Por padrão as migrações são aplicadas na publicação: dotnet ef database update --connection "...".
if (app.Configuration.GetValue<bool>("Banco:MigrarAoIniciar"))
{
    using var escopo = app.Services.CreateScope();
    escopo.ServiceProvider.GetRequiredService<AssistVetDbContext>().Database.Migrate();
}

// Erros do Conta Azul voltam como 502 com a mensagem dele, para facilitar o diagnóstico.
app.UseExceptionHandler(e => e.Run(async ctx =>
{
    if (ctx.Features.Get<IExceptionHandlerFeature>()?.Error is ContaAzulException ex)
    {
        ctx.Response.StatusCode = StatusCodes.Status502BadGateway;
        await ctx.Response.WriteAsJsonAsync(new { erro = ex.Message });
        return;
    }
    ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
    await ctx.Response.WriteAsJsonAsync(new { erro = "Erro interno." });
}));

// A API também serve as páginas (web/). No desenvolvimento, direto da pasta web/ (edições valem na hora);
// na publicação, a pasta web/ é copiada para wwwroot (ver .csproj).
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    var paginas = new PhysicalFileProvider(Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, "..", "..", "web")));
    app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = paginas });
    app.UseStaticFiles(new StaticFileOptions { FileProvider = paginas });
}
else
{
    app.UseDefaultFiles();
    app.UseStaticFiles();
}

app.UseHttpsRedirection();

app.UseCors();

app.UseAuthorization();

app.MapControllers();

app.Run();
