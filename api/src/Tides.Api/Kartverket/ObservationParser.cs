using System.Globalization;
using System.Xml.Linq;

namespace Tides.Api.Kartverket;

public static class ObservationParser
{
    public static IReadOnlyList<Observation> Parse(XDocument document)
    {
        var root = document.Root;
        if (root?.Name == "error")
        {
            throw new KartverketException(root.Value);
        }

        var locationData = root?.Element("locationdata");
        if (locationData is null)
        {
            throw new KartverketException("Missing required element 'locationdata'.");
        }

        if (locationData.Element("nodata") is not null)
        {
            return [];
        }

        var data = locationData.Element("data");
        if (data is null)
        {
            throw new KartverketException("Missing required element 'data'.");
        }

        var observations = data.Elements("waterlevel")
            .Select(waterLevel => new Observation(
                DateTimeOffset.Parse(ReadRequiredAttribute(waterLevel, "time"), CultureInfo.InvariantCulture).ToUniversalTime(),
                double.Parse(ReadRequiredAttribute(waterLevel, "value"), CultureInfo.InvariantCulture)
            ))
            .ToList();

        return observations;
    }

    private static string ReadRequiredAttribute(XElement element, string attributeName) =>
        element.Attribute(attributeName)?.Value
            ?? throw new KartverketException($"Missing attribute '{attributeName}' on <{element.Name}>.");
}
