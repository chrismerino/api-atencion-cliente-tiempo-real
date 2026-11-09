using AtencionClientes.Api.Dtos;
using AtencionClientes.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace AtencionClientes.Api.Controllers;

[ApiController]
[Route("api/clientes")]
public class ClientesController : ControllerBase
{
    private readonly ClientesService _clientesService;

    public ClientesController(ClientesService clientesService)
    {
        _clientesService = clientesService;
    }

    [HttpPost]
    public async Task<ActionResult<ClienteRespuesta>> Crear(ClienteSolicitud datos, CancellationToken ct)
    {
        var cliente = await _clientesService.CrearAsync(datos, ct);
        return CreatedAtAction(nameof(Obtener), new { id = cliente.Id }, cliente);
    }

    [HttpGet]
    public async Task<ActionResult<List<ClienteRespuesta>>> Listar(CancellationToken ct)
    {
        return Ok(await _clientesService.ListarAsync(ct));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ClienteRespuesta>> Obtener(int id, CancellationToken ct)
    {
        return Ok(await _clientesService.ObtenerAsync(id, ct));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ClienteRespuesta>> Actualizar(int id, ClienteSolicitud datos, CancellationToken ct)
    {
        return Ok(await _clientesService.ActualizarAsync(id, datos, ct));
    }
}
