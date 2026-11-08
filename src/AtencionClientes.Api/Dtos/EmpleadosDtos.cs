using System.ComponentModel.DataAnnotations;

namespace AtencionClientes.Api.Dtos;

public record EmpleadoSolicitud([Required, MaxLength(100)] string Nombre);
public record EmpleadoRespuesta(int Id, string Nombre);
