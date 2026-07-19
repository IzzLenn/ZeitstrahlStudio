using ZeitstrahlStudio.App;
using ZeitstrahlStudio.Domain;

namespace ZeitstrahlStudio.IntegrationTests;

public sealed class ProjectSettingsDialogViewModelTests
{
    [Fact]
    public void TryBuildResult_ReturnsValidatedProjectSettings()
    {
        var viewModel = new ProjectSettingsDialogViewModel(new ProjectSettings());
        viewModel.SelectedTheme = viewModel.ThemeOptions.Single(option =>
            option.Value == ApplicationTheme.Dark);
        viewModel.SelectedOrientation = viewModel.OrientationOptions.Single(option =>
            option.Value == TimelineOrientation.Vertical);
        viewModel.DefaultEventColorHex = "#AABBCC";
        viewModel.TimelineCardFontSize = 18;
        viewModel.TimelineAxisFontSize = 13;
        viewModel.ExportFontSize = 11;
        viewModel.CompressLargeGaps = false;

        Assert.True(viewModel.TryBuildResult());
        var result = Assert.IsType<ProjectSettings>(viewModel.Result);
        Assert.Equal(ApplicationTheme.Dark, result.Theme);
        Assert.Equal(TimelineOrientation.Vertical, result.PreferredOrientation);
        Assert.Equal("#AABBCC", result.DefaultEventColorHex);
        Assert.Equal(18, result.TimelineCardFontSize);
        Assert.Equal(13, result.TimelineAxisFontSize);
        Assert.Equal(11, result.ExportFontSize);
        Assert.False(result.CompressLargeGaps);
        Assert.False(viewModel.HasError);
    }

    [Fact]
    public void TryBuildResult_ReportsInvalidValuesWithoutResult()
    {
        var viewModel = new ProjectSettingsDialogViewModel(new ProjectSettings())
        {
            DefaultEventColorHex = "rot",
        };

        Assert.False(viewModel.TryBuildResult());
        Assert.Null(viewModel.Result);
        Assert.True(viewModel.HasError);
        Assert.Contains("#RRGGBB", viewModel.ErrorMessage);
    }

    [Fact]
    public void EventEditor_UsesConfiguredDefaultColorOnlyForNewEvents()
    {
        var newEvent = new EventEditorDialogViewModel(null, "#AABBCC");
        var existing = TimelineEvent.Create(
            Guid.NewGuid(),
            "Bestehend",
            EventDate.Year(2026),
            new DateTimeOffset(2026, 7, 19, 12, 0, 0, TimeSpan.Zero));
        existing.SetClassification(
            EventPriority.Normal,
            EventStatus.Active,
            "#112233",
            new DateTimeOffset(2026, 7, 19, 12, 1, 0, TimeSpan.Zero));

        var existingEvent = new EventEditorDialogViewModel(existing, "#AABBCC");

        Assert.Equal("#AABBCC", newEvent.ColorHex);
        Assert.Equal("#112233", existingEvent.ColorHex);
    }
}
