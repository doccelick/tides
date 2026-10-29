using Scalar.AspNetCore;
using Tides.Api.Imports;
using Tides.Api.Locations;
using Tides.Api.Stations;
using Tides.Api.Tides;
using Tides.Api.WaterLevels;

namespace Tides.Api.Startup;

public static class RequestPipeline
{
    // The exception handler comes first so it also handles exceptions thrown by later middleware.
    public static WebApplication UseApiMiddleware(this WebApplication app)
    {
        app.UseExceptionHandler();
        app.UseStatusCodePages();
        app.UseCors();

        return app;
    }

    public static WebApplication MapApiEndpoints(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapScalarApiReference();
        }

        app.MapHealthChecks("/health");

        var v1 = app.MapGroup("/v1");
        v1.MapStationEndpoints();
        v1.MapTideEndpoints();
        v1.MapWaterLevelEndpoints();
        v1.MapLocationEndpoints();
        v1.MapImportEndpoints();

        return app;
    }
}
