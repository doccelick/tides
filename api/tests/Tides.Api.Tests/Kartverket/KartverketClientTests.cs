using System.Globalization;
using System.Net;
using Tides.Api.Kartverket;

namespace Tides.Api.Tests.Kartverket;

public class KartverketClientTests
{
    private sealed class StubHandler(HttpStatusCode statusCode, string content) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            return Task.FromResult(new HttpResponseMessage(statusCode) { Content = new StringContent(content) });
        }
    }

    [Fact]
    public async Task GetStationsAsync_ValidResponse_RequestsPermanentStationList()
    {
        var stationListXmlPath = Path.Combine(AppContext.BaseDirectory, "Kartverket", "TestData", "stationlist.xml");
        var cancellationToken = TestContext.Current.CancellationToken;
        var stationListXmlContent = await File.ReadAllTextAsync(stationListXmlPath, cancellationToken);
        var handler = new StubHandler(HttpStatusCode.OK, stationListXmlContent);
        var client = new KartverketClient(new HttpClient(handler) { BaseAddress = new Uri("https://kartverket.test/") });

        var stations = await client.GetStationsAsync(cancellationToken);

        Assert.Equal(3, stations.Count);
        Assert.Equal(new Uri("https://kartverket.test/tideapi.php?tide_request=stationlist&type=perm&lang=en"), handler.Request!.RequestUri);
    }

    [Fact]
    public async Task GetStationsAsync_ServerError_ThrowsHttpRequestException()
    {
        var handler = new StubHandler(HttpStatusCode.InternalServerError, string.Empty);
        var client = new KartverketClient(new HttpClient(handler) { BaseAddress = new Uri("https://kartverket.test/") });

        var cancellationToken = TestContext.Current.CancellationToken;
        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetStationsAsync(cancellationToken));
    }

    [Fact]
    public async Task GetTidesAsync_ValidResponse_RequestsLocationDataInUtc()
    {
        var locationDataXmlPath = Path.Combine(AppContext.BaseDirectory, "Kartverket", "TestData", "locationdata.xml");
        var cancellationToken = TestContext.Current.CancellationToken;
        var locationDataXmlContent = await File.ReadAllTextAsync(locationDataXmlPath, cancellationToken);
        var handler = new StubHandler(HttpStatusCode.OK, locationDataXmlContent);
        var client = new KartverketClient(new HttpClient(handler) { BaseAddress = new Uri("https://kartverket.test/") });

        var from = new DateTimeOffset(2026, 10, 18, 2, 0, 0, TimeSpan.FromHours(2));
        var to = from.AddDays(1);

        var tides = await client.GetTidesAsync(67.29233, 14.39977, from, to, cancellationToken);

        Assert.Equal(4, tides.Count);

        var expectedUri = new Uri(
            "https://kartverket.test/tideapi.php?tide_request=locationdata" +
            "&lat=67.29233&lon=14.39977" +
            "&fromtime=2026-10-18T00:00&totime=2026-10-19T00:00" +
            "&datatype=tab&refcode=msl&lang=en&tzone=0");

        Assert.Equal(expectedUri, handler.Request!.RequestUri);
    }

    [Fact]
    public async Task GetTidesAsync_UnderNorwegianCulture_SendsDecimalPoints()
    {
        var locationDataXmlPath = Path.Combine(AppContext.BaseDirectory, "Kartverket", "TestData", "locationdata.xml");
        var cancellationToken = TestContext.Current.CancellationToken;
        var locationDataXmlContent = await File.ReadAllTextAsync(locationDataXmlPath, cancellationToken);
        var handler = new StubHandler(HttpStatusCode.OK, locationDataXmlContent);
        var client = new KartverketClient(new HttpClient(handler) { BaseAddress = new Uri("https://kartverket.test/") });

        var from = new DateTimeOffset(2026, 10, 18, 2, 0, 0, TimeSpan.FromHours(2));
        var to = from.AddDays(1);

        var originalCulture = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("nb-NO");
            await client.GetTidesAsync(67.29233, 14.39977, from, to, cancellationToken);
            Assert.Contains("&lat=67.29233&lon=14.39977&", handler.Request!.RequestUri!.Query);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public async Task GetTidesAsync_RangeOverThousandDays_ThrowsWithoutRequest()
    {
        var handler = new StubHandler(HttpStatusCode.OK, string.Empty);
        var client = new KartverketClient(new HttpClient(handler) { BaseAddress = new Uri("https://kartverket.test/") });
        var from = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        var cancellationToken = TestContext.Current.CancellationToken;
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => client.GetTidesAsync(67.29233, 14.39977, from, from.AddDays(1001), cancellationToken));

        Assert.Null(handler.Request);
    }
}
