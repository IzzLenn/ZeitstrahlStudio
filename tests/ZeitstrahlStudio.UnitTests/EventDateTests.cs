using System.Globalization;
using ZeitstrahlStudio.Domain;

namespace ZeitstrahlStudio.UnitTests;

public sealed class EventDateTests
{
    [Fact]
    public void Year_PreservesMissingMonthAndDay()
    {
        var value = EventDate.Year(2024);

        Assert.Equal(DatePrecision.Year, value.Precision);
        Assert.Null(value.StartMonth);
        Assert.Null(value.StartDay);
        Assert.Equal("2024", value.ToDisplayString());
    }

    [Fact]
    public void MonthAndYear_DoesNotDisplayInventedDay()
    {
        var value = EventDate.MonthAndYear(2024, 5);

        Assert.Equal(DatePrecision.MonthAndYear, value.Precision);
        Assert.Null(value.StartDay);
        Assert.Equal("Mai 2024", value.ToDisplayString(CultureInfo.GetCultureInfo("de-DE")));
    }

    [Fact]
    public void ExactWithTime_PreservesTimeAndFormatsIt()
    {
        var value = EventDate.ExactWithTime(new DateOnly(2024, 5, 3), new TimeOnly(9, 7));

        Assert.Equal(new TimeOnly(9, 7), value.StartTime);
        Assert.Equal("03.05.2024 09:07", value.ToDisplayString());
    }

    [Fact]
    public void Range_RejectsEndBeforeStart()
    {
        var error = Assert.Throws<DomainValidationException>(() =>
            EventDate.Range(new DateOnly(2024, 5, 2), new DateOnly(2024, 5, 1)));

        Assert.Contains("Enddatum", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CompareTo_SortsPartialDatesByTechnicalStartWithoutChangingPrecision()
    {
        var year = EventDate.Year(2024);
        var month = EventDate.MonthAndYear(2024, 5);
        var day = EventDate.Exact(new DateOnly(2024, 5, 3));

        var sorted = new[] { day, month, year }.Order().ToArray();

        Assert.Equal(new[] { year, month, day }, sorted);
        Assert.Null(year.StartMonth);
    }
}
