namespace Tides.Api.Stations;

public sealed record StationsSnapshot(
    StationsResponse Response,
    string ETag
);
