using Microsoft.Extensions.Options;
using Scalar.AspNetCore;
using Tides.Api.Kartverket;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

builder.Services.AddOptions<KartverketOptions>()
    .BindConfiguration(KartverketOptions.SectionName)
    .Validate(options => options.BaseAddress is not null, "Kartverket:BaseAddress is required.")
    .ValidateOnStart();

builder.Services.AddHttpClient<KartverketClient>((services, client) =>
    client.BaseAddress = services.GetRequiredService<IOptions<KartverketOptions>>().Value.BaseAddress);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapHealthChecks("/health");

app.Run();
