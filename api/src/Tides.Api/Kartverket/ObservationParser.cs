using System.Globalization;
using System.Xml.Linq;

namespace Tides.Api.Kartverket;

public static class ObservationParser
{
    public static IReadOnlyList<Observation> Parse(XDocument document)
    {
        var observations = LocationDataReader.ReadWaterLevels(document)
            .Select(waterLevel => new Observation(
                DateTimeOffset.Parse(LocationDataReader.ReadRequiredAttribute(waterLevel, "time"), CultureInfo.InvariantCulture).ToUniversalTime(),
                double.Parse(LocationDataReader.ReadRequiredAttribute(waterLevel, "value"), CultureInfo.InvariantCulture)
            ))
            .ToList();

        return observations;
    }
}
