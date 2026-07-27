using ZeitstrahlStudio.App;
using ZeitstrahlStudio.Domain;

namespace ZeitstrahlStudio.IntegrationTests;

public sealed class ApplicationThemeServiceTests
{
    [Fact]
    public async Task SavedTheme_IsRestoredByANewServiceInstance()
    {
        var directory = Path.Combine(Path.GetTempPath(), "ZeitstrahlStudio.Tests", Guid.NewGuid().ToString("N"));
        var settingsPath = Path.Combine(directory, "appearance-settings.json");
        try
        {
            using (var writer = new ApplicationThemeService(settingsPath))
            {
                await writer.ApplyAndSaveAsync(ApplicationTheme.Dark, CancellationToken.None);
                Assert.Equal(ApplicationTheme.Dark, writer.CurrentTheme);
                Assert.True(writer.IsDark);
            }

            using var reader = new ApplicationThemeService(settingsPath);
            await reader.InitializeAsync(CancellationToken.None);

            Assert.Equal(ApplicationTheme.Dark, reader.CurrentTheme);
            Assert.True(reader.IsDark);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task DamagedThemeFile_FallsBackWithoutBlockingStartup()
    {
        var directory = Path.Combine(Path.GetTempPath(), "ZeitstrahlStudio.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var settingsPath = Path.Combine(directory, "appearance-settings.json");
        try
        {
            await File.WriteAllTextAsync(settingsPath, "{ungültig");
            using var service = new ApplicationThemeService(settingsPath);

            await service.InitializeAsync(CancellationToken.None);

            Assert.Equal(ApplicationTheme.FollowWindows, service.CurrentTheme);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
