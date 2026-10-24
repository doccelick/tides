using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tides.Api.Data;
using Tides.Api.Locations;

namespace Tides.Api.IntegrationTests.Locations;

public class LocationEndpointsTests(SqlServerFixture database)
{
    private const string Tides = """
        <tide><locationdata><data>
            <waterlevel value="250.0" time="2026-11-01T00:05:00+00:00" flag="high"/>
        </data></locationdata></tide>
    """;

    private const string Predictions = """
        <tide><locationdata><data>
            <waterlevel value="248.0" time="2026-11-01T00:00:00+00:00" flag="pre"/>
            <waterlevel value="250.0" time="2026-11-01T00:10:00+00:00" flag="pre"/>
            <waterlevel value="249.0" time="2026-11-01T00:20:00+00:00" flag="pre"/>
        </data></locationdata></tide>
    """;

    private const string NoData = """
        <tide>
            <locationdata>
                <nodata info="Sorry. There are no sea level data for N61°54.0' E10°16.8' because N61°54.0' E10°16.8' is too far away from the coast."/>
            </locationdata>
        </tide>
    """;

    [Fact]
    public async Task CreateLocation_ValidRequest_ReturnsCreatedLocation()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var handler = new FakeKartverketHandler(Tides, Predictions);
        await using var factory = new TidesApiFactory(database, handler);
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
        var handler = new FakeKartverketHandler(Tides, Predictions);
        await using var factory = new TidesApiFactory(database, handler);
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
        var handler = new FakeKartverketHandler(Tides, Predictions);
        await using var factory = new TidesApiFactory(database, handler);
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
        var handler = new FakeKartverketHandler(Tides, Predictions);
        await using var factory = new TidesApiFactory(database, handler);
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
        var handler = new FakeKartverketHandler(Tides, Predictions);
        await using var factory = new TidesApiFactory(database, handler);
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
        var handler = new FakeKartverketHandler(Tides, Predictions);
        await using var factory = new TidesApiFactory(database, handler);
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
        var handler = new FakeKartverketHandler(Tides, Predictions);
        await using var factory = new TidesApiFactory(database, handler);
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

    [Fact]
    public async Task CreateLocation_InlandCoordinate_ReturnsUnprocessableEntity()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var handler = new FakeKartverketHandler(Tides, NoData);
        await using var factory = new TidesApiFactory(database, handler);
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/v1/locations",
            new CreateLocationRequest("Lesja", 61.54, 10.168),
            cancellationToken);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<TidesDbContext>();
            var isSaved = await context.Locations.AnyAsync(l => l.Name == "Lesja", cancellationToken);

            Assert.False(isSaved);
        }
    }

    [Fact]
    public async Task GetLocations_AfterCreate_ReturnsCreatedLocation()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var handler = new FakeKartverketHandler(Tides, Predictions);
        await using var factory = new TidesApiFactory(database, handler);
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/v1/locations",
            new CreateLocationRequest("Bodø", 67.29, 14.40),
            cancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var location = await response.Content.ReadFromJsonAsync<LocationResponse>(cancellationToken);
        Assert.NotNull(location);

        var getResponse = await client.GetFromJsonAsync<LocationsResponse>(
            "/v1/locations",
            cancellationToken);

        Assert.NotNull(getResponse);
        var storedLocation = getResponse.Locations.FirstOrDefault(l => l.Id == location.Id);
        Assert.NotNull(storedLocation);
        Assert.Equal(location.Latitude, storedLocation.Latitude);
        Assert.Equal(location.Longitude, storedLocation.Longitude);
    }
}
