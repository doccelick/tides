namespace Tides.Api.Samples;

public sealed record SamplesResponse(
    int LocationId,
    string TimeZone,
    string Unit,
    string Reference,
    int IntervalMinutes,
    IReadOnlyList<SampleResponse> Samples
);
