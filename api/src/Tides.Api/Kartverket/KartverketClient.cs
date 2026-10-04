using System.Xml.Linq;

namespace Tides.Api.Kartverket;

public sealed class KartverketClient(HttpClient httpClient)
{
    public async Task<IReadOnlyList<Station>> GetStationsAsync(CancellationToken cancellationToken = default)
    {
        var requestUri = new Uri("tideapi.php?tide_request=stationlist&type=perm&lang=en", UriKind.Relative);

        using var response = await httpClient.GetAsync(requestUri, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

        var document = await XDocument.LoadAsync(stream, LoadOptions.None, cancellationToken);
        return StationListParser.Parse(document);
    }
}
