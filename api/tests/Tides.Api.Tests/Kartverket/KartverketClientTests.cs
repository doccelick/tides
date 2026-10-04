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
}
