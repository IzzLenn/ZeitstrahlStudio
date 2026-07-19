using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using ZeitstrahlStudio.Application;
using ZeitstrahlStudio.Domain;
using ZeitstrahlStudio.Infrastructure;

namespace ZeitstrahlStudio.IntegrationTests;

public sealed class ProjectArchiveAndWorkspaceTests
{
    private static readonly DateTimeOffset BaseTime = new(2026, 7, 19, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ExportAndImport_RoundTripsDatabaseAndAttachmentWithManifest()
    {
        await using var root = new TemporaryRoot();
        var repository = new SqliteProjectRepository();
        var archiveService = new ProjectArchiveService(repository, new FixedTimeProvider(BaseTime));
        var source = await CreatePopulatedWorkspaceAsync(root.Path, repository);
        var archivePath = System.IO.Path.Combine(root.Path, "transfer.zeitprojekt");
        var progress = new SynchronousProgress<FileOperationProgress>();

        await archiveService.ExportAsync(source.WorkingDirectory, archivePath, progress, CancellationToken.None);

        Assert.True(File.Exists(archivePath));
        Assert.NotEmpty(progress.Values);
        using (var archive = ZipFile.OpenRead(archivePath))
        {
            Assert.Equal("manifest.json", archive.Entries[^1].FullName);
            Assert.Contains(archive.Entries, entry => entry.FullName == "project.db");
            Assert.Contains(archive.Entries, entry => entry.FullName == source.Attachment.ProjectRelativePath);
            var manifestEntry = Assert.Single(archive.Entries, entry => entry.FullName == "manifest.json");
            using var stream = manifestEntry.Open();
            using var document = await JsonDocument.ParseAsync(stream);
            Assert.Equal("ZeitstrahlStudio.Project", document.RootElement.GetProperty("format").GetString());
            Assert.Equal(1, document.RootElement.GetProperty("formatVersion").GetInt32());
            Assert.Equal(source.Project.Id, document.RootElement.GetProperty("projectId").GetGuid());
        }

        var targetDirectory = System.IO.Path.Combine(root.Path, "imported-workspace");
        var imported = await archiveService.ImportAsync(
            archivePath,
            targetDirectory,
            progress: null,
            CancellationToken.None);

        Assert.True(imported.IsSuccess, imported.Error?.TechnicalDetails);
        Assert.Equal(source.Project.Id, imported.Value!.Workspace.Project.Id);
        Assert.Single(imported.Value.Workspace.Project.Events);
        var importedAttachment = Assert.Single(imported.Value.Workspace.Project.Events[0].Attachments);
        var importedFile = System.IO.Path.Combine(
            targetDirectory,
            importedAttachment.ProjectRelativePath.Replace('/', System.IO.Path.DirectorySeparatorChar));
        Assert.Equal(source.AttachmentContent, await File.ReadAllTextAsync(importedFile));
    }

    [Fact]
    public async Task Import_RejectsMissingManifestWithoutCreatingTarget()
    {
        await using var root = new TemporaryRoot();
        var archivePath = System.IO.Path.Combine(root.Path, "ohne-manifest.zeitprojekt");
        using (var archive = ZipFile.Open(archivePath, ZipArchiveMode.Create))
        {
            var entry = archive.CreateEntry("project.db");
            await using var stream = entry.Open();
            await stream.WriteAsync("keine Datenbank"u8.ToArray());
        }

        var service = new ProjectArchiveService(new SqliteProjectRepository());
        var target = System.IO.Path.Combine(root.Path, "target");
        var result = await service.ImportAsync(archivePath, target, null, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.False(Directory.Exists(target));
        Assert.Contains("Manifest", result.Error!.TechnicalDetails!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Import_RejectsPathTraversalBeforeExtraction()
    {
        await using var root = new TemporaryRoot();
        var archivePath = System.IO.Path.Combine(root.Path, "traversal.zeitprojekt");
        var payload = Encoding.UTF8.GetBytes("Angriff");
        var manifest = new
        {
            format = "ZeitstrahlStudio.Project",
            formatVersion = 1,
            minimumReaderVersion = 1,
            applicationVersion = "1.0.0",
            projectId = Guid.NewGuid(),
            projectName = "Manipuliert",
            createdAtUtc = BaseTime,
            exportedAtUtc = BaseTime,
            files = new[]
            {
                new
                {
                    path = "../ausbruch.txt",
                    length = payload.Length,
                    sha256 = Convert.ToHexString(SHA256.HashData(payload)).ToLowerInvariant(),
                },
            },
        };

        using (var archive = ZipFile.Open(archivePath, ZipArchiveMode.Create))
        {
            var payloadEntry = archive.CreateEntry("../ausbruch.txt");
            await using (var stream = payloadEntry.Open())
            {
                await stream.WriteAsync(payload);
            }

            var manifestEntry = archive.CreateEntry("manifest.json");
            await using var manifestStream = manifestEntry.Open();
            await JsonSerializer.SerializeAsync(manifestStream, manifest);
        }

        var service = new ProjectArchiveService(new SqliteProjectRepository());
        var target = System.IO.Path.Combine(root.Path, "safe-target");
        var outside = System.IO.Path.Combine(root.Path, "ausbruch.txt");
        var result = await service.ImportAsync(archivePath, target, null, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.False(Directory.Exists(target));
        Assert.False(File.Exists(outside));
        Assert.Contains("unsichere", result.Error!.TechnicalDetails!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Import_RejectsChangedFileChecksum()
    {
        await using var root = new TemporaryRoot();
        var repository = new SqliteProjectRepository();
        var archiveService = new ProjectArchiveService(repository);
        var source = await CreatePopulatedWorkspaceAsync(root.Path, repository);
        var archivePath = System.IO.Path.Combine(root.Path, "manipuliert.zeitprojekt");
        await archiveService.ExportAsync(source.WorkingDirectory, archivePath, null, CancellationToken.None);

        using (var archive = ZipFile.Open(archivePath, ZipArchiveMode.Update))
        {
            var existing = archive.GetEntry(source.Attachment.ProjectRelativePath)!;
            existing.Delete();
            var changed = archive.CreateEntry(source.Attachment.ProjectRelativePath);
            await using var stream = changed.Open();
            await stream.WriteAsync("verändert"u8.ToArray());
        }

        var target = System.IO.Path.Combine(root.Path, "checksum-target");
        var result = await archiveService.ImportAsync(archivePath, target, null, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.False(Directory.Exists(target));
        Assert.Contains("Größe", result.Error!.TechnicalDetails!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Import_RejectsInsufficientDiskSpaceBeforeCreatingTarget()
    {
        await using var root = new TemporaryRoot();
        var repository = new SqliteProjectRepository();
        var archiveService = new ProjectArchiveService(repository);
        var source = await CreatePopulatedWorkspaceAsync(root.Path, repository);
        var archivePath = System.IO.Path.Combine(root.Path, "zu-gross.zeitprojekt");
        await archiveService.ExportAsync(
            source.WorkingDirectory,
            archivePath,
            progress: null,
            CancellationToken.None);
        var constrainedService = new ProjectArchiveService(
            repository,
            timeProvider: null,
            _ => 0);
        var target = System.IO.Path.Combine(root.Path, "ohne-speicherplatz");

        var result = await constrainedService.ImportAsync(
            archivePath,
            target,
            progress: null,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("Speicherplatz", result.Error!.TechnicalDetails!, StringComparison.OrdinalIgnoreCase);
        Assert.False(Directory.Exists(target));
        Assert.Empty(Directory.EnumerateDirectories(root.Path, "ohne-speicherplatz.importing-*"));
    }

    [Fact]
    public async Task Import_CancellationAfterFirstFileRemovesStagingDirectory()
    {
        await using var root = new TemporaryRoot();
        var repository = new SqliteProjectRepository();
        var service = new ProjectArchiveService(repository);
        var source = await CreatePopulatedWorkspaceAsync(root.Path, repository);
        var archivePath = System.IO.Path.Combine(root.Path, "abbruch.zeitprojekt");
        await service.ExportAsync(
            source.WorkingDirectory,
            archivePath,
            progress: null,
            CancellationToken.None);
        using var cancellation = new CancellationTokenSource();
        var progress = new CallbackProgress<FileOperationProgress>(value =>
        {
            if (value.CompletedItems == 1)
            {
                cancellation.Cancel();
            }
        });
        var target = System.IO.Path.Combine(root.Path, "abbruch-ziel");

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.ImportAsync(
            archivePath,
            target,
            progress,
            cancellation.Token));

        Assert.False(Directory.Exists(target));
        Assert.Empty(Directory.EnumerateDirectories(root.Path, "abbruch-ziel.importing-*"));
    }

    [Fact]
    public async Task Export_LockedExistingArchiveRemainsUnchangedAndLeavesNoTemporaryFile()
    {
        await using var root = new TemporaryRoot();
        var repository = new SqliteProjectRepository();
        var service = new ProjectArchiveService(repository);
        var source = await CreatePopulatedWorkspaceAsync(root.Path, repository);
        var archivePath = System.IO.Path.Combine(root.Path, "gesperrt.zeitprojekt");
        await service.ExportAsync(
            source.WorkingDirectory,
            archivePath,
            progress: null,
            CancellationToken.None);
        var originalBytes = await File.ReadAllBytesAsync(archivePath);
        source.Project.AddEvent(
            TimelineEvent.Create(
                Guid.NewGuid(),
                "Noch nicht übernommenes Ereignis",
                EventDate.Year(2027),
                BaseTime),
            BaseTime.AddMinutes(3));
        await repository.SaveAsync(
            source.Project,
            System.IO.Path.Combine(source.WorkingDirectory, "project.db"),
            CancellationToken.None);

        Exception? exception;
        await using (var fileLock = new FileStream(
            archivePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.None))
        {
            exception = await Record.ExceptionAsync(() => service.ExportAsync(
                source.WorkingDirectory,
                archivePath,
                progress: null,
                CancellationToken.None));
        }

        Assert.True(exception is IOException or UnauthorizedAccessException, exception?.ToString());
        Assert.Equal(originalBytes, await File.ReadAllBytesAsync(archivePath));
        Assert.Empty(Directory.EnumerateFiles(root.Path, ".gesperrt.zeitprojekt.*.tmp"));
    }

    [Fact]
    public async Task WorkspaceCheckpoint_PersistsRecoveryCopyWithoutExportingArchive()
    {
        await using var root = new TemporaryRoot();
        var workspaces = System.IO.Path.Combine(root.Path, "workspaces");
        var archives = System.IO.Path.Combine(root.Path, "archives");
        Directory.CreateDirectory(archives);
        var repository = new SqliteProjectRepository();
        var archiveService = new ProjectArchiveService(repository, new FixedTimeProvider(BaseTime));
        var service = new LocalProjectWorkspaceService(
            repository,
            archiveService,
            workspaces,
            new FixedTimeProvider(BaseTime));
        var archivePath = System.IO.Path.Combine(archives, "Checkpoint.zeitprojekt");
        var workspace = await service.CreateAsync("Checkpoint", archivePath, CancellationToken.None);
        workspace.Project.AddEvent(
            TimelineEvent.Create(
                Guid.NewGuid(),
                "Nur in der Arbeitskopie",
                EventDate.Exact(new DateOnly(2026, 7, 19)),
                BaseTime),
            BaseTime.AddMinutes(1));
        workspace = workspace with { HasUnsavedChanges = true };

        workspace = await service.CheckpointAsync(workspace, CancellationToken.None);

        Assert.True(workspace.HasUnsavedChanges);
        var checkpointProject = await repository.LoadAsync(
            System.IO.Path.Combine(workspace.WorkingDirectory, "project.db"),
            CancellationToken.None);
        Assert.Single(checkpointProject.Events);
        var archivedWorkspace = await service.OpenAsync(archivePath, CancellationToken.None);
        Assert.Empty(archivedWorkspace.Project.Events);
        await service.CloseAsync(archivedWorkspace, CancellationToken.None);
        await service.CloseAsync(workspace, CancellationToken.None);
    }

    [Fact]
    public async Task WorkspaceService_CreatesSavesOpensDuplicatesClosesAndDeletes()
    {
        await using var root = new TemporaryRoot();
        var workspaces = System.IO.Path.Combine(root.Path, "workspaces");
        var archives = System.IO.Path.Combine(root.Path, "archives");
        Directory.CreateDirectory(archives);
        var repository = new SqliteProjectRepository();
        var archiveService = new ProjectArchiveService(repository, new FixedTimeProvider(BaseTime));
        var service = new LocalProjectWorkspaceService(
            repository,
            archiveService,
            workspaces,
            new FixedTimeProvider(BaseTime));
        var originalArchive = System.IO.Path.Combine(archives, "Original.zeitprojekt");

        var workspace = await service.CreateAsync("Original", originalArchive, CancellationToken.None);
        var timelineEvent = TimelineEvent.Create(
            Guid.NewGuid(),
            "Gespeichertes Ereignis",
            EventDate.MonthAndYear(2026, 7),
            BaseTime);
        workspace.Project.AddEvent(timelineEvent, BaseTime.AddMinutes(1));
        workspace = workspace with { HasUnsavedChanges = true };
        var saveAsArchive = System.IO.Path.Combine(archives, "Gespeichert unter.zeitprojekt");
        workspace = await service.SaveAsync(workspace, saveAsArchive, CancellationToken.None);

        Assert.False(workspace.HasUnsavedChanges);
        Assert.Equal(System.IO.Path.GetFullPath(saveAsArchive), workspace.ArchivePath);
        using (var savedArchive = ZipFile.OpenRead(saveAsArchive))
        {
            Assert.DoesNotContain(
                savedArchive.Entries,
                entry => entry.FullName.StartsWith("metadata/session.json", StringComparison.OrdinalIgnoreCase));
        }

        var opened = await service.OpenAsync(saveAsArchive, CancellationToken.None);
        Assert.Single(opened.Project.Events);

        var duplicateArchive = System.IO.Path.Combine(archives, "Kopie.zeitprojekt");
        var duplicate = await service.DuplicateAsync(opened, duplicateArchive, CancellationToken.None);
        Assert.NotEqual(opened.Project.Id, duplicate.Project.Id);
        Assert.Equal("Kopie", duplicate.Project.Name);
        Assert.Single(duplicate.Project.Events);
        var reopenedDuplicate = await service.OpenAsync(duplicateArchive, CancellationToken.None);
        Assert.Equal(duplicate.Project.Id, reopenedDuplicate.Project.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.DeleteArchiveAsync(duplicateArchive, deletionConfirmed: false, CancellationToken.None));
        await service.CloseAsync(reopenedDuplicate, CancellationToken.None);
        await service.CloseAsync(duplicate, CancellationToken.None);
        await service.CloseAsync(opened, CancellationToken.None);
        await service.CloseAsync(workspace, CancellationToken.None);
        await service.DeleteArchiveAsync(duplicateArchive, deletionConfirmed: true, CancellationToken.None);
        Assert.False(File.Exists(duplicateArchive));
    }

    private static async Task<PopulatedWorkspace> CreatePopulatedWorkspaceAsync(
        string root,
        SqliteProjectRepository repository)
    {
        var workingDirectory = System.IO.Path.Combine(root, "source-workspace-" + Guid.NewGuid().ToString("N"));
        var attachmentDirectory = System.IO.Path.Combine(workingDirectory, "attachments", "intern");
        Directory.CreateDirectory(attachmentDirectory);
        foreach (var directory in new[] { "thumbnails", "extracted-text", "logs", "metadata" })
        {
            Directory.CreateDirectory(System.IO.Path.Combine(workingDirectory, directory));
        }

        const string attachmentContent = "Frei erfundener lokaler Testinhalt.";
        var attachmentBytes = Encoding.UTF8.GetBytes(attachmentContent);
        var attachment = new Attachment(
            Guid.NewGuid(),
            "beispiel.txt",
            "text/plain",
            attachmentBytes.Length,
            Convert.ToHexString(SHA256.HashData(attachmentBytes)).ToLowerInvariant(),
            "C:\\nicht-erforderlich\\beispiel.txt",
            BaseTime,
            "attachments/intern/beispiel.txt",
            AttachmentState.Ready);
        await File.WriteAllBytesAsync(
            System.IO.Path.Combine(attachmentDirectory, "beispiel.txt"),
            attachmentBytes);

        var project = TimelineProject.Create(Guid.NewGuid(), "Transferprojekt", BaseTime);
        var timelineEvent = TimelineEvent.Create(
            Guid.NewGuid(),
            "Transferereignis",
            EventDate.Exact(new DateOnly(2026, 7, 19)),
            BaseTime);
        timelineEvent.AddAttachment(attachment, BaseTime.AddMinutes(1));
        project.AddEvent(timelineEvent, BaseTime.AddMinutes(2));
        await repository.SaveAsync(
            project,
            System.IO.Path.Combine(workingDirectory, "project.db"),
            CancellationToken.None);
        return new PopulatedWorkspace(workingDirectory, project, attachment, attachmentContent);
    }

    private sealed record PopulatedWorkspace(
        string WorkingDirectory,
        TimelineProject Project,
        Attachment Attachment,
        string AttachmentContent);

    private sealed class SynchronousProgress<T> : IProgress<T>
    {
        public List<T> Values { get; } = [];
        public void Report(T value) => Values.Add(value);
    }

    private sealed class CallbackProgress<T>(Action<T> callback) : IProgress<T>
    {
        public void Report(T value) => callback(value);
    }

    private sealed class FixedTimeProvider(DateTimeOffset timestamp) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => timestamp;
    }

    private sealed class TemporaryRoot : IAsyncDisposable
    {
        public TemporaryRoot()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "ZeitstrahlStudio.Tests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public ValueTask DisposeAsync()
        {
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }

            return ValueTask.CompletedTask;
        }
    }
}
