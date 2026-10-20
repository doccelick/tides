using System.Xml.Linq;
using Tides.Api.Kartverket;

namespace Tides.Api.Tests.Kartverket;

public class ObservationParserTests
{
    private static XDocument LoadSampleFile(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Kartverket", "TestData", fileName);
        return XDocument.Load(path);
    }

    [Fact]
    public void Parse_ObservationData_ReturnsObservationsInUtc()
    {
        var document = LoadSampleFile("observations.xml");

        var observations = ObservationParser.Parse(document);

        var time = new DateTimeOffset(2026, 10, 20, 20, 30, 0, TimeSpan.Zero);
        Assert.Equal(12, observations.Count);
        Assert.Equal(new Observation(time, 204.0), observations[^1]);
    }

    [Fact]
    public void Parse_NoData_ReturnsEmptyList()
    {
        var document = LoadSampleFile("locationdata-nodata.xml");

        var observations = ObservationParser.Parse(document);

        Assert.Empty(observations);
    }

    [Fact]
    public void Parse_ErrorResponse_ThrowsKartverketException()
    {
        var document = LoadSampleFile("locationdata-error.xml");

        var exception = Assert.Throws<KartverketException>(() => ObservationParser.Parse(document));

        Assert.Equal("Position outside area", exception.Message);
    }
}
