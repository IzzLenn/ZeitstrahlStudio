using System.Collections.Concurrent;
using System.Globalization;
using SkiaSharp;
using ZeitstrahlStudio.Application;
using ZeitstrahlStudio.Domain;

namespace ZeitstrahlStudio.Export;

/// <summary>Erzeugt ausschließlich lokal druckoptimierte Vektor-PDFs mit optionalen Miniaturbildern.</summary>
public sealed class SkiaPdfExportService : IPdfExportService
{
    private const long MaximumImageSourceBytes = 50L * 1024 * 1024;

    private readonly PdfExportPlanner planner;
    private readonly IAttachmentFileService attachmentFileService;
    private readonly IPdfPreviewService pdfPreviewService;

    public SkiaPdfExportService(
        PdfExportPlanner planner,
        IAttachmentFileService attachmentFileService,
        IPdfPreviewService pdfPreviewService)
    {
        this.planner = planner;
        this.attachmentFileService = attachmentFileService;
        this.pdfPreviewService = pdfPreviewService;
    }

    public Task<ExportPreview> CreatePreviewAsync(
        ProjectWorkspace workspace,
        PdfExportOptions options,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        cancellationToken.ThrowIfCancellationRequested();
        var plan = planner.Create(workspace.Project, options);
        return Task.FromResult(new ExportPreview(
            plan.Pages.Count,
            plan.WidthMillimeters,
            plan.HeightMillimeters,
            plan.Warnings));
    }

    public async Task ExportAsync(
        ProjectWorkspace workspace,
        PdfExportOptions options,
        string targetPath,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        if (string.IsNullOrWhiteSpace(targetPath))
        {
            throw new ArgumentException("Der Zielpfad für den PDF-Export darf nicht leer sein.", nameof(targetPath));
        }

        var fullTargetPath = Path.GetFullPath(targetPath);
        if (!Path.GetExtension(fullTargetPath).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Die Zieldatei des PDF-Exports muss die Endung .pdf besitzen.", nameof(targetPath));
        }

        EnsureTargetOutsideWorkspace(workspace, fullTargetPath);
        var directory = Path.GetDirectoryName(fullTargetPath);
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException(
                "Der Zielordner für den PDF-Export wurde nicht gefunden.");
        }

        var plan = planner.Create(workspace.Project, options);
        cancellationToken.ThrowIfCancellationRequested();
        var thumbnails = await LoadThumbnailsAsync(workspace, plan, cancellationToken).ConfigureAwait(false);
        var pdfBytes = await Task.Run(
            () => RenderPdf(workspace.Project, plan, thumbnails, cancellationToken),
            cancellationToken).ConfigureAwait(false);

        var temporaryPath = Path.Combine(
            directory,
            $".{Path.GetFileName(fullTargetPath)}.{Guid.NewGuid():N}.tmp");
        try
        {
            await File.WriteAllBytesAsync(temporaryPath, pdfBytes, cancellationToken).ConfigureAwait(false);
            File.Move(temporaryPath, fullTargetPath, overwrite: true);
        }
        catch (UnauthorizedAccessException exception)
        {
            throw new UnauthorizedAccessException(
                "Die PDF-Datei konnte nicht gespeichert werden. Bitte prüfen Sie die Zugriffsrechte des Zielordners.",
                exception);
        }
        catch (IOException exception)
        {
            throw new IOException(
                "Die PDF-Datei konnte nicht vollständig gespeichert werden. " +
                "Bitte prüfen Sie freien Speicherplatz und Dateisperren.",
                exception);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                try
                {
                    File.Delete(temporaryPath);
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                {
                }
            }
        }
    }

    private static void EnsureTargetOutsideWorkspace(ProjectWorkspace workspace, string fullTargetPath)
    {
        var workspaceRoot = Path.GetFullPath(workspace.WorkingDirectory)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
            Path.DirectorySeparatorChar;
        if (fullTargetPath.StartsWith(workspaceRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Die PDF darf nicht innerhalb des aktiven Projektarbeitsordners gespeichert werden. " +
                "Bitte wählen Sie einen anderen Zielordner.");
        }
    }

    private async Task<IReadOnlyDictionary<Guid, byte[]>> LoadThumbnailsAsync(
        ProjectWorkspace workspace,
        PdfExportPlan plan,
        CancellationToken cancellationToken)
    {
        var requiredEventIds = plan.Pages
            .SelectMany(page => page.EventBlocks)
            .Where(block => block.HasThumbnailCandidate)
            .Select(block => block.EventId)
            .Distinct()
            .ToArray();
        var thumbnails = new ConcurrentDictionary<Guid, byte[]>();

        await Parallel.ForEachAsync(
            requiredEventIds,
            new ParallelOptions
            {
                CancellationToken = cancellationToken,
                MaxDegreeOfParallelism = 2,
            },
            async (eventId, token) =>
            {
                var timelineEvent = workspace.Project.Events.Single(item => item.Id == eventId);
                var primary = TimelineThumbnailSelection.SelectPrimary(timelineEvent);
                if (primary is null)
                {
                    return;
                }

                try
                {
                    var localPath = await attachmentFileService
                        .GetValidatedLocalPathAsync(workspace, primary, token)
                        .ConfigureAwait(false);
                    byte[]? data;
                    if (primary.MediaType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
                    {
                        var preview = await pdfPreviewService.RenderPageAsync(
                            localPath,
                            primary.LinkedPdfPage ?? 1,
                            0.35,
                            token).ConfigureAwait(false);
                        data = preview.PngData;
                    }
                    else
                    {
                        var fileInfo = new FileInfo(localPath);
                        data = fileInfo.Length <= MaximumImageSourceBytes
                            ? await File.ReadAllBytesAsync(localPath, token).ConfigureAwait(false)
                            : null;
                    }

                    if (data is { Length: > 0 })
                    {
                        thumbnails[eventId] = data;
                    }
                }
                catch (Exception exception) when (
                    exception is IOException or UnauthorizedAccessException or InvalidDataException or
                    ArgumentException or InvalidOperationException)
                {
                    // Der Textverweis auf den Anhang bleibt erhalten; ein defektes Vorschaubild
                    // darf den vollständigen Zeitstrahl-Export nicht verhindern.
                }
            }).ConfigureAwait(false);

        return thumbnails;
    }

    private static byte[] RenderPdf(
        TimelineProject project,
        PdfExportPlan plan,
        IReadOnlyDictionary<Guid, byte[]> thumbnails,
        CancellationToken cancellationToken)
    {
        using var output = new MemoryStream();
        using var document = SKDocument.CreatePdf(output, 300f)
            ?? throw new InvalidOperationException("Die lokale PDF-Engine konnte nicht initialisiert werden.");
        using var regularTypeface = SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Normal)
            ?? SKTypeface.Default;
        using var semiboldTypeface = SKTypeface.FromFamilyName("Segoe UI Semibold", SKFontStyle.Normal)
            ?? SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Bold)
            ?? SKTypeface.Default;
        using var bodyPaint = new SKPaint { IsAntialias = true, Color = SKColors.Black };
        using var mutedPaint = new SKPaint { IsAntialias = true, Color = new SKColor(71, 85, 105) };
        using var whitePaint = new SKPaint { IsAntialias = true, Color = SKColors.White };
        using var linePaint = new SKPaint
        {
            IsAntialias = true,
            Color = new SKColor(100, 116, 139),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1.1f,
        };
        using var axisPaint = new SKPaint
        {
            IsAntialias = true,
            Color = new SKColor(30, 41, 59),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1.6f,
        };
        using var cardFillPaint = new SKPaint
        {
            IsAntialias = true,
            Color = SKColors.White,
            Style = SKPaintStyle.Fill,
        };

        var events = project.Events.ToDictionary(item => item.Id);
        foreach (var page in plan.Pages)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var canvas = document.BeginPage(page.WidthPoints, page.HeightPoints);
            canvas.Clear(SKColors.White);
            DrawHeader(canvas, project, plan, page, regularTypeface, semiboldTypeface, bodyPaint, mutedPaint);
            DrawAxis(canvas, page, axisPaint);

            foreach (var block in page.EventBlocks)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var timelineEvent = events[block.EventId];
                DrawEventBlock(
                    canvas,
                    page,
                    block,
                    timelineEvent,
                    plan.FontSizePoints,
                    thumbnails.GetValueOrDefault(block.EventId),
                    regularTypeface,
                    semiboldTypeface,
                    bodyPaint,
                    mutedPaint,
                    whitePaint,
                    linePaint,
                    axisPaint,
                    cardFillPaint);
            }

            DrawFooter(canvas, project, plan, page, regularTypeface, mutedPaint);
            document.EndPage();
        }

        document.Close();
        return output.ToArray();
    }

    private static void DrawHeader(
        SKCanvas canvas,
        TimelineProject project,
        PdfExportPlan plan,
        PdfPagePlan page,
        SKTypeface regularTypeface,
        SKTypeface semiboldTypeface,
        SKPaint bodyPaint,
        SKPaint mutedPaint)
    {
        using var titleFont = new SKFont(semiboldTypeface, 15);
        using var metaFont = new SKFont(regularTypeface, 8.5f);
        var titleY = PdfExportPlanner.MarginPoints + 15;
        canvas.DrawText(project.Name, PdfExportPlanner.MarginPoints, titleY, SKTextAlign.Left, titleFont, bodyPaint);

        var period = GetPlanPeriod(project, page);
        var meta = string.IsNullOrWhiteSpace(project.Subtitle)
            ? period
            : $"{project.Subtitle} · {period}";
        canvas.DrawText(
            meta,
            PdfExportPlanner.MarginPoints,
            titleY + 15,
            SKTextAlign.Left,
            metaFont,
            mutedPaint);

        var format = $"{plan.WidthMillimeters:0.#} × {plan.HeightMillimeters:0.#} mm";
        canvas.DrawText(
            format,
            page.WidthPoints - PdfExportPlanner.MarginPoints,
            titleY,
            SKTextAlign.Right,
            metaFont,
            mutedPaint);
    }

    private static void DrawAxis(SKCanvas canvas, PdfPagePlan page, SKPaint axisPaint)
    {
        var axisX = PdfExportPlanner.MarginPoints + (PdfExportPlanner.AxisLaneWidthPoints / 2);
        var top = PdfExportPlanner.MarginPoints + PdfExportPlanner.HeaderHeightPoints;
        var bottom = page.HeightPoints - PdfExportPlanner.MarginPoints - PdfExportPlanner.FooterHeightPoints;
        canvas.DrawLine(axisX, top, axisX, bottom, axisPaint);
    }

    private static void DrawEventBlock(
        SKCanvas canvas,
        PdfPagePlan page,
        PdfEventBlockPlan block,
        TimelineEvent timelineEvent,
        float fontSize,
        byte[]? thumbnailData,
        SKTypeface regularTypeface,
        SKTypeface semiboldTypeface,
        SKPaint bodyPaint,
        SKPaint mutedPaint,
        SKPaint whitePaint,
        SKPaint linePaint,
        SKPaint axisPaint,
        SKPaint cardFillPaint)
    {
        var axisX = PdfExportPlanner.MarginPoints + (PdfExportPlanner.AxisLaneWidthPoints / 2);
        var cardLeft = PdfExportPlanner.MarginPoints + PdfExportPlanner.AxisLaneWidthPoints;
        var cardRight = page.WidthPoints - PdfExportPlanner.MarginPoints;
        var cardRect = new SKRect(cardLeft, block.TopPoints, cardRight, block.TopPoints + block.HeightPoints);
        var centerY = Math.Min(cardRect.Bottom - 12, cardRect.Top + 22);

        canvas.DrawLine(axisX, centerY, cardLeft, centerY, axisPaint);
        canvas.DrawCircle(axisX, centerY, 3.8f, axisPaint);
        canvas.DrawRoundRect(cardRect, 4, 4, cardFillPaint);
        canvas.DrawRoundRect(cardRect, 4, 4, linePaint);

        var eventColor = ParseColor(timelineEvent.ColorHex);
        using var accentPaint = new SKPaint
        {
            IsAntialias = true,
            Color = eventColor,
            Style = SKPaintStyle.Fill,
        };
        canvas.DrawRoundRect(
            new SKRect(cardRect.Left, cardRect.Top, cardRect.Left + 5, cardRect.Bottom),
            3,
            3,
            accentPaint);

        var textRight = cardRect.Right - PdfExportPlanner.CardPaddingPoints;
        if (block.HasThumbnailCandidate && thumbnailData is { Length: > 0 })
        {
            var thumbnailRect = new SKRect(
                cardRect.Right - PdfExportPlanner.CardPaddingPoints - PdfExportPlanner.ThumbnailWidthPoints,
                cardRect.Top + PdfExportPlanner.CardPaddingPoints,
                cardRect.Right - PdfExportPlanner.CardPaddingPoints,
                Math.Min(
                    cardRect.Bottom - PdfExportPlanner.CardPaddingPoints,
                    cardRect.Top + PdfExportPlanner.CardPaddingPoints + 92));
            if (DrawThumbnail(canvas, thumbnailRect, thumbnailData, linePaint))
            {
                textRight = thumbnailRect.Left - PdfExportPlanner.ThumbnailGapPoints;
            }
        }

        var textLeft = cardRect.Left + PdfExportPlanner.CardPaddingPoints + 5;
        var y = cardRect.Top + PdfExportPlanner.CardPaddingPoints;
        canvas.Save();
        canvas.ClipRect(new SKRect(textLeft, cardRect.Top, textRight, cardRect.Bottom));
        foreach (var line in block.Lines)
        {
            var size = PdfExportPlanner.RoleFontSize(line.Role, fontSize);
            using var font = new SKFont(
                line.Role is PdfTextRole.Title or PdfTextRole.Date or PdfTextRole.Deadline
                    ? semiboldTypeface
                    : regularTypeface,
                size);
            var lineHeight = PdfExportPlanner.LineHeight(line.Role, fontSize);
            y += size;
            var paint = line.Role is PdfTextRole.Metadata or PdfTextRole.Date ? mutedPaint : bodyPaint;
            canvas.DrawText(line.Text, textLeft, y, SKTextAlign.Left, font, paint);
            y += lineHeight - size;
        }

        canvas.Restore();

        using var labelFont = new SKFont(semiboldTypeface, 7.5f);
        if (block.IsContinuation)
        {
            DrawBadge(canvas, cardRect.Right - 70, cardRect.Top + 9, "FORTSETZUNG", labelFont, eventColor, whitePaint);
        }

        if (block.ContinuesOnNextPage)
        {
            DrawBadge(canvas, cardRect.Right - 76, cardRect.Bottom - 6, "WEITER →", labelFont, eventColor, whitePaint);
        }
    }

    private static bool DrawThumbnail(
        SKCanvas canvas,
        SKRect destination,
        byte[] data,
        SKPaint borderPaint)
    {
        try
        {
            using var image = SKImage.FromEncodedData(data);
            if (image is null || image.Width <= 0 || image.Height <= 0)
            {
                return false;
            }

            var scale = Math.Min(destination.Width / image.Width, destination.Height / image.Height);
            var width = image.Width * scale;
            var height = image.Height * scale;
            var fitted = new SKRect(
                destination.MidX - (width / 2),
                destination.MidY - (height / 2),
                destination.MidX + (width / 2),
                destination.MidY + (height / 2));
            canvas.DrawImage(image, fitted);
            canvas.DrawRect(fitted, borderPaint);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static void DrawBadge(
        SKCanvas canvas,
        float right,
        float baseline,
        string text,
        SKFont font,
        SKColor color,
        SKPaint whitePaint)
    {
        using var fill = new SKPaint { IsAntialias = true, Color = color, Style = SKPaintStyle.Fill };
        var width = font.MeasureText(text) + 8;
        var rect = new SKRect(right - width, baseline - 8, right, baseline + 3);
        canvas.DrawRoundRect(rect, 2, 2, fill);
        canvas.DrawText(text, rect.Left + 4, baseline, SKTextAlign.Left, font, whitePaint);
    }

    private static void DrawFooter(
        SKCanvas canvas,
        TimelineProject project,
        PdfExportPlan plan,
        PdfPagePlan page,
        SKTypeface regularTypeface,
        SKPaint mutedPaint)
    {
        using var footerFont = new SKFont(regularTypeface, 8);
        var y = page.HeightPoints - PdfExportPlanner.MarginPoints + 5;
        canvas.DrawText(
            "Zeitstrahl Studio · exportierte Momentaufnahme",
            PdfExportPlanner.MarginPoints,
            y,
            SKTextAlign.Left,
            footerFont,
            mutedPaint);
        canvas.DrawText(
            $"Seite {page.PageNumber.ToString(CultureInfo.InvariantCulture)} von {plan.Pages.Count.ToString(CultureInfo.InvariantCulture)}",
            page.WidthPoints - PdfExportPlanner.MarginPoints,
            y,
            SKTextAlign.Right,
            footerFont,
            mutedPaint);
        _ = project;
    }

    private static string GetPlanPeriod(TimelineProject project, PdfPagePlan page)
    {
        var pageEvents = page.EventBlocks
            .Select(block => project.Events.Single(item => item.Id == block.EventId))
            .DistinctBy(item => item.Id)
            .OrderBy(item => item.Date.SortStart)
            .ToArray();
        if (pageEvents.Length == 0)
        {
            return "Kein Ereignis im Exportbereich";
        }

        return pageEvents.Length == 1
            ? pageEvents[0].Date.ToDisplayString()
            : $"{pageEvents[0].Date.ToDisplayString()} bis {pageEvents[^1].Date.ToDisplayString()}";
    }

    private static SKColor ParseColor(string colorHex)
    {
        if (uint.TryParse(colorHex.AsSpan(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var rgb))
        {
            return new SKColor(
                (byte)((rgb >> 16) & 0xFF),
                (byte)((rgb >> 8) & 0xFF),
                (byte)(rgb & 0xFF));
        }

        return new SKColor(37, 99, 235);
    }
}
