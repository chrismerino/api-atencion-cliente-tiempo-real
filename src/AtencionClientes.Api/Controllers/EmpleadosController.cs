using AtencionClientes.Api.Dtos;
using AtencionClientes.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace AtencionClientes.Api.Controllers;

[ApiController]
[Route("api/empleados")]
public class EmpleadosController(EmpleadosService service) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<EmpleadoRespuesta>> Crear(EmpleadoSolicitud datos, CancellationToken ct) =>
        StatusCode(201, await service.CrearAsync(datos, ct));

    [HttpGet("disponibles")]
    public async Task<ActionResult<List<EmpleadoRespuesta>>> Disponibles(CancellationToken ct) => Ok(await service.DisponiblesAsync(ct));
}
