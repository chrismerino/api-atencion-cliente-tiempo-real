using System.ComponentModel.DataAnnotations;
using AtencionClientes.Api.Data;
using AtencionClientes.Api.Dtos;
using AtencionClientes.Api.Models;

namespace AtencionClientes.Api.Services;

public class ClientesService
{
    private readonly ClientesRepository _clientesRepository;

    public ClientesService(ClientesRepository clientesRepository)
    {
        _clientesRepository = clientesRepository;
    }

    public async Task<List<ClienteRespuesta>> ListarAsync(CancellationToken ct = default) =>
        (await _clientesRepository.ListarAsync(ct)).Select(Respuesta).ToList();

    public async Task<ClienteRespuesta> ObtenerAsync(int id, CancellationToken ct = default) => Respuesta(await Buscar(id, ct));

    public async Task<ClienteRespuesta> CrearAsync(ClienteSolicitud datos, CancellationToken ct = default)
    {
        var cliente = new Cliente
        {
            Nombre = datos.Nombre.Trim(),
            Correo = NormalizarCorreo(datos.Correo),
            Telefono = datos.Telefono?.Trim()
        };
        _clientesRepository.Agregar(cliente);
        await _clientesRepository.GuardarAsync(ct);
        return Respuesta(cliente);
    }

    public async Task<ClienteRespuesta> ActualizarAsync(int id, ClienteSolicitud datos, CancellationToken ct = default)
    {
        var cliente = await Buscar(id, ct);
        cliente.Nombre = datos.Nombre.Trim();
        cliente.Correo = NormalizarCorreo(datos.Correo);
        cliente.Telefono = datos.Telefono?.Trim();
        await _clientesRepository.GuardarAsync(ct);
        return Respuesta(cliente);
    }

    private static string NormalizarCorreo(string correo)
    {
        var normalizado = correo.Trim();
        if (normalizado.Any(char.IsWhiteSpace) || !new EmailAddressAttribute().IsValid(normalizado))
            throw new ApiException(400, "Indica un correo válido, sin espacios internos.");
        return normalizado;
    }

    private async Task<Cliente> Buscar(int id, CancellationToken ct) =>
        await _clientesRepository.ObtenerAsync(id, ct) ?? throw new ApiException(404, "El cliente no existe.");

    private static ClienteRespuesta Respuesta(Cliente cliente) => new(cliente.Id, cliente.Nombre, cliente.Correo, cliente.Telefono);
}
