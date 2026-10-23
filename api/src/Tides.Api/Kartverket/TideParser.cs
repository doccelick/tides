using System.Globalization;
using System.Xml.Linq;

namespace Tides.Api.Kartverket;

public static class TideParser
{
    public static IReadOnlyList<Tide> Parse(XDocument document)
    {
        var tides = LocationDataReader.ReadWaterLevels(document)
            .Select(waterLevel => new Tide(
                ParseType(LocationDataReader.ReadRequiredAttribute(waterLevel, "flag")),
                DateTimeOffset.Parse(LocationDataReader.ReadRequiredAttribute(waterLevel, "time"), CultureInfo.InvariantCulture).ToUniversalTime(),
                double.Parse(LocationDataReader.ReadRequiredAttribute(waterLevel, "value"), CultureInfo.InvariantCulture)
            ))
            .ToList();

        return tides;
    }

    private static TideType ParseType(string flag) => flag switch
    {
        "high" => TideType.High,
        "low" => TideType.Low,
        _ => throw new KartverketException($"Unknown tide flag '{flag}'."),
    };
}
