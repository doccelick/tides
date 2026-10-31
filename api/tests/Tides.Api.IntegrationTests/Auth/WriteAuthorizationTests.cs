using System.Net;

namespace Tides.Api.IntegrationTests.Auth;

public class WriteAuthorizationTests : IAsyncDisposable
{
    private readonly TidesApiFactory _factory;

    public WriteAuthorizationTests(SqlServerFixture database)
    {
        _factory = new TidesApiFactory(database);
    }

    public ValueTask DisposeAsync() => _factory.DisposeAsync();

    public static TheoryData<string, string> WriteEndpoints => new()
    {
        { "POST", "/v1/locations" },
        { "DELETE", "/v1/locations/1" },
        { "POST", "/v1/locations/1/imports" },
        { "DELETE", "/v1/imports/1" },
    };

    [Theory]
    [MemberData(nameof(WriteEndpoints))]
    public async Task Write_WithoutToken_ReturnsUnauthorizedProblem(string method, string path)
    {
        var client = _factory.CreateClient();

        var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), path), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Theory]
    [MemberData(nameof(WriteEndpoints))]
    public async Task Write_WithoutAdminRole_ReturnsForbiddenProblem(string method, string path)
    {
        var client = _factory.CreateSignedInClient();

        var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), path), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }
}
