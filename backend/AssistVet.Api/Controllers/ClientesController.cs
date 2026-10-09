using AssistVet.Api.Domain;
using AssistVet.Api.Servicos;
using Microsoft.AspNetCore.Mvc;

namespace AssistVet.Api.Controllers;

/// <summary>
/// Cadastro de clientes sem link do tutor, para a futura tela interna da clínica.
/// Protegido pela senha da clínica: devolve dados pessoais pelo CPF/CNPJ.
/// A página do tutor usa /api/autorizacoes/{token}/cliente, que confere o celular do link.
/// </summary>
[ApiController]
[Route("api/clientes")]
[SenhaClinica]
public class ClientesController(CadastroClientes cadastro) : ControllerBase
{
    // Catch-all para aceitar CNPJ formatado, que tem barra (11.222.333/0001-81).
    [HttpGet("{*documento}")]
    public async Task<IActionResult> Buscar(string documento, CancellationToken ct)
    {
        var doc = Documentos.SoDigitos(documento);
        if (Documentos.Tipo(doc) is not { } tipo) return BadRequest("CPF ou CNPJ inválido.");
        var achado = await cadastro.Buscar(doc, tipo, ct);
        return achado is null ? NotFound(new { documento = doc, tipoPessoa = tipo }) : Ok(achado.Dados);
    }

    [HttpPost]
    public async Task<IActionResult> Salvar(ClienteDto req, CancellationToken ct)
    {
        try
        {
            return Ok(await cadastro.Salvar(req, ct));
        }
        catch (ArgumentException e)
        {
            return BadRequest(e.Message);
        }
    }
}
