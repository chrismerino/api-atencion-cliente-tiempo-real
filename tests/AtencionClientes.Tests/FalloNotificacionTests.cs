using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AtencionClientes.Api.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

namespace AtencionClientes.Tests;

[Collection("Api")]
public class FalloNotificacionTests(ApiFactory factory) : ApiTest(factory)
{
    [Fact]
    public async Task FalloDelTransporteNoConvierteUnaCreacionPersistidaEnError()
    {
        var cliente = await Cliente();
        using var aplicacion = Factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.AddSingleton<IHubContext<TurnosHub>>(new HubSinTransporte())));
        using var http = aplicacion.CreateClient();
        var response = await http.PostAsJsonAsync("/api/turnos", new { clienteId = cliente });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var turno = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Pendiente", (await Consultar($"/api/turnos/{turno.GetProperty("id").GetInt32()}")).GetProperty("estado").GetString());
        Assert.Single((await Consultar("/api/turnos/pendientes")).EnumerateArray());
    }

    private class HubSinTransporte : IHubContext<TurnosHub>
    {
        public IHubClients Clients => throw new IOException("Transporte no disponible durante la prueba.");
        public IGroupManager Groups => throw new NotSupportedException();
    }
}
