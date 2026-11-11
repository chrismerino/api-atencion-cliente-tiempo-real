using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using AtencionClientes.Api.Dtos;
using AtencionClientes.Api.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

namespace AtencionClientes.Tests;

[Collection("Api")]
public class OrdenNotificacionesTests(ApiFactory factory) : ApiTest(factory)
{
    [Fact]
    public async Task AvisosConcurrentesNoDejanUnaPosicionAnteriorAlFinal()
    {
        var ana = await Cliente();
        var bea = await Cliente("Beatriz");
        var primero = await Turno(ana);
        var captura = new Captura(bea);
        using var aplicacion = Factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.AddSingleton<IHubContext<TurnosHub>>(new HubConPausa(captura))));
        using var http = aplicacion.CreateClient();
        var crear = http.PostAsJsonAsync("/api/turnos", new { clienteId = bea });
        await captura.Bloqueada.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var cancelar = http.PatchAsJsonAsync($"/api/turnos/{primero}/estado", new { estado = "Cancelado" });
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            while ((await Consultar($"/api/turnos/{primero}")).GetProperty("estado").GetString() != "Cancelado")
                await Task.Delay(20, timeout.Token);
            await Task.WhenAny(captura.PosicionNueva.Task, Task.Delay(200));
        }
        finally { captura.Liberar.TrySetResult(); }
        Assert.Equal(HttpStatusCode.Created, (await crear).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await cancelar).StatusCode);
        Assert.Equal(1, captura.Posiciones.Last());
    }

    public sealed class Captura(int clienteId)
    {
        private int pausada;
        public TaskCompletionSource Bloqueada { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Liberar { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource PosicionNueva { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public ConcurrentQueue<int> Posiciones { get; } = new();

        public async Task Recibir(string metodo, object?[] argumentos, CancellationToken ct)
        {
            if (metodo != "PosicionActualizada" || argumentos[0] is not PosicionRespuesta aviso || aviso.ClienteId != clienteId) return;
            if (aviso.Posicion == 2 && Interlocked.Exchange(ref pausada, 1) == 0)
            {
                Bloqueada.TrySetResult();
                await Liberar.Task.WaitAsync(ct);
            }
            Posiciones.Enqueue(aviso.Posicion!.Value);
            if (aviso.Posicion == 1) PosicionNueva.TrySetResult();
        }
    }

    private sealed class Receptor(Captura captura) : IClientProxy
    {
        public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default) => captura.Recibir(method, args, cancellationToken);
    }

    public class ClientesProxy : DispatchProxy
    {
        public Captura Captura { get; set; } = null!;
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            targetMethod?.Name == "Group" ? new Receptor(Captura) : throw new NotSupportedException();
    }

    private sealed class HubConPausa : IHubContext<TurnosHub>
    {
        public IHubClients Clients { get; }
        public IGroupManager Groups => throw new NotSupportedException();
        public HubConPausa(Captura captura)
        {
            Clients = DispatchProxy.Create<IHubClients, ClientesProxy>();
            ((ClientesProxy)Clients).Captura = captura;
        }
    }
}
