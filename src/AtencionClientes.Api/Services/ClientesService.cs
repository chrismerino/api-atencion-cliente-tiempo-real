using AtencionClientes.Api.Data;
using AtencionClientes.Api.Dtos;
using AtencionClientes.Api.Models;

namespace AtencionClientes.Api.Services;

public class ClientesService(ClientesRepository repository)
{
    public async Task<List<ClienteRespuesta>> ListarAsync(CancellationToken ct = default) =>
        (await repository.ListarAsync(ct)).Select(Respuesta).ToList();

    public async Task<ClienteRespuesta> ObtenerAsync(int id, CancellationToken ct = default) => Respuesta(await Buscar(id, ct));

    public async Task<ClienteRespuesta> CrearAsync(ClienteSolicitud datos, CancellationToken ct = default)
    {
        var cliente = new Cliente { Nombre = datos.Nombre.Trim(), Correo = datos.Correo.Trim(), Telefono = datos.Telefono?.Trim() };
        repository.Agregar(cliente);
        await repository.GuardarAsync(ct);
        return Respuesta(cliente);
    }

    public async Task<ClienteRespuesta> ActualizarAsync(int id, ClienteSolicitud datos, CancellationToken ct = default)
    {
        var cliente = await Buscar(id, ct);
        cliente.Nombre = datos.Nombre.Trim();
        cliente.Correo = datos.Correo.Trim();
        cliente.Telefono = datos.Telefono?.Trim();
        await repository.GuardarAsync(ct);
        return Respuesta(cliente);
    }

    private async Task<Cliente> Buscar(int id, CancellationToken ct) =>
        await repository.ObtenerAsync(id, ct) ?? throw new ApiException(404, "El cliente no existe.");

    private static ClienteRespuesta Respuesta(Cliente cliente) => new(cliente.Id, cliente.Nombre, cliente.Correo, cliente.Telefono);
}
