using System.Text.Json.Serialization;

namespace EloSolidario.Api.Domain;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Conservacao
{
    AMBIENTE,
    REFRIGERADO,
    CONGELADO
}
