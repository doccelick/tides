using System.Text.Json.Serialization;

namespace Tides.Api.Tides;

[JsonConverter(typeof(JsonStringEnumConverter<TideSource>))]
public enum TideSource
{
    Database,
    Kartverket
}
