using System.Globalization;
using System.Xml.Linq;

namespace Tides.Api.Kartverket;

public static class StationListParser
{
    public static IReadOnlyList<Station> Parse(XDocument document)
    {
        var error = document.Root?.Element("error")?.Value;
        if (error is not null)
        {
            throw new KartverketException(error);
        }

        var stationInfo = document.Root?.Element("stationinfo");
        if (stationInfo is null)
        {
            throw new KartverketException("Missing required element 'stationinfo'.");
        }

        return stationInfo.Elements("location")
            .Select(location => new Station(
                ReadRequiredAttribute(location, "code"),
                ReadRequiredAttribute(location, "name"),
                double.Parse(ReadRequiredAttribute(location, "latitude"), CultureInfo.InvariantCulture),
                double.Parse(ReadRequiredAttribute(location, "longitude"), CultureInfo.InvariantCulture)
            ))
            .ToList();
    }

    private static string ReadRequiredAttribute(XElement element, string attributeName) =>
        element.Attribute(attributeName)?.Value
            ?? throw new KartverketException($"Missing attribute '{attributeName}' on <{element.Name}>.");
}
