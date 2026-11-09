using AtencionClientes.Api.Dtos;
using AtencionClientes.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace AtencionClientes.Api.Controllers;

[ApiController]
[Route("api/turnos")]
public class TurnosController(TurnosService service) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<TurnoRespuesta>> Crear(CrearTurnoSolicitud datos, CancellationToken ct)
    {
        var turno = await service.CrearAsync(datos, ct);
        return CreatedAtAction(nameof(Obtener), new { id = turno.Id }, turno);
    }

    [HttpGet("pendientes")]
    public async Task<ActionResult<List<TurnoRespuesta>>> Pendientes(CancellationToken ct) => Ok(await service.PendientesAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TurnoRespuesta>> Obtener(int id, CancellationToken ct) => Ok(await service.ObtenerAsync(id, ct));

    [HttpGet("{id:int}/posicion")]
    public async Task<ActionResult<PosicionRespuesta>> Posicion(int id, CancellationToken ct) => Ok(await service.PosicionAsync(id, ct));

    [HttpPatch("{id:int}/estado")]
    public async Task<ActionResult<TurnoRespuesta>> CambiarEstado(int id, CambiarEstadoSolicitud datos, CancellationToken ct) =>
        Ok(await service.CambiarEstadoAsync(id, datos, ct));

    [HttpPatch("{id:int}/asignacion")]
    public async Task<ActionResult<TurnoRespuesta>> Asignar(int id, AsignarTurnoSolicitud datos, CancellationToken ct) =>
        Ok(await service.AsignarAsync(id, datos, ct));

    [HttpPost("{id:int}/finalizacion")]
    public async Task<ActionResult<TurnoRespuesta>> Finalizar(int id, CancellationToken ct) => Ok(await service.FinalizarAsync(id, ct));
}
