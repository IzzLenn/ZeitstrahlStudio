namespace ZeitstrahlStudio.Domain;

/// <summary>Persistierbare Darstellungs- und Sicherungseinstellungen eines Projekts.</summary>
public sealed record ProjectSettings
{
    public TimelineOrientation PreferredOrientation { get; init; } = TimelineOrientation.Horizontal;
    public ApplicationTheme Theme { get; init; } = ApplicationTheme.FollowWindows;
    public string DefaultEventColorHex { get; init; } = "#3B82F6";
    public double TimelineCardFontSize { get; init; } = 14;
    public double TimelineAxisFontSize { get; init; } = 12;
    public double ExportFontSize { get; init; } = 10;
    public bool CompressLargeGaps { get; init; } = true;
    public int AutoSaveIntervalSeconds { get; init; } = 60;
    public int CurrentDayBackupCount { get; init; } = 6;
    public int DailyBackupCount { get; init; } = 7;
    public int WeeklyBackupCount { get; init; } = 8;

    /// <summary>Prüft alle Einstellungen vor der Speicherung.</summary>
    public void Validate()
    {
        ValidateColor(DefaultEventColorHex);
        ValidateFontSize(TimelineCardFontSize, nameof(TimelineCardFontSize));
        ValidateFontSize(TimelineAxisFontSize, nameof(TimelineAxisFontSize));
        ValidateFontSize(ExportFontSize, nameof(ExportFontSize));

        if (AutoSaveIntervalSeconds is < 15 or > 3600)
        {
            throw new DomainValidationException(
                "Das automatische Speicherintervall muss zwischen 15 und 3600 Sekunden liegen.",
                nameof(AutoSaveIntervalSeconds));
        }

        if (CurrentDayBackupCount is < 1 or > 48 || DailyBackupCount is < 1 or > 90 || WeeklyBackupCount is < 1 or > 104)
        {
            throw new DomainValidationException("Die Aufbewahrungswerte für Sicherungen sind ungültig.");
        }
    }

    private static void ValidateColor(string value)
    {
        if (value.Length != 7 || value[0] != '#' || !value.AsSpan(1).ToArray().All(Uri.IsHexDigit))
        {
            throw new DomainValidationException("Die Standardfarbe muss als #RRGGBB angegeben werden.");
        }
    }

    private static void ValidateFontSize(double value, string parameterName)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value is < 8 or > 48)
        {
            throw new DomainValidationException("Die Schriftgröße muss zwischen 8 und 48 Punkt liegen.", parameterName);
        }
    }
}
