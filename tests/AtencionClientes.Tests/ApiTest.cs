using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace AtencionClientes.Tests;

public abstract class ApiTest(ApiFactory factory) : IAsyncLifetime
{
    protected ApiFactory Factory { get; } = factory;
    protected HttpClient Http { get; } = factory.CreateClient();

    public Task InitializeAsync() => Factory.ResetAsync();
    public Task DisposeAsync() { Http.Dispose(); return Task.CompletedTask; }

    protected async Task<JsonElement> Crear(string ruta, object datos)
    {
        var response = await Http.PostAsJsonAsync(ruta, datos);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    protected async Task<int> Cliente(string nombre = "Ana") =>
        (await Crear("/api/clientes", new { nombre, correo = "ana@ejemplo.com" })).GetProperty("id").GetInt32();

    protected async Task<int> Empleado() =>
        (await Crear("/api/empleados", new { nombre = "Luis" })).GetProperty("id").GetInt32();

    protected async Task<int> Turno(int clienteId) =>
        (await Crear("/api/turnos", new { clienteId })).GetProperty("id").GetInt32();

    protected async Task<JsonElement> Consultar(string ruta)
    {
        var response = await Http.GetAsync(ruta);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }
}
