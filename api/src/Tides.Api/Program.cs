using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;
using Tides.Api.Kartverket;
using Tides.Api.Stations;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

// Without this, OpenAPI describes every number as number or string.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict);

builder.Services.AddOptions<KartverketOptions>()
    .BindConfiguration(KartverketOptions.SectionName)
    .Validate(
        options => options.BaseAddress is { IsAbsoluteUri: true, Scheme: "http" or "https" },
        "Kartverket:BaseAddress must be an absolute http or https address.")
    .ValidateOnStart();

builder.Services.AddHttpClient<KartverketClient>((services, client) =>
    client.BaseAddress = services.GetRequiredService<IOptions<KartverketOptions>>().Value.BaseAddress);

builder.Services.AddHybridCache();
builder.Services.AddScoped<StationService>();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<KartverketExceptionHandler>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapHealthChecks("/health");
app.MapGroup("/v1").MapStationEndpoints();

app.Run();
