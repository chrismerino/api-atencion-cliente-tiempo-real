using AtencionClientes.Api.Dtos;
using AtencionClientes.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace AtencionClientes.Api.Controllers;

[ApiController]
[Route("api/clientes")]
public class ClientesController(ClientesService service) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ClienteRespuesta>> Crear(ClienteSolicitud datos, CancellationToken ct)
    {
        var cliente = await service.CrearAsync(datos, ct);
        return CreatedAtAction(nameof(Obtener), new { id = cliente.Id }, cliente);
    }

    [HttpGet]
    public async Task<ActionResult<List<ClienteRespuesta>>> Listar(CancellationToken ct) => Ok(await service.ListarAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ClienteRespuesta>> Obtener(int id, CancellationToken ct) => Ok(await service.ObtenerAsync(id, ct));

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ClienteRespuesta>> Actualizar(int id, ClienteSolicitud datos, CancellationToken ct) =>
        Ok(await service.ActualizarAsync(id, datos, ct));
}
