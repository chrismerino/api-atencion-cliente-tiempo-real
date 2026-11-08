using AtencionClientes.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AtencionClientes.Api.Data;

public class ClientesRepository(AtencionDbContext db)
{
    public Task<List<Cliente>> ListarAsync(CancellationToken ct) => db.Clientes.AsNoTracking().OrderBy(c => c.Id).ToListAsync(ct);
    public Task<Cliente?> ObtenerAsync(int id, CancellationToken ct) => db.Clientes.SingleOrDefaultAsync(c => c.Id == id, ct);
    public void Agregar(Cliente cliente) => db.Clientes.Add(cliente);
    public Task GuardarAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
