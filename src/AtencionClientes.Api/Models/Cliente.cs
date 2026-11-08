namespace AtencionClientes.Api.Models;

public class Cliente
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Correo { get; set; } = "";
    public string? Telefono { get; set; }
}
