using System.Globalization;
using System.Xml.Linq;

namespace Tides.Api.Kartverket;

public sealed class KartverketClient(HttpClient httpClient)
{
    private static readonly TimeSpan MaxLocationDataRange = TimeSpan.FromDays(1000);

    public async Task<IReadOnlyList<Station>> GetStationsAsync(CancellationToken cancellationToken = default)
    {
        var requestUri = new Uri("tideapi.php?tide_request=stationlist&type=perm&lang=en", UriKind.Relative);
        var document = await GetDocumentAsync(requestUri, cancellationToken);
        return StationListParser.Parse(document);
    }

    public async Task<IReadOnlyList<Tide>> GetTidesAsync(
        double latitude,
        double longitude,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default)
    {
        var document = await GetLocationDataAsync(latitude, longitude, from, to, "datatype=tab", cancellationToken);
        return TideParser.Parse(document);
    }

    public async Task<IReadOnlyList<Observation>> GetObservationsAsync(
        double latitude,
        double longitude,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default)
    {
        var document = await GetLocationDataAsync(latitude, longitude, from, to, "datatype=obs&interval=10", cancellationToken);
        return ObservationParser.Parse(document);
    }

    public async Task<IReadOnlyList<Prediction>> GetPredictionsAsync(
        double latitude,
        double longitude,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default)
    {
        var document = await GetLocationDataAsync(latitude, longitude, from, to, "datatype=pre&interval=10", cancellationToken);
        return PredictionParser.Parse(document);
    }

    private static string FormatAsUtc(DateTimeOffset time) => time.UtcDateTime.ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture);

    private async Task<XDocument> GetDocumentAsync(
        Uri requestUri,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(requestUri, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await XDocument.LoadAsync(stream, LoadOptions.None, cancellationToken);
    }

    private async Task<XDocument> GetLocationDataAsync(
        double latitude,
        double longitude,
        DateTimeOffset from,
        DateTimeOffset to,
        string dataQuery,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(to - from, MaxLocationDataRange);

        var requestUri = new Uri(string.Create(CultureInfo.InvariantCulture,
            $"tideapi.php?tide_request=locationdata&lat={latitude}&lon={longitude}&fromtime={FormatAsUtc(from)}&totime={FormatAsUtc(to)}&{dataQuery}&refcode=cd&lang=en&tzone=0"),
            UriKind.Relative);

        return await GetDocumentAsync(requestUri, cancellationToken);
    }
}
