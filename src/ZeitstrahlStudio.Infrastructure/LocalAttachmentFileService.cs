using System.Diagnostics;
using System.Security.Cryptography;
using ZeitstrahlStudio.Application;
using ZeitstrahlStudio.Domain;

namespace ZeitstrahlStudio.Infrastructure;

/// <summary>Prüft die Integrität lokaler Projektkopien vor Vorschau oder externem Öffnen.</summary>
public sealed class LocalAttachmentFileService : IAttachmentFileService
{
    public async Task<string> GetValidatedLocalPathAsync(
        ProjectWorkspace workspace,
        Attachment attachment,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentNullException.ThrowIfNull(attachment);
        var path = ResolveSafePath(workspace.WorkingDirectory, attachment.ProjectRelativePath);
        EnsureNoReparsePoint(workspace.WorkingDirectory, attachment.ProjectRelativePath);

        var before = new FileInfo(path);
        if (!before.Exists)
        {
            throw new FileNotFoundException(
                $"Die Projektkopie von „{attachment.OriginalFileName}“ wurde nicht gefunden.",
                path);
        }

        if (before.Length != attachment.FileSize)
        {
            throw new InvalidDataException(
                $"Die Größe der Projektkopie von „{attachment.OriginalFileName}“ hat sich geändert.");
        }

        var initialLength = before.Length;
        var initialWriteTime = before.LastWriteTimeUtc;
        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            128 * 1024,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false);
        var after = new FileInfo(path);
        if (!after.Exists ||
            after.Length != initialLength ||
            after.LastWriteTimeUtc != initialWriteTime)
        {
            throw new InvalidDataException(
                $"Die Projektkopie von „{attachment.OriginalFileName}“ wurde während der Prüfung verändert.");
        }

        var actualHash = Convert.ToHexString(hash).ToLowerInvariant();
        if (!string.Equals(actualHash, attachment.Sha256, StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                $"Die Prüfsumme der Projektkopie von „{attachment.OriginalFileName}“ stimmt nicht.");
        }

        return path;
    }

    public async Task OpenWithDefaultApplicationAsync(
        ProjectWorkspace workspace,
        Attachment attachment,
        CancellationToken cancellationToken)
    {
        var path = await GetValidatedLocalPathAsync(workspace, attachment, cancellationToken)
            .ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true,
        });
        if (process is null)
        {
            throw new InvalidOperationException(
                $"Für „{attachment.OriginalFileName}“ konnte kein Windows-Standardprogramm gestartet werden.");
        }
    }

    private static string ResolveSafePath(string workingDirectory, string projectRelativePath)
    {
        var root = Path.GetFullPath(workingDirectory);
        var rootWithSeparator = root.EndsWith(Path.DirectorySeparatorChar)
            ? root
            : root + Path.DirectorySeparatorChar;
        var path = Path.GetFullPath(
            Path.Combine(root, projectRelativePath.Replace('/', Path.DirectorySeparatorChar)));
        if (!path.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException(
                "Der interne Anhangspfad verlässt den Projektarbeitsordner.");
        }

        return path;
    }

    private static void EnsureNoReparsePoint(string workingDirectory, string projectRelativePath)
    {
        var current = Path.GetFullPath(workingDirectory);
        if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
        {
            throw new InvalidDataException(
                "Der Projektarbeitsordner darf keine Dateisystemverknüpfung sein.");
        }

        foreach (var segment in projectRelativePath.Split('/'))
        {
            current = Path.Combine(current, segment);
            if (File.Exists(current) || Directory.Exists(current))
            {
                if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                {
                    throw new InvalidDataException(
                        "Der Anhangspfad darf keine Dateisystemverknüpfung enthalten.");
                }
            }
        }
    }
}
