namespace ZeitstrahlStudio.Domain;

/// <summary>Sortiert Ereignisse chronologisch und berücksichtigt manuelle Reihenfolgen nur bei gleichem Datum.</summary>
public sealed class TimelineEventComparer : IComparer<TimelineEvent>
{
    public static TimelineEventComparer Instance { get; } = new();

    private TimelineEventComparer()
    {
    }

    public int Compare(TimelineEvent? x, TimelineEvent? y)
    {
        if (ReferenceEquals(x, y))
        {
            return 0;
        }

        if (x is null)
        {
            return -1;
        }

        if (y is null)
        {
            return 1;
        }

        var dateComparison = x.Date.CompareTo(y.Date);
        if (dateComparison != 0)
        {
            return dateComparison;
        }

        var manualComparison = (x.ManualSortPosition ?? decimal.MaxValue)
            .CompareTo(y.ManualSortPosition ?? decimal.MaxValue);
        if (manualComparison != 0)
        {
            return manualComparison;
        }

        var createdComparison = x.CreatedAtUtc.CompareTo(y.CreatedAtUtc);
        return createdComparison != 0 ? createdComparison : x.Id.CompareTo(y.Id);
    }
}
