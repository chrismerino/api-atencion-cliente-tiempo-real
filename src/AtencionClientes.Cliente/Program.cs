using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;

if (args.Length == 0 || (args[0] != "empleado" && args[0] != "cliente"))
{
    Console.WriteLine("Uso: empleado [url] | cliente <id> [url]");
    return 1;
}
var esEmpleado = args[0] == "empleado";
var clienteId = 0;
if (!esEmpleado && (args.Length < 2 || !int.TryParse(args[1], out clienteId) || clienteId <= 0))
{
    Console.WriteLine("Indica un identificador de cliente válido.");
    return 1;
}
var indiceUrl = esEmpleado ? 1 : 2;
var direccion = args.Length > indiceUrl ? args[indiceUrl] : "http://localhost:5180";
if (!Uri.TryCreate(direccion, UriKind.Absolute, out var url) || (url.Scheme != "http" && url.Scheme != "https"))
{
    Console.WriteLine("La URL debe comenzar con http:// o https://.");
    return 1;
}
using var http = new HttpClient { BaseAddress = url };
await using var conexion = new HubConnectionBuilder()
    .WithUrl(new Uri(url, "/hubs/turnos"))
    .WithAutomaticReconnect()
    .Build();
var conocidos = new ConcurrentDictionary<int, byte>();
foreach (var evento in new[] { "TurnoCreado", "TurnoEnAtencion", "TurnoAtendido", "TurnoCancelado", "PosicionActualizada" })
{
    conexion.On<JsonElement>(evento, datos =>
    {
        if (datos.TryGetProperty("turnoId", out var turno) || datos.TryGetProperty("id", out turno))
            conocidos.TryAdd(turno.GetInt32(), 0);
        Console.WriteLine($"{DateTime.Now:HH:mm:ss} {evento}: {datos.GetRawText()}");
    });
}

async Task SuscribirYConsultar()
{
    if (esEmpleado) await conexion.InvokeAsync("SuscribirEmpleado");
    else await conexion.InvokeAsync("SuscribirCliente", clienteId);
    var ruta = esEmpleado ? "/api/turnos/pendientes" : $"/api/turnos?clienteId={clienteId}";
    var turnos = await http.GetFromJsonAsync<JsonElement>(ruta);
    foreach (var turno in turnos.EnumerateArray())
    {
        if (esEmpleado || turno.GetProperty("clienteId").GetInt32() == clienteId)
        {
            conocidos.TryAdd(turno.GetProperty("id").GetInt32(), 0);
            Console.WriteLine($"Turno actual: {turno.GetRawText()}");
        }
    }
    foreach (var id in conocidos.Keys)
    {
        var actual = await http.GetFromJsonAsync<JsonElement>($"/api/turnos/{id}");
        Console.WriteLine($"Estado actual: {actual.GetRawText()}");
    }
}

conexion.Reconnecting += error =>
{
    Console.WriteLine("Conexión interrumpida. Reconectando...");
    return Task.CompletedTask;
};
conexion.Reconnected += async id =>
{
    try
    {
        await SuscribirYConsultar();
        Console.WriteLine("Suscripción recuperada.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"No se pudo recuperar la suscripción: {ex.Message}");
    }
};
var salida = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
conexion.Closed += error =>
{
    Console.WriteLine("La conexión se cerró.");
    salida.TrySetResult();
    return Task.CompletedTask;
};
Console.CancelKeyPress += (_, evento) =>
{
    evento.Cancel = true;
    salida.TrySetResult();
};
try
{
    await conexion.StartAsync();
    await SuscribirYConsultar();
    Console.WriteLine($"Conectado como {(esEmpleado ? "empleado" : $"cliente {clienteId}")}. Ctrl+C para salir.");
    await salida.Task;
    await conexion.StopAsync();
    return 0;
}
catch (Exception ex)
{
    Console.WriteLine($"No se pudo conectar: {ex.Message}");
    return 1;
}
