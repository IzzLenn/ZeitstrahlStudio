using ZeitstrahlStudio.Application;
using ZeitstrahlStudio.Domain;
using ZeitstrahlStudio.Shared;

namespace ZeitstrahlStudio.Infrastructure;

/// <summary>Führt zeitgesteuerte Projektspeicherungen seriell und abbrechbar aus.</summary>
public sealed class ProjectAutosaveService : IProjectAutosaveService, IDisposable
{
    private readonly IProjectWorkspaceService workspaceService;
    private readonly IBackupService? backupService;
    private readonly TimeProvider timeProvider;
    private readonly SemaphoreSlim runGate = new(1, 1);

    /// <summary>Initialisiert den Autosave-Koordinator.</summary>
    public ProjectAutosaveService(
        IProjectWorkspaceService workspaceService,
        TimeProvider? timeProvider = null)
    {
        this.workspaceService = workspaceService ?? throw new ArgumentNullException(nameof(workspaceService));
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>Initialisiert Autosave einschließlich fälliger rotierender Sicherungen.</summary>
    public ProjectAutosaveService(
        IProjectWorkspaceService workspaceService,
        IBackupService backupService,
        TimeProvider? timeProvider = null)
        : this(workspaceService, timeProvider)
    {
        this.backupService = backupService ?? throw new ArgumentNullException(nameof(backupService));
    }

    /// <inheritdoc />
    public async Task RunAsync(
        Func<ProjectWorkspace?> currentWorkspace,
        Action<ProjectWorkspace> workspaceUpdated,
        TimeSpan interval,
        IProgress<AutosaveStatus>? progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(currentWorkspace);
        ArgumentNullException.ThrowIfNull(workspaceUpdated);
        if (interval < TimeSpan.FromSeconds(15) || interval > TimeSpan.FromHours(1))
        {
            throw new ArgumentOutOfRangeException(
                nameof(interval),
                "Das Autosave-Intervall muss zwischen 15 Sekunden und einer Stunde liegen.");
        }

        if (!await runGate.WaitAsync(0, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("Die automatische Speicherung läuft bereits.");
        }

        try
        {
            using var timer = new PeriodicTimer(interval, timeProvider);
            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
            {
                var workspace = currentWorkspace();
                if (workspace is not { HasUnsavedChanges: true })
                {
                    continue;
                }

                try
                {
                    var updated = await workspaceService.SaveAsync(
                        workspace,
                        targetArchivePath: null,
                        cancellationToken).ConfigureAwait(false);
                    workspaceUpdated(updated);
                    BackupRecord? createdBackup = null;
                    ApplicationError? backupError = null;
                    if (backupService is not null)
                    {
                        try
                        {
                            createdBackup = await backupService.CreateAutomaticIfDueAsync(
                                updated,
                                cancellationToken).ConfigureAwait(false);
                        }
                        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                        {
                            throw;
                        }
                        catch (Exception exception) when (
                            exception is IOException or UnauthorizedAccessException or InvalidDataException or
                            InvalidOperationException or Microsoft.Data.Sqlite.SqliteException)
                        {
                            backupError = new ApplicationError(
                                "Backup.AutomaticFailed",
                                "Das Projekt wurde gespeichert, die automatische Sicherung ist jedoch fehlgeschlagen.",
                                exception.ToString());
                        }
                    }

                    progress?.Report(new AutosaveStatus(
                        timeProvider.GetUtcNow(),
                        Succeeded: backupError is null,
                        backupError is not null
                            ? backupError.UserMessage
                            : createdBackup is null
                                ? "Das Projekt wurde automatisch gespeichert."
                                : "Das Projekt wurde automatisch gespeichert und gesichert.",
                        backupError));
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception) when (
                    exception is IOException or UnauthorizedAccessException or InvalidDataException or InvalidOperationException)
                {
                    progress?.Report(new AutosaveStatus(
                        timeProvider.GetUtcNow(),
                        Succeeded: false,
                        "Das Projekt konnte nicht automatisch gespeichert werden. Die Arbeitskopie bleibt geöffnet.",
                        new ApplicationError(
                            "Autosave.Failed",
                            "Das automatische Speichern ist fehlgeschlagen.",
                            exception.ToString())));
                }
            }
        }
        finally
        {
            runGate.Release();
        }
    }

    /// <inheritdoc />
    public void Dispose() => runGate.Dispose();
}
