using AssistVet.Api.Domain;
using AssistVet.Api.Integracoes.Cep;
using Microsoft.AspNetCore.Mvc;

namespace AssistVet.Api.Controllers;

[ApiController]
[Route("api/cep")]
public class CepController(CepClient cep) : ControllerBase
{
    /// <summary>Endereço do CEP, para preencher o formulário antes do número e complemento.</summary>
    [HttpGet("{numero}")]
    public async Task<IActionResult> Buscar(string numero, CancellationToken ct)
    {
        var digitos = Documentos.SoDigitos(numero);
        if (digitos.Length != 8) return BadRequest("CEP deve ter 8 dígitos.");
        var endereco = await cep.Buscar(digitos, ct);
        return endereco is null ? NotFound("CEP não encontrado.") : Ok(endereco);
    }
}
