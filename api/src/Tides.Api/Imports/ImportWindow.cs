namespace Tides.Api.Imports;

public static class ImportWindow
{
    // Returns the range every location keeps imported: the previous, current and next UTC month.
    // `To` is the 1st of the month after the next.
    public static (DateTimeOffset From, DateTimeOffset To) Around(DateTimeOffset now)
    {
        var utcNow = now.ToUniversalTime();
        var monthStart = new DateTimeOffset(utcNow.Year, utcNow.Month, 1, 0, 0, 0, TimeSpan.Zero);

        return (monthStart.AddMonths(-1), monthStart.AddMonths(2));
    }
}
