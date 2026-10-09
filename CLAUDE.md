# Sistema de gestão — Clínica Cavani Vets

Clínica de oftalmologia veterinária (cães, gatos e equinos). Responsável: M.V. Juliane Cavani Pimentel, CRMV-SP 11.064. A clínica atende a pedido de outros veterinários, indo até a clínica, haras ou hípica onde o animal está.

## Problema que o sistema resolve

Hoje a coleta de dados do tutor, o envio do orçamento e a autorização são feitos manualmente pelo WhatsApp, com cadastro manual no Conta Azul. Há muitos casos de inadimplência. O sistema deve fazer o tutor preencher os dados, ver os valores e autorizar a consulta sozinho, com aceite registrado.

## Stack

- Backend: ASP.NET Core Web API (.NET 10), EF Core, SQL Server (Azure SQL). Pasta `backend/`.
- Frontend: HTML, CSS e JavaScript puros, mobile first, partindo dos protótipos aprovados. Pasta `web/`. Flutter foi descartado (out/2026): as páginas são abertas por links do WhatsApp no 4G, e o Flutter web baixava 5 a 9 MB contra cerca de 0,1 MB do HTML. Se um dia precisar de app instalável, o caminho é PWA; app de loja só com necessidade concreta (offline pesado, Bluetooth).
- Integrações: Conta Azul API v2 (OAuth, cadastro de clientes e orçamentos; tokens na tabela `ContaAzulConexoes`; credenciais em `dotnet user-secrets`), CEP via ViaCEP com BrasilAPI de reserva.
- Editor: VS Code.

## Ambiente de desenvolvimento (máquina local)

- Banco de desenvolvimento: Azure SQL `assist-vet-dev` (oferta gratuita), no servidor `assist-vet.database.windows.net`, separado do de produção (`assist-vet`). A conexão (com senha) fica em user-secrets: `dotnet user-secrets set "ConnectionStrings:Cavani" "..."`. O IP da máquina precisa estar liberado no firewall do servidor no portal do Azure.
- O PostgreSQL portátil em `C:\dev\pgsql` e os scripts `scripts\banco-*.ps1` eram do banco anterior (até out/2026) e não são mais usados.
- A API serve as páginas de `web/`: em desenvolvimento direto da pasta (http://localhost:5273/; edições valem na hora); na publicação, `web/` é copiada para `wwwroot`. Página e API no mesmo endereço, sem CORS. Painel interno em `/` (index.html).
- Migrações: `dotnet ef migrations add <Nome> -o Data/Migrations` e `dotnet ef database update`, dentro de `backend\CavaniVets.Api`.

## Publicação (Azure)

- App Service **Windows**, .NET 10, com a API servindo as páginas. Banco: **Azure SQL Database** na oferta gratuita (serverless: pausa sem uso; a conexão tem novas tentativas automáticas para o primeiro acesso depois da pausa). Trocado de PostgreSQL para SQL Server em out/2026 por causa do custo; as migrações foram recriadas do zero (`Inicial`).
- Cota grátis do Azure SQL: 100 mil vCore-segundos por mês por banco, com cobrança extra desligada (se acabar, o banco fica indisponível até o mês seguinte). O banco só pausa depois de ~1 h sem nenhum acesso, então **nada pode consultar o banco periodicamente** (o painel atualiza só ao voltar para a aba ou no botão).
- Hospedagem 100% gratuita: App Service **Free (F1)** + Azure SQL gratuito. O F1 dorme após ~20 min sem acesso (primeiro acesso leva 10-20 s), tem 60 min de CPU por dia e não aceita domínio próprio; para usar com clientes, considerar o B1 Linux (~US$ 13/mês).
- Migrações **não** rodam ao iniciar (acordaria o banco a cada vez que o site acorda). Na publicação: `dotnet ef database update --connection "<cadeia do assist-vet>"` a partir da máquina de desenvolvimento (IP liberado no firewall). `Banco__MigrarAoIniciar=true` liga a migração automática se um dia o plano for pago. Os atalhos `/api/dev/*` ficam desligados em produção.
- Configurações do App Service (variáveis de ambiente; segredos nunca no código):
  - `ConnectionStrings__Cavani` (formato ADO.NET do portal do Azure SQL, com a senha), `Clinica__Senha` (senha do painel), `Email__SenhaApp`, `GoogleMaps__ChaveApi`, `ContaAzul__ClientId`, `ContaAzul__ClientSecret`, `ContaAzul__RedirectUri`
  - `Site__UrlPublica` (base dos links enviados por WhatsApp e e-mail) e `Armazenamento__Pasta` = `D:\home\dados` no Windows ou `/home/dados` no Linux (fora da pasta publicada, senão os arquivos somem a cada publicação).
- Rotas internas exigem a senha da clínica (`X-Senha-Clinica`); sem senha configurada, só funcionam na própria máquina.
- O Conta Azul é conectado pelo botão do painel. Os tokens ficam no banco; o refresh token muda a cada renovação, então o mesmo token não pode ser usado em dois bancos ao mesmo tempo.

## Protótipos aprovados (HTML, na pasta `prototipos/`)

- `solicitacao-veterinario.html`: página do veterinário solicitante.
- `autorizacao-consulta.html`: página do tutor (cadastro, valores, termo e aceite).

Os protótipos são a referência de fluxo, textos e visual. Paleta e logo devem ser reaproveitados.

## Fluxo

1. O veterinário manda mensagem no WhatsApp da Dra. Juliane. Ela responde com o link da solicitação, que leva o celular dele (`?cel=11999999999`).
2. **Celular é a chave do veterinário.** Se já tem cadastro, a página mostra os dados dele e ele só confirma. Se não tem, informa nome completo, CRMV e UF uma única vez.
3. A assistente da Dra. pode preencher a solicitação no lugar do veterinário, usando o mesmo fluxo. Registrar internamente quem preencheu (veterinário ou clínica), sem mostrar ao tutor.
4. Caso clínico: só campos escritos, todos opcionais: nome do animal, espécie (cão, gato, equino, outro), raça, idade, olho acometido (OD/OE/AO), prioridade (rotina, até 48 h, urgente), queixa e histórico, medicações em uso. O áudio saiu da página (out/2026): sem extração do conteúdo, ele só gerava trabalho manual. Fase 2: o áudio volta pela conversa no WhatsApp, com transcrição e preenchimento da ficha. A API já aceita áudio no envio da solicitação (`POST /api/solicitacoes`, campo `audio`).
5. Local do atendimento: lista pesquisável de locais cadastrados (tipos: clínica/hospital, haras, hípica, residência), com opção de adicionar novo. Se espécie for equino ou o local for haras/hípica, mostrar tratador responsável e celular dele (opcionais).
6. Tutor: nome, celular e se já sabe que a consulta tem custo próprio (todos opcionais). **O orçamento e a autorização vão sempre para o tutor**, que é o responsável financeiro.
7. Pergunta obrigatória ao veterinário: se quer receber os relatórios clínicos do caso (sim / só o tutor).
8. A clínica envia ao tutor o link de autorização pelo WhatsApp (link carrega pet, veterinário solicitante, local, nome e celular do tutor).
9. Página do tutor, 4 etapas: dados pessoais (nome, CPF validado, celular, e-mail, endereço completo) → animal → valores → termo e aceite.
10. Aceite: três confirmações + nome digitado igual ao do cadastro. Salvar data, hora, IP, user agent e versão exata do termo.
11. Pós-atendimento (a desenhar): registrar o que foi feito, orçamento de procedimentos adicionais com aprovação do tutor, relatório em PDF enviado ao tutor e, se marcado, ao veterinário solicitante.

## Tabela de valores (2026)

- Consulta inicial: R$ 800,00 (medicamentos para diagnóstico incluídos).
- Acompanhamento oftálmico até a alta clínica: R$ 300,00.
- Deslocamento: R$ 2,50 por km + pedágio.
- Materiais estéreis para diagnóstico: cobrados à parte.
- Medicamentos e materiais para tratamento: à parte.
- Ultrassonografia oftálmica: R$ 550,00.
- Infiltrações (por aplicação): subconjuntival R$ 350, retrobulbar R$ 450, intravítrea R$ 700, intralesional R$ 750.
- Cirurgias: sob orçamento.

Valores devem ser configuráveis no sistema, não fixos no código.

## Pendências

- Texto do termo é rascunho; precisa de revisão jurídica. Versionar o termo.
- Definir condições de pagamento (antecipado via Pix ou depois).
- Fase 2: integração com Conta Azul (cadastro de clientes), Pix (Asaas/Efí/Mercado Pago), envio automático via WhatsApp Business API.
- Tela interna da clínica: lista de solicitações com status (aguardando tutor, autorizado, atendido), player do áudio ao lado da ficha, cadastro de locais e veterinários.
- LGPD: consentimento já previsto no termo.

## Contato da clínica

Rua Arandu, 885, Brooklin Paulista, São Paulo, CEP 04562-031. (11) 94767-0145. clinicapimentelvets@gmail.com
