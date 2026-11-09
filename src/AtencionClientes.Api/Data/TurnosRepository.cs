using AtencionClientes.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AtencionClientes.Api.Data;

public class TurnosRepository
{
    private readonly AtencionDbContext _context;

    public TurnosRepository(AtencionDbContext context)
    {
        _context = context;
    }

    public Task<Turno?> ObtenerAsync(int id, CancellationToken ct) => _context.Turnos.SingleOrDefaultAsync(t => t.Id == id, ct);
    public Task<List<Turno>> PendientesAsync(CancellationToken ct) => _context.Turnos.AsNoTracking()
        .Where(t => t.Estado == EstadoTurno.Pendiente).OrderBy(t => t.CreadoEn).ThenBy(t => t.Id).ToListAsync(ct);

    public async Task<int?> PosicionAsync(Turno turno, CancellationToken ct)
    {
        if (turno.Estado != EstadoTurno.Pendiente) return null;
        return 1 + await _context.Turnos.CountAsync(t => t.Estado == EstadoTurno.Pendiente &&
            (t.CreadoEn < turno.CreadoEn || (t.CreadoEn == turno.CreadoEn && t.Id < turno.Id)), ct);
    }

    public Task<List<Turno>> PorClienteAsync(int clienteId, CancellationToken ct) => _context.Turnos.AsNoTracking()
        .Where(t => t.ClienteId == clienteId).OrderBy(t => t.CreadoEn).ThenBy(t => t.Id).ToListAsync(ct);

    public async Task<int?> GuardarConPosicionAsync(Turno turno, CancellationToken ct)
    {
        await using var transaccion = await _context.Database.BeginTransactionAsync(ct);
        await _context.SaveChangesAsync(ct);
        var posicion = await PosicionAsync(turno, ct);
        await transaccion.CommitAsync(ct);
        return posicion;
    }

    public void Agregar(Turno turno) => _context.Turnos.Add(turno);
}
