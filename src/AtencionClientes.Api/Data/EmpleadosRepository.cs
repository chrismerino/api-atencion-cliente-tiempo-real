using AtencionClientes.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AtencionClientes.Api.Data;

public class EmpleadosRepository(AtencionDbContext db)
{
    public Task<List<Empleado>> DisponiblesAsync(CancellationToken ct) => db.Empleados.AsNoTracking()
        .Where(e => !db.Turnos.Any(t => t.EmpleadoId == e.Id && t.Estado == EstadoTurno.EnAtencion))
        .OrderBy(e => e.Id).ToListAsync(ct);
    public Task<Empleado?> ObtenerAsync(int id, CancellationToken ct) => db.Empleados.SingleOrDefaultAsync(e => e.Id == id, ct);
    public Task<bool> EstaOcupadoAsync(int id, CancellationToken ct) => db.Turnos.AnyAsync(t => t.EmpleadoId == id && t.Estado == EstadoTurno.EnAtencion, ct);
    public void Agregar(Empleado empleado) => db.Empleados.Add(empleado);
    public Task GuardarAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
