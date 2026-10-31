namespace Tides.Api.Samples;

public sealed record SamplesSnapshot(
    SamplesResponse Response,
    string ETag
);
