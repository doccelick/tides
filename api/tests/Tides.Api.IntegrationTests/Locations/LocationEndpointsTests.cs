using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Tides.Api.Locations;

namespace Tides.Api.IntegrationTests.Locations;

public class LocationEndpointsTests(SqlServerFixture database)
{
    [Fact]
    public async Task CreateLocation_ValidRequest_ReturnsCreatedLocation()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var factory = new TidesApiFactory(database);
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/v1/locations",
            new CreateLocationRequest("Bodø", 67.29, 14.40),
            cancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var location = await response.Content.ReadFromJsonAsync<LocationResponse>(cancellationToken);
        Assert.NotNull(location);
        Assert.Equal("Bodø", location.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateLocation_MissingName_ReturnsValidationProblem(string name)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var factory = new TidesApiFactory(database);
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/v1/locations",
            new CreateLocationRequest(name, 67.29, 14.40),
            cancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(cancellationToken);
        Assert.NotNull(problem);
        Assert.Contains("Name", problem.Errors.Keys);
    }

    [Fact]
    public async Task CreateLocation_NameTooLong_ReturnsValidationProblem()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var factory = new TidesApiFactory(database);
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/v1/locations",
            new CreateLocationRequest(new string('a', Location.NameMaxLength + 1), 67.29, 14.40),
            cancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(cancellationToken);
        Assert.NotNull(problem);
        Assert.Contains("Name", problem.Errors.Keys);
    }

    [Fact]
    public async Task CreateLocation_MissingLatitude_ReturnsValidationProblem()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var factory = new TidesApiFactory(database);
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/v1/locations",
            new { Name = "Bodø", Longitude = 14.40 },
            cancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(cancellationToken);
        Assert.NotNull(problem);
        Assert.Contains("Latitude", problem.Errors.Keys);
    }

    [Fact]
    public async Task CreateLocation_MissingLongitude_ReturnsValidationProblem()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var factory = new TidesApiFactory(database);
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/v1/locations",
            new { Name = "Bodø", Latitude = 67.29 },
            cancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(cancellationToken);
        Assert.NotNull(problem);
        Assert.Contains("Longitude", problem.Errors.Keys);
    }

    [Theory]
    [InlineData(-91)]
    [InlineData(91)]
    public async Task CreateLocation_LatitudeOutsideRange_ReturnsValidationProblem(double latitude)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var factory = new TidesApiFactory(database);
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/v1/locations",
            new CreateLocationRequest("Bodø", latitude, 10.00),
            cancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(cancellationToken);
        Assert.NotNull(problem);
        Assert.Contains("Latitude", problem.Errors.Keys);
    }

    [Theory]
    [InlineData(-181)]
    [InlineData(181)]
    public async Task CreateLocation_LongitudeOutsideRange_ReturnsValidationProblem(double longitude)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var factory = new TidesApiFactory(database);
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/v1/locations",
            new CreateLocationRequest("Bodø", 0, longitude),
            cancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(cancellationToken);
        Assert.NotNull(problem);
        Assert.Contains("Longitude", problem.Errors.Keys);
    }
}
