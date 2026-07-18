using System.Globalization;

namespace ZeitstrahlStudio.Domain;

/// <summary>
/// Erhält die tatsächlich eingegebenen Datumskomponenten, ohne fehlende Werte fachlich zu erfinden.
/// Technische Vergleichswerte werden ausschließlich für die Sortierung gebildet.
/// </summary>
public sealed record EventDate : IComparable<EventDate>
{
    private static readonly CultureInfo GermanCulture = CultureInfo.GetCultureInfo("de-DE");

    private EventDate(
        DatePrecision precision,
        int startYear,
        int? startMonth,
        int? startDay,
        TimeOnly? startTime,
        int? endYear,
        int? endMonth,
        int? endDay)
    {
        Precision = precision;
        StartYear = startYear;
        StartMonth = startMonth;
        StartDay = startDay;
        StartTime = startTime;
        EndYear = endYear;
        EndMonth = endMonth;
        EndDay = endDay;
    }

    /// <summary>Genauigkeit der ursprünglichen Datumsangabe.</summary>
    public DatePrecision Precision { get; }

    /// <summary>Tatsächlich eingegebenes Startjahr.</summary>
    public int StartYear { get; }

    /// <summary>Tatsächlich eingegebener Startmonat oder <see langword="null"/>.</summary>
    public int? StartMonth { get; }

    /// <summary>Tatsächlich eingegebener Starttag oder <see langword="null"/>.</summary>
    public int? StartDay { get; }

    /// <summary>Tatsächlich eingegebene Uhrzeit oder <see langword="null"/>.</summary>
    public TimeOnly? StartTime { get; }

    /// <summary>Endjahr eines Zeitraums oder <see langword="null"/>.</summary>
    public int? EndYear { get; }

    /// <summary>Endmonat eines Zeitraums oder <see langword="null"/>.</summary>
    public int? EndMonth { get; }

    /// <summary>Endtag eines Zeitraums oder <see langword="null"/>.</summary>
    public int? EndDay { get; }

    /// <summary>Technischer Sortierwert; er verändert die sichtbare Genauigkeit nicht.</summary>
    public DateTime SortStart => new(
        StartYear,
        StartMonth ?? 1,
        StartDay ?? 1,
        StartTime?.Hour ?? 0,
        StartTime?.Minute ?? 0,
        StartTime?.Second ?? 0,
        DateTimeKind.Unspecified);

    /// <summary>Erzeugt ein exaktes Datum.</summary>
    public static EventDate Exact(DateOnly date) => new(
        DatePrecision.ExactDate,
        date.Year,
        date.Month,
        date.Day,
        null,
        null,
        null,
        null);

    /// <summary>Erzeugt ein exaktes Datum mit Uhrzeit.</summary>
    public static EventDate ExactWithTime(DateOnly date, TimeOnly time) => new(
        DatePrecision.ExactDateTime,
        date.Year,
        date.Month,
        date.Day,
        time,
        null,
        null,
        null);

    /// <summary>Erzeugt eine Monatsangabe, ohne einen Tag zu ergänzen.</summary>
    public static EventDate MonthAndYear(int year, int month)
    {
        ValidateYear(year, nameof(year));
        if (month is < 1 or > 12)
        {
            throw new DomainValidationException("Der Monat muss zwischen 1 und 12 liegen.", nameof(month));
        }

        return new EventDate(DatePrecision.MonthAndYear, year, month, null, null, null, null, null);
    }

    /// <summary>Erzeugt eine Jahresangabe, ohne Monat oder Tag zu ergänzen.</summary>
    public static EventDate Year(int year)
    {
        ValidateYear(year, nameof(year));
        return new EventDate(DatePrecision.Year, year, null, null, null, null, null, null);
    }

    /// <summary>Erzeugt einen geschlossenen Zeitraum aus zwei exakten Datumswerten.</summary>
    public static EventDate Range(DateOnly start, DateOnly end)
    {
        if (end < start)
        {
            throw new DomainValidationException(
                "Das Enddatum eines Zeitraums darf nicht vor dem Startdatum liegen.",
                nameof(end));
        }

        return new EventDate(
            DatePrecision.DateRange,
            start.Year,
            start.Month,
            start.Day,
            null,
            end.Year,
            end.Month,
            end.Day);
    }

    /// <summary>Formatiert die Datumsangabe mit ihrer tatsächlichen Genauigkeit.</summary>
    public string ToDisplayString(CultureInfo? culture = null)
    {
        culture ??= GermanCulture;

        return Precision switch
        {
            DatePrecision.ExactDate => FormatStartDate(culture),
            DatePrecision.ExactDateTime => $"{FormatStartDate(culture)} {StartTime!.Value:HH\\:mm}",
            DatePrecision.MonthAndYear => new DateTime(StartYear, StartMonth!.Value, 1)
                .ToString("MMMM yyyy", culture),
            DatePrecision.Year => StartYear.ToString(culture),
            DatePrecision.DateRange => $"{FormatStartDate(culture)} – {FormatEndDate(culture)}",
            _ => throw new InvalidOperationException("Die Datumsgenauigkeit wird nicht unterstützt."),
        };
    }

    /// <inheritdoc />
    public int CompareTo(EventDate? other)
    {
        if (other is null)
        {
            return 1;
        }

        var startComparison = SortStart.CompareTo(other.SortStart);
        if (startComparison != 0)
        {
            return startComparison;
        }

        var precisionComparison = GetPrecisionOrder(Precision).CompareTo(GetPrecisionOrder(other.Precision));
        if (precisionComparison != 0)
        {
            return precisionComparison;
        }

        return GetSortEnd().CompareTo(other.GetSortEnd());
    }

    /// <inheritdoc />
    public override string ToString() => ToDisplayString();

    private static void ValidateYear(int year, string parameterName)
    {
        if (year is < 1 or > 9999)
        {
            throw new DomainValidationException("Das Jahr muss zwischen 1 und 9999 liegen.", parameterName);
        }
    }

    private static int GetPrecisionOrder(DatePrecision precision) => precision switch
    {
        DatePrecision.Year => 0,
        DatePrecision.MonthAndYear => 1,
        DatePrecision.ExactDate => 2,
        DatePrecision.ExactDateTime => 3,
        DatePrecision.DateRange => 4,
        _ => throw new InvalidOperationException("Die Datumsgenauigkeit wird nicht unterstützt."),
    };

    private DateTime GetSortEnd() => EndYear is null
        ? SortStart
        : new DateTime(EndYear.Value, EndMonth!.Value, EndDay!.Value);

    private string FormatStartDate(CultureInfo culture) =>
        new DateOnly(StartYear, StartMonth!.Value, StartDay!.Value).ToString("d", culture);

    private string FormatEndDate(CultureInfo culture) =>
        new DateOnly(EndYear!.Value, EndMonth!.Value, EndDay!.Value).ToString("d", culture);
}
