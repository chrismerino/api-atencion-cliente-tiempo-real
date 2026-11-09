using System.ComponentModel.DataAnnotations;
using AtencionClientes.Api.Models;

namespace AtencionClientes.Api.Dtos;

public record CrearTurnoSolicitud([Range(1, int.MaxValue)] int ClienteId);
public record AsignarTurnoSolicitud([Range(1, int.MaxValue)] int EmpleadoId);
public record CambiarEstadoSolicitud([Required] EstadoTurno? Estado, [Range(1, int.MaxValue)] int? EmpleadoId = null);
public record TurnoRespuesta(int Id, int ClienteId, int? EmpleadoId, EstadoTurno Estado,
    DateTime CreadoEn, DateTime? IniciadoEn, DateTime? FinalizadoEn, int? Posicion);
public record PosicionRespuesta(int TurnoId, int ClienteId, EstadoTurno Estado, int? Posicion);
