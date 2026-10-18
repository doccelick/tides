namespace Tides.Api.Tides;

public sealed record TidesSnapshot(
    TidesResponse Response,
    string ETag
);
