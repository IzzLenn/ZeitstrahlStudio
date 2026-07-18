using System.Buffers;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using ZeitstrahlStudio.Application;
using ZeitstrahlStudio.Shared;

namespace ZeitstrahlStudio.Infrastructure;

/// <summary>Erzeugt und importiert das sichere, versionierte `.zeitprojekt`-Format.</summary>
public sealed partial class ProjectArchiveService : IProjectArchiveService
{
    internal const string FormatIdentifier = "ZeitstrahlStudio.Project";
    internal const int CurrentFormatVersion = 1;
    internal const int MaximumEntryCount = 100_000;
    internal const long MaximumSingleFileBytes = 64L * 1024 * 1024 * 1024;
    internal const long MaximumTotalBytes = 512L * 1024 * 1024 * 1024;
    internal const long MaximumManifestBytes = 4L * 1024 * 1024;
    internal const long DiskSpaceReserveBytes = 64L * 1024 * 1024;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
    };

    private readonly IProjectRepository repository;
    private readonly TimeProvider timeProvider;

    /// <summary>Initialisiert die lokale Archivverwaltung.</summary>
    public ProjectArchiveService(IProjectRepository repository, TimeProvider? timeProvider = null)
    {
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public partial Task ExportAsync(
        string workingDirectory,
        string targetArchivePath,
        IProgress<FileOperationProgress>? progress,
        CancellationToken cancellationToken);

    /// <inheritdoc />
    public partial Task<OperationResult<ProjectImportResult>> ImportAsync(
        string archivePath,
        string targetWorkingDirectory,
        IProgress<FileOperationProgress>? progress,
        CancellationToken cancellationToken);

    private static async Task<ProjectArchiveManifest> ReadManifestAsync(
        ZipArchive archive,
        CancellationToken cancellationToken)
    {
        var manifestEntries = archive.Entries
            .Where(entry => string.Equals(entry.FullName, "manifest.json", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (manifestEntries.Length != 1 || manifestEntries[0].FullName != "manifest.json")
        {
            throw new InvalidDataException("Das Projektarchiv enthält keine eindeutige Manifestdatei 'manifest.json'.");
        }

        var entry = manifestEntries[0];
        if (entry.Length is <= 0 or > MaximumManifestBytes)
        {
            throw new InvalidDataException("Die Manifestdatei ist leer oder ungewöhnlich groß.");
        }

        await using var stream = entry.Open();
        var manifest = await JsonSerializer.DeserializeAsync<ProjectArchiveManifest>(
            stream,
            JsonOptions,
            cancellationToken).ConfigureAwait(false);
        return manifest ?? throw new InvalidDataException("Die Manifestdatei ist ungültig.");
    }

    private static ValidatedArchive ValidateArchiveStructure(
        ZipArchive archive,
        ProjectArchiveManifest manifest)
    {
        if (!string.Equals(manifest.Format, FormatIdentifier, StringComparison.Ordinal))
        {
            throw new InvalidDataException("Die Datei ist kein Projektarchiv von Zeitstrahl Studio.");
        }

        if (manifest.FormatVersion is < 1 or > CurrentFormatVersion ||
            manifest.MinimumReaderVersion > CurrentFormatVersion)
        {
            throw new InvalidDataException(
                $"Die Projektformat-Version {manifest.FormatVersion} wird von dieser Anwendung nicht unterstützt.");
        }

        if (manifest.ProjectId == Guid.Empty || string.IsNullOrWhiteSpace(manifest.ProjectName))
        {
            throw new InvalidDataException("Das Manifest enthält keine gültigen Projektinformationen.");
        }

        if (manifest.Files.Count is < 1 or > MaximumEntryCount)
        {
            throw new InvalidDataException("Das Projektarchiv enthält eine unzulässige Anzahl von Dateien.");
        }

        var manifestFiles = new Dictionary<string, ProjectArchiveFileEntry>(StringComparer.OrdinalIgnoreCase);
        long totalBytes = 0;
        foreach (var file in manifest.Files)
        {
            var path = ArchivePathValidator.ValidateAndNormalize(file.Path);
            if (file.Length is < 0 or > MaximumSingleFileBytes ||
                file.Sha256.Length != 64 ||
                !file.Sha256.All(Uri.IsHexDigit))
            {
                throw new InvalidDataException($"Der Manifesteintrag '{path}' enthält ungültige Dateidaten.");
            }

            if (!manifestFiles.TryAdd(path, file with { Path = path, Sha256 = file.Sha256.ToLowerInvariant() }))
            {
                throw new InvalidDataException($"Der Archivpfad '{path}' ist mehrfach im Manifest vorhanden.");
            }

            totalBytes = checked(totalBytes + file.Length);
            if (totalBytes > MaximumTotalBytes)
            {
                throw new InvalidDataException("Die deklarierte Gesamtgröße des Projektarchivs ist zu groß.");
            }
        }

        if (!manifestFiles.ContainsKey("project.db"))
        {
            throw new InvalidDataException("Das Projektarchiv enthält keine Projektdatenbank 'project.db'.");
        }

        var archiveFiles = new Dictionary<string, ZipArchiveEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in archive.Entries)
        {
            if (entry.FullName == "manifest.json")
            {
                continue;
            }

            if (entry.Name.Length == 0)
            {
                var directoryPath = entry.FullName.TrimEnd('/');
                if (directoryPath.Length > 0)
                {
                    ArchivePathValidator.ValidateAndNormalize(directoryPath);
                }

                continue;
            }

            var path = ArchivePathValidator.ValidateAndNormalize(entry.FullName);
            if (!archiveFiles.TryAdd(path, entry))
            {
                throw new InvalidDataException($"Der Archivpfad '{path}' ist mehrfach enthalten.");
            }

            if (!manifestFiles.TryGetValue(path, out var manifestFile) || manifestFile.Length != entry.Length)
            {
                throw new InvalidDataException($"Die Datei '{path}' fehlt im Manifest oder besitzt eine falsche Größe.");
            }

            if (entry.Length > 10L * 1024 * 1024 &&
                entry.CompressedLength > 0 &&
                entry.Length / (double)entry.CompressedLength > 1000)
            {
                throw new InvalidDataException($"Die Datei '{path}' weist ein unsicheres Kompressionsverhältnis auf.");
            }
        }

        if (archiveFiles.Count != manifestFiles.Count)
        {
            var missing = manifestFiles.Keys.First(path => !archiveFiles.ContainsKey(path));
            throw new InvalidDataException($"Die im Manifest genannte Datei '{missing}' fehlt im Archiv.");
        }

        return new ValidatedArchive(manifest, archiveFiles, totalBytes);
    }

    private static async Task<(long Length, string Sha256)> CopyAndHashAsync(
        Stream source,
        Stream? destination,
        CancellationToken cancellationToken)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(128 * 1024);
        try
        {
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            long length = 0;
            while (true)
            {
                var read = await source.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)
                    .ConfigureAwait(false);
                if (read == 0)
                {
                    break;
                }

                hash.AppendData(buffer, 0, read);
                if (destination is not null)
                {
                    await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                }

                length = checked(length + read);
                if (length > MaximumSingleFileBytes)
                {
                    throw new InvalidDataException("Eine Datei überschreitet die zulässige Archivgröße.");
                }
            }

            return (length, Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant());
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static void EnsureDiskSpace(string targetPath, long requiredBytes)
    {
        var root = Path.GetPathRoot(Path.GetFullPath(targetPath));
        if (string.IsNullOrWhiteSpace(root))
        {
            throw new IOException("Der freie Speicherplatz des Ziellaufwerks konnte nicht bestimmt werden.");
        }

        var drive = new DriveInfo(root);
        if (drive.AvailableFreeSpace < checked(requiredBytes + DiskSpaceReserveBytes))
        {
            throw new IOException(
                "Auf dem Ziellaufwerk ist nicht genügend freier Speicherplatz für das Projektarchiv vorhanden.");
        }
    }

    private static ApplicationError ToApplicationError(string operation, Exception exception) => new(
        "ProjectArchive.Invalid",
        $"Das Projektarchiv konnte nicht {operation} werden. " +
        "Die Quelldatei und vorhandene Projekte wurden nicht verändert.",
        exception.ToString());

    private sealed record ValidatedArchive(
        ProjectArchiveManifest Manifest,
        IReadOnlyDictionary<string, ZipArchiveEntry> Entries,
        long TotalBytes);
}
