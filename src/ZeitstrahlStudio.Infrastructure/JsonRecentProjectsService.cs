using System.Text.Json;
using ZeitstrahlStudio.Application;

namespace ZeitstrahlStudio.Infrastructure;

/// <summary>Speichert höchstens 20 zuletzt geöffnete Projekte in einer lokalen JSON-Datei.</summary>
public sealed class JsonRecentProjectsService : IRecentProjectsService, IDisposable
{
    private const int MaximumRecentProjects = 20;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
    };

    private readonly string stateFilePath;
    private readonly TimeProvider timeProvider;
    private readonly SemaphoreSlim gate = new(1, 1);

    /// <summary>Initialisiert den lokalen Anwendungszustand.</summary>
    public JsonRecentProjectsService(string? stateFilePath = null, TimeProvider? timeProvider = null)
    {
        this.stateFilePath = Path.GetFullPath(stateFilePath ?? GetDefaultStateFilePath());
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RecentProject>> GetAsync(CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var state = await ReadStateAsync(cancellationToken).ConfigureAwait(false);
            return state.RecentProjects
                .OrderByDescending(item => item.LastOpenedAtUtc)
                .Take(MaximumRecentProjects)
                .Select(item => new RecentProject(
                    item.ProjectName,
                    item.ArchivePath,
                    item.LastOpenedAtUtc,
                    File.Exists(item.ArchivePath)))
                .ToArray();
        }
        finally
        {
            gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task RecordOpenedAsync(ProjectWorkspace workspace, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        if (workspace.ArchivePath is null)
        {
            throw new InvalidOperationException("Ein Projekt ohne Archivpfad kann nicht als zuletzt verwendet gespeichert werden.");
        }

        var archivePath = ValidateArchivePath(workspace.ArchivePath);
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var state = await ReadStateAsync(cancellationToken).ConfigureAwait(false);
            var projects = state.RecentProjects
                .Where(item => !string.Equals(item.ArchivePath, archivePath, StringComparison.OrdinalIgnoreCase))
                .Prepend(new RecentProjectState(workspace.Project.Name, archivePath, timeProvider.GetUtcNow()))
                .Take(MaximumRecentProjects)
                .ToArray();
            await WriteStateAsync(new ApplicationState(1, projects), cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task RemoveAsync(string archivePath, CancellationToken cancellationToken)
    {
        var fullPath = ValidateArchivePath(archivePath);
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var state = await ReadStateAsync(cancellationToken).ConfigureAwait(false);
            var projects = state.RecentProjects
                .Where(item => !string.Equals(item.ArchivePath, fullPath, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            await WriteStateAsync(new ApplicationState(1, projects), cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task RemoveMissingAsync(CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var state = await ReadStateAsync(cancellationToken).ConfigureAwait(false);
            var projects = state.RecentProjects.Where(item => File.Exists(item.ArchivePath)).ToArray();
            await WriteStateAsync(new ApplicationState(1, projects), cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            gate.Release();
        }
    }

    /// <inheritdoc />
    public void Dispose() => gate.Dispose();

    private async Task<ApplicationState> ReadStateAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(stateFilePath))
        {
            return new ApplicationState(1, Array.Empty<RecentProjectState>());
        }

        await using var stream = new FileStream(
            stateFilePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            32 * 1024,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        ApplicationState? state;
        try
        {
            state = await JsonSerializer.DeserializeAsync<ApplicationState>(stream, JsonOptions, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException(
                "Die lokale Liste zuletzt verwendeter Projekte ist beschädigt. Die Projektarchive selbst sind nicht betroffen.",
                exception);
        }

        if (state is null || state.Version != 1 || state.RecentProjects.Count > 100)
        {
            throw new InvalidDataException("Die lokale Liste zuletzt verwendeter Projekte besitzt ein unbekanntes Format.");
        }

        foreach (var item in state.RecentProjects)
        {
            _ = ValidateArchivePath(item.ArchivePath);
            if (string.IsNullOrWhiteSpace(item.ProjectName) || item.LastOpenedAtUtc.Offset != TimeSpan.Zero)
            {
                throw new InvalidDataException("Ein Eintrag der zuletzt verwendeten Projekte ist ungültig.");
            }
        }

        return state;
    }

    private async Task WriteStateAsync(ApplicationState state, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(stateFilePath)!;
        Directory.CreateDirectory(directory);
        var temporaryPath = stateFilePath + $".{Guid.NewGuid():N}.tmp";
        try
        {
            await using (var stream = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                32 * 1024,
                FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await JsonSerializer.SerializeAsync(stream, state, JsonOptions, cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                stream.Flush(flushToDisk: true);
            }

            if (File.Exists(stateFilePath))
            {
                File.Replace(temporaryPath, stateFilePath, destinationBackupFileName: null, ignoreMetadataErrors: true);
            }
            else
            {
                File.Move(temporaryPath, stateFilePath);
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

    private static string ValidateArchivePath(string archivePath)
    {
        if (string.IsNullOrWhiteSpace(archivePath) ||
            !string.Equals(Path.GetExtension(archivePath), ".zeitprojekt", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("Ein zuletzt verwendetes Projekt besitzt keinen gültigen Archivpfad.");
        }

        return Path.GetFullPath(archivePath);
    }

    private static string GetDefaultStateFilePath() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Zeitstrahl Studio",
        "application-state.json");

    private sealed record ApplicationState(int Version, IReadOnlyList<RecentProjectState> RecentProjects);
    private sealed record RecentProjectState(
        string ProjectName,
        string ArchivePath,
        DateTimeOffset LastOpenedAtUtc);
}
