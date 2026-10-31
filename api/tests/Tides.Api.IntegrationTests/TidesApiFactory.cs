using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tides.Api.Data;
using Tides.Api.Kartverket;
using Tides.Api.Locations;

namespace Tides.Api.IntegrationTests;

public sealed class TidesApiFactory(
    SqlServerFixture database,
    HttpMessageHandler? kartverketHandler = null,
    TimeProvider? timeProvider = null) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Tides", database.ConnectionString);

        builder.ConfigureTestServices(services =>
        {
            if (kartverketHandler is not null)
            {
                services.AddHttpClient<KartverketClient>().ConfigurePrimaryHttpMessageHandler(() => kartverketHandler);
            }

            if (timeProvider is not null)
            {
                services.AddSingleton(timeProvider);
            }

            // Tokens are signed by TestTokens in place of Entra, so the API
            // must not download Entra's metadata and signing keys.
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.ConfigurationManager = null;
                options.TokenValidationParameters.IssuerValidator = null;
                options.TokenValidationParameters.ValidIssuer = TestTokens.Issuer;
                options.TokenValidationParameters.IssuerSigningKey = TestTokens.SigningKey;
            });
        });
    }

    public HttpClient CreateSignedInClient(params string[] roles)
    {
        var audience = Services.GetRequiredService<IConfiguration>()["AzureAd:ClientId"]!;

        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestTokens.Create(audience, roles));

        return client;
    }

    public async Task<Location> AddLocationAsync(CancellationToken cancellationToken)
    {
        var location = new Location { Name = "Bodø", Latitude = 67.29, Longitude = 14.40 };

        await using var scope = Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<TidesDbContext>();
        context.Locations.Add(location);
        await context.SaveChangesAsync(cancellationToken);

        return location;
    }
}
