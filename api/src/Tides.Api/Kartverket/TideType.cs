using System.Text.Json.Serialization;

namespace Tides.Api.Kartverket;

[JsonConverter(typeof(JsonStringEnumConverter<TideType>))]
public enum TideType
{
    High,
    Low
}
