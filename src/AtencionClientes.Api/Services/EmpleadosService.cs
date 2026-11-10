using AtencionClientes.Api.Data;
using AtencionClientes.Api.Dtos;
using AtencionClientes.Api.Models;

namespace AtencionClientes.Api.Services;

public class EmpleadosService
{
    private readonly EmpleadosRepository _empleadosRepository;

    public EmpleadosService(EmpleadosRepository empleadosRepository)
    {
        _empleadosRepository = empleadosRepository;
    }

    public async Task<EmpleadoRespuesta> CrearAsync(EmpleadoSolicitud datos, CancellationToken ct = default)
    {
        var empleado = new Empleado { Nombre = datos.Nombre.Trim() };
        _empleadosRepository.Agregar(empleado);
        await _empleadosRepository.GuardarAsync(ct);
        return new(empleado.Id, empleado.Nombre);
    }

    public async Task<List<EmpleadoRespuesta>> DisponiblesAsync(CancellationToken ct = default) =>
        (await _empleadosRepository.DisponiblesAsync(ct)).Select(e => new EmpleadoRespuesta(e.Id, e.Nombre)).ToList();
}
