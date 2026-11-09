using AtencionClientes.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AtencionClientes.Api.Data;

public class ClientesRepository
{
    private readonly AtencionDbContext _context;

    public ClientesRepository(AtencionDbContext context)
    {
        _context = context;
    }

    public Task<List<Cliente>> ListarAsync(CancellationToken ct) => _context.Clientes.AsNoTracking().OrderBy(c => c.Id).ToListAsync(ct);
    public Task<Cliente?> ObtenerAsync(int id, CancellationToken ct) => _context.Clientes.SingleOrDefaultAsync(c => c.Id == id, ct);
    public void Agregar(Cliente cliente) => _context.Clientes.Add(cliente);
    public Task GuardarAsync(CancellationToken ct) => _context.SaveChangesAsync(ct);
}
