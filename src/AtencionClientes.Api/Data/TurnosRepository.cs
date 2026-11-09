using AtencionClientes.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AtencionClientes.Api.Data;

public class TurnosRepository(AtencionDbContext db)
{
    public Task<Turno?> ObtenerAsync(int id, CancellationToken ct) => db.Turnos.SingleOrDefaultAsync(t => t.Id == id, ct);
    public Task<List<Turno>> PendientesAsync(CancellationToken ct) => db.Turnos.AsNoTracking()
        .Where(t => t.Estado == EstadoTurno.Pendiente).OrderBy(t => t.CreadoEn).ThenBy(t => t.Id).ToListAsync(ct);

    public async Task<int?> PosicionAsync(Turno turno, CancellationToken ct)
    {
        if (turno.Estado != EstadoTurno.Pendiente) return null;
        return 1 + await db.Turnos.CountAsync(t => t.Estado == EstadoTurno.Pendiente &&
            (t.CreadoEn < turno.CreadoEn || (t.CreadoEn == turno.CreadoEn && t.Id < turno.Id)), ct);
    }

    public void Agregar(Turno turno) => db.Turnos.Add(turno);
    public Task GuardarAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
