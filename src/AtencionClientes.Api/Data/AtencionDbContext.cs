using AtencionClientes.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AtencionClientes.Api.Data;

public class AtencionDbContext : DbContext
{
    public AtencionDbContext(DbContextOptions<AtencionDbContext> options) : base(options)
    {
    }

    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Empleado> Empleados => Set<Empleado>();
    public DbSet<Turno> Turnos => Set<Turno>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Cliente>(entity =>
        {
            entity.Property(c => c.Nombre).HasMaxLength(100).IsRequired();
            entity.Property(c => c.Correo).HasMaxLength(150).IsRequired();
            entity.Property(c => c.Telefono).HasMaxLength(25);
        });
        modelBuilder.Entity<Empleado>().Property(e => e.Nombre).HasMaxLength(100).IsRequired();
        modelBuilder.Entity<Turno>(entity =>
        {
            entity.Property(t => t.Version).IsRowVersion();
            entity.HasOne(t => t.Cliente).WithMany().HasForeignKey(t => t.ClienteId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(t => t.Empleado).WithMany().HasForeignKey(t => t.EmpleadoId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(t => new { t.Estado, t.CreadoEn, t.Id });
            // Un empleado solo puede tener un turno en atención.
            entity.HasIndex(t => t.EmpleadoId).IsUnique().HasFilter("[Estado] = 1 AND [EmpleadoId] IS NOT NULL");
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("CK_Turnos_Estado", "[Estado] BETWEEN 0 AND 3");
                table.HasCheckConstraint("CK_Turnos_EmpleadoEnAtencion", "[Estado] <> 1 OR [EmpleadoId] IS NOT NULL");
            });
        });
    }
}
