namespace ZeitstrahlStudio.Domain;

/// <summary>Metadaten einer vollständig in das Projekt kopierten Datei.</summary>
public sealed record Attachment
{
    /// <summary>Initialisiert Anhangsmetadaten und prüft den internen relativen Pfad.</summary>
    public Attachment(
        Guid id,
        string originalFileName,
        string mediaType,
        long fileSize,
        string sha256,
        string? originalSourcePath,
        DateTimeOffset importedAtUtc,
        string projectRelativePath,
        AttachmentState state = AttachmentState.Imported,
        int? linkedPdfPage = null)
    {
        if (id == Guid.Empty)
        {
            throw new DomainValidationException("Ein Anhang benötigt eine gültige ID.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(originalFileName) || Path.GetFileName(originalFileName) != originalFileName)
        {
            throw new DomainValidationException("Der ursprüngliche Dateiname ist ungültig.", nameof(originalFileName));
        }

        if (string.IsNullOrWhiteSpace(mediaType))
        {
            throw new DomainValidationException("Der Dateityp darf nicht leer sein.", nameof(mediaType));
        }

        if (fileSize < 0)
        {
            throw new DomainValidationException("Die Dateigröße darf nicht negativ sein.", nameof(fileSize));
        }

        if (sha256.Length != 64 || !sha256.All(Uri.IsHexDigit))
        {
            throw new DomainValidationException("Die SHA-256-Prüfsumme ist ungültig.", nameof(sha256));
        }

        if (importedAtUtc.Offset != TimeSpan.Zero)
        {
            throw new DomainValidationException("Der Importzeitpunkt muss in UTC gespeichert werden.", nameof(importedAtUtc));
        }

        if (linkedPdfPage is < 1)
        {
            throw new DomainValidationException("Eine verknüpfte PDF-Seite muss mindestens 1 sein.", nameof(linkedPdfPage));
        }

        Id = id;
        OriginalFileName = originalFileName;
        MediaType = mediaType.Trim();
        FileSize = fileSize;
        Sha256 = sha256.ToLowerInvariant();
        OriginalSourcePath = string.IsNullOrWhiteSpace(originalSourcePath) ? null : originalSourcePath;
        ImportedAtUtc = importedAtUtc;
        ProjectRelativePath = NormalizeRelativePath(projectRelativePath);
        State = state;
        LinkedPdfPage = linkedPdfPage;
    }

    public Guid Id { get; }
    public string OriginalFileName { get; }
    public string MediaType { get; }
    public long FileSize { get; }
    public string Sha256 { get; }
    public string? OriginalSourcePath { get; }
    public DateTimeOffset ImportedAtUtc { get; }
    public string ProjectRelativePath { get; }
    public AttachmentState State { get; }
    public int? LinkedPdfPage { get; }

    private static string NormalizeRelativePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || Path.IsPathRooted(path))
        {
            throw new DomainValidationException("Der interne Projektpfad muss relativ sein.", nameof(path));
        }

        var normalized = path.Replace('\\', '/').Trim('/');
        var segments = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0 || segments.Any(segment => segment is "." or ".."))
        {
            throw new DomainValidationException(
                "Der interne Projektpfad enthält unsichere Pfadsegmente.",
                nameof(path));
        }

        return string.Join('/', segments);
    }
}
