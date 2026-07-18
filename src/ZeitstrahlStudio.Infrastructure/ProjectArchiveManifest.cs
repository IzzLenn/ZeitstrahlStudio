namespace ZeitstrahlStudio.Infrastructure;

internal sealed record ProjectArchiveManifest
{
    public required string Format { get; init; }
    public required int FormatVersion { get; init; }
    public required int MinimumReaderVersion { get; init; }
    public required string ApplicationVersion { get; init; }
    public required Guid ProjectId { get; init; }
    public required string ProjectName { get; init; }
    public required DateTimeOffset CreatedAtUtc { get; init; }
    public required DateTimeOffset ExportedAtUtc { get; init; }
    public required IReadOnlyList<ProjectArchiveFileEntry> Files { get; init; }
}

internal sealed record ProjectArchiveFileEntry
{
    public required string Path { get; init; }
    public required long Length { get; init; }
    public required string Sha256 { get; init; }
}
