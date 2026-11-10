using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AtencionClientes.Api.Services;

public class ApiExceptionHandler : IExceptionHandler
{
    private readonly ILogger<ApiExceptionHandler> _logger;

    public ApiExceptionHandler(ILogger<ApiExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        (int Estado, string Mensaje) error = exception switch
        {
            ApiException ex => (ex.StatusCode, ex.Message),
            DbUpdateConcurrencyException => (409, "El turno cambió durante la operación. Consulta su estado e intenta nuevamente."),
            DbUpdateException { InnerException: SqlException { Number: 2601 or 2627 } } =>
                (409, "El empleado ya tiene un turno en atención."),
            _ => (500, "Ocurrió un error al procesar la solicitud.")
        };
        if (error.Estado == 500)
            _logger.LogError(exception, "Error al procesar {Ruta}", context.Request.Path);
        context.Response.StatusCode = error.Estado;
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = error.Estado,
            Title = error.Estado == 500 ? "Error del servidor" : "No se pudo realizar la operación",
            Detail = error.Mensaje,
            Instance = context.Request.Path
        }, ct);
        return true;
    }
}
