using AtencionClientes.Api.Data;
using Microsoft.AspNetCore.SignalR;

namespace AtencionClientes.Api.Hubs;

public class TurnosHub : Hub
{
    private readonly ClientesRepository _clientesRepository;

    public TurnosHub(ClientesRepository clientesRepository)
    {
        _clientesRepository = clientesRepository;
    }

    internal const string GrupoEmpleados = "empleados";
    internal static string GrupoCliente(int id) => $"cliente-{id}";

    public Task SuscribirEmpleado() => CambiarGrupo(GrupoEmpleados);

    public async Task SuscribirCliente(int clienteId)
    {
        if (clienteId <= 0 || await _clientesRepository.ObtenerAsync(clienteId, Context.ConnectionAborted) is null)
            throw new HubException("El cliente no existe.");
        await CambiarGrupo(GrupoCliente(clienteId));
    }

    private async Task CambiarGrupo(string grupo)
    {
        if (Context.Items.TryGetValue("grupo", out var anterior) && anterior is string nombre && nombre != grupo)
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, nombre);
        await Groups.AddToGroupAsync(Context.ConnectionId, grupo);
        Context.Items["grupo"] = grupo;
    }
}
