using System.Text.Json.Serialization;
using Scalar.AspNetCore;
using Tides.Api.Kartverket;
using Tides.Api.Stations;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

// Without this, OpenAPI describes every number as number or string.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict);

builder.Services.AddKartverket();
builder.Services.AddHybridCache();
builder.Services.AddScoped<StationService>();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<KartverketExceptionHandler>();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy => policy
        .WithOrigins(allowedOrigins)
        .WithMethods("GET")
        .WithHeaders("If-None-Match")
        .WithExposedHeaders("ETag")));

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapHealthChecks("/health");
app.MapGroup("/v1").MapStationEndpoints();

app.Run();
