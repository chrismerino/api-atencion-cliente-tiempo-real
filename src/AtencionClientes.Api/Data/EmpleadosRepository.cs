using AtencionClientes.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AtencionClientes.Api.Data;

public class EmpleadosRepository
{
    private readonly AtencionDbContext _context;

    public EmpleadosRepository(AtencionDbContext context)
    {
        _context = context;
    }

    public Task<List<Empleado>> DisponiblesAsync(CancellationToken ct) => _context.Empleados.AsNoTracking()
        .Where(e => !_context.Turnos.Any(t => t.EmpleadoId == e.Id && t.Estado == EstadoTurno.EnAtencion))
        .OrderBy(e => e.Id).ToListAsync(ct);
    public Task<Empleado?> ObtenerAsync(int id, CancellationToken ct) => _context.Empleados.SingleOrDefaultAsync(e => e.Id == id, ct);
    public Task<bool> EstaOcupadoAsync(int id, CancellationToken ct) => _context.Turnos.AnyAsync(t => t.EmpleadoId == id && t.Estado == EstadoTurno.EnAtencion, ct);
    public void Agregar(Empleado empleado) => _context.Empleados.Add(empleado);
    public Task GuardarAsync(CancellationToken ct) => _context.SaveChangesAsync(ct);
}
