using System.Xml.Linq;

namespace Tides.Api.Kartverket;

internal static class LocationDataReader
{
    public static IEnumerable<XElement> ReadWaterLevels(XDocument document)
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

        return data.Elements("waterlevel");
    }

    public static string ReadRequiredAttribute(XElement element, string attributeName) =>
        element.Attribute(attributeName)?.Value
            ?? throw new KartverketException($"Missing attribute '{attributeName}' on <{element.Name}>.");
}
