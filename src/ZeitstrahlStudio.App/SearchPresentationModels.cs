namespace ZeitstrahlStudio.App;

/// <summary>Beschrifteter Auswahlwert für die kompakte Filteroberfläche.</summary>
public sealed record SearchChoice(string Label, object? Value);

/// <summary>Sortierung der lokalen Trefferliste.</summary>
public enum SearchSortMode
{
    Relevance,
    Date,
}
