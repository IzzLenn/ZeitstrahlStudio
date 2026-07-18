using Microsoft.Data.Sqlite;
using ZeitstrahlStudio.Application;
using ZeitstrahlStudio.Domain;

namespace ZeitstrahlStudio.Infrastructure;

/// <summary>Verwaltet lokale Arbeitskopien und verbindet sie mit atomaren Projektarchiven.</summary>
public sealed class LocalProjectWorkspaceService : IProjectWorkspaceService
{
    private static readonly string[] WorkspaceDirectories =
    [
        "attachments",
        "thumbnails",
        "extracted-text",
        "logs",
        "metadata",
    ];

    private readonly IProjectRepository repository;
    private readonly IProjectArchiveService archiveService;
    private readonly string workspaceRoot;
    private readonly TimeProvider timeProvider;

    /// <summary>Initialisiert den Arbeitsordnerdienst.</summary>
    public LocalProjectWorkspaceService(
        IProjectRepository repository,
        IProjectArchiveService archiveService,
        string? workspaceRoot = null,
        TimeProvider? timeProvider = null)
    {
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
        this.archiveService = archiveService ?? throw new ArgumentNullException(nameof(archiveService));
        this.workspaceRoot = Path.GetFullPath(workspaceRoot ?? GetDefaultWorkspaceRoot());
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public async Task<ProjectWorkspace> CreateAsync(
        string projectName,
        string archivePath,
        CancellationToken cancellationToken)
    {
        var targetArchivePath = ValidateArchivePath(archivePath);
        var workingDirectory = AllocateWorkingDirectory();
        try
        {
            CreateWorkspaceDirectories(workingDirectory);
            var project = TimelineProject.Create(Guid.NewGuid(), projectName, timeProvider.GetUtcNow());
            await repository.SaveAsync(
                project,
                Path.Combine(workingDirectory, "project.db"),
                cancellationToken).ConfigureAwait(false);
            await archiveService.ExportAsync(
                workingDirectory,
                targetArchivePath,
                progress: null,
                cancellationToken).ConfigureAwait(false);
            return new ProjectWorkspace(project, workingDirectory, targetArchivePath, HasUnsavedChanges: false);
        }
        catch
        {
            await DeleteManagedDirectoryBestEffortAsync(workingDirectory).ConfigureAwait(false);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ProjectWorkspace> OpenAsync(string archivePath, CancellationToken cancellationToken)
    {
        var workingDirectory = AllocateWorkingDirectory();
        var result = await archiveService.ImportAsync(
            ValidateArchivePath(archivePath),
            workingDirectory,
            progress: null,
            cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess || result.Value is null)
        {
            throw new InvalidDataException(
                result.Error?.UserMessage ?? "Das Projektarchiv konnte nicht geöffnet werden.");
        }

        return result.Value.Workspace;
    }

    /// <inheritdoc />
    public async Task<ProjectWorkspace> SaveAsync(
        ProjectWorkspace workspace,
        string? targetArchivePath,
        CancellationToken cancellationToken)
    {
        EnsureManagedWorkspace(workspace.WorkingDirectory);
        var targetPath = ValidateArchivePath(targetArchivePath ?? workspace.ArchivePath ?? string.Empty);
        await repository.SaveAsync(
            workspace.Project,
            Path.Combine(workspace.WorkingDirectory, "project.db"),
            cancellationToken).ConfigureAwait(false);
        await archiveService.ExportAsync(
            workspace.WorkingDirectory,
            targetPath,
            progress: null,
            cancellationToken).ConfigureAwait(false);
        return workspace with { ArchivePath = targetPath, HasUnsavedChanges = false };
    }

    /// <inheritdoc />
    public async Task<ProjectWorkspace> DuplicateAsync(
        ProjectWorkspace workspace,
        string targetArchivePath,
        CancellationToken cancellationToken)
    {
        EnsureManagedWorkspace(workspace.WorkingDirectory);
        var targetPath = ValidateArchivePath(targetArchivePath);
        var targetDirectory = Path.GetDirectoryName(targetPath)!;
        Directory.CreateDirectory(targetDirectory);
        var temporaryArchive = Path.Combine(
            targetDirectory,
            $".{Path.GetFileNameWithoutExtension(targetPath)}.{Guid.NewGuid():N}.zeitprojekt");
        string? duplicatedWorkingDirectory = null;
        try
        {
            await archiveService.ExportAsync(
                workspace.WorkingDirectory,
                temporaryArchive,
                progress: null,
                cancellationToken).ConfigureAwait(false);
            duplicatedWorkingDirectory = AllocateWorkingDirectory();
            var importResult = await archiveService.ImportAsync(
                temporaryArchive,
                duplicatedWorkingDirectory,
                progress: null,
                cancellationToken).ConfigureAwait(false);
            if (!importResult.IsSuccess || importResult.Value is null)
            {
                throw new InvalidDataException(
                    importResult.Error?.UserMessage ?? "Die Projektkopie konnte nicht angelegt werden.");
            }

            var duplicateName = Path.GetFileNameWithoutExtension(targetPath);
            var duplicate = CloneProject(
                importResult.Value.Workspace.Project,
                string.IsNullOrWhiteSpace(duplicateName) ? workspace.Project.Name + " – Kopie" : duplicateName,
                timeProvider.GetUtcNow());
            await ReidentifyProjectAsync(
                Path.Combine(duplicatedWorkingDirectory, "project.db"),
                importResult.Value.Workspace.Project.Id,
                duplicate.Id,
                cancellationToken).ConfigureAwait(false);
            await repository.SaveAsync(
                duplicate,
                Path.Combine(duplicatedWorkingDirectory, "project.db"),
                cancellationToken).ConfigureAwait(false);
            await archiveService.ExportAsync(
                duplicatedWorkingDirectory,
                targetPath,
                progress: null,
                cancellationToken).ConfigureAwait(false);
            return new ProjectWorkspace(
                duplicate,
                duplicatedWorkingDirectory,
                targetPath,
                HasUnsavedChanges: false);
        }
        catch
        {
            if (duplicatedWorkingDirectory is not null)
            {
                await DeleteManagedDirectoryBestEffortAsync(duplicatedWorkingDirectory).ConfigureAwait(false);
            }

            throw;
        }
        finally
        {
            if (File.Exists(temporaryArchive))
            {
                File.Delete(temporaryArchive);
            }
        }
    }

    /// <inheritdoc />
    public async Task CloseAsync(ProjectWorkspace workspace, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        EnsureManagedWorkspace(workspace.WorkingDirectory);
        cancellationToken.ThrowIfCancellationRequested();
        SqliteConnection.ClearAllPools();
        await Task.Run(
            () => Directory.Delete(workspace.WorkingDirectory, recursive: true),
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task DeleteArchiveAsync(
        string archivePath,
        bool deletionConfirmed,
        CancellationToken cancellationToken)
    {
        if (!deletionConfirmed)
        {
            throw new InvalidOperationException("Das Projektarchiv wurde nicht gelöscht, weil keine Bestätigung vorliegt.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        var fullPath = ValidateArchivePath(archivePath);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    private static TimelineProject CloneProject(
        TimelineProject source,
        string duplicateName,
        DateTimeOffset createdAtUtc) =>
        TimelineProject.Restore(
            Guid.NewGuid(),
            duplicateName,
            source.Subtitle,
            source.InfoText,
            source.Description,
            source.OverallStart,
            source.OverallEnd,
            createdAtUtc,
            createdAtUtc,
            source.Settings,
            source.Events,
            source.LayoutPositions);

    private static async Task ReidentifyProjectAsync(
        string databasePath,
        Guid oldProjectId,
        Guid newProjectId,
        CancellationToken cancellationToken)
    {
        await using var connection = await SqliteConnectionFactory.OpenAsync(databasePath, cancellationToken)
            .ConfigureAwait(false);
        await using var transaction = (SqliteTransaction)await connection
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            UPDATE Projects SET Id = $newProjectId WHERE Id = $oldProjectId;
            UPDATE SearchIndex SET ProjectId = $newProjectId WHERE ProjectId = $oldProjectId;
            """;
        command.Parameters.AddWithValue("$oldProjectId", oldProjectId.ToString("D"));
        command.Parameters.AddWithValue("$newProjectId", newProjectId.ToString("D"));
        if (await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false) < 1)
        {
            throw new InvalidDataException("Die interne Projekt-ID der Kopie konnte nicht aktualisiert werden.");
        }

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    private static string GetDefaultWorkspaceRoot() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Zeitstrahl Studio",
        "Workspaces");

    private string AllocateWorkingDirectory()
    {
        Directory.CreateDirectory(workspaceRoot);
        if ((File.GetAttributes(workspaceRoot) & FileAttributes.ReparsePoint) != 0)
        {
            throw new InvalidDataException("Der Arbeitsordner-Stamm darf keine Dateisystemverknüpfung sein.");
        }

        return Path.Combine(workspaceRoot, Guid.NewGuid().ToString("N"));
    }

    private static void CreateWorkspaceDirectories(string workingDirectory)
    {
        Directory.CreateDirectory(workingDirectory);
        foreach (var directory in WorkspaceDirectories)
        {
            Directory.CreateDirectory(Path.Combine(workingDirectory, directory));
        }
    }

    private void EnsureManagedWorkspace(string workingDirectory)
    {
        if (!ArchivePathValidator.IsUnderRoot(workspaceRoot, workingDirectory) ||
            !Directory.Exists(workingDirectory))
        {
            throw new InvalidOperationException("Der Projektarbeitsordner wird nicht von dieser Anwendung verwaltet.");
        }
    }

    private async Task DeleteManagedDirectoryBestEffortAsync(string workingDirectory)
    {
        if (!ArchivePathValidator.IsUnderRoot(workspaceRoot, workingDirectory) ||
            !Directory.Exists(workingDirectory))
        {
            return;
        }

        SqliteConnection.ClearAllPools();
        try
        {
            await Task.Run(() => Directory.Delete(workingDirectory, recursive: true)).ConfigureAwait(false);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static string ValidateArchivePath(string archivePath)
    {
        if (string.IsNullOrWhiteSpace(archivePath) ||
            !string.Equals(Path.GetExtension(archivePath), ".zeitprojekt", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Projektarchive müssen einen Pfad mit der Endung '.zeitprojekt' besitzen.", nameof(archivePath));
        }

        return Path.GetFullPath(archivePath);
    }
}
