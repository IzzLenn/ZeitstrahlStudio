using System.IO.Compression;
using System.Text;
using SkiaSharp;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

namespace ZeitstrahlStudio.SampleGenerator;

internal sealed record SampleDocumentSet(
    string ProjectKickoffPdf,
    string MeetingMinutesPdf,
    string PlanningBoardPng,
    string WorkshopNoteDocx,
    string MilestonesXlsx)
{
    public IReadOnlyList<string> All =>
    [
        ProjectKickoffPdf,
        MeetingMinutesPdf,
        PlanningBoardPng,
        WorkshopNoteDocx,
        MilestonesXlsx,
    ];
}

internal static class SampleDocumentFactory
{
    private static readonly DateTimeOffset ArchiveTimestamp =
        new(2024, 1, 2, 10, 0, 0, TimeSpan.Zero);

    public static async Task<SampleDocumentSet> CreateAsync(
        string directory,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        var fullDirectory = Path.GetFullPath(directory);
        Directory.CreateDirectory(fullDirectory);

        var projectKickoffPdf = Path.Combine(fullDirectory, "Projektauftakt.pdf");
        var meetingMinutesPdf = Path.Combine(fullDirectory, "Besprechungsprotokoll.pdf");
        var planningBoardPng = Path.Combine(fullDirectory, "Planungstafel.png");
        var workshopNoteDocx = Path.Combine(fullDirectory, "Werkstattnotiz.docx");
        var milestonesXlsx = Path.Combine(fullDirectory, "Meilensteine.xlsx");

        await CreatePdfAsync(
            projectKickoffPdf,
            "Projektauftakt Nordfluegel",
            [
                "Frei erfundene lokale Testdatei fuer Zeitstrahl Studio.",
                "Planungsrunde am 12.04.2024.",
                "Pruefbegriff: Kupferstern.",
                "Alle Angaben dienen ausschliesslich der Demonstration.",
            ],
            cancellationToken).ConfigureAwait(false);
        await CreatePdfAsync(
            meetingMinutesPdf,
            "Besprechungsprotokoll",
            [
                "Fiktives Protokoll der Werkstattplanung.",
                "Beschluss vom 30.04.2024: Modell wird freigegeben.",
                "Keine realen Personen, Einrichtungen oder Projektdaten.",
            ],
            cancellationToken).ConfigureAwait(false);
        await CreatePlanningBoardAsync(planningBoardPng, cancellationToken).ConfigureAwait(false);
        await CreateDocxAsync(workshopNoteDocx, cancellationToken).ConfigureAwait(false);
        await CreateXlsxAsync(milestonesXlsx, cancellationToken).ConfigureAwait(false);

        return new SampleDocumentSet(
            projectKickoffPdf,
            meetingMinutesPdf,
            planningBoardPng,
            workshopNoteDocx,
            milestonesXlsx);
    }

    private static async Task CreatePdfAsync(
        string path,
        string title,
        IReadOnlyList<string> lines,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        byte[] bytes;
        using (var builder = new PdfDocumentBuilder())
        {
            builder.DocumentInformation.Title = title;
            builder.DocumentInformation.Author = "Zeitstrahl Studio Beispieldaten";
            builder.DocumentInformation.Subject = "Frei erfundene lokale Testdaten";
            var regular = builder.AddStandard14Font(Standard14Font.Helvetica);
            var bold = builder.AddStandard14Font(Standard14Font.HelveticaBold);
            var page = builder.AddPage(PageSize.A4);
            page.AddText(title, 20, new PdfPoint(54, 780), bold);
            var y = 740d;
            foreach (var line in lines)
            {
                page.AddText(line, 11, new PdfPoint(54, y), regular);
                y -= 24;
            }

            page.AddText(
                "Lizenz: MIT; vollstaendig programmatisch erzeugt.",
                9,
                new PdfPoint(54, 90),
                regular);
            bytes = builder.Build();
        }

        await File.WriteAllBytesAsync(path, bytes, cancellationToken).ConfigureAwait(false);
    }

    private static async Task CreatePlanningBoardAsync(
        string path,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var bitmap = new SKBitmap(1200, 720, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(new SKColor(241, 245, 249));
        using var panelPaint = new SKPaint
        {
            IsAntialias = true,
            Color = SKColors.White,
            Style = SKPaintStyle.Fill,
        };
        using var linePaint = new SKPaint
        {
            IsAntialias = true,
            Color = new SKColor(14, 116, 144),
            StrokeWidth = 8,
            Style = SKPaintStyle.Stroke,
        };
        using var accentPaint = new SKPaint
        {
            IsAntialias = true,
            Color = new SKColor(8, 145, 178),
            Style = SKPaintStyle.Fill,
        };
        using var darkPaint = new SKPaint
        {
            IsAntialias = true,
            Color = new SKColor(15, 23, 42),
            Style = SKPaintStyle.Fill,
        };
        using var mutedPaint = new SKPaint
        {
            IsAntialias = true,
            Color = new SKColor(71, 85, 105),
            Style = SKPaintStyle.Fill,
        };
        using var typeface = SKTypeface.FromFamilyName("Segoe UI") ?? SKTypeface.Default;
        using var titleFont = new SKFont(typeface, 48);
        using var bodyFont = new SKFont(typeface, 28);
        using var smallFont = new SKFont(typeface, 22);

        var panel = new SKRect(70, 65, 1130, 655);
        canvas.DrawRoundRect(panel, 26, 26, panelPaint);
        canvas.DrawText(
            "Planungstafel Bürgerlabor Sonnenwinkel",
            118,
            150,
            SKTextAlign.Left,
            titleFont,
            darkPaint);
        canvas.DrawText(
            "Frei erfundene, programmatisch erzeugte Testgrafik",
            118,
            198,
            SKTextAlign.Left,
            smallFont,
            mutedPaint);

        const float axisY = 390;
        canvas.DrawLine(150, axisY, 1050, axisY, linePaint);
        var stations = new[]
        {
            (X: 190f, Label: "Idee", Date: "12.04.2024"),
            (X: 475f, Label: "Modell", Date: "03.06.2024"),
            (X: 760f, Label: "Probe", Date: "Juni 2025"),
            (X: 1010f, Label: "Plan", Date: "2026"),
        };
        foreach (var station in stations)
        {
            canvas.DrawCircle(station.X, axisY, 22, accentPaint);
            canvas.DrawText(
                station.Label,
                station.X,
                axisY - 52,
                SKTextAlign.Center,
                bodyFont,
                darkPaint);
            canvas.DrawText(
                station.Date,
                station.X,
                axisY + 68,
                SKTextAlign.Center,
                smallFont,
                mutedPaint);
        }

        canvas.DrawText(
            "Lokale Beispieldaten · keine realen Personen oder Projekte · MIT-Lizenz",
            600,
            590,
            SKTextAlign.Center,
            smallFont,
            mutedPaint);

        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(SKEncodedImageFormat.Png, 100)
            ?? throw new InvalidOperationException("Die PNG-Testgrafik konnte nicht kodiert werden.");
        await File.WriteAllBytesAsync(path, encoded.ToArray(), cancellationToken).ConfigureAwait(false);
    }

    private static Task CreateDocxAsync(string path, CancellationToken cancellationToken) =>
        CreateZipAsync(
            path,
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["[Content_Types].xml"] =
                    """
                    <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                    <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
                      <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
                      <Default Extension="xml" ContentType="application/xml"/>
                      <Override PartName="/word/document.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"/>
                      <Override PartName="/docProps/core.xml" ContentType="application/vnd.openxmlformats-package.core-properties+xml"/>
                    </Types>
                    """,
                ["_rels/.rels"] =
                    """
                    <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                    <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                      <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="word/document.xml"/>
                      <Relationship Id="rId2" Type="http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties" Target="docProps/core.xml"/>
                    </Relationships>
                    """,
                ["word/document.xml"] =
                    """
                    <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                    <w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
                      <w:body>
                        <w:p><w:r><w:t>Werkstattnotiz zur Materialprobe</w:t></w:r></w:p>
                        <w:p><w:r><w:t>Am 17.06.2025 wurde die frei erfundene Probe Morgenfalter dokumentiert.</w:t></w:r></w:p>
                        <w:p><w:r><w:t>Das Material bleibt bis zum 15.07.2025 ausschließlich lokal im Beispielprojekt.</w:t></w:r></w:p>
                        <w:p><w:r><w:t>Diese Datei enthält keine realen Personen- oder Projektdaten.</w:t></w:r></w:p>
                        <w:sectPr/>
                      </w:body>
                    </w:document>
                    """,
                ["docProps/core.xml"] =
                    """
                    <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                    <cp:coreProperties
                        xmlns:cp="http://schemas.openxmlformats.org/package/2006/metadata/core-properties"
                        xmlns:dc="http://purl.org/dc/elements/1.1/"
                        xmlns:dcterms="http://purl.org/dc/terms/"
                        xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
                      <dc:title>Werkstattnotiz Morgenfalter</dc:title>
                      <dc:creator>Zeitstrahl Studio Beispieldaten</dc:creator>
                      <dc:subject>Frei erfundene lokale Testdaten</dc:subject>
                      <dcterms:created xsi:type="dcterms:W3CDTF">2024-01-02T10:00:00Z</dcterms:created>
                    </cp:coreProperties>
                    """,
            },
            cancellationToken);

    private static Task CreateXlsxAsync(string path, CancellationToken cancellationToken) =>
        CreateZipAsync(
            path,
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["[Content_Types].xml"] =
                    """
                    <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                    <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
                      <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
                      <Default Extension="xml" ContentType="application/xml"/>
                      <Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>
                      <Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
                      <Override PartName="/xl/sharedStrings.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sharedStrings+xml"/>
                      <Override PartName="/docProps/core.xml" ContentType="application/vnd.openxmlformats-package.core-properties+xml"/>
                    </Types>
                    """,
                ["_rels/.rels"] =
                    """
                    <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                    <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                      <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/>
                      <Relationship Id="rId2" Type="http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties" Target="docProps/core.xml"/>
                    </Relationships>
                    """,
                ["xl/workbook.xml"] =
                    """
                    <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                    <workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"
                              xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
                      <sheets><sheet name="Meilensteine" sheetId="1" r:id="rId1"/></sheets>
                    </workbook>
                    """,
                ["xl/_rels/workbook.xml.rels"] =
                    """
                    <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                    <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                      <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/>
                      <Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/sharedStrings" Target="sharedStrings.xml"/>
                    </Relationships>
                    """,
                ["xl/sharedStrings.xml"] =
                    """
                    <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                    <sst xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" count="10" uniqueCount="10">
                      <si><t>Meilenstein</t></si>
                      <si><t>Termin</t></si>
                      <si><t>Status</t></si>
                      <si><t>Prüfbegriff</t></si>
                      <si><t>Modellfreigabe</t></si>
                      <si><t>03.06.2024</t></si>
                      <si><t>Abgeschlossen</t></si>
                      <si><t>Blattgold</t></si>
                      <si><t>Materialentscheidung</t></si>
                      <si><t>15.07.2025</t></si>
                    </sst>
                    """,
                ["xl/worksheets/sheet1.xml"] =
                    """
                    <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                    <worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
                      <sheetData>
                        <row r="1">
                          <c r="A1" t="s"><v>0</v></c>
                          <c r="B1" t="s"><v>1</v></c>
                          <c r="C1" t="s"><v>2</v></c>
                          <c r="D1" t="s"><v>3</v></c>
                        </row>
                        <row r="2">
                          <c r="A2" t="s"><v>4</v></c>
                          <c r="B2" t="s"><v>5</v></c>
                          <c r="C2" t="s"><v>6</v></c>
                          <c r="D2" t="s"><v>7</v></c>
                        </row>
                        <row r="3">
                          <c r="A3" t="s"><v>8</v></c>
                          <c r="B3" t="s"><v>9</v></c>
                          <c r="C3"><v>42</v></c>
                        </row>
                      </sheetData>
                    </worksheet>
                    """,
                ["docProps/core.xml"] =
                    """
                    <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                    <cp:coreProperties
                        xmlns:cp="http://schemas.openxmlformats.org/package/2006/metadata/core-properties"
                        xmlns:dc="http://purl.org/dc/elements/1.1/"
                        xmlns:dcterms="http://purl.org/dc/terms/"
                        xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
                      <dc:title>Meilensteinplan Blattgold</dc:title>
                      <dc:creator>Zeitstrahl Studio Beispieldaten</dc:creator>
                      <dc:subject>Frei erfundene lokale Testdaten</dc:subject>
                      <dcterms:created xsi:type="dcterms:W3CDTF">2024-01-02T10:00:00Z</dcterms:created>
                    </cp:coreProperties>
                    """,
            },
            cancellationToken);

    private static async Task CreateZipAsync(
        string path,
        IReadOnlyDictionary<string, string> entries,
        CancellationToken cancellationToken)
    {
        await using var file = new FileStream(
            path,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 64 * 1024,
            useAsync: true);
        using var archive = new ZipArchive(file, ZipArchiveMode.Create, leaveOpen: true);
        foreach (var pair in entries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var entry = archive.CreateEntry(pair.Key, CompressionLevel.Optimal);
            entry.LastWriteTime = ArchiveTimestamp;
            await using var stream = entry.Open();
            await using var writer = new StreamWriter(
                stream,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                bufferSize: 1024,
                leaveOpen: false);
            await writer.WriteAsync(pair.Value.AsMemory(), cancellationToken).ConfigureAwait(false);
        }
    }
}
