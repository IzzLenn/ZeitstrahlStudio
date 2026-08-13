using System.Collections.Concurrent;
using System.Data.Common;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using SkiaSharp;
using ZeitstrahlStudio.Application;
using ZeitstrahlStudio.Domain;

namespace ZeitstrahlStudio.Export;

/// <summary>Erzeugt eine offlinefähige HTML-Momentaufnahme als Einzeldatei oder geprüftes Dokumentpaket.</summary>
public sealed class StandaloneHtmlExportService : IHtmlExportService
{
    private const int CopyBufferSize = 128 * 1024;
    private const long MaximumImageSourceBytes = 50L * 1024 * 1024;
    private const int MaximumImageEdge = 8_000;
    private const long MaximumImagePixels = 24_000_000;
    private const int MaximumThumbnailWidth = 360;
    private const int MaximumThumbnailHeight = 240;
    private const string PackageHtmlPath = "index.html";
    private const string PackageReadmePath = "LESMICH.txt";
    private const string PackageReadme = """
        Zeitstrahl Studio – HTML-Exportpaket

        1. Entpacken Sie dieses ZIP-Archiv vollständig in einen lokalen Ordner.
        2. Öffnen Sie anschließend index.html in einem modernen Browser.
        3. Die Dokumentverweise in der HTML-Datei öffnen die mitgelieferten Kopien aus dem Ordner Dokumente.

        Das Paket arbeitet vollständig lokal und sendet keine Daten an externe Dienste.
        """;
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
        var validExtension = options.IncludeDocumentCopies
            ? extension.Equals(".zip", StringComparison.OrdinalIgnoreCase)
            : extension.Equals(".html", StringComparison.OrdinalIgnoreCase) ||
              extension.Equals(".htm", StringComparison.OrdinalIgnoreCase);
        if (!validExtension)
        {
            throw new ArgumentException(
                options.IncludeDocumentCopies
                    ? "Das HTML-Exportpaket mit Dokumentkopien muss die Endung .zip besitzen."
                    : "Die Zieldatei des Standalone-Exports muss die Endung .html oder .htm besitzen.",
                nameof(targetPath));
        }

        EnsureTargetOutsideWorkspace(workspace, fullTargetPath);
        var directory = Path.GetDirectoryName(fullTargetPath);
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException(
                "Der Zielordner für den HTML-Export wurde nicht gefunden.");
        }

        var documentExports = options.IncludeDocumentCopies
            ? CreateDocumentExports(workspace.Project)
            : [];
        var documentPaths = documentExports.ToDictionary(
            item => item.Attachment.Id,
            item => item.PackagePath);
        var payload = await CreatePayloadAsync(
            workspace,
            options,
            documentPaths,
            cancellationToken).ConfigureAwait(false);
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
            if (options.IncludeDocumentCopies)
            {
                await WritePackageAsync(
                    workspace,
                    html,
                    documentExports,
                    temporaryPath,
                    cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await File.WriteAllTextAsync(
                    temporaryPath,
                    html,
                    new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                    cancellationToken).ConfigureAwait(false);
            }

            cancellationToken.ThrowIfCancellationRequested();
            ReplaceAtomically(temporaryPath, fullTargetPath);
        }
        catch (InvalidDataException)
        {
            throw;
        }
        catch (UnauthorizedAccessException exception)
        {
            throw new UnauthorizedAccessException(
                "Der HTML-Export konnte nicht gespeichert werden. Bitte prüfen Sie die Zugriffsrechte des Zielordners.",
                exception);
        }
        catch (IOException exception)
        {
            throw new IOException(
                "Der HTML-Export konnte nicht vollständig gespeichert werden. " +
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
        IReadOnlyDictionary<Guid, string> documentPaths,
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
                    documentPaths,
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
            options.ShowSnapshotBanner,
            chronologicalEvents.Select(timelineEvent => results[timelineEvent.Id]).ToArray());
    }

    private async Task<HtmlEventPayload> CreateEventPayloadAsync(
        ProjectWorkspace workspace,
        TimelineEvent timelineEvent,
        HtmlExportOptions options,
        IReadOnlyDictionary<Guid, string> documentPaths,
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

        var primaryAttachment = TimelineThumbnailSelection.SelectPrimary(timelineEvent);
        var thumbnailDataUrl = options.IncludeThumbnails && primaryAttachment is not null
            ? await CreateThumbnailDataUrlAsync(
                workspace,
                primaryAttachment,
                cancellationToken).ConfigureAwait(false)
            : null;
        var thumbnailDocumentPath = thumbnailDataUrl is not null &&
                                    primaryAttachment is not null &&
                                    documentPaths.TryGetValue(primaryAttachment.Id, out var primaryPath)
            ? primaryPath
            : null;
        var attachments = timelineEvent.Attachments
            .Select(attachment => new HtmlAttachmentPayload(
                attachment.OriginalFileName,
                attachment.MediaType,
                attachment.FileSize,
                attachment.LinkedPdfPage,
                documentPaths.TryGetValue(attachment.Id, out var documentPath) ? documentPath : null))
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
            thumbnailDocumentPath,
            searchText);
    }

    private async Task<string?> CreateThumbnailDataUrlAsync(
        ProjectWorkspace workspace,
        Attachment primary,
        CancellationToken cancellationToken)
    {
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
                "Der HTML-Export darf nicht innerhalb des aktiven Projektarbeitsordners gespeichert werden. " +
                "Bitte wählen Sie einen anderen Zielordner.");
        }
    }

    private static IReadOnlyList<HtmlDocumentExport> CreateDocumentExports(TimelineProject project)
    {
        var exports = new List<HtmlDocumentExport>();
        var knownIds = new HashSet<Guid>();
        foreach (var timelineEvent in project.GetChronologicalEvents())
        {
            foreach (var attachment in timelineEvent.Attachments)
            {
                if (!knownIds.Add(attachment.Id))
                {
                    throw new InvalidDataException(
                        $"Die Anhangs-ID '{attachment.Id}' ist im Projekt mehrfach vergeben.");
                }

                exports.Add(new HtmlDocumentExport(
                    attachment,
                    $"Dokumente/{attachment.Id:N}{GetSafeDocumentExtension(attachment)}"));
            }
        }

        return exports;
    }

    private async Task WritePackageAsync(
        ProjectWorkspace workspace,
        string html,
        IReadOnlyList<HtmlDocumentExport> documentExports,
        string temporaryPath,
        CancellationToken cancellationToken)
    {
        await using (var output = new FileStream(
            temporaryPath,
            FileMode.CreateNew,
            FileAccess.ReadWrite,
            FileShare.None,
            CopyBufferSize,
            FileOptions.Asynchronous | FileOptions.SequentialScan))
        {
            using (var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
            {
                await WriteTextEntryAsync(
                    archive,
                    PackageHtmlPath,
                    html,
                    CompressionLevel.Optimal,
                    cancellationToken).ConfigureAwait(false);
                await WriteTextEntryAsync(
                    archive,
                    PackageReadmePath,
                    PackageReadme,
                    CompressionLevel.Optimal,
                    cancellationToken).ConfigureAwait(false);

                foreach (var documentExport in documentExports)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    try
                    {
                        var localPath = await attachmentFileService.GetValidatedLocalPathAsync(
                            workspace,
                            documentExport.Attachment,
                            cancellationToken).ConfigureAwait(false);
                        var entry = archive.CreateEntry(
                            documentExport.PackagePath,
                            CompressionLevel.NoCompression);
                        await using var source = new FileStream(
                            localPath,
                            FileMode.Open,
                            FileAccess.Read,
                            FileShare.Read,
                            CopyBufferSize,
                            FileOptions.Asynchronous | FileOptions.SequentialScan);
                        await using var destination = entry.Open();
                        var copied = await CopyAndHashAsync(
                            source,
                            destination,
                            cancellationToken).ConfigureAwait(false);
                        if (copied.Length != documentExport.Attachment.FileSize ||
                            !string.Equals(
                                copied.Sha256,
                                documentExport.Attachment.Sha256,
                                StringComparison.OrdinalIgnoreCase))
                        {
                            throw new InvalidDataException(
                                $"Die Projektkopie von „{documentExport.Attachment.OriginalFileName}“ " +
                                "stimmt beim Kopieren nicht mit ihren gespeicherten Metadaten überein.");
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception exception) when (
                        exception is IOException or UnauthorizedAccessException or
                        InvalidOperationException or ArgumentException)
                    {
                        throw new InvalidDataException(
                            $"Die Projektkopie von „{documentExport.Attachment.OriginalFileName}“ " +
                            $"konnte nicht in das HTML-Exportpaket übernommen werden: {exception.Message}",
                            exception);
                    }
                }
            }

            await output.FlushAsync(cancellationToken).ConfigureAwait(false);
            output.Flush(flushToDisk: true);
        }

        await VerifyPackageAsync(
            temporaryPath,
            html,
            documentExports,
            cancellationToken).ConfigureAwait(false);
    }

    private static async Task WriteTextEntryAsync(
        ZipArchive archive,
        string entryPath,
        string text,
        CompressionLevel compressionLevel,
        CancellationToken cancellationToken)
    {
        var entry = archive.CreateEntry(entryPath, compressionLevel);
        await using var destination = entry.Open();
        var data = Encoding.UTF8.GetBytes(text);
        await destination.WriteAsync(data, cancellationToken).ConfigureAwait(false);
    }

    private static async Task VerifyPackageAsync(
        string packagePath,
        string html,
        IReadOnlyList<HtmlDocumentExport> documentExports,
        CancellationToken cancellationToken)
    {
        await using var input = new FileStream(
            packagePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            CopyBufferSize,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        using var archive = new ZipArchive(input, ZipArchiveMode.Read, leaveOpen: false);
        var expectedPaths = new HashSet<string>(StringComparer.Ordinal)
        {
            PackageHtmlPath,
            PackageReadmePath,
        };
        foreach (var documentExport in documentExports)
        {
            expectedPaths.Add(documentExport.PackagePath);
        }

        var actualPaths = archive.Entries.Select(entry => entry.FullName).ToArray();
        if (actualPaths.Length != expectedPaths.Count ||
            actualPaths.Distinct(StringComparer.Ordinal).Count() != actualPaths.Length ||
            actualPaths.Any(path => !expectedPaths.Contains(path)))
        {
            throw new InvalidDataException("Das erzeugte HTML-Exportpaket besitzt eine unerwartete Dateistruktur.");
        }

        await VerifyTextEntryAsync(
            archive,
            PackageHtmlPath,
            html,
            "Die index.html des erzeugten HTML-Exportpakets ist unvollständig oder beschädigt.",
            cancellationToken).ConfigureAwait(false);
        await VerifyTextEntryAsync(
            archive,
            PackageReadmePath,
            PackageReadme,
            "Die LESMICH.txt des erzeugten HTML-Exportpakets ist unvollständig oder beschädigt.",
            cancellationToken).ConfigureAwait(false);

        foreach (var documentExport in documentExports)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var entry = archive.GetEntry(documentExport.PackagePath)
                ?? throw new InvalidDataException(
                    $"Das erzeugte HTML-Exportpaket enthält „{documentExport.Attachment.OriginalFileName}“ nicht.");
            if (entry.Length != documentExport.Attachment.FileSize)
            {
                throw new InvalidDataException(
                    $"Die Größe von „{documentExport.Attachment.OriginalFileName}“ im HTML-Exportpaket stimmt nicht.");
            }

            await using var entryStream = entry.Open();
            var hash = await SHA256.HashDataAsync(entryStream, cancellationToken).ConfigureAwait(false);
            if (!string.Equals(
                    Convert.ToHexString(hash),
                    documentExport.Attachment.Sha256,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException(
                    $"Die Prüfsumme von „{documentExport.Attachment.OriginalFileName}“ im HTML-Exportpaket stimmt nicht.");
            }
        }
    }

    private static async Task VerifyTextEntryAsync(
        ZipArchive archive,
        string entryPath,
        string expectedText,
        string errorMessage,
        CancellationToken cancellationToken)
    {
        var entry = archive.GetEntry(entryPath)
            ?? throw new InvalidDataException(errorMessage);
        var expectedBytes = Encoding.UTF8.GetBytes(expectedText);
        if (entry.Length != expectedBytes.Length)
        {
            throw new InvalidDataException(errorMessage);
        }

        await using var entryStream = entry.Open();
        var actualHash = await SHA256.HashDataAsync(entryStream, cancellationToken).ConfigureAwait(false);
        var expectedHash = SHA256.HashData(expectedBytes);
        if (!CryptographicOperations.FixedTimeEquals(actualHash, expectedHash))
        {
            throw new InvalidDataException(errorMessage);
        }
    }
    private static async Task<(long Length, string Sha256)> CopyAndHashAsync(
        Stream source,
        Stream destination,
        CancellationToken cancellationToken)
    {
        var buffer = new byte[CopyBufferSize];
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        long length = 0;
        while (true)
        {
            var read = await source.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                break;
            }

            hash.AppendData(buffer, 0, read);
            await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
            length = checked(length + read);
        }

        return (length, Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant());
    }

    private static string GetSafeDocumentExtension(Attachment attachment)
    {
        foreach (var candidate in new[] { attachment.OriginalFileName, attachment.ProjectRelativePath })
        {
            string extension;
            try
            {
                extension = Path.GetExtension(candidate);
            }
            catch (ArgumentException)
            {
                continue;
            }

            if (extension.Length is >= 2 and <= 20 &&
                extension[0] == '.' &&
                extension[1..].All(char.IsAsciiLetterOrDigit))
            {
                return extension.ToLowerInvariant();
            }
        }

        return ".bin";
    }

    private static void ReplaceAtomically(string temporaryPath, string targetPath)
    {
        if (!File.Exists(targetPath))
        {
            File.Move(temporaryPath, targetPath);
            return;
        }

        var backupPath = targetPath + $".{Guid.NewGuid():N}.previous";
        File.Replace(temporaryPath, targetPath, backupPath, ignoreMetadataErrors: true);
        try
        {
            File.Delete(backupPath);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Der gültige vorherige Export bleibt bei einer nicht möglichen Bereinigung wiederherstellbar.
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
        bool ShowSnapshotBanner,
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
        string? ThumbnailDocumentPath,
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
        int? LinkedPdfPage,
        string? DocumentPath);

    private sealed record HtmlWebLinkPayload(string Address, string? Label);

    private sealed record HtmlDocumentExport(Attachment Attachment, string PackagePath);
}
