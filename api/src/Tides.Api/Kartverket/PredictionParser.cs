using System.Globalization;
using System.Xml.Linq;

namespace Tides.Api.Kartverket;

public static class PredictionParser
{
    public static IReadOnlyList<Prediction> Parse(XDocument document)
    {
        var predictions = LocationDataReader.ReadWaterLevels(document)
            .Select(waterLevel => new Prediction(
                DateTimeOffset.Parse(LocationDataReader.ReadRequiredAttribute(waterLevel, "time"), CultureInfo.InvariantCulture).ToUniversalTime(),
                double.Parse(LocationDataReader.ReadRequiredAttribute(waterLevel, "value"), CultureInfo.InvariantCulture)
            ))
            .ToList();

        return predictions;
    }
}
