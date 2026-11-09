using AtencionClientes.Api.Data;
using AtencionClientes.Api.Dtos;
using AtencionClientes.Api.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace AtencionClientes.Api.Services;

public class NotificacionesService
{
    private readonly IHubContext<TurnosHub> _hubContext;
    private readonly TurnosRepository _turnosRepository;
    private readonly ILogger<NotificacionesService> _logger;

    public NotificacionesService(IHubContext<TurnosHub> hubContext, TurnosRepository turnosRepository, ILogger<NotificacionesService> logger)
    {
        _hubContext = hubContext;
        _turnosRepository = turnosRepository;
        _logger = logger;
    }

    private static readonly SemaphoreSlim _ordenEnvios = new(1, 1);

    public async Task NotificarAsync(string evento, TurnoRespuesta turno)
    {
        // El aviso continúa aunque se cierre la petición HTTP.
        using var plazo = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        try
        {
            await _ordenEnvios.WaitAsync(plazo.Token);
            try
            {
                var grupo = evento == "TurnoCreado" ? TurnosHub.GrupoEmpleados : TurnosHub.GrupoCliente(turno.ClienteId);
                await _hubContext.Clients.Group(grupo).SendAsync(evento, turno, plazo.Token);
                await ActualizarPosicionesAsync(plazo.Token);
            }
            finally
            {
                _ordenEnvios.Release();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo enviar {Evento} del turno {TurnoId}", evento, turno.Id);
        }
    }

    private async Task ActualizarPosicionesAsync(CancellationToken ct = default)
    {
        var pendientes = await _turnosRepository.PendientesAsync(ct);
        for (var i = 0; i < pendientes.Count; i++)
        {
            var turno = pendientes[i];
            var aviso = new PosicionRespuesta(turno.Id, turno.ClienteId, turno.Estado, i + 1);
            await _hubContext.Clients.Group(TurnosHub.GrupoCliente(turno.ClienteId)).SendAsync("PosicionActualizada", aviso, ct);
        }
    }
}
