using AtencionClientes.Api.Dtos;
using AtencionClientes.Api.Data;
using AtencionClientes.Api.Hubs;
using AtencionClientes.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.Converters.Add(new EstadoTurnoJsonConverter()));
builder.Services.AddSignalR().AddJsonProtocol(options =>
    options.PayloadSerializerOptions.Converters.Add(new EstadoTurnoJsonConverter()));
builder.Services.AddCors(options => options.AddPolicy("ClientesExternos", policy =>
    policy.WithOrigins(builder.Configuration.GetSection("Cors:Origenes").Get<string[]>() ?? [])
        .AllowAnyHeader().AllowAnyMethod().AllowCredentials()));
builder.Services.AddScoped<NotificacionesService>();
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
app.UseCors("ClientesExternos");
app.MapControllers();
app.MapHub<TurnosHub>("/hubs/turnos");
app.Run();

public partial class Program;
