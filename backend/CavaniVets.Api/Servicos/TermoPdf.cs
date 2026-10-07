using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CavaniVets.Api.Data;
using CavaniVets.Api.Domain;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CavaniVets.Api.Servicos;

/// <summary>
/// PDF do termo assinado, gerado a partir do registro do aceite (sempre o mesmo resultado; nada é guardado).
/// Traz um código de verificação (SHA-256 dos dados do aceite): se um PDF for alterado, o código deixa de
/// bater com o que está no banco.
/// </summary>
public class TermoPdf(CavaniDbContext db)
{
    static readonly TimeZoneInfo Brasilia = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
    static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");
    static readonly Lazy<byte[]> Logo = new(() => File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Recursos", "logo.png")));

    const string Ink = "#0E3A53", Muted = "#56707C", Line = "#D5E5EA", Aqua = "#E4F6F9";

    static TermoPdf()
    {
        // Licença gratuita: empresas com faturamento anual abaixo de US$ 1 milhão.
        QuestPDF.Settings.License = LicenseType.Community;
    }

    /// <summary>PDF da autorização da solicitação, ou nulo se ela ainda não foi autorizada.</summary>
    public async Task<(byte[] Pdf, string NomeArquivo)?> Gerar(int solicitacaoId, CancellationToken ct)
    {
        var s = await db.Solicitacoes.AsNoTracking()
            .Include(x => x.Veterinario).Include(x => x.Local)
            .Include(x => x.Autorizacao).ThenInclude(x => x!.Tutor)
            .Include(x => x.Autorizacao).ThenInclude(x => x!.Animal)
            .Include(x => x.Autorizacao).ThenInclude(x => x!.VersaoTermo)
            .SingleOrDefaultAsync(x => x.Id == solicitacaoId, ct);
        if (s?.Autorizacao is not { } a) return null;
        return (Montar(s, a), $"termo-{s.Protocolo}.pdf");
    }

    /// <summary>Código de verificação: SHA-256 dos dados que compõem a prova do aceite.</summary>
    public static string CodigoVerificacao(Solicitacao s, Autorizacao a)
    {
        var dados = string.Join("|", s.Protocolo, a.Tutor.Documento, a.NomeAssinado,
            a.AceitoEm.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.ffffffZ", CultureInfo.InvariantCulture),
            a.Ip, a.UserAgent, a.VersaoTermo.Versao, a.TextoTermoAceito, a.ValoresExibidosJson);
        var hex = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(dados)));
        return string.Join(" ", Enumerable.Range(0, hex.Length / 8).Select(i => hex.Substring(i * 8, 8)));
    }

    static byte[] Montar(Solicitacao s, Autorizacao a)
    {
        var t = a.Tutor;
        var animal = a.Animal;
        var quando = TimeZoneInfo.ConvertTime(a.AceitoEm, Brasilia).ToString("dd/MM/yyyy 'às' HH:mm:ss", PtBr);
        var valores = JsonSerializer.Deserialize<List<ValorExibido>>(a.ValoresExibidosJson) ?? [];
        var paragrafos = a.TextoTermoAceito.Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0).ToList();

        return Document.Create(doc => doc.Page(p =>
        {
            p.Size(PageSizes.A4);
            p.Margin(1.8f, Unit.Centimetre);
            p.DefaultTextStyle(x => x.FontSize(9.5f).FontColor("#14262F").LineHeight(1.35f));

            p.Header().PaddingBottom(10).BorderBottom(1).BorderColor(Line).PaddingBottom(8).Row(r =>
            {
                r.ConstantItem(42).Image(Logo.Value);
                r.RelativeItem().PaddingLeft(10).AlignMiddle().Column(c =>
                {
                    c.Item().Text("Clínica Cavani Vets").FontSize(13).Bold().FontColor(Ink);
                    c.Item().Text("Oftalmologia Veterinária · M.V. Juliane Cavani Pimentel · CRMV-SP 11.064").FontSize(8.5f).FontColor(Muted);
                });
                r.ConstantItem(130).AlignRight().AlignMiddle().Column(c =>
                {
                    c.Item().AlignRight().Text("Protocolo").FontSize(8).FontColor(Muted);
                    c.Item().AlignRight().Text(s.Protocolo).Bold().FontColor(Ink);
                });
            });

            p.Content().PaddingTop(12).Column(col =>
            {
                col.Spacing(10);
                col.Item().Text("Termo de autorização e responsabilidade financeira").FontSize(15).Bold().FontColor(Ink);

                Secao(col, "Responsável financeiro", e => e.Column(c =>
                {
                    Linha(c, t.TipoPessoa == TipoPessoa.Juridica ? "Razão social" : "Nome", t.Nome);
                    Linha(c, t.TipoPessoa == TipoPessoa.Juridica ? "CNPJ" : "CPF", FormatarDocumento(t.Documento));
                    Linha(c, "Celular", FormatarCelular(t.Celular));
                    Linha(c, "E-mail", t.Email);
                    Linha(c, "Endereço", $"{t.Rua}, {t.Numero}{(t.Complemento is null ? "" : " " + t.Complemento)} · {t.Bairro} · {t.Cidade}/{t.Uf} · CEP {FormatarCep(t.Cep)}");
                }));

                Secao(col, "Animal e atendimento", e => e.Column(c =>
                {
                    Linha(c, "Animal", string.Join(" · ", new[] { animal.Nome, NomeEspecie(animal.Especie), animal.Sexo == Sexo.Femea ? "Fêmea" : "Macho", animal.Raca, animal.Idade, animal.Peso }.Where(x => !string.IsNullOrWhiteSpace(x))));
                    Linha(c, "Solicitado por", $"{s.Veterinario.Nome} · CRMV-{s.Veterinario.Uf} {s.Veterinario.Crmv}");
                    if (s.Local is not null) Linha(c, "Local", s.Local.Nome + (s.Local.Cidade is null ? "" : $", {s.Local.Cidade}"));
                }));

                Secao(col, $"Termo (versão {a.VersaoTermo.Versao})", e => e.Column(c =>
                {
                    c.Spacing(4);
                    foreach (var par in paragrafos) c.Item().Text(par).Justify();
                }));

                Secao(col, "Valores apresentados ao responsável", e => e.Table(tb =>
                {
                    tb.ColumnsDefinition(cd => { cd.RelativeColumn(4); cd.RelativeColumn(2); });
                    foreach (var v in valores)
                    {
                        tb.Cell().BorderBottom(0.5f).BorderColor(Line).PaddingVertical(3).Text(v.Descricao);
                        tb.Cell().BorderBottom(0.5f).BorderColor(Line).PaddingVertical(3).AlignRight().Text(
                            v.Valor is { } valor
                                ? valor.ToString("C", PtBr) + (v.Observacao is null ? "" : $" ({v.Observacao})")
                                : v.Observacao ?? "—");
                    }
                }));

                Secao(col, "Confirmações", e => e.Column(c =>
                {
                    c.Item().Text("(x) Li o termo e autorizo a consulta oftalmológica.");
                    c.Item().Text("(x) Assumo a responsabilidade financeira pelo atendimento.");
                    c.Item().Text("(x) Concordo com o uso dos meus dados como descrito no item 6.");
                }));

                col.Item().Background(Aqua).Padding(10).Column(c =>
                {
                    c.Item().PaddingBottom(4).Text("Assinatura eletrônica").Bold().FontColor(Ink);
                    Linha(c, "Assinado por", a.NomeAssinado);
                    Linha(c, "Data e hora", $"{quando} (horário de Brasília)");
                    Linha(c, "Endereço IP", a.Ip);
                    Linha(c, "Dispositivo", a.UserAgent);
                    Linha(c, "Verificação", CodigoVerificacao(s, a));
                    c.Item().PaddingTop(4).Text("Documento gerado a partir do registro eletrônico do aceite. Para confirmar a autenticidade, " +
                                                "informe o protocolo e o código de verificação à clínica.").FontSize(8).FontColor(Muted);
                });
            });

            p.Footer().PaddingTop(6).BorderTop(1).BorderColor(Line).PaddingTop(6).Row(r =>
            {
                r.RelativeItem().Text("Rua Arandu, 885, Brooklin Paulista, São Paulo · (11) 94767-0145 · clinicapimentelvets@gmail.com")
                    .FontSize(7.5f).FontColor(Muted);
                r.ConstantItem(70).AlignRight().Text(x =>
                {
                    x.DefaultTextStyle(st => st.FontSize(7.5f).FontColor(Muted));
                    x.Span("Página ");
                    x.CurrentPageNumber();
                    x.Span(" de ");
                    x.TotalPages();
                });
            });
        })).GeneratePdf();
    }

    /// <summary>Título da seção e, logo abaixo, o conteúdo.</summary>
    static void Secao(ColumnDescriptor col, string titulo, Action<IContainer> conteudo)
    {
        col.Item().PaddingTop(4).Text(titulo).FontSize(10.5f).Bold().FontColor(Ink);
        conteudo(col.Item());
    }

    static void Linha(ColumnDescriptor c, string rotulo, string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor)) return;
        c.Item().Row(r =>
        {
            r.ConstantItem(85).Text(rotulo).FontColor(Muted);
            r.RelativeItem().Text(valor);
        });
    }

    static string NomeEspecie(Especie e) => e switch { Especie.Cao => "Cão", Especie.Gato => "Gato", Especie.Equino => "Equino", _ => "Outro" };

    static string FormatarDocumento(string d) => d.Length == 11
        ? $"{d[..3]}.{d[3..6]}.{d[6..9]}-{d[9..]}"
        : d.Length == 14 ? $"{d[..2]}.{d[2..5]}.{d[5..8]}/{d[8..12]}-{d[12..]}" : d;

    static string FormatarCelular(string d) => d.Length switch
    {
        11 => $"({d[..2]}) {d[2..7]}-{d[7..]}",
        10 => $"({d[..2]}) {d[2..6]}-{d[6..]}",
        _ => d,
    };

    static string FormatarCep(string d) => d.Length == 8 ? $"{d[..5]}-{d[5..]}" : d;
}
