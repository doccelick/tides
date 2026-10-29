using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Caching.Hybrid;
using Tides.Api.Kartverket;
using Tides.Api.Locations;
using Tides.Api.Stations;

namespace Tides.Api.WaterLevels;

public sealed class WaterLevelService(
    HybridCache cache,
    KartverketClient kartverket,
    TimeProvider timeProvider)
{
    private static readonly HybridCacheEntryOptions CacheOptions = new()
    {
        Expiration = TimeSpan.FromMinutes(5),
        LocalCacheExpiration = TimeSpan.FromMinutes(5),
    };

    // Kartverket returns no observations when fromtime is less than about an hour ago.
    private static readonly TimeSpan RequestRange = TimeSpan.FromHours(2);

    private static readonly TimeSpan MaxAge = TimeSpan.FromMinutes(30);

    // Kartverket returns predictions and observations every 10 minutes on the same times.
    private static readonly TimeSpan PredictionInterval = TimeSpan.FromMinutes(10);

    public async ValueTask<WaterLevelSnapshot> GetWaterLevelAsync(StationResponse station, CancellationToken cancellationToken = default)
    {
        return await cache.GetOrCreateAsync($"water-level:{station.Code}", async ct =>
        {
            var observation = await GetRecentObservationAsync(station, ct);

            var response = observation is null
                ? null
                : new WaterLevelResponse(station.Code, "UTC", "cm", "CD", observation.Time, observation.LevelCm);

            return new WaterLevelSnapshot(response, ComputeETag(response));
        }, CacheOptions, cancellationToken: cancellationToken);
    }

    public async ValueTask<EstimatedWaterLevelSnapshot> GetEstimatedWaterLevelAsync(
        Location location,
        NearbyStation nearbyStation,
        CancellationToken cancellationToken = default)
    {
        return await cache.GetOrCreateAsync($"estimated-water-level:{location.Id}", async ct =>
        {
            var response = await EstimateWaterLevelAsync(location, nearbyStation, ct);

            return new EstimatedWaterLevelSnapshot(response, ComputeETag(response));
        }, CacheOptions, cancellationToken: cancellationToken);
    }

    // The weather effect is the station's observed level minus its predicted level. It is added to
    // the location's predicted level at the same time.
    private async Task<EstimatedWaterLevelResponse?> EstimateWaterLevelAsync(
        Location location,
        NearbyStation nearbyStation,
        CancellationToken cancellationToken)
    {
        var station = nearbyStation.Station;
        var observation = await GetRecentObservationAsync(station, cancellationToken);

        if (observation is null)
        {
            return null;
        }

        var stationPrediction = await GetPredictionAsync(station.Latitude, station.Longitude, observation.Time, cancellationToken);
        var locationPrediction = await GetPredictionAsync(location.Latitude, location.Longitude, observation.Time, cancellationToken);

        if (stationPrediction is null || locationPrediction is null)
        {
            return null;
        }

        var weatherEffectCm = observation.LevelCm - stationPrediction.LevelCm;

        return new EstimatedWaterLevelResponse(
            location.Id,
            "UTC",
            "cm",
            "CD",
            observation.Time,
            Math.Round(locationPrediction.LevelCm + weatherEffectCm, 1),
            station.Code,
            station.Name,
            Math.Round(nearbyStation.DistanceKm, 1),
            Math.Round(weatherEffectCm, 1));
    }

    private async Task<Prediction?> GetPredictionAsync(
        double latitude,
        double longitude,
        DateTimeOffset time,
        CancellationToken cancellationToken)
    {
        var predictions = await kartverket.GetPredictionsAsync(latitude, longitude, time, time + PredictionInterval, cancellationToken);

        return predictions.FirstOrDefault(p => p.Time == time);
    }

    private async Task<Observation?> GetRecentObservationAsync(StationResponse station, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var observations = await kartverket.GetObservationsAsync(station.Latitude, station.Longitude, now - RequestRange, now, cancellationToken);
        var newest = observations.LastOrDefault();

        return newest is null || now - newest.Time > MaxAge ? null : newest;
    }

    private static string ComputeETag<T>(T response)
    {
        var hash = SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(response));
        return $"\"{Convert.ToHexString(hash)}\"";
    }
}
