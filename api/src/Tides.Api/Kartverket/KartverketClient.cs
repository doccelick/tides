using System.Globalization;
using System.Xml.Linq;

namespace Tides.Api.Kartverket;

public sealed class KartverketClient(HttpClient httpClient)
{
    private static readonly TimeSpan MaxTideRange = TimeSpan.FromDays(1000);

    public async Task<IReadOnlyList<Station>> GetStationsAsync(CancellationToken cancellationToken = default)
    {
        var requestUri = new Uri("tideapi.php?tide_request=stationlist&type=perm&lang=en", UriKind.Relative);

        using var response = await httpClient.GetAsync(requestUri, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

        var document = await XDocument.LoadAsync(stream, LoadOptions.None, cancellationToken);
        return StationListParser.Parse(document);
    }


    public async Task<IReadOnlyList<Tide>> GetTidesAsync(
        double latitude,
        double longitude,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(to - from, MaxTideRange);

        var requestUri = new Uri(string.Create(CultureInfo.InvariantCulture,
            $"tideapi.php?tide_request=locationdata&lat={latitude}&lon={longitude}&fromtime={FormatAsUtc(from)}&totime={FormatAsUtc(to)}&datatype=tab&refcode=cd&lang=en&tzone=0"),
            UriKind.Relative);

        using var response = await httpClient.GetAsync(requestUri, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

        var document = await XDocument.LoadAsync(stream, LoadOptions.None, cancellationToken);
        return TideParser.Parse(document);
    }

    public async Task<IReadOnlyList<Observation>> GetObservationsAsync(
        double latitude,
        double longitude,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default)
    {
        var requestUri = new Uri(string.Create(CultureInfo.InvariantCulture,
            $"tideapi.php?tide_request=locationdata&lat={latitude}&lon={longitude}&fromtime={FormatAsUtc(from)}&totime={FormatAsUtc(to)}&datatype=obs&interval=10&refcode=cd&lang=en&tzone=0"),
            UriKind.Relative);

        using var response = await httpClient.GetAsync(requestUri, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

        var document = await XDocument.LoadAsync(stream, LoadOptions.None, cancellationToken);
        return ObservationParser.Parse(document);
    }

    private static string FormatAsUtc(DateTimeOffset time) => time.UtcDateTime.ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture);
}
