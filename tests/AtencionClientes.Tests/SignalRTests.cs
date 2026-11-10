using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;

namespace AtencionClientes.Tests;

[Collection("Api")]
public class SignalRTests(ApiFactory factory) : ApiTest(factory)
{
    private async Task<Conexion> Conectar(int? clienteId = null)
    {
        var conexion = new Conexion(Factory);
        await conexion.Hub.StartAsync();
        if (clienteId is { } id) await conexion.Hub.InvokeAsync("SuscribirCliente", id);
        else await conexion.Hub.InvokeAsync("SuscribirEmpleado");
        return conexion;
    }

    [Fact]
    public async Task CreacionInicioYFinalizacionNotificanAlDestinatarioCorrespondiente()
    {
        var ana = await Cliente("Ana");
        var bea = await Cliente("Beatriz");
        var empleado = await Empleado();
        await using var empleados = await Conectar();
        await using var clienteAna = await Conectar(ana);
        await using var clienteBea = await Conectar(bea);
        var turnoAna = await Turno(ana);
        Assert.Equal(turnoAna, (await empleados.Leer("TurnoCreado")).GetProperty("id").GetInt32());
        var turnoBea = await Turno(bea);
        Assert.Equal(turnoBea, (await empleados.Leer("TurnoCreado")).GetProperty("id").GetInt32());
        await clienteBea.Leer("PosicionActualizada");
        var respuesta = await Http.PatchAsJsonAsync($"/api/turnos/{turnoAna}/estado", new { estado = "EnAtencion", empleadoId = empleado });
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var aviso = await clienteAna.Leer("TurnoEnAtencion");
        Assert.Equal(turnoAna, aviso.GetProperty("id").GetInt32());
        Assert.Equal("EnAtencion", aviso.GetProperty("estado").GetString());
        Assert.Equal(1, (await clienteBea.Leer("PosicionActualizada")).GetProperty("posicion").GetInt32());
        await clienteBea.SinEvento("TurnoEnAtencion");
        Assert.Equal(HttpStatusCode.OK, (await Http.PostAsync($"/api/turnos/{turnoAna}/finalizacion", null)).StatusCode);
        Assert.Equal(turnoAna, (await clienteAna.Leer("TurnoAtendido")).GetProperty("id").GetInt32());
        var posicion = await clienteBea.Leer("PosicionActualizada");
        Assert.Equal(turnoBea, posicion.GetProperty("turnoId").GetInt32());
        Assert.Equal(1, posicion.GetProperty("posicion").GetInt32());
    }

    [Fact]
    public async Task CancelarNotificaAlPropietarioYAdelantaAlClienteAfectado()
    {
        var ana = await Cliente();
        var bea = await Cliente("Beatriz");
        var uno = await Turno(ana);
        var dos = await Turno(bea);
        await using var clienteAna = await Conectar(ana);
        await using var clienteBea = await Conectar(bea);
        Assert.Equal(HttpStatusCode.OK, (await Http.PatchAsJsonAsync($"/api/turnos/{uno}/estado", new { estado = "Cancelado" })).StatusCode);
        Assert.Equal(uno, (await clienteAna.Leer("TurnoCancelado")).GetProperty("id").GetInt32());
        var aviso = await clienteBea.Leer("PosicionActualizada");
        Assert.Equal(dos, aviso.GetProperty("turnoId").GetInt32());
        Assert.Equal(1, aviso.GetProperty("posicion").GetInt32());
        await clienteBea.SinEvento("TurnoCancelado");
    }

    [Fact]
    public async Task EstadoRepetidoYOperacionesInvalidasNoEnvianNuevosEventos()
    {
        var cliente = await Cliente();
        var turno = await Turno(cliente);
        await using var conexion = await Conectar(cliente);
        await Http.PatchAsJsonAsync($"/api/turnos/{turno}/estado", new { estado = "Cancelado" });
        await conexion.Leer("TurnoCancelado");
        Assert.Equal(HttpStatusCode.OK, (await Http.PatchAsJsonAsync($"/api/turnos/{turno}/estado", new { estado = "Cancelado" })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Http.PatchAsJsonAsync($"/api/turnos/{turno}/estado", new { estado = "Pendiente" })).StatusCode);
        await conexion.SinEvento("TurnoCancelado");
    }

    [Fact]
    public async Task SuscripcionRechazaClienteInexistenteYAdmiteUnaConexionNueva()
    {
        await using var invalida = new Conexion(Factory);
        await invalida.Hub.StartAsync();
        await Assert.ThrowsAsync<Microsoft.AspNetCore.SignalR.HubException>(() => invalida.Hub.InvokeAsync("SuscribirCliente", 999999));
        var id = await Cliente();
        await using (var anterior = await Conectar(id)) await anterior.Hub.StopAsync();
        await using var nueva = await Conectar(id);
        var turno = await Turno(id);
        Assert.Equal(turno, (await nueva.Leer("PosicionActualizada")).GetProperty("turnoId").GetInt32());
    }

    private sealed class Conexion : IAsyncDisposable
    {
        private readonly Dictionary<string, Channel<JsonElement>> eventos = new();
        public HubConnection Hub { get; }

        public Conexion(ApiFactory factory)
        {
            Hub = new HubConnectionBuilder().WithUrl(new Uri(factory.Server.BaseAddress, "/hubs/turnos"), options =>
            {
                options.Transports = HttpTransportType.LongPolling;
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
            }).Build();
            foreach (var nombre in new[] { "TurnoCreado", "TurnoEnAtencion", "TurnoAtendido", "TurnoCancelado", "PosicionActualizada" })
            {
                var canal = Channel.CreateUnbounded<JsonElement>();
                eventos[nombre] = canal;
                Hub.On<JsonElement>(nombre, dato => canal.Writer.TryWrite(dato));
            }
        }

        public async Task<JsonElement> Leer(string evento)
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            return await eventos[evento].Reader.ReadAsync(timeout.Token);
        }

        public async Task SinEvento(string evento)
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await eventos[evento].Reader.ReadAsync(timeout.Token));
        }

        public ValueTask DisposeAsync() => Hub.DisposeAsync();
    }
}
