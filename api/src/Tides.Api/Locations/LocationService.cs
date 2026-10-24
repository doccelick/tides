using Tides.Api.Kartverket;

namespace Tides.Api.Locations;

public sealed class LocationService(KartverketClient kartverket, TimeProvider timeProvider)
{
    public async Task<bool> IsNearCoastAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken = default)
    {
        var from = timeProvider.GetUtcNow();
        var to = from.AddHours(1);

        var predictions = await kartverket.GetPredictionsAsync(
            latitude,
            longitude,
            from,
            to,
            cancellationToken);

        return predictions.Count > 0;
    }
}
