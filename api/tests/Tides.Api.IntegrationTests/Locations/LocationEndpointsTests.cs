using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tides.Api.Data;
using Tides.Api.Imports;
using Tides.Api.Locations;

namespace Tides.Api.IntegrationTests.Locations;

public class LocationEndpointsTests : IAsyncDisposable
{
    private readonly SqlServerFixture _database;
    private readonly TidesApiFactory _factory;
    private readonly HttpClient _client;

    public LocationEndpointsTests(SqlServerFixture database)
    {
        _database = database;
        _factory = new TidesApiFactory(database, new FakeKartverketHandler(KartverketXml.Tides, KartverketXml.Predictions));
        _client = _factory.CreateSignedInClient("Admin");
    }

    public ValueTask DisposeAsync() => _factory.DisposeAsync();

    [Fact]
    public async Task CreateLocation_ValidRequest_ReturnsCreatedLocation()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        var response = await _client.PostAsJsonAsync(
            "/v1/locations",
            new CreateLocationRequest("Bodø", 67.29, 14.40),
            cancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var location = await response.Content.ReadFromJsonAsync<LocationResponse>(cancellationToken);
        Assert.NotNull(location);
        Assert.Equal("Bodø", location.Name);
    }

    public static TheoryData<object, string> InvalidCreateRequests => new()
    {
        { new { Name = "", Latitude = 67.29, Longitude = 14.40 }, "Name" },
        { new { Name = "   ", Latitude = 67.29, Longitude = 14.40 }, "Name" },
        { new { Name = new string('a', Location.NameMaxLength + 1), Latitude = 67.29, Longitude = 14.40 }, "Name" },
        { new { Name = "Bodø", Longitude = 14.40 }, "Latitude" },
        { new { Name = "Bodø", Latitude = -91.0, Longitude = 14.40 }, "Latitude" },
        { new { Name = "Bodø", Latitude = 91.0, Longitude = 14.40 }, "Latitude" },
        { new { Name = "Bodø", Latitude = 67.29 }, "Longitude" },
        { new { Name = "Bodø", Latitude = 67.29, Longitude = -181.0 }, "Longitude" },
        { new { Name = "Bodø", Latitude = 67.29, Longitude = 181.0 }, "Longitude" },
    };

    [Theory]
    [MemberData(nameof(InvalidCreateRequests))]
    public async Task CreateLocation_InvalidRequest_ReturnsValidationProblem(object request, string field)
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        var response = await _client.PostAsJsonAsync("/v1/locations", request, cancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(cancellationToken);
        Assert.NotNull(problem);
        Assert.Contains(field, problem.Errors.Keys);
    }

    [Fact]
    public async Task CreateLocation_InlandCoordinate_ReturnsUnprocessableEntity()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var inlandFactory = new TidesApiFactory(_database, new FakeKartverketHandler(KartverketXml.Tides, KartverketXml.NoData));
        var inlandClient = inlandFactory.CreateSignedInClient("Admin");

        var response = await inlandClient.PostAsJsonAsync(
            "/v1/locations",
            new CreateLocationRequest("Lesja", 61.54, 10.168),
            cancellationToken);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        await using (var scope = inlandFactory.Services.CreateAsyncScope())
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

        var response = await _client.PostAsJsonAsync(
            "/v1/locations",
            new CreateLocationRequest("Bodø", 67.29, 14.40),
            cancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var location = await response.Content.ReadFromJsonAsync<LocationResponse>(cancellationToken);
        Assert.NotNull(location);

        var getResponse = await _client.GetFromJsonAsync<LocationsResponse>(
            "/v1/locations",
            cancellationToken);

        Assert.NotNull(getResponse);
        var storedLocation = getResponse.Locations.FirstOrDefault(l => l.Id == location.Id);
        Assert.NotNull(storedLocation);
        Assert.Equal(location.Latitude, storedLocation.Latitude);
        Assert.Equal(location.Longitude, storedLocation.Longitude);
    }

    [Fact]
    public async Task DeleteLocation_ExistingLocation_ReturnsNoContent()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        var response = await _client.PostAsJsonAsync(
            "/v1/locations",
            new CreateLocationRequest("Bodø", 67.29, 14.40),
            cancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var location = await response.Content.ReadFromJsonAsync<LocationResponse>(cancellationToken);
        Assert.NotNull(location);

        var deleteResponse = await _client.DeleteAsync(
            $"/v1/locations/{location.Id}",
            cancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<TidesDbContext>();
            var isSaved = await context.Locations.AnyAsync(l => l.Id == location.Id, cancellationToken);

            Assert.False(isSaved);
        }
    }

    [Fact]
    public async Task DeleteLocation_UnknownId_ReturnsNotFound()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        var deleteResponse = await _client.DeleteAsync(
            $"/v1/locations/{int.MaxValue}",
            cancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteLocation_WithImport_DeletesEntries()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        var location = new Location { Name = "Bodø", Latitude = 67.29, Longitude = 14.40 };

        var from = new DateTimeOffset(2026, 11, 01, 00, 00, 0, TimeSpan.Zero);
        var to = from.AddMinutes(20);

        Import import;

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<TidesDbContext>();
            context.Locations.Add(location);
            await context.SaveChangesAsync(cancellationToken);

            var service = scope.ServiceProvider.GetRequiredService<ImportService>();
            import = Assert.Single(await service.ImportAsync(location, from, to, cancellationToken));
        }

        var deleteResponse = await _client.DeleteAsync(
            $"/v1/locations/{location.Id}",
            cancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<TidesDbContext>();
            var hasEntries = await context.TideEntries.AnyAsync(e => e.ImportId == import.Id, cancellationToken);

            Assert.False(hasEntries);
        }
    }
}
