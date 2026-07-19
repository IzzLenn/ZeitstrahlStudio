using System.Collections.ObjectModel;
using ZeitstrahlStudio.Application;
using ZeitstrahlStudio.Domain;

namespace ZeitstrahlStudio.App;

public sealed partial class MainWindowViewModel
{
    private CancellationTokenSource? searchCancellation;
    private string searchQuery = string.Empty;
    private DateTime? searchFrom;
    private DateTime? searchUntil;
    private DatePrecision? searchPrecision;
    private bool? searchHasDeadline;
    private DeadlineStatus? searchDeadlineStatus;
    private EventPriority? searchPriority;
    private string? searchColor;
    private string? searchTag;
    private string? searchMediaType;
    private bool? searchHasAttachment;
    private bool? searchHasPdf;
    private SearchSortMode searchSortMode;
    private bool isSearching;
    private bool suppressSearchRefresh;
    private int timelineSelectionNavigationRevision;
    private IReadOnlyCollection<Guid>? timelineVisibleEventIds;

    public ObservableCollection<SearchResult> SearchResults { get; } = [];

    public IReadOnlyList<SearchChoice> SearchPrecisionOptions { get; } =
    [
        new("Alle Datumsarten", null),
        new("Exaktes Datum", DatePrecision.ExactDate),
        new("Datum mit Uhrzeit", DatePrecision.ExactDateTime),
        new("Monat und Jahr", DatePrecision.MonthAndYear),
        new("Nur Jahr", DatePrecision.Year),
        new("Zeitraum", DatePrecision.DateRange),
    ];

    public IReadOnlyList<SearchChoice> SearchDeadlinePresenceOptions { get; } =
    [
        new("Frist: alle", null),
        new("Mit Frist", true),
        new("Ohne Frist", false),
    ];

    public IReadOnlyList<SearchChoice> SearchDeadlineStatusOptions { get; } =
    [
        new("Friststatus: alle", null),
        new("Offen", DeadlineStatus.Open),
        new("Erledigt", DeadlineStatus.Completed),
        new("Abgebrochen", DeadlineStatus.Cancelled),
    ];

    public IReadOnlyList<SearchChoice> SearchPriorityOptions { get; } =
    [
        new("Priorität: alle", null),
        new("Niedrig", EventPriority.Low),
        new("Normal", EventPriority.Normal),
        new("Hoch", EventPriority.High),
        new("Kritisch", EventPriority.Critical),
    ];

    public IReadOnlyList<SearchChoice> SearchAttachmentPresenceOptions { get; } =
    [
        new("Anhänge: alle", null),
        new("Mit Anhang", true),
        new("Ohne Anhang", false),
    ];

    public IReadOnlyList<SearchChoice> SearchPdfPresenceOptions { get; } =
    [
        new("PDF: alle", null),
        new("Mit PDF", true),
        new("Ohne PDF", false),
    ];

    public IReadOnlyList<SearchChoice> SearchSortOptions { get; } =
    [
        new("Relevanz", SearchSortMode.Relevance),
        new("Datum", SearchSortMode.Date),
    ];

    public IReadOnlyList<SearchChoice> SearchColorOptions =>
    [
        new("Farbe: alle", null),
        .. (CurrentProject?.Events
            .Select(timelineEvent => timelineEvent.ColorHex)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .Select(value => new SearchChoice(value, value)) ?? []),
    ];

    public IReadOnlyList<SearchChoice> SearchTagOptions =>
    [
        new("Schlagwort: alle", null),
        .. (CurrentProject?.Events
            .SelectMany(timelineEvent => timelineEvent.Tags)
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .Order(StringComparer.CurrentCultureIgnoreCase)
            .Select(value => new SearchChoice(value, value)) ?? []),
    ];

    public IReadOnlyList<SearchChoice> SearchMediaTypeOptions
    {
        get
        {
            var choices = new List<SearchChoice>
            {
                new("Dateityp: alle", null),
                new("PDF", "application/pdf"),
                new("Bilder", "image/"),
                new("Word-Dokumente", "application/vnd.openxmlformats-officedocument.wordprocessingml.document"),
                new("Excel-Arbeitsmappen", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"),
            };
            var known = choices
                .Where(choice => choice.Value is string)
                .Select(choice => (string)choice.Value!)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var mediaType in CurrentProject?.Events
                         .SelectMany(timelineEvent => timelineEvent.Attachments)
                         .Select(attachment => attachment.MediaType)
                         .Distinct(StringComparer.OrdinalIgnoreCase)
                         .Order(StringComparer.OrdinalIgnoreCase) ?? Enumerable.Empty<string>())
            {
                if (!known.Contains(mediaType) &&
                    !(mediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) &&
                      known.Contains("image/")))
                {
                    choices.Add(new SearchChoice(mediaType, mediaType));
                }
            }

            return choices;
        }
    }

    public AsyncRelayCommand ResetSearchFiltersCommand { get; private set; } = null!;
    public AsyncRelayCommand<SearchResult> SelectSearchResultCommand { get; private set; } = null!;

    public string SearchQuery
    {
        get => searchQuery;
        set
        {
            if (SetProperty(ref searchQuery, value ?? string.Empty))
            {
                OnSearchInputChanged();
            }
        }
    }

    public DateTime? SearchFrom
    {
        get => searchFrom;
        set
        {
            if (SetProperty(ref searchFrom, value))
            {
                OnSearchInputChanged();
            }
        }
    }

    public DateTime? SearchUntil
    {
        get => searchUntil;
        set
        {
            if (SetProperty(ref searchUntil, value))
            {
                OnSearchInputChanged();
            }
        }
    }

    public DatePrecision? SearchPrecision
    {
        get => searchPrecision;
        set
        {
            if (SetProperty(ref searchPrecision, value))
            {
                OnSearchInputChanged();
            }
        }
    }

    public bool? SearchHasDeadline
    {
        get => searchHasDeadline;
        set
        {
            if (SetProperty(ref searchHasDeadline, value))
            {
                OnSearchInputChanged();
            }
        }
    }

    public DeadlineStatus? SearchDeadlineStatus
    {
        get => searchDeadlineStatus;
        set
        {
            if (SetProperty(ref searchDeadlineStatus, value))
            {
                OnSearchInputChanged();
            }
        }
    }

    public EventPriority? SearchPriority
    {
        get => searchPriority;
        set
        {
            if (SetProperty(ref searchPriority, value))
            {
                OnSearchInputChanged();
            }
        }
    }

    public string? SearchColor
    {
        get => searchColor;
        set
        {
            if (SetProperty(ref searchColor, value))
            {
                OnSearchInputChanged();
            }
        }
    }

    public string? SearchTag
    {
        get => searchTag;
        set
        {
            if (SetProperty(ref searchTag, value))
            {
                OnSearchInputChanged();
            }
        }
    }

    public string? SearchMediaType
    {
        get => searchMediaType;
        set
        {
            if (SetProperty(ref searchMediaType, value))
            {
                OnSearchInputChanged();
            }
        }
    }

    public bool? SearchHasAttachment
    {
        get => searchHasAttachment;
        set
        {
            if (SetProperty(ref searchHasAttachment, value))
            {
                OnSearchInputChanged();
            }
        }
    }

    public bool? SearchHasPdf
    {
        get => searchHasPdf;
        set
        {
            if (SetProperty(ref searchHasPdf, value))
            {
                OnSearchInputChanged();
            }
        }
    }

    public SearchSortMode SearchSortMode
    {
        get => searchSortMode;
        set
        {
            if (SetProperty(ref searchSortMode, value))
            {
                OnSearchInputChanged(immediate: true);
            }
        }
    }

    public bool IsSearching
    {
        get => isSearching;
        private set
        {
            if (SetProperty(ref isSearching, value))
            {
                OnPropertyChanged(nameof(SearchSummaryText));
            }
        }
    }

    public int ActiveFilterCount =>
        (string.IsNullOrWhiteSpace(SearchQuery) ? 0 : 1) +
        (SearchFrom.HasValue ? 1 : 0) +
        (SearchUntil.HasValue ? 1 : 0) +
        (SearchPrecision.HasValue ? 1 : 0) +
        (SearchHasDeadline.HasValue ? 1 : 0) +
        (SearchDeadlineStatus.HasValue ? 1 : 0) +
        (SearchPriority.HasValue ? 1 : 0) +
        (string.IsNullOrWhiteSpace(SearchColor) ? 0 : 1) +
        (string.IsNullOrWhiteSpace(SearchTag) ? 0 : 1) +
        (string.IsNullOrWhiteSpace(SearchMediaType) ? 0 : 1) +
        (SearchHasAttachment.HasValue ? 1 : 0) +
        (SearchHasPdf.HasValue ? 1 : 0);

    public bool HasInvalidSearchRange =>
        SearchFrom.HasValue &&
        SearchUntil.HasValue &&
        SearchUntil.Value.Date < SearchFrom.Value.Date;
    public string ActiveFilterText => $"{ActiveFilterCount} aktive Filter";
    public string SearchSummaryText => HasInvalidSearchRange
        ? "Ungültiger Zeitraum"
        : IsSearching
            ? "Suche läuft …"
            : $"{SearchResults.Count} Treffer";
    public int TimelineSelectionNavigationRevision => timelineSelectionNavigationRevision;
    public IReadOnlyCollection<Guid>? TimelineVisibleEventIds => timelineVisibleEventIds;

    private void InitializeSearchPresentation()
    {
        ResetSearchFiltersCommand = new AsyncRelayCommand(
            ResetSearchFiltersAsync,
            () => HasProject && ActiveFilterCount > 0);
        SelectSearchResultCommand = new AsyncRelayCommand<SearchResult>(
            SelectSearchResultAsync,
            result => !IsBusy &&
                CurrentProject?.Events.Any(timelineEvent => timelineEvent.Id == result.EventId) == true);
    }

    private void ResetSearchForWorkspace()
    {
        suppressSearchRefresh = true;
        SearchQuery = string.Empty;
        SearchFrom = null;
        SearchUntil = null;
        SearchPrecision = null;
        SearchHasDeadline = null;
        SearchDeadlineStatus = null;
        SearchPriority = null;
        SearchColor = null;
        SearchTag = null;
        SearchMediaType = null;
        SearchHasAttachment = null;
        SearchHasPdf = null;
        SearchSortMode = global::ZeitstrahlStudio.App.SearchSortMode.Relevance;
        suppressSearchRefresh = false;
        RefreshSearchChoiceProperties();
        ScheduleSearch(immediate: true);
    }

    private Task ResetSearchFiltersAsync()
    {
        ResetSearchForWorkspace();
        StatusMessage = "Alle Suchfilter wurden zurückgesetzt.";
        return Task.CompletedTask;
    }

    private Task SelectSearchResultAsync(SearchResult result)
    {
        var timelineEvent = CurrentProject?.Events
            .FirstOrDefault(item => item.Id == result.EventId);
        if (timelineEvent is null)
        {
            return Task.CompletedTask;
        }

        SelectedEvent = timelineEvent;
        timelineSelectionNavigationRevision = unchecked(timelineSelectionNavigationRevision + 1);
        OnPropertyChanged(nameof(TimelineSelectionNavigationRevision));
        StatusMessage = $"Treffer „{result.EventTitle}“ wurde im Zeitstrahl ausgewählt.";
        return Task.CompletedTask;
    }

    private void OnSearchInputChanged(bool immediate = false)
    {
        OnPropertyChanged(nameof(ActiveFilterCount));
        OnPropertyChanged(nameof(ActiveFilterText));
        OnPropertyChanged(nameof(HasInvalidSearchRange));
        OnPropertyChanged(nameof(SearchSummaryText));
        ResetSearchFiltersCommand.RaiseCanExecuteChanged();
        if (!suppressSearchRefresh)
        {
            ScheduleSearch(immediate);
        }
    }

    private void RefreshSearchPresentation()
    {
        RefreshSearchChoiceProperties();
        ScheduleSearch(immediate: true);
    }

    private void RefreshSearchChoiceProperties()
    {
        OnPropertyChanged(nameof(SearchColorOptions));
        OnPropertyChanged(nameof(SearchTagOptions));
        OnPropertyChanged(nameof(SearchMediaTypeOptions));
        OnPropertyChanged(nameof(ActiveFilterCount));
        OnPropertyChanged(nameof(ActiveFilterText));
        ResetSearchFiltersCommand.RaiseCanExecuteChanged();
    }

    private void ScheduleSearch(bool immediate)
    {
        var previous = searchCancellation;
        searchCancellation = null;
        if (previous is not null)
        {
            previous.Cancel();
            previous.Dispose();
        }

        if (CurrentWorkspace is null)
        {
            SearchResults.Clear();
            UpdateTimelineFilter([]);
            IsSearching = false;
            OnPropertyChanged(nameof(SearchSummaryText));
            return;
        }

        if (HasInvalidSearchRange)
        {
            SearchResults.Clear();
            UpdateTimelineFilter([]);
            IsSearching = false;
            OnPropertyChanged(nameof(SearchSummaryText));
            return;
        }

        var cancellation = CancellationTokenSource.CreateLinkedTokenSource(lifetimeCancellation.Token);
        searchCancellation = cancellation;
        IsSearching = true;
        _ = RunSearchAsync(CurrentWorkspace, cancellation, immediate);
    }

    private async Task RunSearchAsync(
        ProjectWorkspace workspace,
        CancellationTokenSource cancellation,
        bool immediate)
    {
        try
        {
            if (!immediate)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(250), cancellation.Token).ConfigureAwait(true);
            }

            var criteria = new SearchCriteria(
                string.IsNullOrWhiteSpace(SearchQuery) ? null : SearchQuery,
                SearchFrom.HasValue ? DateOnly.FromDateTime(SearchFrom.Value) : null,
                SearchUntil.HasValue ? DateOnly.FromDateTime(SearchUntil.Value) : null,
                SearchPrecision,
                SearchHasDeadline,
                SearchDeadlineStatus,
                SearchPriority,
                SearchColor,
                SearchTag,
                SearchMediaType,
                SearchHasAttachment,
                SearchHasPdf);
            var results = await searchService.SearchAsync(
                workspace,
                criteria,
                cancellation.Token).ConfigureAwait(true);
            cancellation.Token.ThrowIfCancellationRequested();
            if (!ReferenceEquals(searchCancellation, cancellation) ||
                CurrentWorkspace?.Project.Id != workspace.Project.Id)
            {
                return;
            }

            var ordered = SearchSortMode == global::ZeitstrahlStudio.App.SearchSortMode.Date
                ? results.OrderBy(result => result.Date.SortStart)
                    .ThenBy(result => result.EventTitle, StringComparer.CurrentCultureIgnoreCase)
                : results.OrderByDescending(result => result.Relevance)
                    .ThenBy(result => result.Date.SortStart)
                    .ThenBy(result => result.EventTitle, StringComparer.CurrentCultureIgnoreCase);
            SearchResults.Clear();
            foreach (var result in ordered)
            {
                SearchResults.Add(result);
            }

            UpdateTimelineFilter(
                ActiveFilterCount == 0
                    ? null
                    : SearchResults.Select(result => result.EventId).ToArray());
            OnPropertyChanged(nameof(SearchSummaryText));
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            StatusMessage = "Die lokale Suche konnte nicht aktualisiert werden.";
            await TryWriteLogAsync(
                LocalLogLevel.Warning,
                "ProjectSearchFailed",
                StatusMessage,
                exception.ToString()).ConfigureAwait(true);
        }
        finally
        {
            if (ReferenceEquals(searchCancellation, cancellation))
            {
                IsSearching = false;
            }
        }
    }

    private void UpdateTimelineFilter(IReadOnlyCollection<Guid>? eventIds)
    {
        timelineVisibleEventIds = eventIds;
        OnPropertyChanged(nameof(TimelineVisibleEventIds));
        RefreshTimelinePresentation();
    }

    private void RaiseSearchCommandStates()
    {
        ResetSearchFiltersCommand.RaiseCanExecuteChanged();
        SelectSearchResultCommand.RaiseCanExecuteChanged();
    }

    private void DisposeSearchPresentation()
    {
        var cancellation = searchCancellation;
        searchCancellation = null;
        if (cancellation is not null)
        {
            cancellation.Cancel();
            cancellation.Dispose();
        }
    }
}
