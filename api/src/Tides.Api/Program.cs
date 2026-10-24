using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Tides.Api.Data;
using Tides.Api.Imports;
using Tides.Api.Kartverket;
using Tides.Api.Locations;
using Tides.Api.Stations;
using Tides.Api.Tides;
using Tides.Api.WaterLevels;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

// Without this, OpenAPI describes every number as number or string.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict);

builder.Services.AddKartverket();
builder.Services.AddHybridCache();
builder.Services.AddDbContext<TidesDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Tides")));
builder.Services.AddScoped<StationService>();
builder.Services.AddScoped<TideService>();
builder.Services.AddScoped<WaterLevelService>();
builder.Services.AddScoped<ImportService>();
builder.Services.AddScoped<LocationService>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddProblemDetails();
builder.Services.AddValidation();
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
var v1 = app.MapGroup("/v1");
v1.MapStationEndpoints();
v1.MapTideEndpoints();
v1.MapWaterLevelEndpoints();
v1.MapLocationEndpoints();

app.Run();
