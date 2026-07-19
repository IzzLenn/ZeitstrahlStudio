using Microsoft.Data.Sqlite;
using ZeitstrahlStudio.Domain;

namespace ZeitstrahlStudio.Infrastructure;

public sealed partial class SqliteProjectRepository
{
    private static async Task SynchronizeTagsAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        TimelineEvent timelineEvent,
        CancellationToken cancellationToken)
    {
        await using (var deleteCommand = CreateCommand(
            connection,
            transaction,
            "DELETE FROM EventTags WHERE EventId = $eventId;"))
        {
            deleteCommand.Parameters.AddWithValue("$eventId", timelineEvent.Id.ToString("D"));
            await deleteCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        foreach (var tag in timelineEvent.Tags)
        {
            await using var command = CreateCommand(
                connection,
                transaction,
                """
                INSERT INTO Tags (Id, Name)
                VALUES ($tagId, $name)
                ON CONFLICT(Name) DO NOTHING;

                INSERT INTO EventTags (EventId, TagId)
                SELECT $eventId, Id FROM Tags WHERE Name = $name COLLATE NOCASE;
                """);
            command.Parameters.AddWithValue("$tagId", Guid.NewGuid().ToString("D"));
            command.Parameters.AddWithValue("$name", tag);
            command.Parameters.AddWithValue("$eventId", timelineEvent.Id.ToString("D"));
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task UpsertAttachmentsAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        TimelineEvent timelineEvent,
        CancellationToken cancellationToken)
    {
        foreach (var attachment in timelineEvent.Attachments)
        {
            await StageIdAsync(
                connection,
                transaction,
                "SaveAttachmentIds",
                attachment.Id,
                cancellationToken).ConfigureAwait(false);

            await using var command = CreateCommand(
                connection,
                transaction,
                """
                INSERT INTO Attachments (
                    Id, EventId, OriginalFileName, MediaType, FileSize, Sha256,
                    OriginalSourcePath, ImportedAtUtc, ProjectRelativePath, State, LinkedPdfPage)
                VALUES (
                    $id, $eventId, $originalFileName, $mediaType, $fileSize, $sha256,
                    $originalSourcePath, $importedAtUtc, $projectRelativePath, $state, $linkedPdfPage)
                ON CONFLICT(Id) DO UPDATE SET
                    EventId = excluded.EventId,
                    OriginalFileName = excluded.OriginalFileName,
                    MediaType = excluded.MediaType,
                    FileSize = excluded.FileSize,
                    Sha256 = excluded.Sha256,
                    OriginalSourcePath = excluded.OriginalSourcePath,
                    ImportedAtUtc = excluded.ImportedAtUtc,
                    ProjectRelativePath = excluded.ProjectRelativePath,
                    State = excluded.State,
                    LinkedPdfPage = excluded.LinkedPdfPage;
                """);
            AddParameter(command, "$id", attachment.Id.ToString("D"));
            AddParameter(command, "$eventId", timelineEvent.Id.ToString("D"));
            AddParameter(command, "$originalFileName", attachment.OriginalFileName);
            AddParameter(command, "$mediaType", attachment.MediaType);
            AddParameter(command, "$fileSize", attachment.FileSize);
            AddParameter(command, "$sha256", attachment.Sha256);
            AddParameter(command, "$originalSourcePath", attachment.OriginalSourcePath);
            AddParameter(command, "$importedAtUtc", FormatTimestamp(attachment.ImportedAtUtc));
            AddParameter(command, "$projectRelativePath", attachment.ProjectRelativePath);
            AddParameter(command, "$state", (int)attachment.State);
            AddParameter(command, "$linkedPdfPage", attachment.LinkedPdfPage);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task UpsertWebLinksAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        TimelineEvent timelineEvent,
        CancellationToken cancellationToken)
    {
        foreach (var webLink in timelineEvent.WebLinks)
        {
            await StageIdAsync(
                connection,
                transaction,
                "SaveWebLinkIds",
                webLink.Id,
                cancellationToken).ConfigureAwait(false);

            await using var command = CreateCommand(
                connection,
                transaction,
                """
                INSERT INTO WebLinks (Id, EventId, Address, Label)
                VALUES ($id, $eventId, $address, $label)
                ON CONFLICT(Id) DO UPDATE SET
                    EventId = excluded.EventId,
                    Address = excluded.Address,
                    Label = excluded.Label;
                """);
            AddParameter(command, "$id", webLink.Id.ToString("D"));
            AddParameter(command, "$eventId", timelineEvent.Id.ToString("D"));
            AddParameter(command, "$address", webLink.Address.AbsoluteUri);
            AddParameter(command, "$label", webLink.Label);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task SynchronizeLayoutPositionsAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        TimelineProject project,
        CancellationToken cancellationToken)
    {
        await using (var deleteCommand = CreateCommand(
            connection,
            transaction,
            """
            DELETE FROM LayoutPositions
            WHERE EventId IN (SELECT Id FROM Events WHERE ProjectId = $projectId);
            """))
        {
            deleteCommand.Parameters.AddWithValue("$projectId", project.Id.ToString("D"));
            await deleteCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        foreach (var position in project.LayoutPositions)
        {
            await using var command = CreateCommand(
                connection,
                transaction,
                """
                INSERT INTO LayoutPositions (
                    EventId, Orientation, HorizontalOffset, VerticalOffset)
                VALUES ($eventId, $orientation, $horizontalOffset, $verticalOffset);
                """);
            AddParameter(command, "$eventId", position.EventId.ToString("D"));
            AddParameter(command, "$orientation", (int)position.Orientation);
            AddParameter(command, "$horizontalOffset", position.HorizontalOffset);
            AddParameter(command, "$verticalOffset", position.VerticalOffset);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task DeleteRemovedItemsAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        Guid projectId,
        CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(
            connection,
            transaction,
            """
            DELETE FROM Attachments
            WHERE EventId IN (SELECT Id FROM Events WHERE ProjectId = $projectId)
              AND Id NOT IN (SELECT Id FROM SaveAttachmentIds);

            DELETE FROM WebLinks
            WHERE EventId IN (SELECT Id FROM Events WHERE ProjectId = $projectId)
              AND Id NOT IN (SELECT Id FROM SaveWebLinkIds);

            DELETE FROM Events
            WHERE ProjectId = $projectId
              AND Id NOT IN (SELECT Id FROM SaveEventIds);

            DELETE FROM Tags
            WHERE NOT EXISTS (SELECT 1 FROM EventTags WHERE EventTags.TagId = Tags.Id);
            """);
        command.Parameters.AddWithValue("$projectId", projectId.ToString("D"));
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    internal static async Task RefreshSearchIndexAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        Guid projectId,
        CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(
            connection,
            transaction,
            """
            DELETE FROM SearchIndex WHERE ProjectId = $projectId;
            DELETE FROM DocumentSearchIndex WHERE ProjectId = $projectId;

            INSERT INTO SearchIndex (ProjectId, EventId, Content)
            SELECT
                e.ProjectId,
                e.Id,
                trim(
                    p.Name || char(10) ||
                    COALESCE(p.Description, '') || char(10) ||
                    e.Title || char(10) ||
                    COALESCE(e.InfoText, '') || char(10) ||
                    COALESCE(e.Description, '') || char(10) ||
                    COALESCE(e.Notes, '') || char(10) ||
                    COALESCE(e.Source, '') || char(10) ||
                    COALESCE((
                        SELECT group_concat(t.Name, ' ')
                        FROM EventTags et
                        JOIN Tags t ON t.Id = et.TagId
                        WHERE et.EventId = e.Id), '') || char(10) ||
                    COALESCE((
                        SELECT group_concat(a.OriginalFileName, ' ')
                        FROM Attachments a
                        WHERE a.EventId = e.Id), '') || char(10) ||
                    COALESCE((
                        SELECT group_concat(x.Content, char(10))
                        FROM Attachments a
                        JOIN ExtractedTexts x ON x.AttachmentId = a.Id
                        WHERE a.EventId = e.Id), '') || char(10) ||
                    COALESCE((
                        SELECT group_concat(w.Address, ' ')
                        FROM WebLinks w
                        WHERE w.EventId = e.Id), '')
                )
            FROM Events e
            JOIN Projects p ON p.Id = e.ProjectId
            WHERE e.ProjectId = $projectId;

            INSERT INTO DocumentSearchIndex (ProjectId, EventId, Content)
            SELECT
                e.ProjectId,
                e.Id,
                COALESCE(group_concat(x.Content, char(10)), '')
            FROM Events e
            JOIN Attachments a ON a.EventId = e.Id
            JOIN ExtractedTexts x ON x.AttachmentId = a.Id
            WHERE e.ProjectId = $projectId
            GROUP BY e.ProjectId, e.Id;
            """);
        command.Parameters.AddWithValue("$projectId", projectId.ToString("D"));
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}
