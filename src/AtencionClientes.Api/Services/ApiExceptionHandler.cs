using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AtencionClientes.Api.Services;

public class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var error = exception switch
        {
            ApiException ex => (ex.StatusCode, ex.Message),
            DbUpdateConcurrencyException => (409, "El turno cambió durante la operación. Consulta su estado e intenta nuevamente."),
            DbUpdateException { InnerException: SqlException { Number: 2601 or 2627 } } =>
                (409, "El empleado ya tiene un turno en atención."),
            _ => (500, "Ocurrió un error al procesar la solicitud.")
        };
        if (error.Item1 == 500)
            logger.LogError(exception, "Error al procesar {Ruta}", context.Request.Path);
        context.Response.StatusCode = error.Item1;
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = error.Item1,
            Title = error.Item1 == 500 ? "Error del servidor" : "No se pudo realizar la operación",
            Detail = error.Item2,
            Instance = context.Request.Path
        }, ct);
        return true;
    }
}
