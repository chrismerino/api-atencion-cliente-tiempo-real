using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace AtencionClientes.Tests;

[Collection("Api")]
public class TurnosTests(ApiFactory factory) : ApiTest(factory)
{
    private Task<HttpResponseMessage> Estado(int id, string estado, int? empleadoId = null) =>
        Http.PatchAsJsonAsync($"/api/turnos/{id}/estado", new { estado, empleadoId });
    private Task<HttpResponseMessage> Asignar(int id, int empleadoId) =>
        Http.PatchAsJsonAsync($"/api/turnos/{id}/asignacion", new { empleadoId });

    [Fact]
    public async Task FilaOrdenaLosPendientesYCancelarActualizaPosicion()
    {
        var cliente = await Cliente();
        var primero = await Turno(cliente);
        var segundo = await Turno(cliente);
        Assert.Equal(1, (await Consultar($"/api/turnos/{primero}/posicion")).GetProperty("posicion").GetInt32());
        Assert.Equal(2, (await Consultar($"/api/turnos/{segundo}/posicion")).GetProperty("posicion").GetInt32());
        var fila = (await Consultar("/api/turnos/pendientes")).EnumerateArray().ToArray();
        Assert.Equal(new[] { primero, segundo }, fila.Select(t => t.GetProperty("id").GetInt32()));
        Assert.Equal(HttpStatusCode.OK, (await Estado(primero, "Cancelado")).StatusCode);
        Assert.Equal(1, (await Consultar($"/api/turnos/{segundo}/posicion")).GetProperty("posicion").GetInt32());
        Assert.Equal(JsonValueKind.Null, (await Consultar($"/api/turnos/{primero}/posicion")).GetProperty("posicion").ValueKind);
    }

    [Fact]
    public async Task AsignarIniciaAtencionYFinalizarLiberaEmpleado()
    {
        var cliente = await Cliente();
        var empleado = await Empleado();
        var turno = await Turno(cliente);
        Assert.Equal(HttpStatusCode.OK, (await Asignar(turno, empleado)).StatusCode);
        var actual = await Consultar($"/api/turnos/{turno}");
        Assert.Equal("EnAtencion", actual.GetProperty("estado").GetString());
        Assert.Equal(empleado, actual.GetProperty("empleadoId").GetInt32());
        Assert.NotEqual(JsonValueKind.Null, actual.GetProperty("iniciadoEn").ValueKind);
        Assert.Empty((await Consultar("/api/empleados/disponibles")).EnumerateArray());
        Assert.Empty((await Consultar("/api/turnos/pendientes")).EnumerateArray());
        Assert.Equal(HttpStatusCode.OK, (await Http.PostAsync($"/api/turnos/{turno}/finalizacion", null)).StatusCode);
        Assert.Equal("Atendido", (await Consultar($"/api/turnos/{turno}")).GetProperty("estado").GetString());
        Assert.Single((await Consultar("/api/empleados/disponibles")).EnumerateArray());
    }

    [Fact]
    public async Task CambioDeEstadoPermiteIniciarYCancelarAtencion()
    {
        var turno = await Turno(await Cliente());
        var empleado = await Empleado();
        Assert.Equal(HttpStatusCode.OK, (await Estado(turno, "EnAtencion", empleado)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Estado(turno, "Cancelado")).StatusCode);
        Assert.Single((await Consultar("/api/empleados/disponibles")).EnumerateArray());
        Assert.Equal(HttpStatusCode.Conflict, (await Estado(turno, "Pendiente")).StatusCode);
    }

    [Fact]
    public async Task RepetirEstadoNoModificaFechasYEstadosFinalesNoSeReabren()
    {
        var turno = await Turno(await Cliente());
        var empleado = await Empleado();
        await Asignar(turno, empleado);
        await Estado(turno, "Atendido");
        var fecha = (await Consultar($"/api/turnos/{turno}")).GetProperty("finalizadoEn").GetString();
        Assert.Equal(HttpStatusCode.OK, (await Estado(turno, "Atendido")).StatusCode);
        Assert.Equal(fecha, (await Consultar($"/api/turnos/{turno}")).GetProperty("finalizadoEn").GetString());
        Assert.Equal(HttpStatusCode.Conflict, (await Estado(turno, "Pendiente")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Estado(turno, "Cancelado")).StatusCode);
    }

    [Fact]
    public async Task ValidacionesRechazanReferenciasYEstadosInvalidos()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await Http.PostAsJsonAsync("/api/turnos", new { clienteId = 999999 })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Http.PostAsJsonAsync("/api/turnos", new { clienteId = 0 })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Http.GetAsync("/api/turnos/999999")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Http.GetAsync("/api/turnos/999999/posicion")).StatusCode);
        var turno = await Turno(await Cliente());
        Assert.Equal(HttpStatusCode.BadRequest, (await Estado(turno, "Desconocido")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Estado(turno, "EnAtencion")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Asignar(turno, 999999)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Estado(turno, "Atendido")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Http.PostAsync($"/api/turnos/{turno}/finalizacion", null)).StatusCode);
        Assert.Equal("Pendiente", (await Consultar($"/api/turnos/{turno}")).GetProperty("estado").GetString());
    }

    [Fact]
    public async Task EmpleadoOcupadoNoPuedeRecibirOtroTurno()
    {
        var cliente = await Cliente();
        var uno = await Turno(cliente);
        var dos = await Turno(cliente);
        var empleado = await Empleado();
        await Asignar(uno, empleado);
        Assert.Equal(HttpStatusCode.Conflict, (await Asignar(dos, empleado)).StatusCode);
        Assert.Equal("Pendiente", (await Consultar($"/api/turnos/{dos}")).GetProperty("estado").GetString());
    }

    [Fact]
    public async Task DosAsignacionesSimultaneasNoDuplicanLaAtencionDeUnEmpleado()
    {
        var cliente = await Cliente();
        var uno = await Turno(cliente);
        var dos = await Turno(cliente);
        var empleado = await Empleado();
        var responses = await Task.WhenAll(Asignar(uno, empleado), Asignar(dos, empleado));
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.OK);
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Conflict);
        Assert.Single((await Consultar("/api/turnos/pendientes")).EnumerateArray());
    }

    [Fact]
    public async Task DosEmpleadosNoPuedenTomarElMismoTurno()
    {
        var turno = await Turno(await Cliente());
        var uno = await Empleado();
        var dos = await Empleado();
        var responses = await Task.WhenAll(Asignar(turno, uno), Asignar(turno, dos));
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.OK);
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Conflict);
        Assert.Single((await Consultar("/api/empleados/disponibles")).EnumerateArray());
    }
}
