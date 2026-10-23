using System.Net;

namespace Tides.Api.IntegrationTests.Imports;

public sealed class FakeKartverketHandler(string tides, string predictions) : HttpMessageHandler
{
    public int RequestCount { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        RequestCount++;
        var content = request.RequestUri!.Query.Contains("datatype=pre") ? predictions : tides;
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(content) });
    }
}
