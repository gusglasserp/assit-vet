using System.Globalization;
using System.Net;
using System.Text;
using AssistVet.Api.Data;
using AssistVet.Api.Integracoes.ContaAzul;
using AssistVet.Api.Integracoes.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AssistVet.Api.Servicos;

/// <summary>
/// Avisos por e-mail: nova solicitação (para a clínica enviar o link ao tutor) e consulta autorizada
/// (confirmação ao tutor com o termo em PDF anexo, e aviso à clínica). Falhas ficam no log e não desfazem
/// a operação. Quando houver a API do WhatsApp, os mesmos avisos saem também por lá.
/// </summary>
public class Avisos(AssistVetDbContext db, EmailSender email, TermoPdf termoPdf, Deslocamentos deslocamentos, ContaAzulClient contaAzul,
    IOptions<EmailOptions> opcoes, ILogger<Avisos> log)
{
    static readonly TimeZoneInfo Brasilia = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
    static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    /// <summary>Avisa a clínica de uma solicitação nova, com o caso e o link do tutor pronto para enviar.</summary>
    public async Task NovaSolicitacao(int solicitacaoId, string linkTutor, CancellationToken ct)
    {
        var para = opcoes.Value.AvisosPara;
        if (!email.Configurado || string.IsNullOrWhiteSpace(para))
        {
            log.LogWarning("E-mail não configurado; aviso da solicitação {Id} não enviado", solicitacaoId);
            return;
        }

        var s = await db.Solicitacoes.AsNoTracking()
            .Include(x => x.Veterinario).Include(x => x.Local)
            .SingleAsync(x => x.Id == solicitacaoId, ct);
        var v = s.Veterinario;
        var pet = s.PetNome ?? "animal não informado";
        var urgente = s.Prioridade == Domain.Prioridade.Urgente;
        var quando = TimeZoneInfo.ConvertTime(s.CriadoEm, Brasilia).ToString("dd/MM/yyyy 'às' HH:mm");

        // Mensagem pronta para o tutor, como na página do veterinário quando a clínica preenche.
        var primeiroTutor = s.TutorNome is null ? "" : ", " + PrimeiroNome(s.TutorNome);
        var paraPet = s.PetNome is null ? "" : $" para {(s.PetNome.EndsWith('a') ? "a" : "o")} {s.PetNome}";
        var msg = $"Olá{primeiroTutor}! Aqui é da Clínica Pimentel Vets, oftalmologia veterinária. {v.Nome} solicitou um atendimento " +
                  $"oftalmológico{paraPet}. Para agendarmos, preencha seus dados e autorize a consulta por este link: {linkTutor}";
        var wa = $"https://wa.me/{(s.TutorCelular is null ? "" : "55" + s.TutorCelular)}?text={Uri.EscapeDataString(msg)}";
        var estimativa = await deslocamentos.Estimar(s.LocalId, ct);

        var html = Modelo($"""
            <p style="font-size:17px;margin:0 0 4px"><b>{(urgente ? "<span style=\"color:#B3261E\">URGENTE · </span>" : "")}Nova solicitação de {H(v.Nome)}</b></p>
            <p style="margin:0;color:#56707C">Recebida em {quando}{(s.PreenchidoPor == Domain.PreenchidoPor.Clinica ? " · preenchida pela clínica" : "")}</p>
            {Tabela(("Protocolo", s.Protocolo),
                    ("Veterinário", $"{v.Nome} · CRMV-{v.Uf} {v.Crmv}"), ("Celular", Celular(v.Celular)),
                    ("Animal", string.Join(" · ", new[] { s.PetNome, Especie(s.Especie), s.Raca, s.Idade }.Where(x => !string.IsNullOrWhiteSpace(x)))),
                    ("Olho", s.Olho?.ToString()), ("Prioridade", Prioridade(s.Prioridade)),
                    ("Queixa e histórico", s.QueixaHistorico), ("Medicações", s.Medicacoes),
                    ("Local", LocalTexto(s)), ("Endereço", s.Local?.EnderecoCompleto()), ("Como chegar", s.Local?.Referencia),
                    ("Deslocamento estimado", TextoEstimativa(estimativa)),
                    ("Tratador", s.TratadorNome is null ? null : s.TratadorNome + (s.TratadorCelular is null ? "" : " · " + Celular(s.TratadorCelular))),
                    ("Tutor", s.TutorNome), ("Celular do tutor", s.TutorCelular is null ? "não informado" : Celular(s.TutorCelular)),
                    ("Tutor sabe do custo", s.TutorCienteCusto switch { true => "Sim", false => "Ainda não", _ => null }),
                    ("Relatórios", s.VeterinarioRecebeRelatorios ? "Tutor e veterinário" : "Somente tutor"))}
            {BotoesMapa(s.Local)}
            <p style="margin:20px 0 8px"><b>Próximo passo:</b> enviar o link de autorização ao tutor.</p>
            <p style="margin:0 0 12px">
              <a href="{H(wa)}" style="display:inline-block;background:#1F8F4E;color:#ffffff;text-decoration:none;font-weight:bold;padding:12px 18px;border-radius:10px">Enviar ao tutor pelo WhatsApp</a>
            </p>
            <p style="margin:0;font-size:13px;color:#56707C">Link do tutor: <a href="{H(linkTutor)}">{H(linkTutor)}</a></p>
            """);
        var texto = $"Nova solicitação de {v.Nome} ({quando})\nProtocolo: {s.Protocolo}\nAnimal: {pet}\n" +
                    $"Prioridade: {Prioridade(s.Prioridade) ?? "não informada"}\nTutor: {s.TutorNome ?? "não informado"} " +
                    $"{(s.TutorCelular is null ? "" : Celular(s.TutorCelular))}\n\nLink do tutor: {linkTutor}";

        await Tentar(() => email.Enviar(para, $"{(urgente ? "URGENTE · " : "")}Nova solicitação: {pet} · {v.Nome}", html, texto, ct),
            "aviso de nova solicitação", s.Protocolo);
    }

    static string? Especie(Domain.Especie? e) => e switch
    {
        Domain.Especie.Cao => "Cão", Domain.Especie.Gato => "Gato", Domain.Especie.Equino => "Equino", Domain.Especie.Outro => "Outro", _ => null,
    };

    static string? Prioridade(Domain.Prioridade? p) => p switch
    {
        Domain.Prioridade.Rotina => "Rotina", Domain.Prioridade.Ate48h => "Em até 48 h", Domain.Prioridade.Urgente => "Urgente", _ => null,
    };

    public async Task ConsultaAutorizada(int solicitacaoId, OrcamentosContaAzul.Resultado orcamento, string linkArea, CancellationToken ct)
    {
        if (!email.Configurado)
        {
            log.LogWarning("E-mail não configurado; avisos da solicitação {Id} não enviados", solicitacaoId);
            return;
        }

        var s = await db.Solicitacoes.AsNoTracking()
            .Include(x => x.Tutor).Include(x => x.Animal).Include(x => x.Veterinario).Include(x => x.Local)
            .Include(x => x.Autorizacao).ThenInclude(x => x!.VersaoTermo)
            .SingleAsync(x => x.Id == solicitacaoId, ct);
        var a = s.Autorizacao!;
        var tutor = s.Tutor!;
        var pet = s.Animal!.Nome;
        var quando = TimeZoneInfo.ConvertTime(a.AceitoEm, Brasilia).ToString("dd/MM/yyyy 'às' HH:mm");

        if (!string.IsNullOrWhiteSpace(tutor.Email))
        {
            // Termo e orçamento vão só em PDF anexo; o corpo fica curto.
            var anexos = orcamento.OrcamentoId is not null
                ? "Seguem anexos a cópia do termo assinado e o orçamento da consulta (consulta + deslocamento estimado)."
                : "A cópia do termo assinado segue em PDF, anexa a este e-mail.";
            var html = Modelo($"""
                <p>Olá, {H(PrimeiroNome(tutor.Nome))}!</p>
                <p>Recebemos seu cadastro e sua autorização para a <b>consulta oftalmológica de {H(pet)}</b>,
                solicitada por {H(s.Veterinario.Nome)}. A Dra. Juliane entrará em contato para combinar o horário.</p>
                {Tabela(("Protocolo", s.Protocolo), ("Animal", pet), ("Local", LocalTexto(s)), ("Autorizado em", quando))}
                <h3 style="font-size:16px;color:#0E3A53;margin:24px 0 8px">Próximos passos</h3>
                <ol style="margin:0;padding-left:20px">
                  <li>{H(anexos)}</li>
                  <li>Após a consulta, enviaremos o resumo do que foi feito e os valores.</li>
                  <li>Se algum procedimento adicional for indicado, você recebe o orçamento para aprovar antes.</li>
                  <li>O relatório de atendimento chega pelo WhatsApp e por este e-mail.</li>
                </ol>
                <p style="margin:20px 0 8px">Acompanhe a data marcada, os valores e os relatórios na sua área
                (entre com seu CPF e um código enviado a este e-mail):</p>
                <p style="margin:0 0 12px">{Botao(linkArea, "Minha área", "#0B8AA0")}</p>
                """);
            var texto = $"Olá, {PrimeiroNome(tutor.Nome)}!\n\nRecebemos seu cadastro e sua autorização para a consulta " +
                        $"oftalmológica de {pet}, solicitada por {s.Veterinario.Nome}.\nProtocolo: {s.Protocolo}\n" +
                        $"Autorizado em: {quando}\n\n{anexos}\n\n" +
                        "Clínica Pimentel Vets · (11) 94767-0145";
            await Tentar(async () =>
            {
                var arquivos = new List<(string, byte[])>();
                if (await termoPdf.Gerar(s.Id, ct) is { } termo) arquivos.Add((termo.NomeArquivo, termo.Pdf));
                // PDF oficial do Conta Azul; se não vier, o e-mail segue só com o termo.
                if (orcamento.OrcamentoId is not null && await contaAzul.ImprimirOrcamento(orcamento.OrcamentoId, ct) is { } orc)
                    arquivos.Add(($"orcamento-{s.Protocolo}.pdf", orc));
                await email.Enviar(tutor.Email, $"Consulta autorizada · {pet} · Clínica Pimentel Vets", html, texto, ct, arquivos);
            }, "confirmação ao tutor", s.Protocolo);
        }

        var para = opcoes.Value.AvisosPara;
        if (!string.IsNullOrWhiteSpace(para))
        {
            var estimativa = await deslocamentos.Estimar(s.LocalId, ct);
            var textoOrcamento = orcamento.OrcamentoId is not null
                ? "Criado no Conta Azul (consulta + km estimado). Confira e envie ao cliente pelo Conta Azul."
                : $"Não foi criado: {orcamento.Motivo}.";
            var html = Modelo($"""
                <p><b>{H(tutor.Nome)}</b> autorizou a consulta de <b>{H(pet)}</b>.</p>
                {Tabela(("Protocolo", s.Protocolo), ("Tutor", tutor.Nome), ("Celular", Celular(tutor.Celular)),
                        ("E-mail", tutor.Email), ("Animal", pet), ("Veterinário", $"{s.Veterinario.Nome} · {Celular(s.Veterinario.Celular)}"),
                        ("Local", LocalTexto(s)), ("Endereço", s.Local?.EnderecoCompleto()), ("Como chegar", s.Local?.Referencia),
                        ("Deslocamento estimado", TextoEstimativa(estimativa)),
                        ("Orçamento", textoOrcamento),
                        ("Relatórios", s.VeterinarioRecebeRelatorios ? "Tutor e veterinário" : "Somente tutor"),
                        ("Autorizado em", quando))}
                <p style="margin:20px 0 8px"><b>Próximo passo:</b> combinar o horário com o tutor e marcar na agenda.</p>
                <p style="margin:0 0 12px">{Botao(LinkAgenda(s, tutor, pet), "Adicionar à Google Agenda", "#1A73E8")}</p>
                {BotoesMapa(s.Local)}
                """);
            var texto = $"{tutor.Nome} autorizou a consulta de {pet}.\nProtocolo: {s.Protocolo}\nCelular: {Celular(tutor.Celular)}\n" +
                        $"Veterinário: {s.Veterinario.Nome}\nLocal: {LocalTexto(s)}\nEndereço: {s.Local?.EnderecoCompleto()}\n" +
                        $"Orçamento: {textoOrcamento}\nAutorizado em: {quando}\n\nAdicionar à agenda: {LinkAgenda(s, tutor, pet)}";
            await Tentar(() => email.Enviar(para, $"Autorizada: {pet} · {tutor.Nome}", html, texto, ct),
                "aviso à clínica", s.Protocolo);
        }
    }

    /// <summary>
    /// Data marcada (ou remarcada) de um atendimento. Num acompanhamento novo, o orçamento vai anexo.
    /// </summary>
    public async Task AtendimentoMarcado(int atendimentoId, bool enviarOrcamento, string linkArea, CancellationToken ct)
    {
        var a = await db.Atendimentos.AsNoTracking()
            .Include(x => x.Solicitacao).ThenInclude(x => x.Tutor)
            .Include(x => x.Solicitacao).ThenInclude(x => x.Animal)
            .Include(x => x.Solicitacao).ThenInclude(x => x.Local)
            .SingleAsync(x => x.Id == atendimentoId, ct);
        var s = a.Solicitacao;
        if (!PodeAvisarTutor(s)) return;
        var pet = s.Animal?.Nome ?? s.PetNome ?? "seu animal";
        var quando = DataHora(a.MarcadoPara!.Value);
        var oque = a.Tipo == Domain.TipoAtendimento.Consulta ? "consulta oftalmológica" : "visita de acompanhamento oftálmico";
        var orcamento = enviarOrcamento && a.ContaAzulOrcamentoId is not null
            ? "<p>Segue em anexo o orçamento desta visita (acompanhamento + deslocamento estimado). O valor final segue o km rodado.</p>"
            : "";
        var html = Modelo($"""
            <p>Olá, {H(PrimeiroNome(s.Tutor!.Nome))}!</p>
            <p>A <b>{H(oque)} de {H(pet)}</b> está marcada:</p>
            {Tabela(("Data e hora", quando), ("Local", LocalTexto(s)), ("Protocolo", s.Protocolo))}
            {orcamento}
            <p>Se precisar remarcar, fale com a clínica pelo WhatsApp (11) 94767-0145.</p>
            <p style="margin:0 0 12px">{Botao(linkArea, "Acompanhar na minha área", "#0B8AA0")}</p>
            """);
        var texto = $"Olá, {PrimeiroNome(s.Tutor.Nome)}!\n\nA {oque} de {pet} está marcada para {quando}" +
                    $"{(LocalTexto(s) is { } l ? $", em {l}" : "")}.\nProtocolo: {s.Protocolo}\n\nAcompanhe em: {linkArea}";
        var assunto = a.Tipo == Domain.TipoAtendimento.Consulta ? "Consulta marcada" : "Acompanhamento marcado";
        await Tentar(async () =>
        {
            var anexos = new List<(string, byte[])>();
            if (enviarOrcamento && a.ContaAzulOrcamentoId is not null && await contaAzul.ImprimirOrcamento(a.ContaAzulOrcamentoId, ct) is { } pdf)
                anexos.Add(($"orcamento-{s.Protocolo}-{a.Numero}.pdf", pdf));
            await email.Enviar(s.Tutor.Email, $"{assunto}: {quando} · {pet}", html, texto, ct, anexos);
        }, "aviso de data marcada", s.Protocolo);
    }

    /// <summary>
    /// Atendimento concluído: um e-mail só, com o relatório clínico (o que foi feito) e a venda do Conta Azul
    /// (os valores) anexos, e os dados de pagamento.
    /// </summary>
    public async Task AtendimentoConcluido(int atendimentoId, string? instrucoesPagamento, string linkArea,
        (string Titulo, byte[] Pdf)? relatorio, CancellationToken ct)
    {
        var a = await db.Atendimentos.AsNoTracking()
            .Include(x => x.Solicitacao).ThenInclude(x => x.Tutor)
            .Include(x => x.Solicitacao).ThenInclude(x => x.Animal)
            .SingleAsync(x => x.Id == atendimentoId, ct);
        var s = a.Solicitacao;
        if (!PodeAvisarTutor(s)) return;
        var pet = s.Animal?.Nome ?? s.PetNome ?? "seu animal";
        var forma = a.PagamentoForma is { } f && VendasContaAzul.Formas.TryGetValue(f, out var nome) ? nome : null;
        var vencimento = a.PagamentoVencimento?.ToString("dd/MM/yyyy");
        var consulta = a.Tipo == Domain.TipoAtendimento.Consulta;
        var oque = consulta ? "A consulta" : "O acompanhamento";
        var concluido = consulta ? "concluída" : "concluído";
        var quando = a.MarcadoPara is { } m ? TimeZoneInfo.ConvertTime(m, Brasilia).ToString("dd/MM/yyyy") : null;
        var anexosTexto = relatorio is null
            ? "Segue em anexo a venda com os valores."
            : "Seguem em anexo o <b>relatório clínico</b>, com o que foi feito e as orientações, e a <b>venda</b> com os valores.";
        var dadosPagamento = string.IsNullOrWhiteSpace(instrucoesPagamento) ? "" :
            "<h3 style=\"font-size:16px;color:#0E3A53;margin:24px 0 8px\">Dados para pagamento</h3>" +
            $"<p style=\"white-space:pre-line;background:#E4F6F9;border-radius:10px;padding:12px 14px\">{H(instrucoesPagamento)}</p>";
        var html = Modelo($"""
            <p>Olá, {H(PrimeiroNome(s.Tutor!.Nome))}!</p>
            <p>{oque} de <b>{H(pet)}</b>{(quando is null ? "" : $" em {quando}")} foi {concluido}. {anexosTexto}</p>
            {Tabela(("Protocolo", s.Protocolo), ("Venda", a.ContaAzulVendaNumero is { } n ? $"nº {n}" : null),
                    ("Total", a.ValorFinal?.ToString("C", PtBr)), ("Forma de pagamento", forma), ("Vencimento", vencimento))}
            {dadosPagamento}
            <p>O relatório e a venda também ficam na sua área.</p>
            <p style="margin:0 0 12px">{Botao(linkArea, "Acompanhar na minha área", "#0B8AA0")}</p>
            """);
        var texto = $"Olá, {PrimeiroNome(s.Tutor.Nome)}!\n\n{oque} de {pet}{(quando is null ? "" : $" em {quando}")} foi {concluido}. " +
                    (relatorio is null ? "Segue em anexo a venda com os valores." : "Seguem em anexo o relatório clínico e a venda com os valores.") +
                    $"\nProtocolo: {s.Protocolo}\nTotal: {a.ValorFinal?.ToString("C", PtBr)}\nVencimento: {vencimento}\n\n" +
                    (string.IsNullOrWhiteSpace(instrucoesPagamento) ? "" : $"Dados para pagamento:\n{instrucoesPagamento}\n\n") +
                    $"Acompanhe em: {linkArea}";
        await Tentar(async () =>
        {
            var anexos = new List<(string, byte[])>();
            if (relatorio is { } r) anexos.Add(($"relatorio-{s.Protocolo}-{a.Numero}.pdf", r.Pdf));
            if (a.ContaAzulVendaId is not null && await contaAzul.ImprimirOrcamento(a.ContaAzulVendaId, ct) is { } pdf)
                anexos.Add(($"venda-{s.Protocolo}-{a.Numero}.pdf", pdf));
            await email.Enviar(s.Tutor.Email, $"{(consulta ? "Consulta concluída" : "Acompanhamento concluído")}: relatório e valores · {pet} · Clínica Pimentel Vets",
                html, texto, ct, anexos);
        }, "aviso de atendimento concluído", s.Protocolo);
    }



    /// <summary>Relatório novo: vai anexo ao e-mail do tutor.</summary>
    public async Task RelatorioDisponivel(int relatorioId, byte[] pdf, string linkArea, CancellationToken ct)
    {
        var r = await db.Relatorios.AsNoTracking().Include(x => x.Solicitacao).ThenInclude(x => x.Tutor)
            .Include(x => x.Solicitacao).ThenInclude(x => x.Animal).SingleAsync(x => x.Id == relatorioId, ct);
        var s = r.Solicitacao;
        if (!PodeAvisarTutor(s)) return;
        var pet = s.Animal?.Nome ?? s.PetNome ?? "seu animal";
        var html = Modelo($"""
            <p>Olá, {H(PrimeiroNome(s.Tutor!.Nome))}!</p>
            <p>O <b>{H(r.Titulo.ToLowerInvariant())}</b> de {H(pet)} está pronto e segue em anexo.</p>
            <p style="margin:0 0 12px">{Botao(linkArea, "Ver na minha área", "#0B8AA0")}</p>
            """);
        var texto = $"Olá, {PrimeiroNome(s.Tutor.Nome)}!\n\nO {r.Titulo.ToLowerInvariant()} de {pet} está pronto e segue em anexo.\n" +
                    $"Protocolo: {s.Protocolo}\n\nVeja também em: {linkArea}";
        await Tentar(() => email.Enviar(s.Tutor.Email, $"{r.Titulo} · {pet} · Clínica Pimentel Vets", html, texto, ct,
            [($"relatorio-{s.Protocolo}-{r.Id}.pdf", pdf)]), "relatório ao tutor", s.Protocolo);
    }

    bool PodeAvisarTutor(Domain.Solicitacao s)
    {
        if (email.Configurado && !string.IsNullOrWhiteSpace(s.Tutor?.Email)) return true;
        log.LogWarning("Tutor sem e-mail ou e-mail não configurado; aviso da solicitação {Protocolo} não enviado", s.Protocolo);
        return false;
    }

    static string DataHora(DateTimeOffset quando) =>
        TimeZoneInfo.ConvertTime(quando, Brasilia).ToString("dddd, dd/MM/yyyy 'às' HH:mm", PtBr);

    static string? TextoEstimativa(Estimativa? e) => e is null ? null :
        $"{e.KmIdaVolta:0} km ida e volta ≈ {e.Valor.ToString("C", PtBr)}" +
        (e.Pedagio is { } p ? $" + pedágio ≈ {p.ToString("C", PtBr)}" : " + pedágio");

    /// <summary>
    /// Evento da Google Agenda já preenchido (sem data: a Dra. escolhe o horário combinado e salva).
    /// Não precisa de integração: o link abre a agenda de quem estiver logado no Google.
    /// </summary>
    static string LinkAgenda(Domain.Solicitacao s, Domain.Tutor tutor, string pet)
    {
        var detalhes = string.Join("\n", new[]
        {
            $"Protocolo: {s.Protocolo}",
            $"Tutor: {tutor.Nome} · {Celular(tutor.Celular)}",
            $"Veterinário: {s.Veterinario.Nome} · {Celular(s.Veterinario.Celular)}",
            s.Olho is null ? null : $"Olho: {s.Olho}",
            s.QueixaHistorico is null ? null : $"Queixa: {s.QueixaHistorico}",
            s.Local?.Referencia is null ? null : $"Como chegar: {s.Local.Referencia}",
            s.TratadorNome is null ? null : $"Tratador: {s.TratadorNome}{(s.TratadorCelular is null ? "" : " · " + Celular(s.TratadorCelular))}",
            s.Local is null ? null : $"Mapa: {LinkMaps(s.Local)}",
        }.Where(x => x is not null));
        var local = s.Local is null ? "" : $"{s.Local.Nome}{(s.Local.EnderecoCompleto() is { } e ? ", " + e : "")}";
        return "https://calendar.google.com/calendar/render?action=TEMPLATE" +
               $"&text={Uri.EscapeDataString($"Consulta oftalmo · {pet} ({PrimeiroNome(tutor.Nome)})")}" +
               $"&location={Uri.EscapeDataString(local)}&details={Uri.EscapeDataString(detalhes)}";
    }

    static string LinkMaps(Domain.Local l) => l.GooglePlaceId is not null
        ? $"https://www.google.com/maps/search/?api=1&query={Uri.EscapeDataString(l.Nome)}&query_place_id={Uri.EscapeDataString(l.GooglePlaceId)}"
        : $"https://www.google.com/maps/search/?api=1&query={Uri.EscapeDataString(l.EnderecoCompleto() ?? l.Nome)}";

    static string LinkWaze(Domain.Local l) => l.Latitude is { } lat && l.Longitude is { } lng
        ? $"https://waze.com/ul?ll={lat.ToString(CultureInfo.InvariantCulture)},{lng.ToString(CultureInfo.InvariantCulture)}&navigate=yes"
        : $"https://waze.com/ul?q={Uri.EscapeDataString(l.EnderecoCompleto() ?? l.Nome)}&navigate=yes";

    /// <summary>Botões "Google Maps" e "Waze" para o local (se houver endereço ou ponto no mapa).</summary>
    static string BotoesMapa(Domain.Local? l) =>
        l is null || (l.EnderecoCompleto() is null && l.GooglePlaceId is null) ? "" :
        $"<p style=\"margin:8px 0 12px\">{Botao(LinkMaps(l), "Abrir no Google Maps", "#0E3A53")} {Botao(LinkWaze(l), "Abrir no Waze", "#0E3A53")}</p>";

    static string Botao(string url, string texto, string cor) =>
        $"<a href=\"{H(url)}\" style=\"display:inline-block;background:{cor};color:#ffffff;text-decoration:none;font-weight:bold;" +
        $"padding:10px 16px;border-radius:10px;margin:0 6px 6px 0\">{H(texto)}</a>";

    async Task Tentar(Func<Task> envio, string qual, string protocolo)
    {
        try { await envio(); }
        catch (Exception e) { log.LogError(e, "Falha ao enviar {Qual} da solicitação {Protocolo}", qual, protocolo); }
    }

    static string Modelo(string corpo) => $"""
        <div style="font-family:Arial,Helvetica,sans-serif;font-size:15px;line-height:1.55;color:#14262F;max-width:560px">
          <p style="font-size:18px;font-weight:bold;color:#0E3A53;margin:0 0 16px">Clínica Pimentel Vets</p>
          {corpo}
          <p style="margin-top:28px;color:#56707C;font-size:13px">
            Clínica Pimentel Vets · M.V. Juliane Cavani Pimentel · CRMV-SP 11.064<br>
            Rua Arandu, 885, Brooklin Paulista, São Paulo · (11) 94767-0145
          </p>
        </div>
        """;

    static string Tabela(params (string Rotulo, string? Valor)[] linhas)
    {
        var sb = new StringBuilder("<table style=\"border-collapse:collapse;width:100%;margin:16px 0;font-size:14px\">");
        foreach (var (rotulo, valor) in linhas.Where(l => !string.IsNullOrWhiteSpace(l.Valor)))
            sb.Append($"<tr><td style=\"padding:6px 12px 6px 0;color:#56707C;white-space:nowrap\">{H(rotulo)}</td>" +
                      $"<td style=\"padding:6px 0;font-weight:bold\">{H(valor)}</td></tr>");
        return sb.Append("</table>").ToString();
    }

    static string? LocalTexto(Domain.Solicitacao s) =>
        s.Local is null ? null : s.Local.Nome + (s.Local.Cidade is null ? "" : $", {s.Local.Cidade}");

    static string Celular(string d) => d.Length switch
    {
        11 => $"({d[..2]}) {d[2..7]}-{d[7..]}",
        10 => $"({d[..2]}) {d[2..6]}-{d[6..]}",
        _ => d,
    };

    static string PrimeiroNome(string nome) => nome.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? nome;

    static string H(string? s) => WebUtility.HtmlEncode(s ?? "");
}
