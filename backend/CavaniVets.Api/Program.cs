using System.Text.Json.Serialization;
using CavaniVets.Api.Data;
using CavaniVets.Api.Integracoes.Cep;
using CavaniVets.Api.Integracoes.ContaAzul;
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

// O app Flutter roda em outra origem. No desenvolvimento libera qualquer porta do localhost
// (o flutter run muda a porta); em produção, só o endereço publicado do app.
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .SetIsOriginAllowed(origem => builder.Environment.IsDevelopment()
        ? new Uri(origem).Host == "localhost"
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
