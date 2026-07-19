using System.Buffers;
using System.Globalization;
using System.Security.Cryptography;
using ZeitstrahlStudio.Application;
using ZeitstrahlStudio.Domain;

namespace ZeitstrahlStudio.Infrastructure;

/// <summary>Erstellt und verwaltet ausschließlich lokale, validierte Projektarchive als Sicherungen.</summary>
public sealed partial class LocalBackupService : IBackupService, IDisposable
{
    private const string BackupTimestampFormat = "yyyyMMdd'T'HHmmssfff'Z'";
    private readonly IProjectWorkspaceService workspaceService;
    private readonly IAuditLogService auditLogService;
    private readonly BackupRetentionPolicy retentionPolicy;
    private readonly ILocalLogService? logService;
    private readonly string backupRoot;
    private readonly TimeProvider timeProvider;
    private readonly TimeZoneInfo localTimeZone;
    private readonly SemaphoreSlim operationGate = new(1, 1);

    /// <summary>Initialisiert den lokalen Sicherungsdienst.</summary>
    public LocalBackupService(
        IProjectWorkspaceService workspaceService,
        IAuditLogService auditLogService,
        BackupRetentionPolicy retentionPolicy,
        ILocalLogService? logService = null,
        string? backupRoot = null,
        TimeProvider? timeProvider = null,
        TimeZoneInfo? localTimeZone = null)
    {
        this.workspaceService = workspaceService ?? throw new ArgumentNullException(nameof(workspaceService));
        this.auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        this.retentionPolicy = retentionPolicy ?? throw new ArgumentNullException(nameof(retentionPolicy));
        this.logService = logService;
        this.backupRoot = Path.GetFullPath(backupRoot ?? GetDefaultBackupRoot());
        this.timeProvider = timeProvider ?? TimeProvider.System;
        this.localTimeZone = localTimeZone ?? TimeZoneInfo.Local;
    }

    /// <inheritdoc />
    public async Task<BackupRecord> CreateAsync(
        ProjectWorkspace workspace,
        bool automatic,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        await operationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await CreateCoreAsync(workspace, automatic, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            operationGate.Release();
        }
    }

    /// <inheritdoc />
    public async Task<BackupRecord?> CreateAutomaticIfDueAsync(
        ProjectWorkspace workspace,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        await operationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var backups = await ListCoreAsync(workspace, cancellationToken).ConfigureAwait(false);
            var nowUtc = GetUtcNow();
            return retentionPolicy.IsAutomaticBackupDue(backups, workspace.Project.Settings, nowUtc)
                ? await CreateCoreAsync(workspace, automatic: true, cancellationToken).ConfigureAwait(false)
                : null;
        }
        finally
        {
            operationGate.Release();
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<BackupRecord>> ListAsync(
        ProjectWorkspace workspace,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        await operationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await ListCoreAsync(workspace, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            operationGate.Release();
        }
    }

    /// <inheritdoc />
    public async Task<ProjectWorkspace> RestoreAsync(
        ProjectWorkspace currentWorkspace,
        BackupRecord backup,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(currentWorkspace);
        ArgumentNullException.ThrowIfNull(backup);
        await operationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var knownBackups = await ListCoreAsync(currentWorkspace, cancellationToken).ConfigureAwait(false);
            var selected = knownBackups.SingleOrDefault(item => item.Id == backup.Id)
                ?? throw new InvalidDataException("Die ausgewählte Sicherung ist nicht mehr verfügbar.");
            await ValidateRestorableBackupAsync(
                currentWorkspace.Project.Id,
                selected,
                cancellationToken).ConfigureAwait(false);

            await CreateCoreAsync(
                currentWorkspace,
                automatic: false,
                cancellationToken).ConfigureAwait(false);
            await ValidateRestorableBackupAsync(
                currentWorkspace.Project.Id,
                selected,
                cancellationToken).ConfigureAwait(false);

            var archivePath = ResolveBackupPath(currentWorkspace.Project.Id, selected);
            var restored = await workspaceService.RestoreSnapshotAsync(
                currentWorkspace,
                archivePath,
                cancellationToken).ConfigureAwait(false);
            try
            {
                await ValidateRestorableBackupAsync(
                    currentWorkspace.Project.Id,
                    selected,
                    cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                await workspaceService.CloseAsync(restored, CancellationToken.None).ConfigureAwait(false);
                throw;
            }

            await WriteAuditBestEffortAsync(
                restored,
                "BackupRestore",
                $"Sicherung vom {selected.CreatedAtUtc.ToLocalTime():dd.MM.yyyy HH:mm:ss} wiederhergestellt",
                cancellationToken).ConfigureAwait(false);
            return restored;
        }
        finally
        {
            operationGate.Release();
        }
    }

    /// <inheritdoc />
    public async Task ApplyRetentionAsync(
        ProjectWorkspace workspace,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        await operationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await ApplyRetentionCoreAsync(workspace, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            operationGate.Release();
        }
    }

    private async Task<BackupRecord> CreateCoreAsync(
        ProjectWorkspace workspace,
        bool automatic,
        CancellationToken cancellationToken)
    {
        workspace.Project.Settings.Validate();
        var createdAtUtc = GetUtcNow();
        var id = Guid.NewGuid();
        var projectDirectory = GetProjectBackupDirectory(workspace.Project.Id);
        var typeToken = automatic ? "auto" : "manual";
        var fileName =
            $"{createdAtUtc.UtcDateTime.ToString(BackupTimestampFormat, CultureInfo.InvariantCulture)}_" +
            $"{typeToken}_{id:N}.zeitprojekt";
        var fullPath = Path.Combine(projectDirectory, fileName);
        var relativePath = workspace.Project.Id.ToString("N") + "/" + fileName;
        var metadataPersisted = false;
        try
        {
            await workspaceService.ExportSnapshotAsync(
                workspace,
                fullPath,
                cancellationToken).ConfigureAwait(false);
            var fileInfo = new FileInfo(fullPath);
            var sha256 = await ComputeStableSha256Async(fullPath, cancellationToken).ConfigureAwait(false);
            fileInfo.Refresh();
            var record = new BackupRecord(
                id,
                createdAtUtc,
                relativePath,
                fileInfo.Length,
                sha256,
                automatic);
            await UpsertRecordAsync(workspace, record, cancellationToken).ConfigureAwait(false);
            metadataPersisted = true;

            await WriteAuditBestEffortAsync(
                workspace,
                automatic ? "BackupAutomatic" : "BackupManual",
                automatic
                    ? "Automatische Projektsicherung erstellt"
                    : "Manuelle Projektsicherung erstellt",
                cancellationToken).ConfigureAwait(false);
            if (automatic)
            {
                await ApplyRetentionCoreAsync(workspace, cancellationToken).ConfigureAwait(false);
            }

            return record;
        }
        catch
        {
            if (!metadataPersisted)
            {
                DeleteFileBestEffort(fullPath);
            }

            throw;
        }
    }

    private async Task ApplyRetentionCoreAsync(
        ProjectWorkspace workspace,
        CancellationToken cancellationToken)
    {
        var backups = await ListCoreAsync(workspace, cancellationToken).ConfigureAwait(false);
        var keep = retentionPolicy.SelectAutomaticBackupsToKeep(
            backups,
            workspace.Project.Settings,
            GetUtcNow(),
            localTimeZone);
        foreach (var backup in backups.Where(item => item.IsAutomatic && !keep.Contains(item.Id)))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var fullPath = ResolveBackupPath(workspace.Project.Id, backup);
            try
            {
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }

                await DeleteRecordAsync(workspace, backup.Id, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                await WriteLocalWarningBestEffortAsync(
                    "BackupRetention",
                    "Eine ältere automatische Sicherung konnte nicht entfernt werden und bleibt erhalten.",
                    exception).ConfigureAwait(false);
            }
        }
    }

    private async Task ValidateRestorableBackupAsync(
        Guid projectId,
        BackupRecord backup,
        CancellationToken cancellationToken)
    {
        ValidateRecord(backup);
        var fullPath = ResolveBackupPath(projectId, backup);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException(
                "Die ausgewählte Sicherungsdatei wurde nicht gefunden.",
                fullPath);
        }

        if ((File.GetAttributes(fullPath) & FileAttributes.ReparsePoint) != 0)
        {
            throw new InvalidDataException(
                "Die ausgewählte Sicherung ist eine nicht erlaubte Dateisystemverknüpfung.");
        }

        var fileInfo = new FileInfo(fullPath);
        if (fileInfo.Length != backup.FileSize)
        {
            throw new InvalidDataException(
                "Die Größe der Sicherungsdatei stimmt nicht mit den gespeicherten Metadaten überein.");
        }

        var actualSha256 = await ComputeStableSha256Async(fullPath, cancellationToken).ConfigureAwait(false);
        if (!string.Equals(actualSha256, backup.Sha256, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException(
                "Die Prüfsumme der Sicherung stimmt nicht. Die Datei wurde möglicherweise beschädigt.");
        }
    }

    private string GetProjectBackupDirectory(Guid projectId)
    {
        if (projectId == Guid.Empty)
        {
            throw new ArgumentException("Für Sicherungen ist eine gültige Projekt-ID erforderlich.", nameof(projectId));
        }

        Directory.CreateDirectory(backupRoot);
        EnsureDirectoryIsNotReparsePoint(backupRoot);
        var projectDirectory = Path.Combine(backupRoot, projectId.ToString("N"));
        Directory.CreateDirectory(projectDirectory);
        EnsureDirectoryIsNotReparsePoint(projectDirectory);
        return projectDirectory;
    }

    private string ResolveBackupPath(Guid projectId, BackupRecord backup)
    {
        var normalized = ArchivePathValidator.ValidateAndNormalize(backup.RelativeArchivePath);
        var expectedPrefix = projectId.ToString("N") + "/";
        if (!normalized.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase) ||
            normalized[expectedPrefix.Length..].Contains('/'))
        {
            throw new InvalidDataException(
                "Die Sicherungsmetadaten verweisen nicht in den lokalen Projekt-Sicherungsordner.");
        }

        var fullPath = ArchivePathValidator.ResolveUnderRoot(backupRoot, normalized);
        var expectedDirectory = GetProjectBackupDirectory(projectId);
        if (!string.Equals(
                Path.GetDirectoryName(fullPath),
                expectedDirectory,
                StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(Path.GetExtension(fullPath), ".zeitprojekt", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("Der Sicherungspfad ist ungültig.");
        }

        return fullPath;
    }

    private async Task WriteAuditBestEffortAsync(
        ProjectWorkspace workspace,
        string operation,
        string description,
        CancellationToken cancellationToken)
    {
        try
        {
            await auditLogService.WriteAsync(
                workspace,
                new AuditEntry(
                    Guid.NewGuid(),
                    GetUtcNow(),
                    operation,
                    nameof(TimelineProject),
                    workspace.Project.Id,
                    description,
                    Succeeded: true,
                    TechnicalDetails: null),
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or InvalidDataException or
            InvalidOperationException or Microsoft.Data.Sqlite.SqliteException or
            OperationCanceledException)
        {
            await WriteLocalWarningBestEffortAsync(
                "BackupAudit",
                "Der Sicherungsvorgang war erfolgreich, konnte aber nicht im Audit protokolliert werden.",
                exception).ConfigureAwait(false);
        }
    }

    private async Task WriteLocalWarningBestEffortAsync(
        string eventName,
        string message,
        Exception exception)
    {
        if (logService is null)
        {
            return;
        }

        try
        {
            await logService.WriteAsync(
                new LocalLogEntry(
                    GetUtcNow(),
                    LocalLogLevel.Warning,
                    nameof(LocalBackupService),
                    eventName,
                    message,
                    exception.ToString()),
                CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception logException) when (
            logException is IOException or UnauthorizedAccessException)
        {
        }
    }

    private static async Task<string> ComputeStableSha256Async(
        string fullPath,
        CancellationToken cancellationToken)
    {
        var before = new FileInfo(fullPath);
        var beforeLength = before.Length;
        var beforeWriteTime = before.LastWriteTimeUtc;
        await using var stream = new FileStream(
            fullPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            128 * 1024,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buffer = ArrayPool<byte>.Shared.Rent(128 * 1024);
        try
        {
            while (true)
            {
                var read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)
                    .ConfigureAwait(false);
                if (read == 0)
                {
                    break;
                }

                hash.AppendData(buffer, 0, read);
            }

            var after = new FileInfo(fullPath);
            after.Refresh();
            if (after.Length != beforeLength || after.LastWriteTimeUtc != beforeWriteTime)
            {
                throw new IOException("Die Sicherungsdatei wurde während der Prüfung verändert.");
            }

            return Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant();
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static void ValidateRecord(BackupRecord backup)
    {
        if (backup.Id == Guid.Empty ||
            backup.CreatedAtUtc.Offset != TimeSpan.Zero ||
            backup.FileSize < 0 ||
            backup.Sha256.Length != 64 ||
            !backup.Sha256.All(Uri.IsHexDigit))
        {
            throw new InvalidDataException("Die gespeicherten Sicherungsmetadaten sind ungültig.");
        }
    }

    private static void EnsureDirectoryIsNotReparsePoint(string directory)
    {
        if ((File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0)
        {
            throw new InvalidDataException(
                $"Der Sicherungsordner '{directory}' darf keine Dateisystemverknüpfung sein.");
        }
    }

    private static void DeleteFileBestEffort(string fullPath)
    {
        try
        {
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private DateTimeOffset GetUtcNow() => timeProvider.GetUtcNow().ToUniversalTime();

    private static string GetDefaultBackupRoot() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Zeitstrahl Studio",
        "Backups");

    /// <inheritdoc />
    public void Dispose() => operationGate.Dispose();
}
