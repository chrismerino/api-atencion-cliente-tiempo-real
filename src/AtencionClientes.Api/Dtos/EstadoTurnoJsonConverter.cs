using System.Text.Json;
using System.Text.Json.Serialization;
using AtencionClientes.Api.Models;

namespace AtencionClientes.Api.Dtos;

public class EstadoTurnoJsonConverter : JsonConverter<EstadoTurno>
{
    public override EstadoTurno Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var nombre = reader.GetString();
            foreach (var estado in Enum.GetValues<EstadoTurno>())
                if (string.Equals(nombre, estado.ToString(), StringComparison.OrdinalIgnoreCase)) return estado;
        }
        throw new JsonException("Usa Pendiente, EnAtencion, Atendido o Cancelado.");
    }

    public override void Write(Utf8JsonWriter writer, EstadoTurno value, JsonSerializerOptions options) => writer.WriteStringValue(value.ToString());
}
