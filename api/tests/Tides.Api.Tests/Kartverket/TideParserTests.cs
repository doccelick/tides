using System.Globalization;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using Tides.Api.Kartverket;

namespace Tides.Api.Tests.Kartverket;

public class TideParserTests
{
    private static XDocument LoadSampleFile(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Kartverket", "TestData", fileName);
        return XDocument.Load(path);
    }

    [Fact]
    public void Parse_HighLowData_ReturnsTidesInUtc()
    {
        var document = LoadSampleFile("locationdata.xml");

        var tides = TideParser.Parse(document);

        var time = new DateTimeOffset(2026, 10, 18, 9, 45, 0, TimeSpan.Zero);
        Assert.Equal(4, tides.Count);
        Assert.Equal(new Tide(TideType.Low, time, -26.2), tides[1]);
    }

    [Fact]
    public void Parse_NoData_ReturnsEmptyList()
    {
        var document = LoadSampleFile("locationdata-nodata.xml");

        var tides = TideParser.Parse(document);

        Assert.Empty(tides);
    }

    [Fact]
    public void Parse_ErrorResponse_ThrowsKartverketException()
    {
        var document = LoadSampleFile("locationdata-error.xml");

        var exception = Assert.Throws<KartverketException>(() => TideParser.Parse(document));

        Assert.Equal("Position outside area", exception.Message);
    }

    [Fact]
    public void Parse_UnknownFlag_ThrowsKartverketException()
    {
        var document = XDocument.Parse("<tide><locationdata><data><waterlevel value=\"1.0\" time=\"2026-10-18T03:48:00+00:00\" flag=\"mid\"/></data></locationdata></tide>");

        var exception = Assert.Throws<KartverketException>(() => TideParser.Parse(document));

        Assert.Equal("Unknown tide flag 'mid'.", exception.Message);
    }

    [Fact]
    public void Parse_UnderNorwegianCulture_ReadsDecimalPoint()
    {
        var document = LoadSampleFile("locationdata.xml");

        var originalCulture = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("nb-NO");
            var tides = TideParser.Parse(document);
            Assert.Equal(54.1, tides[0].LevelCm);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }
}
