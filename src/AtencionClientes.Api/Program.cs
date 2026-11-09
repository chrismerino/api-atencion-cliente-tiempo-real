using System.Text.Json.Serialization;
using AtencionClientes.Api.Data;
using AtencionClientes.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false)));
builder.Services.AddDbContext<AtencionDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer")
        ?? throw new InvalidOperationException("Configura ConnectionStrings__SqlServer antes de iniciar la API.")));
builder.Services.AddScoped<TurnosRepository>();
builder.Services.AddScoped<TurnosService>();
builder.Services.AddScoped<ClientesRepository>();
builder.Services.AddScoped<EmpleadosRepository>();
builder.Services.AddScoped<ClientesService>();
builder.Services.AddScoped<EmpleadosService>();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();
app.UseExceptionHandler();
app.MapControllers();
app.Run();

public partial class Program;
