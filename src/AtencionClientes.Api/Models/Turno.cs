namespace AtencionClientes.Api.Models;

public class Turno
{
    public int Id { get; set; }
    public int ClienteId { get; set; }
    public Cliente Cliente { get; set; } = null!;
    public int? EmpleadoId { get; set; }
    public Empleado? Empleado { get; set; }
    public EstadoTurno Estado { get; set; } = EstadoTurno.Pendiente;
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
    public DateTime? IniciadoEn { get; set; }
    public DateTime? FinalizadoEn { get; set; }
    public byte[] Version { get; set; } = [];
}
