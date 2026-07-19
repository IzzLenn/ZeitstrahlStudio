using ZeitstrahlStudio.Domain;

namespace ZeitstrahlStudio.SampleGenerator;

internal static class SampleProjectDefinition
{
    public static readonly Guid ProjectId = Guid.Parse("a1000000-0000-4000-8000-000000000001");
    public static readonly DateTimeOffset CreatedAtUtc =
        new(2024, 1, 2, 10, 0, 0, TimeSpan.Zero);
    public static readonly DateTimeOffset GeneratedAtUtc =
        new(2026, 7, 19, 10, 30, 0, TimeSpan.Zero);

    public static readonly Guid PlanningNorthId =
        Guid.Parse("a1000000-0000-4000-8000-000000000105");
    public static readonly Guid PlanningSouthId =
        Guid.Parse("a1000000-0000-4000-8000-000000000106");
    public static readonly Guid WorkshopNoteId =
        Guid.Parse("a1000000-0000-4000-8000-000000000108");
    public static readonly Guid MilestonePlanId =
        Guid.Parse("a1000000-0000-4000-8000-000000000109");

    public static TimelineProject CreateProject()
    {
        var project = TimelineProject.Create(
            ProjectId,
            "Beispielchronik Bürgerlabor Sonnenwinkel",
            CreatedAtUtc);
        project.UpdateInformation(
            project.Name,
            "Frei erfundene Demonstrationsdaten",
            "Zehn Ereignisse zeigen Datumsarten, Fristen, Dokumente und große Zeitlücken.",
            "Das Bürgerlabor Sonnenwinkel, seine Werkstatt und alle Personen, Termine und " +
            "Dokumente in diesem Projekt sind frei erfunden. Die Chronik dient ausschließlich " +
            "der lokalen Demonstration und den automatisierten Abnahmetests von Zeitstrahl Studio.",
            new DateOnly(1954, 1, 1),
            new DateOnly(2038, 12, 31),
            CreatedAtUtc.AddMinutes(1));
        project.ChangeSettings(
            new ProjectSettings
            {
                PreferredOrientation = TimelineOrientation.Horizontal,
                Theme = ApplicationTheme.Light,
                DefaultEventColorHex = "#2563EB",
                TimelineCardFontSize = 14,
                TimelineAxisFontSize = 12,
                ExportFontSize = 10,
                CompressLargeGaps = true,
                AutoSaveIntervalSeconds = 60,
                CurrentDayBackupCount = 6,
                DailyBackupCount = 7,
                WeeklyBackupCount = 8,
            },
            CreatedAtUtc.AddMinutes(2));

        var events = new[]
        {
            CreateEvent(
                101,
                EventDate.Year(1954),
                "Gründung der Werkstatt Sonnenwinkel",
                "Ein leer stehender Geräteschuppen wird zur offenen Nachbarschaftswerkstatt.",
                "Die fiktive Initiative beginnt mit einer Werkbank, Handwerkzeug und einem " +
                "handschriftlichen Ausleihbuch. Dieses frühe Ereignis erzeugt die deutlich " +
                "sichtbare große Zeitlücke der Beispielchronik.",
                EventPriority.Normal,
                EventStatus.Completed,
                "#7C3AED",
                ["Ursprung", "Werkstatt"]),
            CreateEvent(
                102,
                EventDate.MonthAndYear(1987, 4),
                "Erstes Archivregal",
                "Im April 1987 entsteht ein geordnetes Regal für Pläne und Werkstattbücher.",
                "Alle genannten Bestände sind frei erfunden. Die Monatsangabe bleibt ohne " +
                "künstlich ergänzten Tag erhalten.",
                EventPriority.Low,
                EventStatus.Completed,
                "#0F766E",
                ["Archiv", "Bestand"]),
            CreateEvent(
                103,
                EventDate.Range(new DateOnly(2001, 3, 1), new DateOnly(2003, 10, 31)),
                "Umbau zum Bürgerlabor",
                "Die Werkstatt wird über einen mehrjährigen Zeitraum barrierearm erweitert.",
                "Der Zeitraum umfasst Planung, Umbau und die fiktive Wiedereröffnung. " +
                "Eine abgeschlossene Frist demonstriert die unabhängige Fristdarstellung.",
                EventPriority.High,
                EventStatus.Completed,
                "#B45309",
                ["Umbau", "Barrierearm"],
                new Deadline(
                    Guid.Parse("a1000000-0000-4000-8000-000000000203"),
                    new DateOnly(2003, 11, 15),
                    label: "Abschlussdokumentation",
                    status: DeadlineStatus.Completed,
                    reminderNote: "Fiktive Unterlagen in das lokale Archiv übernehmen.")),
            CreateEvent(
                104,
                EventDate.Exact(new DateOnly(2019, 9, 14)),
                "Tag der offenen Werkstatt",
                "Ein vollständig lokaler Demonstrationstag mit Modellbau und Reparaturtisch.",
                "Die Beschreibung enthält bewusst längeren Fließtext, damit Karten, Suche und " +
                "Export eine realistische, aber urheberrechtlich unproblematische Textmenge erhalten.",
                EventPriority.Normal,
                EventStatus.Completed,
                "#2563EB",
                ["Öffentlichkeit", "Modellbau"]),
            CreateEvent(
                105,
                EventDate.Exact(new DateOnly(2024, 4, 12)),
                "Planungsrunde Nordflügel",
                "Erste von zwei Sitzungen am identischen Datum; manuell an Position eins.",
                "Die Runde beschließt den frei erfundenen Prüfbegriff Kupferstern. " +
                "Zwei lokale PDF-Dokumente sind diesem Ereignis zugeordnet.",
                EventPriority.High,
                EventStatus.Active,
                "#DC2626",
                ["Planung", "Nordflügel", "Kupferstern"],
                new Deadline(
                    Guid.Parse("a1000000-0000-4000-8000-000000000205"),
                    new DateOnly(2024, 4, 30),
                    new TimeOnly(16, 0),
                    "Planfreigabe",
                    DeadlineStatus.Completed),
                manualSortPosition: 10m),
            CreateEvent(
                106,
                EventDate.Exact(new DateOnly(2024, 4, 12)),
                "Planungsrunde Südflügel",
                "Zweite Sitzung am identischen Datum; manuell an Position zwei.",
                "Die programmatisch erzeugte Planungstafel ist als Bild angehängt. " +
                "Ein reservierter example.com-Link demonstriert externe Webseitenverweise.",
                EventPriority.Normal,
                EventStatus.Active,
                "#0891B2",
                ["Planung", "Südflügel", "Bild"],
                manualSortPosition: 20m,
                webLink: new WebLink(
                    Guid.Parse("a1000000-0000-4000-8000-000000000306"),
                    new Uri("https://example.com/zeitstrahl-studio-beispiel"),
                    "Reservierte Beispieldomain")),
            CreateEvent(
                107,
                EventDate.ExactWithTime(
                    new DateOnly(2024, 6, 3),
                    new TimeOnly(18, 30)),
                "Abendtermin: Modellfreigabe",
                "Ein Ereignis mit erhaltener Uhrzeit.",
                "Die kritische Priorität und eine offene Frist werden zusätzlich durch Text " +
                "und nicht ausschließlich durch Farbe kenntlich gemacht.",
                EventPriority.Critical,
                EventStatus.Completed,
                "#BE123C",
                ["Freigabe", "Abendtermin"],
                new Deadline(
                    Guid.Parse("a1000000-0000-4000-8000-000000000207"),
                    new DateOnly(2024, 6, 10),
                    new TimeOnly(12, 0),
                    "Rückmeldung",
                    DeadlineStatus.Open,
                    "Nur lokale Erinnerung innerhalb des Projekts.")),
            CreateEvent(
                108,
                EventDate.MonthAndYear(2025, 6),
                "Werkstattnotiz zur Materialprobe",
                "Eine DOCX-Projektkopie liefert lokal extrahierbaren Text.",
                "Das Dokument enthält den frei erfundenen Suchbegriff Morgenfalter und eine " +
                "Datumserkennung, ohne Ereignisdaten automatisch zu überschreiben.",
                EventPriority.Normal,
                EventStatus.Active,
                "#4D7C0F",
                ["Dokument", "Materialprobe", "Morgenfalter"],
                new Deadline(
                    Guid.Parse("a1000000-0000-4000-8000-000000000208"),
                    new DateOnly(2025, 7, 15),
                    label: "Materialentscheidung",
                    status: DeadlineStatus.Open)),
            CreateEvent(
                109,
                EventDate.Year(2026),
                "Meilensteinplan wird verabschiedet",
                "Eine XLSX-Projektkopie zeigt fiktive Termine und Zustände.",
                "Die Jahresangabe bleibt ohne Monat und Tag erhalten. Im Tabelleninhalt steht " +
                "der eindeutige Prüfbegriff Blattgold.",
                EventPriority.High,
                EventStatus.Active,
                "#7E22CE",
                ["Tabelle", "Meilenstein", "Blattgold"]),
            CreateEvent(
                110,
                EventDate.Range(new DateOnly(2037, 5, 1), new DateOnly(2038, 11, 30)),
                "Ausblick: Wanderausstellung",
                "Ein später fiktiver Zeitraum bildet den Abschluss der Chronik.",
                "Die Planung ist archiviert; eine stornierte Frist bleibt als nachvollziehbare " +
                "historische Information im Projekt erhalten.",
                EventPriority.Low,
                EventStatus.Archived,
                "#475569",
                ["Ausblick", "Ausstellung"],
                new Deadline(
                    Guid.Parse("a1000000-0000-4000-8000-000000000210"),
                    new DateOnly(2038, 12, 15),
                    label: "Reservierung",
                    status: DeadlineStatus.Cancelled)),
        };

        foreach (var timelineEvent in events)
        {
            project.AddEvent(timelineEvent, CreatedAtUtc.AddHours(1));
        }

        project.SetLayoutPosition(
            new LayoutPosition(PlanningNorthId, TimelineOrientation.Horizontal, 18, -22),
            CreatedAtUtc.AddHours(2));
        project.SetLayoutPosition(
            new LayoutPosition(PlanningSouthId, TimelineOrientation.Vertical, 24, 16),
            CreatedAtUtc.AddHours(2));
        return project;
    }

    private static TimelineEvent CreateEvent(
        int number,
        EventDate date,
        string title,
        string infoText,
        string description,
        EventPriority priority,
        EventStatus status,
        string colorHex,
        IReadOnlyList<string> tags,
        Deadline? deadline = null,
        decimal? manualSortPosition = null,
        WebLink? webLink = null)
    {
        var id = Guid.Parse($"a1000000-0000-4000-8000-{number:D12}");
        var timestamp = CreatedAtUtc.AddMinutes(number);
        var timelineEvent = TimelineEvent.Create(id, title, date, CreatedAtUtc);
        timelineEvent.UpdateContent(
            title,
            infoText,
            description,
            "Frei erfundene Beispieldaten von Zeitstrahl Studio",
            $"Technische Beispielnotiz {number}; enthält keine realen Personen- oder Projektdaten.",
            timestamp);
        timelineEvent.SetClassification(priority, status, colorHex, timestamp);
        timelineEvent.SetDeadline(deadline, timestamp);
        timelineEvent.SetManualSortPosition(manualSortPosition, timestamp);
        foreach (var tag in tags)
        {
            timelineEvent.AddTag(tag, timestamp);
        }

        if (webLink is not null)
        {
            timelineEvent.AddWebLink(webLink, timestamp);
        }

        return timelineEvent;
    }
}
