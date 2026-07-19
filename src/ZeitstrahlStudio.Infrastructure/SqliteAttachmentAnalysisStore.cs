using System.Globalization;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using ZeitstrahlStudio.Application;
using ZeitstrahlStudio.Domain;

namespace ZeitstrahlStudio.Infrastructure;

/// <summary>Speichert Analyseergebnisse und aktualisiert den lokalen Volltextindex atomar.</summary>
public sealed class SqliteAttachmentAnalysisStore : IAttachmentAnalysisStore
{
    private const string TitleKey = "ZeitstrahlStudio.Analysis.Title";
    private const string DatesKey = "ZeitstrahlStudio.Analysis.DateSuggestions";
    private const string ThumbnailKey = "ZeitstrahlStudio.Analysis.Thumbnail";
    private const string PageCountKey = "ZeitstrahlStudio.Analysis.PageCount";
    private readonly TimeProvider timeProvider;

    public SqliteAttachmentAnalysisStore(TimeProvider? timeProvider = null)
    {
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task SaveAsync(
        ProjectWorkspace workspace,
        Attachment attachment,
        DocumentAnalysisResult result,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentNullException.ThrowIfNull(attachment);
        ArgumentNullException.ThrowIfNull(result);
        var databasePath = GetDatabasePath(workspace);
        await using var connection = await SqliteConnectionFactory
            .OpenAsync(databasePath, cancellationToken)
            .ConfigureAwait(false);
        await using var transaction = (SqliteTransaction)await connection
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        await EnsureAttachmentBelongsToProjectAsync(
            connection,
            transaction,
            workspace.Project.Id,
            attachment.Id,
            cancellationToken).ConfigureAwait(false);
        await DeletePreviousResultAsync(
            connection,
            transaction,
            attachment.Id,
            cancellationToken).ConfigureAwait(false);

        var metadata = new Dictionary<string, string>(result.Metadata, StringComparer.OrdinalIgnoreCase);
        AddOptional(metadata, TitleKey, result.Title);
        AddOptional(metadata, ThumbnailKey, result.ThumbnailRelativePath);
        if (result.PageCount.HasValue)
        {
            metadata[PageCountKey] = result.PageCount.Value.ToString(CultureInfo.InvariantCulture);
        }

        metadata[DatesKey] = JsonSerializer.Serialize(result.DateSuggestions);
        foreach (var item in metadata)
        {
            await InsertMetadataAsync(
                connection,
                transaction,
                attachment.Id,
                item.Key,
                item.Value,
                cancellationToken).ConfigureAwait(false);
        }

        await using (var command = CreateCommand(
                         connection,
                         transaction,
                         """
                         INSERT INTO ExtractedTexts (
                             AttachmentId, Content, ExtractionMethod, Language, ExtractedAtUtc)
                         VALUES (
                             $attachmentId, $content, $method, NULL, $extractedAtUtc);

                         UPDATE Attachments SET State = $state WHERE Id = $attachmentId;
                         """))
        {
            command.Parameters.AddWithValue("$attachmentId", attachment.Id.ToString("D"));
            command.Parameters.AddWithValue("$content", result.ExtractedText);
            command.Parameters.AddWithValue("$method", (int)result.ExtractionMethod);
            command.Parameters.AddWithValue(
                "$extractedAtUtc",
                timeProvider.GetUtcNow().ToString("O", CultureInfo.InvariantCulture));
            var state = result.ExtractionMethod is
                TextExtractionMethod.Ocr or TextExtractionMethod.EmbeddedTextAndOcr
                ? AttachmentState.Warning
                : AttachmentState.Ready;
            command.Parameters.AddWithValue("$state", (int)state);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await SqliteProjectRepository.RefreshSearchIndexAsync(
            connection,
            transaction,
            workspace.Project.Id,
            cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<DocumentAnalysisResult?> LoadAsync(
        ProjectWorkspace workspace,
        Attachment attachment,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentNullException.ThrowIfNull(attachment);
        var databasePath = GetDatabasePath(workspace);
        await using var connection = await SqliteConnectionFactory
            .OpenAsync(databasePath, cancellationToken)
            .ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Content, ExtractionMethod
            FROM ExtractedTexts
            WHERE AttachmentId = $attachmentId;
            """;
        command.Parameters.AddWithValue("$attachmentId", attachment.Id.ToString("D"));
        string content;
        TextExtractionMethod method;
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
        {
            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                return null;
            }

            content = reader.GetString(0);
            method = (TextExtractionMethod)reader.GetInt32(1);
        }

        command.Parameters.Clear();
        command.CommandText = """
            SELECT MetadataKey, MetadataValue
            FROM AttachmentMetadata
            WHERE AttachmentId = $attachmentId;
            """;
        command.Parameters.AddWithValue("$attachmentId", attachment.Id.ToString("D"));
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
        {
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                metadata[reader.GetString(0)] = reader.GetString(1);
            }
        }

        metadata.Remove(TitleKey, out var title);
        metadata.Remove(ThumbnailKey, out var thumbnail);
        metadata.Remove(PageCountKey, out var pageCountText);
        metadata.Remove(DatesKey, out var datesJson);
        var dates = string.IsNullOrWhiteSpace(datesJson)
            ? []
            : JsonSerializer.Deserialize<string[]>(datesJson) ?? [];
        int? pageCount = int.TryParse(
            pageCountText,
            NumberStyles.None,
            CultureInfo.InvariantCulture,
            out var parsedPageCount)
            ? parsedPageCount
            : null;
        return new DocumentAnalysisResult(
            attachment.MediaType,
            title,
            content,
            method,
            metadata,
            dates,
            thumbnail,
            pageCount);
    }

    private static async Task EnsureAttachmentBelongsToProjectAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        Guid projectId,
        Guid attachmentId,
        CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(
            connection,
            transaction,
            """
            SELECT COUNT(*)
            FROM Attachments a
            JOIN Events e ON e.Id = a.EventId
            WHERE a.Id = $attachmentId AND e.ProjectId = $projectId;
            """);
        command.Parameters.AddWithValue("$attachmentId", attachmentId.ToString("D"));
        command.Parameters.AddWithValue("$projectId", projectId.ToString("D"));
        if (Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false)) != 1)
        {
            throw new InvalidDataException("Der Anhang gehört nicht zur geöffneten Projektdatenbank.");
        }
    }

    private static async Task DeletePreviousResultAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        Guid attachmentId,
        CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(
            connection,
            transaction,
            """
            DELETE FROM AttachmentMetadata WHERE AttachmentId = $attachmentId;
            DELETE FROM ExtractedTexts WHERE AttachmentId = $attachmentId;
            """);
        command.Parameters.AddWithValue("$attachmentId", attachmentId.ToString("D"));
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task InsertMetadataAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        Guid attachmentId,
        string key,
        string value,
        CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(
            connection,
            transaction,
            """
            INSERT INTO AttachmentMetadata (AttachmentId, MetadataKey, MetadataValue)
            VALUES ($attachmentId, $key, $value);
            """);
        command.Parameters.AddWithValue("$attachmentId", attachmentId.ToString("D"));
        command.Parameters.AddWithValue("$key", key);
        command.Parameters.AddWithValue("$value", value);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static SqliteCommand CreateCommand(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string commandText)
    {
        var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = commandText;
        return command;
    }

    private static string GetDatabasePath(ProjectWorkspace workspace)
    {
        var databasePath = Path.GetFullPath(Path.Combine(workspace.WorkingDirectory, "project.db"));
        if (!File.Exists(databasePath))
        {
            throw new InvalidDataException("Die Projektdatenbank wurde nicht gefunden.");
        }

        return databasePath;
    }

    private static void AddOptional(IDictionary<string, string> metadata, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            metadata[key] = value;
        }
    }
}
