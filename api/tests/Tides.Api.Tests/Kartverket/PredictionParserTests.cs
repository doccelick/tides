using System.Xml.Linq;
using Tides.Api.Kartverket;

namespace Tides.Api.Tests.Kartverket;

public class PredictionParserTests
{
    private static XDocument LoadSampleFile(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Kartverket", "TestData", fileName);
        return XDocument.Load(path);
    }

    [Fact]
    public void Parse_PredictionData_ReturnsPredictionsInUtc()
    {
        var document = LoadSampleFile("predictions.xml");

        var predictions = PredictionParser.Parse(document);

        Assert.Equal(7, predictions.Count);
        var predictionTime1 = new DateTimeOffset(2026, 10, 23, 00, 00, 0, TimeSpan.Zero);
        var predictionTime2 = new DateTimeOffset(2026, 10, 23, 00, 10, 0, TimeSpan.Zero);
        var predictionTime3 = new DateTimeOffset(2026, 10, 23, 00, 20, 0, TimeSpan.Zero);
        Assert.Equal(new Prediction(predictionTime1, 176.4), predictions[0]);
        Assert.Equal(new Prediction(predictionTime2, 169.4), predictions[1]);
        Assert.Equal(new Prediction(predictionTime3, 162.5), predictions[2]);
    }
}
