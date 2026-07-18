using System.Text.Json;
using ZeitstrahlStudio.Application;

namespace ZeitstrahlStudio.Infrastructure;

/// <summary>Schreibt größenbegrenzt rotierende JSON-Lines-Protokolle ausschließlich lokal.</summary>
public sealed class JsonLinesLocalLogService : ILocalLogService, IDisposable
{
    private const int MaximumMessageCharacters = 16_384;
    private const int MaximumTechnicalDetailCharacters = 65_536;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly string logFilePath;
    private readonly long maximumFileBytes;
    private readonly int maximumFiles;
    private readonly SemaphoreSlim gate = new(1, 1);

    /// <summary>Initialisiert den lokalen Logdienst.</summary>
    public JsonLinesLocalLogService(
        string? logDirectory = null,
        long maximumFileBytes = 5L * 1024 * 1024,
        int maximumFiles = 5)
    {
        if (maximumFileBytes is < 64 * 1024 or > 100L * 1024 * 1024)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumFileBytes));
        }

        if (maximumFiles is < 2 or > 20)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumFiles));
        }

        var directory = Path.GetFullPath(logDirectory ?? GetDefaultLogDirectory());
        logFilePath = Path.Combine(directory, "application.log.jsonl");
        this.maximumFileBytes = maximumFileBytes;
        this.maximumFiles = maximumFiles;
    }

    /// <inheritdoc />
    public async Task WriteAsync(LocalLogEntry entry, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ValidateEntry(entry);
        var normalized = entry with
        {
            Category = entry.Category.Trim(),
            EventName = entry.EventName.Trim(),
            Message = Truncate(entry.Message, MaximumMessageCharacters),
            TechnicalDetails = entry.TechnicalDetails is null
                ? null
                : Truncate(entry.TechnicalDetails, MaximumTechnicalDetailCharacters),
        };
        var payload = JsonSerializer.SerializeToUtf8Bytes(normalized, JsonOptions);

        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(logFilePath)!);
            RotateIfNecessary(payload.Length + 1);
            await using var stream = new FileStream(
                logFilePath,
                FileMode.Append,
                FileAccess.Write,
                FileShare.Read,
                32 * 1024,
                FileOptions.Asynchronous | FileOptions.WriteThrough);
            await stream.WriteAsync(payload, cancellationToken).ConfigureAwait(false);
            await stream.WriteAsync("\n"u8.ToArray(), cancellationToken).ConfigureAwait(false);
            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<LocalLogEntry>> ReadRecentAsync(
        int maximumEntries,
        CancellationToken cancellationToken)
    {
        if (maximumEntries is < 1 or > 10_000)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumEntries));
        }

        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var queue = new Queue<LocalLogEntry>(maximumEntries);
            foreach (var path in EnumerateLogFilesOldestFirst())
            {
                if (!File.Exists(path))
                {
                    continue;
                }

                await using var stream = new FileStream(
                    path,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite,
                    32 * 1024,
                    FileOptions.Asynchronous | FileOptions.SequentialScan);
                using var reader = new StreamReader(stream);
                while (await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false) is { } line)
                {
                    if (TryDeserialize(line) is not { } entry)
                    {
                        continue;
                    }

                    if (queue.Count == maximumEntries)
                    {
                        queue.Dequeue();
                    }

                    queue.Enqueue(entry);
                }
            }

            return queue.Reverse().ToArray();
        }
        finally
        {
            gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task ExportAsync(string targetPath, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(targetPath))
        {
            throw new ArgumentException("Der Exportpfad für das Protokoll darf nicht leer sein.", nameof(targetPath));
        }

        var fullTargetPath = Path.GetFullPath(targetPath);
        if (EnumerateLogFilesOldestFirst().Any(path =>
            string.Equals(path, fullTargetPath, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Das aktive Protokoll kann nicht mit seinem eigenen Export überschrieben werden.");
        }

        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var targetDirectory = Path.GetDirectoryName(fullTargetPath)!;
            Directory.CreateDirectory(targetDirectory);
            var temporaryPath = fullTargetPath + $".{Guid.NewGuid():N}.tmp";
            try
            {
                await using (var destination = new FileStream(
                    temporaryPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    32 * 1024,
                    FileOptions.Asynchronous | FileOptions.WriteThrough))
                {
                    foreach (var sourcePath in EnumerateLogFilesOldestFirst())
                    {
                        if (!File.Exists(sourcePath))
                        {
                            continue;
                        }

                        await using var source = new FileStream(
                            sourcePath,
                            FileMode.Open,
                            FileAccess.Read,
                            FileShare.ReadWrite,
                            32 * 1024,
                            FileOptions.Asynchronous | FileOptions.SequentialScan);
                        await source.CopyToAsync(destination, cancellationToken).ConfigureAwait(false);
                    }

                    await destination.FlushAsync(cancellationToken).ConfigureAwait(false);
                    destination.Flush(flushToDisk: true);
                }

                if (File.Exists(fullTargetPath))
                {
                    File.Replace(temporaryPath, fullTargetPath, destinationBackupFileName: null, ignoreMetadataErrors: true);
                }
                else
                {
                    File.Move(temporaryPath, fullTargetPath);
                }
            }
            finally
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
        }
        finally
        {
            gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task ClearAsync(CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            foreach (var path in EnumerateLogFilesOldestFirst())
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }
        finally
        {
            gate.Release();
        }
    }

    /// <inheritdoc />
    public void Dispose() => gate.Dispose();

    private void RotateIfNecessary(int pendingBytes)
    {
        if (!File.Exists(logFilePath) || new FileInfo(logFilePath).Length + pendingBytes <= maximumFileBytes)
        {
            return;
        }

        var oldestPath = GetRotatedPath(maximumFiles - 1);
        if (File.Exists(oldestPath))
        {
            File.Delete(oldestPath);
        }

        for (var index = maximumFiles - 2; index >= 1; index--)
        {
            var source = GetRotatedPath(index);
            if (File.Exists(source))
            {
                File.Move(source, GetRotatedPath(index + 1));
            }
        }

        File.Move(logFilePath, GetRotatedPath(1));
    }

    private IEnumerable<string> EnumerateLogFilesOldestFirst()
    {
        for (var index = maximumFiles - 1; index >= 1; index--)
        {
            yield return GetRotatedPath(index);
        }

        yield return logFilePath;
    }

    private string GetRotatedPath(int index) => logFilePath + $".{index}";

    private static LocalLogEntry? TryDeserialize(string line)
    {
        try
        {
            return JsonSerializer.Deserialize<LocalLogEntry>(line, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static void ValidateEntry(LocalLogEntry entry)
    {
        if (entry.TimestampUtc.Offset != TimeSpan.Zero ||
            string.IsNullOrWhiteSpace(entry.Category) ||
            string.IsNullOrWhiteSpace(entry.EventName) ||
            string.IsNullOrWhiteSpace(entry.Message))
        {
            throw new ArgumentException("Der lokale Protokolleintrag ist unvollständig.", nameof(entry));
        }
    }

    private static string Truncate(string value, int maximumCharacters) =>
        value.Length <= maximumCharacters ? value : value[..maximumCharacters];

    private static string GetDefaultLogDirectory() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Zeitstrahl Studio",
        "Logs");
}
