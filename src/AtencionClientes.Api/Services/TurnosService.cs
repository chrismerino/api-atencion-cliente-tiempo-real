using AtencionClientes.Api.Data;
using AtencionClientes.Api.Dtos;
using AtencionClientes.Api.Models;

namespace AtencionClientes.Api.Services;

public class TurnosService
{
    private readonly TurnosRepository _turnosRepository;
    private readonly ClientesRepository _clientesRepository;
    private readonly EmpleadosRepository _empleadosRepository;
    private readonly NotificacionesService _notificacionesService;

    public TurnosService(
        TurnosRepository turnosRepository,
        ClientesRepository clientesRepository,
        EmpleadosRepository empleadosRepository,
        NotificacionesService notificacionesService)
    {
        _turnosRepository = turnosRepository;
        _clientesRepository = clientesRepository;
        _empleadosRepository = empleadosRepository;
        _notificacionesService = notificacionesService;
    }

    public async Task<TurnoRespuesta> CrearAsync(CrearTurnoSolicitud datos, CancellationToken ct = default)
    {
        if (await _clientesRepository.ObtenerAsync(datos.ClienteId, ct) is null)
            throw new ApiException(404, "El cliente no existe.");
        var turno = new Turno { ClienteId = datos.ClienteId };
        _turnosRepository.Agregar(turno);
        var posicion = await _turnosRepository.GuardarConPosicionAsync(turno, ct);
        var respuesta = Respuesta(turno, posicion);
        await _notificacionesService.NotificarAsync("TurnoCreado", respuesta);
        return respuesta;
    }

    public async Task<List<TurnoRespuesta>> PendientesAsync(CancellationToken ct = default) =>
        (await _turnosRepository.PendientesAsync(ct)).Select((turno, indice) => Respuesta(turno, indice + 1)).ToList();

    public async Task<List<TurnoRespuesta>> PorClienteAsync(int clienteId, CancellationToken ct = default)
    {
        if (await _clientesRepository.ObtenerAsync(clienteId, ct) is null)
        {
            throw new ApiException(404, "El cliente no existe.");
        }
        var resultado = new List<TurnoRespuesta>();
        foreach (var turno in await _turnosRepository.PorClienteAsync(clienteId, ct))
            resultado.Add(Respuesta(turno, await _turnosRepository.PosicionAsync(turno, ct)));
        return resultado;
    }

    public async Task<TurnoRespuesta> ObtenerAsync(int id, CancellationToken ct = default)
    {
        var turno = await Buscar(id, ct);
        return Respuesta(turno, await _turnosRepository.PosicionAsync(turno, ct));
    }

    public async Task<PosicionRespuesta> PosicionAsync(int id, CancellationToken ct = default)
    {
        var turno = await Buscar(id, ct);
        return new(turno.Id, turno.ClienteId, turno.Estado, await _turnosRepository.PosicionAsync(turno, ct));
    }

    public Task<TurnoRespuesta> AsignarAsync(int id, AsignarTurnoSolicitud datos, CancellationToken ct = default) =>
        CambiarEstadoAsync(id, new(EstadoTurno.EnAtencion, datos.EmpleadoId), ct);

    public Task<TurnoRespuesta> FinalizarAsync(int id, CancellationToken ct = default) =>
        CambiarEstadoAsync(id, new(EstadoTurno.Atendido), ct);

    public async Task<TurnoRespuesta> CambiarEstadoAsync(int id, CambiarEstadoSolicitud datos, CancellationToken ct = default)
    {
        var turno = await Buscar(id, ct);
        var estado = datos.Estado ?? throw new ApiException(400, "Indica el estado del turno.");
        if (turno.Estado == estado)
        {
            if (estado == EstadoTurno.EnAtencion && datos.EmpleadoId is not null && datos.EmpleadoId != turno.EmpleadoId)
                throw new ApiException(409, "El turno ya está asignado a otro empleado.");
            return Respuesta(turno, await _turnosRepository.PosicionAsync(turno, ct));
        }
        if (turno.Estado is EstadoTurno.Atendido or EstadoTurno.Cancelado)
            throw new ApiException(409, "El turno ya finalizó y no puede cambiar de estado.");

        switch (estado)
        {
            case EstadoTurno.EnAtencion when turno.Estado == EstadoTurno.Pendiente:
                if (datos.EmpleadoId is null or <= 0)
                {
                    throw new ApiException(400, "Indica el empleado que atenderá el turno.");
                }
                if (await _empleadosRepository.ObtenerAsync(datos.EmpleadoId.Value, ct) is null)
                {
                    throw new ApiException(404, "El empleado no existe.");
                }
                if (await _empleadosRepository.EstaOcupadoAsync(datos.EmpleadoId.Value, ct))
                {
                    throw new ApiException(409, "El empleado ya está atendiendo un turno.");
                }
                turno.EmpleadoId = datos.EmpleadoId;
                turno.IniciadoEn = DateTime.UtcNow;
                break;
            case EstadoTurno.Atendido when turno.Estado == EstadoTurno.EnAtencion:
            case EstadoTurno.Cancelado:
                turno.FinalizadoEn = DateTime.UtcNow;
                break;
            default:
                throw new ApiException(409, "El cambio de estado no está permitido.");
        }
        turno.Estado = estado;
        var posicion = await _turnosRepository.GuardarConPosicionAsync(turno, ct);
        var respuesta = Respuesta(turno, posicion);
        var evento = estado switch
        {
            EstadoTurno.EnAtencion => "TurnoEnAtencion",
            EstadoTurno.Atendido => "TurnoAtendido",
            _ => "TurnoCancelado"
        };
        await _notificacionesService.NotificarAsync(evento, respuesta);
        return respuesta;
    }

    private async Task<Turno> Buscar(int id, CancellationToken ct) =>
        await _turnosRepository.ObtenerAsync(id, ct) ?? throw new ApiException(404, "El turno no existe.");

    private static TurnoRespuesta Respuesta(Turno turno, int? posicion) => new(
        turno.Id, turno.ClienteId, turno.EmpleadoId, turno.Estado,
        DateTime.SpecifyKind(turno.CreadoEn, DateTimeKind.Utc),
        turno.IniciadoEn is { } inicio ? DateTime.SpecifyKind(inicio, DateTimeKind.Utc) : null,
        turno.FinalizadoEn is { } fin ? DateTime.SpecifyKind(fin, DateTimeKind.Utc) : null, posicion);
}
