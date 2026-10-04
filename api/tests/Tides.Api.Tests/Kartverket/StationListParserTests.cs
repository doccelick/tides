using System.Globalization;
using System.Xml.Linq;
using Tides.Api.Kartverket;

namespace Tides.Api.Tests.Kartverket;

public class StationListParserTests
{
    private static XDocument LoadSampleFile(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Kartverket", "TestData", fileName);
        return XDocument.Load(path);
    }

    [Fact]
    public void Parse_StationList_ReturnsAllStations()
    {
        var document = LoadSampleFile("stationlist.xml");

        var stations = StationListParser.Parse(document);

        Assert.Equal(3, stations.Count);
        Assert.Equal(new Station("BOO", "Bodø", 67.29233, 14.39977), stations[1]);
    }

    [Fact]
    public void Parse_ErrorResponse_ThrowsKartverketException()
    {
        var document = LoadSampleFile("stationlist-error.xml");

        var exception = Assert.Throws<KartverketException>(() => StationListParser.Parse(document));

        Assert.Equal("Invalid station type", exception.Message);
    }

    [Fact]
    public void Parse_LocationWithoutCode_ThrowsKartverketException()
    {
        var document = XDocument.Parse("<tide><stationinfo><location name=\"X\" latitude=\"1\" longitude=\"2\"/></stationinfo></tide>");

        var exception = Assert.Throws<KartverketException>(() => StationListParser.Parse(document));

        Assert.Equal("Missing attribute 'code' on <location>.", exception.Message);
    }

    [Fact]
    public void Parse_NoLocations_ThrowsKartverketException()
    {
        var document = XDocument.Parse("<tide><stationinfo/></tide>");

        var exception = Assert.Throws<KartverketException>(() => StationListParser.Parse(document));

        Assert.Equal("No 'location' elements in 'stationinfo'.", exception.Message);
    }

    [Fact]
    public void Parse_UnderNorwegianCulture_ReadsDecimalPoint()
    {
        var document = LoadSampleFile("stationlist.xml");

        var originalCulture = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("nb-NO");
            var stations = StationListParser.Parse(document);
            Assert.Equal(60.398046, stations[0].Latitude);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }
}
