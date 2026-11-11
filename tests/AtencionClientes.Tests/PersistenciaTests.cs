using System.Data.Common;
using System.Net;
using System.Net.Http.Json;
using AtencionClientes.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace AtencionClientes.Tests;

[Collection("Api")]
public class PersistenciaTests(ApiFactory factory) : ApiTest(factory)
{
    [Fact]
    public async Task FalloAlCalcularPosicionNoDejaUnRegistroQueLaApiReportoFallido()
    {
        var cliente = await Cliente();
        using var aplicacion = Factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.AddDbContext<AtencionDbContext>(options => options.AddInterceptors(new FalloLecturaPosicion()))));
        using var http = aplicacion.CreateClient();
        Assert.Equal(HttpStatusCode.InternalServerError, (await http.PostAsJsonAsync("/api/turnos", new { clienteId = cliente })).StatusCode);
        Assert.Empty((await Consultar("/api/turnos/pendientes")).EnumerateArray());
    }

    private sealed class FalloLecturaPosicion : DbCommandInterceptor
    {
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("COUNT(") && command.CommandText.Contains("[Turnos]"))
                throw new IOException("Fallo de lectura simulado durante el cálculo de posición.");
            return ValueTask.FromResult(result);
        }
    }
}
