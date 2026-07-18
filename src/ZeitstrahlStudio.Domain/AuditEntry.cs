namespace ZeitstrahlStudio.Domain;

/// <summary>Unveränderlicher Eintrag des lokalen Änderungsprotokolls.</summary>
public sealed record AuditEntry(
    Guid Id,
    DateTimeOffset TimestampUtc,
    string Operation,
    string EntityType,
    Guid? EntityId,
    string Description,
    bool Succeeded,
    string? TechnicalDetails);

/// <summary>Metadaten einer Projektsicherung.</summary>
public sealed record BackupRecord(
    Guid Id,
    DateTimeOffset CreatedAtUtc,
    string RelativeArchivePath,
    long FileSize,
    string Sha256,
    bool IsAutomatic);
