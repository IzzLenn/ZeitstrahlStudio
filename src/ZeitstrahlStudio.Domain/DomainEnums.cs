namespace ZeitstrahlStudio.Domain;

/// <summary>Genauigkeit einer Datumsangabe.</summary>
public enum DatePrecision
{
    ExactDate,
    ExactDateTime,
    MonthAndYear,
    Year,
    DateRange,
}

/// <summary>Fachliche Priorität eines Ereignisses.</summary>
public enum EventPriority
{
    Low,
    Normal,
    High,
    Critical,
}

/// <summary>Bearbeitungszustand eines Ereignisses.</summary>
public enum EventStatus
{
    Active,
    Completed,
    Archived,
}

/// <summary>Bearbeitungszustand einer Frist.</summary>
public enum DeadlineStatus
{
    Open,
    Completed,
    Cancelled,
}

/// <summary>Visuelle Dringlichkeit einer Frist.</summary>
public enum DeadlineUrgency
{
    None,
    Open,
    Upcoming,
    Overdue,
    Completed,
}

/// <summary>Bevorzugte Ausrichtung eines Zeitstrahls.</summary>
public enum TimelineOrientation
{
    Horizontal,
    Vertical,
}

/// <summary>Farbschema der Anwendung.</summary>
public enum ApplicationTheme
{
    Light,
    Dark,
    FollowWindows,
}

/// <summary>Zustand eines importierten Dokuments.</summary>
public enum AttachmentState
{
    Imported,
    Processing,
    Ready,
    Warning,
    Failed,
}

/// <summary>Herkunft eines extrahierten Texts.</summary>
public enum TextExtractionMethod
{
    None,
    EmbeddedText,
    Ocr,
    OfficeDocument,
    EmbeddedTextAndOcr,
}
