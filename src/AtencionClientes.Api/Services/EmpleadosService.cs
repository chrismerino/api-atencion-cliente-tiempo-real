using AtencionClientes.Api.Data;
using AtencionClientes.Api.Dtos;
using AtencionClientes.Api.Models;

namespace AtencionClientes.Api.Services;

public class EmpleadosService(EmpleadosRepository repository)
{
    public async Task<EmpleadoRespuesta> CrearAsync(EmpleadoSolicitud datos, CancellationToken ct = default)
    {
        var empleado = new Empleado { Nombre = datos.Nombre.Trim() };
        repository.Agregar(empleado);
        await repository.GuardarAsync(ct);
        return new(empleado.Id, empleado.Nombre);
    }

    public async Task<List<EmpleadoRespuesta>> DisponiblesAsync(CancellationToken ct = default) =>
        (await repository.DisponiblesAsync(ct)).Select(e => new EmpleadoRespuesta(e.Id, e.Nombre)).ToList();
}
