using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace AtencionClientes.Tests;

[Collection("Api")]
public class ClientesEmpleadosTests(ApiFactory factory) : ApiTest(factory)
{
    [Fact]
    public async Task RegistrarYActualizarClienteConservaLosDatos()
    {
        var response = await Http.PostAsJsonAsync("/api/clientes", new { nombre = " Ana ", correo = "ana@ejemplo.com", telefono = "70001111" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var cliente = await response.Content.ReadFromJsonAsync<JsonElement>();
        var id = cliente.GetProperty("id").GetInt32();
        Assert.Equal("Ana", (await Consultar(response.Headers.Location!.ToString())).GetProperty("nombre").GetString());
        var updated = await Http.PutAsJsonAsync($"/api/clientes/{id}", new { nombre = "Ana López", correo = "ana.lopez@ejemplo.com" });
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        Assert.Equal("ana.lopez@ejemplo.com", (await Consultar($"/api/clientes/{id}")).GetProperty("correo").GetString());
        Assert.Single((await Consultar("/api/clientes")).EnumerateArray());
    }

    [Theory]
    [InlineData("", "ana@ejemplo.com")]
    [InlineData("   ", "ana@ejemplo.com")]
    [InlineData(null, "ana@ejemplo.com")]
    [InlineData("Ana", "correo-invalido")]
    public async Task RegistrarClienteRechazaDatosInvalidos(string? nombre, string correo)
    {
        var response = await Http.PostAsJsonAsync("/api/clientes", new { nombre, correo });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty((await Consultar("/api/clientes")).EnumerateArray());
    }

    [Fact]
    public async Task ConsultarOActualizarClienteInexistenteDevuelve404()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await Http.GetAsync("/api/clientes/999999")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Http.PutAsJsonAsync("/api/clientes/999999", new { nombre = "Ana", correo = "ana@ejemplo.com" })).StatusCode);
    }

    [Fact]
    public async Task EmpleadoRegistradoApareceDisponible()
    {
        var id = await Empleado();
        var disponibles = await Consultar("/api/empleados/disponibles");
        Assert.Equal(id, Assert.Single(disponibles.EnumerateArray()).GetProperty("id").GetInt32());
        Assert.Equal(HttpStatusCode.BadRequest, (await Http.PostAsJsonAsync("/api/empleados", new { nombre = " " })).StatusCode);
    }
    [Theory]
    [InlineData(" @ ")]
    [InlineData("ana @ejemplo.com")]
    [InlineData("ana@ ejemplo.com")]
    public async Task CorreoConEspaciosInternosNoSeRegistraNiActualiza(string correo)
    {
        Assert.Equal(HttpStatusCode.BadRequest, (await Http.PostAsJsonAsync("/api/clientes", new { nombre = "Ana", correo })).StatusCode);
        Assert.Empty((await Consultar("/api/clientes")).EnumerateArray());
        var cliente = await Cliente();
        Assert.Equal(HttpStatusCode.BadRequest, (await Http.PutAsJsonAsync($"/api/clientes/{cliente}", new { nombre = "Ana", correo })).StatusCode);
        Assert.Equal("ana@ejemplo.com", (await Consultar($"/api/clientes/{cliente}")).GetProperty("correo").GetString());
    }

}
