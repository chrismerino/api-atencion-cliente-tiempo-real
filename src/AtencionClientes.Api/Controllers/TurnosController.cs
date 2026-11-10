using System.ComponentModel.DataAnnotations;
using AtencionClientes.Api.Dtos;
using AtencionClientes.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace AtencionClientes.Api.Controllers;

[ApiController]
[Route("api/turnos")]
public class TurnosController : ControllerBase
{
    private readonly TurnosService _turnosService;

    public TurnosController(TurnosService turnosService)
    {
        _turnosService = turnosService;
    }

    [HttpPost]
    public async Task<ActionResult<TurnoRespuesta>> Crear(CrearTurnoSolicitud datos, CancellationToken ct)
    {
        var turno = await _turnosService.CrearAsync(datos, ct);
        return CreatedAtAction(nameof(Obtener), new { id = turno.Id }, turno);
    }

    [HttpGet]
    public async Task<ActionResult<List<TurnoRespuesta>>> PorCliente([FromQuery, Range(1, int.MaxValue)] int clienteId, CancellationToken ct)
    {
        return Ok(await _turnosService.PorClienteAsync(clienteId, ct));
    }

    [HttpGet("pendientes")]
    public async Task<ActionResult<List<TurnoRespuesta>>> Pendientes(CancellationToken ct)
    {
        return Ok(await _turnosService.PendientesAsync(ct));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TurnoRespuesta>> Obtener(int id, CancellationToken ct)
    {
        return Ok(await _turnosService.ObtenerAsync(id, ct));
    }

    [HttpGet("{id:int}/posicion")]
    public async Task<ActionResult<PosicionRespuesta>> Posicion(int id, CancellationToken ct)
    {
        return Ok(await _turnosService.PosicionAsync(id, ct));
    }

    [HttpPatch("{id:int}/estado")]
    public async Task<ActionResult<TurnoRespuesta>> CambiarEstado(int id, CambiarEstadoSolicitud datos, CancellationToken ct)
    {
        return Ok(await _turnosService.CambiarEstadoAsync(id, datos, ct));
    }

    [HttpPatch("{id:int}/asignacion")]
    public async Task<ActionResult<TurnoRespuesta>> Asignar(int id, AsignarTurnoSolicitud datos, CancellationToken ct)
    {
        return Ok(await _turnosService.AsignarAsync(id, datos, ct));
    }

    [HttpPost("{id:int}/finalizacion")]
    public async Task<ActionResult<TurnoRespuesta>> Finalizar(int id, CancellationToken ct)
    {
        return Ok(await _turnosService.FinalizarAsync(id, ct));
    }
}
