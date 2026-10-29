using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Tides.Api.Data;
using Tides.Api.Imports;
using Tides.Api.Kartverket;
using Tides.Api.Locations;
using Tides.Api.Stations;
using Tides.Api.Tides;
using Tides.Api.WaterLevels;

namespace Tides.Api.Startup;

public static class ServiceRegistration
{
    public static IServiceCollection AddApiInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOpenApi();
        services.AddHealthChecks();

        // Without this, OpenAPI describes every number as number or string.
        services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict);

        services.AddProblemDetails();
        services.AddValidation();
        services.AddExceptionHandler<KartverketExceptionHandler>();

        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        services.AddCors(options =>
            options.AddDefaultPolicy(policy => policy
                .WithOrigins(allowedOrigins)
                .WithMethods("GET")
                .WithHeaders("If-None-Match")
                .WithExposedHeaders("ETag")));

        return services;
    }

    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<TidesDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("Tides")));

        return services;
    }

    public static IServiceCollection AddTideServices(this IServiceCollection services)
    {
        services.AddKartverket();
        services.AddHybridCache();
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<StationService>();
        services.AddScoped<TideService>();
        services.AddScoped<WaterLevelService>();
        services.AddScoped<ImportService>();
        services.AddScoped<LocationService>();

        return services;
    }
}
