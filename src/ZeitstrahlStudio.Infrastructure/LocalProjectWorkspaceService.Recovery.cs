using System.Diagnostics;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using ZeitstrahlStudio.Application;

namespace ZeitstrahlStudio.Infrastructure;

public sealed partial class LocalProjectWorkspaceService
{
    private static readonly JsonSerializerOptions RecoveryJsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
    };

    /// <inheritdoc />
    public async Task<IReadOnlyList<RecoveryCandidate>> FindAsync(CancellationToken cancellationToken)
    {
        if (!Directory.Exists(workspaceRoot))
        {
            return Array.Empty<RecoveryCandidate>();
        }

        var candidates = new List<RecoveryCandidate>();
        foreach (var directory in Directory.EnumerateDirectories(workspaceRoot, "*", SearchOption.TopDirectoryOnly))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if ((File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0)
            {
                continue;
            }

            var databasePath = Path.Combine(directory, "project.db");
            if (!File.Exists(databasePath))
            {
                continue;
            }

            var marker = await TryReadRecoveryMarkerAsync(directory, cancellationToken).ConfigureAwait(false);
            if (marker is not null && IsProcessStillActive(marker.ProcessId, marker.ProcessStartedAtUtc))
            {
                continue;
            }

            try
            {
                var project = await repository.LoadAsync(databasePath, cancellationToken).ConfigureAwait(false);
                candidates.Add(new RecoveryCandidate(
                    project.Id,
                    project.Name,
                    directory,
                    marker?.ArchivePath,
                    marker?.LastUpdatedAtUtc ?? new DateTimeOffset(File.GetLastWriteTimeUtc(databasePath))));
            }
            catch (InvalidDataException)
            {
                // Beschädigte Arbeitskopien werden nicht als wiederherstellbar angeboten.
            }
            catch (SqliteException)
            {
                // Beschädigte Arbeitskopien werden nicht als wiederherstellbar angeboten.
            }
        }

        return candidates.OrderByDescending(candidate => candidate.LastUpdatedAtUtc).ToArray();
    }

    /// <inheritdoc />
    public async Task<ProjectWorkspace> RecoverAsync(
        RecoveryCandidate candidate,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        EnsureManagedWorkspace(candidate.WorkingDirectory);
        var project = await repository.LoadAsync(
            Path.Combine(candidate.WorkingDirectory, "project.db"),
            cancellationToken).ConfigureAwait(false);
        if (project.Id != candidate.ProjectId)
        {
            throw new InvalidDataException("Die Arbeitskopie gehört nicht zum ausgewählten Wiederherstellungseintrag.");
        }

        var workspace = new ProjectWorkspace(
            project,
            candidate.WorkingDirectory,
            candidate.ArchivePath,
            HasUnsavedChanges: true);
        await WriteRecoveryMarkerAsync(workspace, cancellationToken).ConfigureAwait(false);
        return workspace;
    }

    /// <inheritdoc />
    public async Task DiscardAsync(RecoveryCandidate candidate, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        EnsureManagedWorkspace(candidate.WorkingDirectory);
        cancellationToken.ThrowIfCancellationRequested();
        SqliteConnection.ClearAllPools();
        await Task.Run(
            () => Directory.Delete(candidate.WorkingDirectory, recursive: true),
            cancellationToken).ConfigureAwait(false);
    }

    private async Task WriteRecoveryMarkerAsync(
        ProjectWorkspace workspace,
        CancellationToken cancellationToken)
    {
        var metadataDirectory = Path.Combine(workspace.WorkingDirectory, "metadata");
        Directory.CreateDirectory(metadataDirectory);
        var markerPath = Path.Combine(metadataDirectory, "session.json");
        var temporaryPath = markerPath + $".{Guid.NewGuid():N}.tmp";
        using var process = Process.GetCurrentProcess();
        var marker = new RecoveryMarker(
            1,
            workspace.Project.Id,
            workspace.Project.Name,
            workspace.ArchivePath,
            timeProvider.GetUtcNow(),
            Environment.ProcessId,
            new DateTimeOffset(process.StartTime.ToUniversalTime()));
        try
        {
            await using (var stream = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                16 * 1024,
                FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await JsonSerializer.SerializeAsync(
                    stream,
                    marker,
                    RecoveryJsonOptions,
                    cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                stream.Flush(flushToDisk: true);
            }

            if (File.Exists(markerPath))
            {
                File.Replace(temporaryPath, markerPath, destinationBackupFileName: null, ignoreMetadataErrors: true);
            }
            else
            {
                File.Move(temporaryPath, markerPath);
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

    private static async Task<RecoveryMarker?> TryReadRecoveryMarkerAsync(
        string workingDirectory,
        CancellationToken cancellationToken)
    {
        var markerPath = Path.Combine(workingDirectory, "metadata", "session.json");
        if (!File.Exists(markerPath))
        {
            return null;
        }

        try
        {
            await using var stream = new FileStream(
                markerPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                16 * 1024,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            var marker = await JsonSerializer.DeserializeAsync<RecoveryMarker>(
                stream,
                RecoveryJsonOptions,
                cancellationToken).ConfigureAwait(false);
            if (marker is null ||
                marker.Version != 1 ||
                marker.ProjectId == Guid.Empty ||
                string.IsNullOrWhiteSpace(marker.ProjectName) ||
                marker.LastUpdatedAtUtc.Offset != TimeSpan.Zero ||
                marker.ProcessStartedAtUtc.Offset != TimeSpan.Zero)
            {
                return null;
            }

            return marker;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static bool IsProcessStillActive(int processId, DateTimeOffset expectedStartUtc)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            var actualStartUtc = new DateTimeOffset(process.StartTime.ToUniversalTime());
            return !process.HasExited && Math.Abs((actualStartUtc - expectedStartUtc).TotalSeconds) < 2;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return false;
        }
    }

    private sealed record RecoveryMarker(
        int Version,
        Guid ProjectId,
        string ProjectName,
        string? ArchivePath,
        DateTimeOffset LastUpdatedAtUtc,
        int ProcessId,
        DateTimeOffset ProcessStartedAtUtc);
}
