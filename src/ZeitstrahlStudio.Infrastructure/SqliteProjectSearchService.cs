using System.Globalization;
using System.Text;
using Microsoft.Data.Sqlite;
using ZeitstrahlStudio.Application;
using ZeitstrahlStudio.Domain;

namespace ZeitstrahlStudio.Infrastructure;

/// <summary>
/// Kombiniert den persistenten FTS5-Index für Dokumentinhalte mit dem aktuellen
/// Projektaggregat, damit noch ungespeicherte fachliche Änderungen sofort auffindbar sind.
/// </summary>
public sealed class SqliteProjectSearchService : IProjectSearchService
{
    private const int MaximumResults = 5_000;
    private const int MaximumTerms = 32;
    private const int MaximumTermLength = 64;
    private static readonly CompareInfo GermanComparison =
        CultureInfo.GetCultureInfo("de-DE").CompareInfo;
    private const CompareOptions SearchCompareOptions =
        CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace;

    public async Task<IReadOnlyList<SearchResult>> SearchAsync(
        ProjectWorkspace workspace,
        SearchCriteria criteria,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentNullException.ThrowIfNull(criteria);
        if (criteria.From.HasValue && criteria.Until.HasValue && criteria.Until < criteria.From)
        {
            throw new ArgumentException(
                "Das Ende des Suchzeitraums darf nicht vor dessen Beginn liegen.",
                nameof(criteria));
        }

        var terms = Tokenize(criteria.Query);
        var ftsMatches = terms.Count > 0
            ? await ReadFullTextMatchesAsync(workspace, terms, cancellationToken).ConfigureAwait(false)
            : new Dictionary<Guid, FullTextMatch>();
        var results = new List<SearchResult>();
        foreach (var timelineEvent in workspace.Project.GetChronologicalEvents())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!MatchesFilters(timelineEvent, criteria))
            {
                continue;
            }

            var currentFields = GetCurrentSearchFields(workspace.Project, timelineEvent);
            var currentMatch = terms.Count == 0 || terms.All(term =>
                currentFields.Any(field => Contains(field.Value, term)));
            var hasFullTextMatch = ftsMatches.TryGetValue(timelineEvent.Id, out var fullTextMatch);
            if (terms.Count > 0 && !currentMatch && !hasFullTextMatch)
            {
                continue;
            }

            var highlights = new List<string>(4);
            if (fullTextMatch is not null && !string.IsNullOrWhiteSpace(fullTextMatch.Snippet))
            {
                highlights.Add(NormalizeSnippet(fullTextMatch.Snippet));
            }

            foreach (var field in currentFields)
            {
                if (highlights.Count >= 4)
                {
                    break;
                }

                var highlighted = HighlightFirstMatch(field, terms);
                if (highlighted is not null && !highlights.Contains(highlighted, StringComparer.Ordinal))
                {
                    highlights.Add(highlighted);
                }
            }

            var currentRelevance = terms.Count == 0
                ? 0
                : terms.Count(term => currentFields.Any(field => Contains(field.Value, term)));
            var relevance = hasFullTextMatch
                ? 10 + Math.Max(0, fullTextMatch!.Relevance * 100_000)
                : currentRelevance;
            results.Add(new SearchResult(
                timelineEvent.Id,
                timelineEvent.Title,
                timelineEvent.Date,
                relevance,
                highlights));
            if (results.Count == MaximumResults)
            {
                break;
            }
        }

        return results
            .OrderByDescending(result => result.Relevance)
            .ThenBy(result => result.Date.SortStart)
            .ThenBy(result => result.EventTitle, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }

    private static async Task<Dictionary<Guid, FullTextMatch>> ReadFullTextMatchesAsync(
        ProjectWorkspace workspace,
        IReadOnlyList<string> terms,
        CancellationToken cancellationToken)
    {
        var databasePath = Path.Combine(workspace.WorkingDirectory, "project.db");
        if (!File.Exists(databasePath))
        {
            return [];
        }

        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = Path.GetFullPath(databasePath),
            Mode = SqliteOpenMode.ReadOnly,
            Cache = SqliteCacheMode.Private,
            Pooling = false,
            DefaultTimeout = 5,
        }.ToString();
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                EventId,
                -bm25(DocumentSearchIndex) AS Relevance,
                snippet(DocumentSearchIndex, 2, '⟦', '⟧', ' … ', 24) AS Highlight
            FROM DocumentSearchIndex
            WHERE ProjectId = $projectId
              AND DocumentSearchIndex MATCH $query
            ORDER BY bm25(DocumentSearchIndex)
            LIMIT $maximumResults;
            """;
        command.Parameters.AddWithValue("$projectId", workspace.Project.Id.ToString("D"));
        command.Parameters.AddWithValue(
            "$query",
            string.Join(" AND ", terms.Select(term => $@"""{term}""*")));
        command.Parameters.AddWithValue("$maximumResults", MaximumResults);
        var matches = new Dictionary<Guid, FullTextMatch>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            if (Guid.TryParse(reader.GetString(0), out var eventId))
            {
                matches[eventId] = new FullTextMatch(
                    reader.GetDouble(1),
                    reader.IsDBNull(2) ? string.Empty : reader.GetString(2));
            }
        }

        return matches;
    }

    private static bool MatchesFilters(TimelineEvent timelineEvent, SearchCriteria criteria)
    {
        var start = DateOnly.FromDateTime(timelineEvent.Date.SortStart);
        var end = GetEventEnd(timelineEvent);
        if (criteria.From.HasValue && end < criteria.From.Value ||
            criteria.Until.HasValue && start > criteria.Until.Value ||
            criteria.Precision.HasValue && timelineEvent.Date.Precision != criteria.Precision.Value ||
            criteria.HasDeadline.HasValue && (timelineEvent.Deadline is not null) != criteria.HasDeadline.Value ||
            criteria.DeadlineStatus.HasValue && timelineEvent.Deadline?.Status != criteria.DeadlineStatus.Value ||
            criteria.Priority.HasValue && timelineEvent.Priority != criteria.Priority.Value ||
            !MatchesOptional(criteria.ColorHex, timelineEvent.ColorHex) ||
            !MatchesTag(timelineEvent, criteria.Tag) ||
            !MatchesMediaType(timelineEvent, criteria.MediaType) ||
            criteria.HasAttachment.HasValue &&
            (timelineEvent.Attachments.Count > 0) != criteria.HasAttachment.Value ||
            criteria.HasPdf.HasValue &&
            timelineEvent.Attachments.Any(IsPdf) != criteria.HasPdf.Value)
        {
            return false;
        }

        return true;
    }

    private static bool MatchesTag(TimelineEvent timelineEvent, string? tag) =>
        string.IsNullOrWhiteSpace(tag) ||
        timelineEvent.Tags.Contains(tag.Trim(), StringComparer.CurrentCultureIgnoreCase);

    private static bool MatchesMediaType(TimelineEvent timelineEvent, string? mediaType)
    {
        if (string.IsNullOrWhiteSpace(mediaType))
        {
            return true;
        }

        var normalized = mediaType.Trim();
        return timelineEvent.Attachments.Any(attachment =>
            normalized.EndsWith("/", StringComparison.Ordinal)
                ? attachment.MediaType.StartsWith(normalized, StringComparison.OrdinalIgnoreCase)
                : string.Equals(attachment.MediaType, normalized, StringComparison.OrdinalIgnoreCase));
    }

    private static bool MatchesOptional(string? expected, string actual) =>
        string.IsNullOrWhiteSpace(expected) ||
        string.Equals(expected.Trim(), actual, StringComparison.OrdinalIgnoreCase);

    private static bool IsPdf(Attachment attachment) =>
        string.Equals(attachment.MediaType, "application/pdf", StringComparison.OrdinalIgnoreCase);

    private static DateOnly GetEventEnd(TimelineEvent timelineEvent) =>
        timelineEvent.Date.EndYear.HasValue
            ? new DateOnly(
                timelineEvent.Date.EndYear.Value,
                timelineEvent.Date.EndMonth!.Value,
                timelineEvent.Date.EndDay!.Value)
            : DateOnly.FromDateTime(timelineEvent.Date.SortStart);

    private static IReadOnlyList<SearchField> GetCurrentSearchFields(
        TimelineProject project,
        TimelineEvent timelineEvent)
    {
        var result = new List<SearchField>
        {
            new("Projekt", project.Name),
            new("Projektbeschreibung", project.Description ?? string.Empty),
            new("Titel", timelineEvent.Title),
            new("Infotext", timelineEvent.InfoText ?? string.Empty),
            new("Beschreibung", timelineEvent.Description ?? string.Empty),
            new("Notizen", timelineEvent.Notes ?? string.Empty),
            new("Quelle", timelineEvent.Source ?? string.Empty),
            new("Schlagwörter", string.Join(", ", timelineEvent.Tags)),
            new("Dateien", string.Join(", ", timelineEvent.Attachments.Select(item => item.OriginalFileName))),
            new("Webseiten", string.Join(", ", timelineEvent.WebLinks.Select(item => item.Address.AbsoluteUri))),
        };
        return result;
    }

    private static string? HighlightFirstMatch(SearchField field, IReadOnlyList<string> terms)
    {
        if (string.IsNullOrWhiteSpace(field.Value) || terms.Count == 0)
        {
            return null;
        }

        foreach (var term in terms)
        {
            var index = GermanComparison.IndexOf(field.Value, term, SearchCompareOptions);
            if (index < 0)
            {
                continue;
            }

            const int contextLength = 62;
            var start = Math.Max(0, index - contextLength);
            var end = Math.Min(field.Value.Length, index + term.Length + contextLength);
            var excerpt = field.Value[start..end].ReplaceLineEndings(" ");
            var localIndex = index - start;
            if (localIndex + term.Length <= excerpt.Length)
            {
                excerpt = excerpt.Insert(localIndex + term.Length, "⟧").Insert(localIndex, "⟦");
            }

            return $"{field.Label}: {(start > 0 ? "… " : string.Empty)}{excerpt}{(end < field.Value.Length ? " …" : string.Empty)}";
        }

        return null;
    }

    private static bool Contains(string value, string term) =>
        !string.IsNullOrWhiteSpace(value) &&
        GermanComparison.IndexOf(value, term, SearchCompareOptions) >= 0;

    private static IReadOnlyList<string> Tokenize(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        var result = new List<string>();
        var token = new StringBuilder();
        foreach (var character in query.AsSpan().Trim())
        {
            if (char.IsLetterOrDigit(character))
            {
                if (token.Length < MaximumTermLength)
                {
                    token.Append(character);
                }
            }
            else
            {
                AddToken(result, token);
                if (result.Count == MaximumTerms)
                {
                    break;
                }
            }
        }

        AddToken(result, token);
        return result;
    }

    private static void AddToken(List<string> result, StringBuilder token)
    {
        if (token.Length == 0 || result.Count == MaximumTerms)
        {
            token.Clear();
            return;
        }

        var value = token.ToString();
        if (!result.Contains(value, StringComparer.OrdinalIgnoreCase))
        {
            result.Add(value);
        }

        token.Clear();
    }

    private static string NormalizeSnippet(string value) =>
        string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private sealed record FullTextMatch(double Relevance, string Snippet);

    private sealed record SearchField(string Label, string Value);
}
