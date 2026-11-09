using AtencionClientes.Api.Dtos;
using AtencionClientes.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace AtencionClientes.Api.Controllers;

[ApiController]
[Route("api/empleados")]
public class EmpleadosController : ControllerBase
{
    private readonly EmpleadosService _empleadosService;

    public EmpleadosController(EmpleadosService empleadosService)
    {
        _empleadosService = empleadosService;
    }

    [HttpPost]
    public async Task<ActionResult<EmpleadoRespuesta>> Crear(EmpleadoSolicitud datos, CancellationToken ct)
    {
        return StatusCode(StatusCodes.Status201Created, await _empleadosService.CrearAsync(datos, ct));
    }

    [HttpGet("disponibles")]
    public async Task<ActionResult<List<EmpleadoRespuesta>>> Disponibles(CancellationToken ct)
    {
        return Ok(await _empleadosService.DisponiblesAsync(ct));
    }
}
