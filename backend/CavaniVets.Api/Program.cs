using System.Text.Json.Serialization;
using CavaniVets.Api.Data;
using CavaniVets.Api.Integracoes.Cep;
using CavaniVets.Api.Integracoes.ContaAzul;
using CavaniVets.Api.Integracoes.Email;
using CavaniVets.Api.Integracoes.Mapas;
using CavaniVets.Api.Servicos;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<CavaniDbContext>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("Cavani")));

builder.Services.AddMemoryCache();
builder.Services.Configure<ContaAzulOptions>(builder.Configuration.GetSection(ContaAzulOptions.Secao));
builder.Services.AddHttpClient<ContaAzulClient>();
builder.Services.AddHttpClient<CepClient>(c => c.Timeout = TimeSpan.FromSeconds(5));
builder.Services.AddScoped<CadastroClientes>();
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection(EmailOptions.Secao));
builder.Services.AddSingleton<EmailSender>();
builder.Services.AddScoped<Avisos>();
builder.Services.AddScoped<TermoPdf>();
builder.Services.Configure<GoogleMapsOptions>(builder.Configuration.GetSection(GoogleMapsOptions.Secao));
builder.Services.AddHttpClient<GoogleMapsClient>(c => c.Timeout = TimeSpan.FromSeconds(10));
builder.Services.Configure<DeslocamentoOptions>(builder.Configuration.GetSection(DeslocamentoOptions.Secao));
builder.Services.AddScoped<Deslocamentos>();
builder.Services.AddScoped<OrcamentosContaAzul>();
builder.Services.AddSingleton<ConfirmacaoPorEmail>();

// Em produção as páginas ficam no Netlify (outra origem). No desenvolvimento libera o localhost.
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .SetIsOriginAllowed(origem => builder.Environment.IsDevelopment()
        ? Uri.TryCreate(origem, UriKind.Absolute, out var u) && u.Host == "localhost"
        : origem == "https://assist-vet.netlify.app")
    .AllowAnyHeader()
    .AllowAnyMethod()));

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // No desenvolvimento a API também serve as páginas da pasta web/ (http://localhost:5273/),
    // assim página e API ficam no mesmo endereço. Em produção as páginas ficam no Netlify.
    var paginas = new PhysicalFileProvider(Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, "..", "..", "web")));
    app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = paginas });
    app.UseStaticFiles(new StaticFileOptions { FileProvider = paginas });
}

app.UseHttpsRedirection();

app.UseCors();

app.UseAuthorization();

app.MapControllers();

app.Run();
