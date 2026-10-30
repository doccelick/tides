namespace Tides.Api.Imports;

public static class ImportGaps
{
    // Returns the parts of [from, to) that no import covers, in time order.
    public static IReadOnlyList<(DateTimeOffset From, DateTimeOffset To)> Find(
        DateTimeOffset from,
        DateTimeOffset to,
        IEnumerable<(DateTimeOffset From, DateTimeOffset To)> imports)
    {
        var gaps = new List<(DateTimeOffset From, DateTimeOffset To)>();
        var coveredUntil = from;

        foreach (var import in imports.OrderBy(i => i.From))
        {
            if (import.From >= to)
            {
                break;
            }

            if (import.From > coveredUntil)
            {
                gaps.Add((coveredUntil, import.From));
            }

            if (import.To > coveredUntil)
            {
                coveredUntil = import.To;
            }
        }

        if (coveredUntil < to)
        {
            gaps.Add((coveredUntil, to));
        }

        return gaps;
    }
}
