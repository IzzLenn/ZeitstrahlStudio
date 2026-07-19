using System.Collections.Concurrent;
using System.Data.Common;
using System.Text;
using SkiaSharp;
using ZeitstrahlStudio.Application;
using ZeitstrahlStudio.Domain;

namespace ZeitstrahlStudio.Export;

/// <summary>Erzeugt eine einzelne offlinefähige HTML-Momentaufnahme ohne externe Ressourcen.</summary>
public sealed class StandaloneHtmlExportService : IHtmlExportService
{
    private const long MaximumImageSourceBytes = 50L * 1024 * 1024;
    private const int MaximumImageEdge = 8_000;
    private const long MaximumImagePixels = 24_000_000;
    private const int MaximumThumbnailWidth = 360;
    private const int MaximumThumbnailHeight = 240;
    private readonly IAttachmentFileService attachmentFileService;
    private readonly IPdfPreviewService pdfPreviewService;
    private readonly IAttachmentAnalysisStore analysisStore;

    public StandaloneHtmlExportService(
        IAttachmentFileService attachmentFileService,
        IPdfPreviewService pdfPreviewService,
        IAttachmentAnalysisStore analysisStore)
    {
        this.attachmentFileService = attachmentFileService;
        this.pdfPreviewService = pdfPreviewService;
        this.analysisStore = analysisStore;
    }

    public async Task ExportAsync(
        ProjectWorkspace workspace,
        HtmlExportOptions options,
        string targetPath,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentNullException.ThrowIfNull(options);
        if (string.IsNullOrWhiteSpace(targetPath))
        {
            throw new ArgumentException("Der Zielpfad für den HTML-Export darf nicht leer sein.", nameof(targetPath));
        }

        var fullTargetPath = Path.GetFullPath(targetPath);
        var extension = Path.GetExtension(fullTargetPath);
        if (!extension.Equals(".html", StringComparison.OrdinalIgnoreCase) &&
            !extension.Equals(".htm", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "Die Zieldatei des Standalone-Exports muss die Endung .html oder .htm besitzen.",
                nameof(targetPath));
        }

        EnsureTargetOutsideWorkspace(workspace, fullTargetPath);
        var directory = Path.GetDirectoryName(fullTargetPath);
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException(
                "Der Zielordner für den HTML-Export wurde nicht gefunden.");
        }

        var payload = await CreatePayloadAsync(workspace, options, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        var json = StandaloneHtmlDataEncoder.Serialize(payload);
        var html = StandaloneHtmlTemplate.Content.Replace(
            StandaloneHtmlTemplate.DataPlaceholder,
            json,
            StringComparison.Ordinal);
        var temporaryPath = Path.Combine(
            directory,
            $".{Path.GetFileName(fullTargetPath)}.{Guid.NewGuid():N}.tmp");
        try
        {
            await File.WriteAllTextAsync(
                temporaryPath,
                html,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                cancellationToken).ConfigureAwait(false);
            File.Move(temporaryPath, fullTargetPath, overwrite: true);
        }
        catch (UnauthorizedAccessException exception)
        {
            throw new UnauthorizedAccessException(
                "Die HTML-Datei konnte nicht gespeichert werden. Bitte prüfen Sie die Zugriffsrechte des Zielordners.",
                exception);
        }
        catch (IOException exception)
        {
            throw new IOException(
                "Die HTML-Datei konnte nicht vollständig gespeichert werden. " +
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

    private async Task<HtmlProjectPayload> CreatePayloadAsync(
        ProjectWorkspace workspace,
        HtmlExportOptions options,
        CancellationToken cancellationToken)
    {
        var chronologicalEvents = workspace.Project.GetChronologicalEvents();
        var results = new ConcurrentDictionary<Guid, HtmlEventPayload>();
        await Parallel.ForEachAsync(
            chronologicalEvents,
            new ParallelOptions
            {
                CancellationToken = cancellationToken,
                MaxDegreeOfParallelism = 2,
            },
            async (timelineEvent, token) =>
            {
                results[timelineEvent.Id] = await CreateEventPayloadAsync(
                    workspace,
                    timelineEvent,
                    options,
                    token).ConfigureAwait(false);
            }).ConfigureAwait(false);

        return new HtmlProjectPayload(
            workspace.Project.Name,
            workspace.Project.Subtitle,
            workspace.Project.InfoText,
            workspace.Project.Description,
            workspace.Project.OverallStart?.ToString("yyyy-MM-dd"),
            workspace.Project.OverallEnd?.ToString("yyyy-MM-dd"),
            DateTimeOffset.UtcNow,
            options.InitialOrientation == TimelineOrientation.Horizontal ? "horizontal" : "vertical",
            chronologicalEvents.Select(timelineEvent => results[timelineEvent.Id]).ToArray());
    }

    private async Task<HtmlEventPayload> CreateEventPayloadAsync(
        ProjectWorkspace workspace,
        TimelineEvent timelineEvent,
        HtmlExportOptions options,
        CancellationToken cancellationToken)
    {
        var extractedTexts = new List<string>();
        foreach (var attachment in timelineEvent.Attachments)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var analysis = await analysisStore.LoadAsync(
                    workspace,
                    attachment,
                    cancellationToken).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(analysis?.ExtractedText))
                {
                    extractedTexts.Add(analysis.ExtractedText);
                }
            }
            catch (Exception exception) when (
                exception is IOException or UnauthorizedAccessException or InvalidDataException or
                InvalidOperationException or ArgumentException or DbException)
            {
                // Ein beschädigtes Analyseergebnis darf den Export der übrigen Ereignisdaten nicht verhindern.
            }
        }

        var thumbnailDataUrl = options.IncludeThumbnails
            ? await CreateThumbnailDataUrlAsync(workspace, timelineEvent, cancellationToken).ConfigureAwait(false)
            : null;
        var attachments = timelineEvent.Attachments
            .Select(attachment => new HtmlAttachmentPayload(
                attachment.OriginalFileName,
                attachment.MediaType,
                attachment.FileSize,
                attachment.LinkedPdfPage))
            .ToArray();
        var webLinks = timelineEvent.WebLinks
            .Select(link => new HtmlWebLinkPayload(link.Address.AbsoluteUri, link.Label))
            .ToArray();
        var deadline = timelineEvent.Deadline is null
            ? null
            : new HtmlDeadlinePayload(
                timelineEvent.Deadline.DueDate.ToString("yyyy-MM-dd"),
                timelineEvent.Deadline.DueDate.ToString("dd.MM.yyyy"),
                timelineEvent.Deadline.DueTime?.ToString("HH\\:mm"),
                timelineEvent.Deadline.Label,
                timelineEvent.Deadline.Status.ToString().ToLowerInvariant(),
                TranslateDeadlineStatus(timelineEvent.Deadline.Status),
                timelineEvent.Deadline.ReminderNote);
        var (filterStart, filterEnd) = GetFilterRange(timelineEvent.Date);
        var searchText = string.Join(
            '\n',
            new[]
            {
                workspace.Project.Name,
                workspace.Project.Subtitle,
                workspace.Project.InfoText,
                workspace.Project.Description,
                timelineEvent.Title,
                timelineEvent.InfoText,
                timelineEvent.Description,
                timelineEvent.Source,
                options.IncludeNotes ? timelineEvent.Notes : null,
                string.Join(' ', timelineEvent.Tags),
                string.Join(' ', attachments.Select(item => item.FileName)),
                string.Join(' ', webLinks.Select(item => item.Address)),
                string.Join('\n', extractedTexts),
            }.Where(value => !string.IsNullOrWhiteSpace(value)));

        return new HtmlEventPayload(
            timelineEvent.Id,
            filterStart.ToString("yyyy-MM-dd"),
            filterEnd.ToString("yyyy-MM-dd"),
            timelineEvent.Date.ToDisplayString(),
            TranslateDatePrecision(timelineEvent.Date.Precision),
            timelineEvent.Title,
            timelineEvent.InfoText,
            timelineEvent.Description,
            options.IncludeNotes ? timelineEvent.Notes : null,
            timelineEvent.Source,
            TranslatePriority(timelineEvent.Priority),
            TranslateEventStatus(timelineEvent.Status),
            timelineEvent.ColorHex,
            timelineEvent.Tags.Order(StringComparer.CurrentCultureIgnoreCase).ToArray(),
            deadline,
            attachments,
            webLinks,
            thumbnailDataUrl,
            searchText);
    }

    private async Task<string?> CreateThumbnailDataUrlAsync(
        ProjectWorkspace workspace,
        TimelineEvent timelineEvent,
        CancellationToken cancellationToken)
    {
        var primary = timelineEvent.Attachments.FirstOrDefault(attachment =>
            attachment.MediaType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
            ?? timelineEvent.Attachments.FirstOrDefault(attachment =>
                attachment.MediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase));
        if (primary is null)
        {
            return null;
        }

        try
        {
            var localPath = await attachmentFileService.GetValidatedLocalPathAsync(
                workspace,
                primary,
                cancellationToken).ConfigureAwait(false);
            byte[]? sourceData;
            if (primary.MediaType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
            {
                var preview = await pdfPreviewService.RenderPageAsync(
                    localPath,
                    primary.LinkedPdfPage ?? 1,
                    0.35,
                    cancellationToken).ConfigureAwait(false);
                sourceData = preview.PngData;
            }
            else
            {
                var fileInfo = new FileInfo(localPath);
                sourceData = fileInfo.Length <= MaximumImageSourceBytes
                    ? await File.ReadAllBytesAsync(localPath, cancellationToken).ConfigureAwait(false)
                    : null;
            }

            if (sourceData is not { Length: > 0 })
            {
                return null;
            }

            var thumbnail = await Task.Run(
                () => EncodeThumbnail(sourceData, cancellationToken),
                cancellationToken).ConfigureAwait(false);
            return thumbnail is null ? null : "data:image/jpeg;base64," + Convert.ToBase64String(thumbnail);
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or InvalidDataException or
            InvalidOperationException or ArgumentException)
        {
            return null;
        }
    }

    private static byte[]? EncodeThumbnail(byte[] sourceData, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var encodedSource = SKData.CreateCopy(sourceData);
        using var codec = SKCodec.Create(encodedSource);
        if (codec is null ||
            codec.Info.Width <= 0 ||
            codec.Info.Height <= 0 ||
            codec.Info.Width > MaximumImageEdge ||
            codec.Info.Height > MaximumImageEdge ||
            (long)codec.Info.Width * codec.Info.Height > MaximumImagePixels)
        {
            return null;
        }

        using var image = SKImage.FromEncodedData(encodedSource);
        if (image is null || image.Width <= 0 || image.Height <= 0)
        {
            return null;
        }

        var scale = Math.Min(
            1d,
            Math.Min(
                MaximumThumbnailWidth / (double)image.Width,
                MaximumThumbnailHeight / (double)image.Height));
        var width = Math.Max(1, (int)Math.Round(image.Width * scale));
        var height = Math.Max(1, (int)Math.Round(image.Height * scale));
        using var surface = SKSurface.Create(new SKImageInfo(
            width,
            height,
            SKColorType.Rgba8888,
            SKAlphaType.Premul));
        if (surface is null)
        {
            return null;
        }

        surface.Canvas.Clear(SKColors.White);
        surface.Canvas.DrawImage(image, new SKRect(0, 0, width, height));
        cancellationToken.ThrowIfCancellationRequested();
        using var snapshot = surface.Snapshot();
        using var encoded = snapshot.Encode(SKEncodedImageFormat.Jpeg, 80);
        return encoded?.ToArray();
    }

    private static (DateOnly Start, DateOnly End) GetFilterRange(EventDate date) => date.Precision switch
    {
        DatePrecision.Year =>
            (new DateOnly(date.StartYear, 1, 1), new DateOnly(date.StartYear, 12, 31)),
        DatePrecision.MonthAndYear =>
            (new DateOnly(date.StartYear, date.StartMonth!.Value, 1),
             new DateOnly(
                 date.StartYear,
                 date.StartMonth.Value,
                 DateTime.DaysInMonth(date.StartYear, date.StartMonth.Value))),
        DatePrecision.DateRange =>
            (new DateOnly(date.StartYear, date.StartMonth!.Value, date.StartDay!.Value),
             new DateOnly(date.EndYear!.Value, date.EndMonth!.Value, date.EndDay!.Value)),
        _ =>
            (new DateOnly(date.StartYear, date.StartMonth!.Value, date.StartDay!.Value),
             new DateOnly(date.StartYear, date.StartMonth.Value, date.StartDay.Value)),
    };

    private static void EnsureTargetOutsideWorkspace(ProjectWorkspace workspace, string fullTargetPath)
    {
        var workspaceRoot = Path.GetFullPath(workspace.WorkingDirectory)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
            Path.DirectorySeparatorChar;
        if (fullTargetPath.StartsWith(workspaceRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Die HTML-Datei darf nicht innerhalb des aktiven Projektarbeitsordners gespeichert werden. " +
                "Bitte wählen Sie einen anderen Zielordner.");
        }
    }

    private static string TranslatePriority(EventPriority priority) => priority switch
    {
        EventPriority.Low => "Niedrig",
        EventPriority.Normal => "Normal",
        EventPriority.High => "Hoch",
        EventPriority.Critical => "Kritisch",
        _ => priority.ToString(),
    };

    private static string TranslateDatePrecision(DatePrecision precision) => precision switch
    {
        DatePrecision.Year => "year",
        DatePrecision.MonthAndYear => "monthAndYear",
        DatePrecision.ExactDate => "exactDate",
        DatePrecision.ExactDateTime => "exactDateTime",
        DatePrecision.DateRange => "dateRange",
        _ => throw new ArgumentOutOfRangeException(nameof(precision)),
    };

    private static string TranslateEventStatus(EventStatus status) => status switch
    {
        EventStatus.Active => "Aktiv",
        EventStatus.Completed => "Abgeschlossen",
        EventStatus.Archived => "Archiviert",
        _ => status.ToString(),
    };

    private static string TranslateDeadlineStatus(DeadlineStatus status) => status switch
    {
        DeadlineStatus.Open => "Offen",
        DeadlineStatus.Completed => "Erledigt",
        DeadlineStatus.Cancelled => "Entfallen",
        _ => status.ToString(),
    };

    private sealed record HtmlProjectPayload(
        string Name,
        string? Subtitle,
        string? InfoText,
        string? Description,
        string? OverallStart,
        string? OverallEnd,
        DateTimeOffset ExportedAtUtc,
        string InitialOrientation,
        IReadOnlyList<HtmlEventPayload> Events);

    private sealed record HtmlEventPayload(
        Guid Id,
        string StartDate,
        string EndDate,
        string DateLabel,
        string DatePrecision,
        string Title,
        string? InfoText,
        string? Description,
        string? Notes,
        string? Source,
        string Priority,
        string Status,
        string Color,
        IReadOnlyList<string> Tags,
        HtmlDeadlinePayload? Deadline,
        IReadOnlyList<HtmlAttachmentPayload> Attachments,
        IReadOnlyList<HtmlWebLinkPayload> WebLinks,
        string? ThumbnailDataUrl,
        string SearchText);

    private sealed record HtmlDeadlinePayload(
        string DueDate,
        string DueDateLabel,
        string? DueTime,
        string? Label,
        string Status,
        string StatusLabel,
        string? ReminderNote);

    private sealed record HtmlAttachmentPayload(
        string FileName,
        string MediaType,
        long FileSize,
        int? LinkedPdfPage);

    private sealed record HtmlWebLinkPayload(string Address, string? Label);
}
