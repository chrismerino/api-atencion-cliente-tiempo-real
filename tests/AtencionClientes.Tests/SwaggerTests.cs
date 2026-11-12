using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AtencionClientes.Tests;

public class SwaggerTests
{
    [Theory]
    [InlineData("Development", HttpStatusCode.OK)]
    [InlineData("Production", HttpStatusCode.NotFound)]
    public async Task SwaggerSoloEstaDisponibleEnDesarrollo(string entorno, HttpStatusCode esperado)
    {
        using var factory = CrearApi(entorno);
        using var http = factory.CreateClient();
        Assert.Equal(esperado, (await http.GetAsync("/swagger/index.html")).StatusCode);
        Assert.Equal(esperado, (await http.GetAsync("/swagger/v1/swagger.json")).StatusCode);
    }

    [Fact]
    public async Task DocumentoIncluyeLosControladoresYEstadosComoTexto()
    {
        using var factory = CrearApi("Development");
        using var http = factory.CreateClient();
        var response = await http.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var documento = await response.Content.ReadFromJsonAsync<JsonElement>();
        var rutas = documento.GetProperty("paths");
        Assert.True(rutas.GetProperty("/api/clientes").TryGetProperty("post", out _));
        Assert.True(rutas.GetProperty("/api/empleados").TryGetProperty("post", out _));
        Assert.True(rutas.GetProperty("/api/turnos").TryGetProperty("post", out _));
        Assert.True(rutas.GetProperty("/api/turnos/{id}/estado").TryGetProperty("patch", out _));
        var estado = documento.GetProperty("components").GetProperty("schemas")
            .GetProperty("CambiarEstadoSolicitud").GetProperty("properties").GetProperty("estado");
        Assert.Equal("string", estado.GetProperty("type").GetString());
        Assert.Equal(new[] { "Pendiente", "EnAtencion", "Atendido", "Cancelado" },
            estado.GetProperty("enum").EnumerateArray().Select(valor => valor.GetString()).ToArray());
    }

    private static WebApplicationFactory<Program> CrearApi(string entorno) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(entorno);
            builder.UseSetting("ConnectionStrings:SqlServer",
                "Server=localhost;Database=SwaggerTests;Integrated Security=true;TrustServerCertificate=True");
        });
}
