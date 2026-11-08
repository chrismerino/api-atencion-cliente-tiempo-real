using System.ComponentModel.DataAnnotations;

namespace AtencionClientes.Api.Dtos;

public record ClienteSolicitud(
    [Required, MaxLength(100)] string Nombre,
    [Required, EmailAddress, MaxLength(150)] string Correo,
    [MaxLength(25)] string? Telefono = null);

public record ClienteRespuesta(int Id, string Nombre, string Correo, string? Telefono);
