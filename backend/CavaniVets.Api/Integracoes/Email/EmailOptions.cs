namespace CavaniVets.Api.Integracoes.Email;

/// <summary>
/// Envio pelo Gmail da clínica (SMTP). A SenhaApp é uma "senha de app" do Google: só envia e-mail e pode ser
/// revogada sem trocar a senha da conta. Fica em user-secrets no desenvolvimento e em variável de ambiente
/// (Email__SenhaApp) no servidor; nunca no appsettings nem no código.
/// </summary>
public class EmailOptions
{
    public const string Secao = "Email";

    public string Remetente { get; set; } = "";
    public string NomeRemetente { get; set; } = "Clínica Cavani Vets";
    public string SenhaApp { get; set; } = "";
    public string Host { get; set; } = "smtp.gmail.com";
    public int Porta { get; set; } = 587;
    /// <summary>Quem recebe os avisos internos (nova solicitação, tutor autorizou).</summary>
    public string AvisosPara { get; set; } = "";
}
