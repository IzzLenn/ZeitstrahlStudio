using System.Windows.Media;
using ZeitstrahlStudio.App;
using ZeitstrahlStudio.Domain;

namespace ZeitstrahlStudio.IntegrationTests;

public sealed class NativeWindowTitleBarThemeTests
{
    private static readonly nint WindowHandle = new(42);

    [Fact]
    public void Windows11DarkMode_UsesImmersiveModeAndExplicitThemeColors()
    {
        var api = new RecordingDwmApi();
        const uint captionColor = 0x002A170F;
        const uint textColor = 0x00FCFAF8;

        NativeWindowTitleBarTheme.ApplyAttributes(
            WindowHandle,
            dark: true,
            supportsExplicitColors: true,
            captionColor,
            textColor,
            api);

        Assert.Equal(
            [
                new AttributeCall(NativeWindowTitleBarTheme.UseImmersiveDarkModeAttribute, 1),
                new AttributeCall(NativeWindowTitleBarTheme.CaptionColorAttribute, captionColor),
                new AttributeCall(NativeWindowTitleBarTheme.TextColorAttribute, textColor),
            ],
            api.Calls);
    }

    [Fact]
    public void LightMode_DisablesImmersiveModeAndResetsExplicitColorsToWindowsDefaults()
    {
        var api = new RecordingDwmApi();

        NativeWindowTitleBarTheme.ApplyAttributes(
            WindowHandle,
            dark: false,
            supportsExplicitColors: true,
            captionColor: 0x002A170F,
            textColor: 0x00FCFAF8,
            api);

        Assert.Equal(
            [
                new AttributeCall(NativeWindowTitleBarTheme.UseImmersiveDarkModeAttribute, 0),
                new AttributeCall(
                    NativeWindowTitleBarTheme.CaptionColorAttribute,
                    NativeWindowTitleBarTheme.DefaultDwmColor),
                new AttributeCall(
                    NativeWindowTitleBarTheme.TextColorAttribute,
                    NativeWindowTitleBarTheme.DefaultDwmColor),
            ],
            api.Calls);
    }

    [Fact]
    public void Windows10Fallback_UsesLegacyImmersiveAttributeOnlyAfterPrimaryFailure()
    {
        var api = new RecordingDwmApi
        {
            ImmersiveResult = unchecked((int)0x80070057),
        };

        NativeWindowTitleBarTheme.ApplyAttributes(
            WindowHandle,
            dark: true,
            supportsExplicitColors: false,
            captionColor: 0,
            textColor: 0,
            api);

        Assert.Equal(
            [
                new AttributeCall(NativeWindowTitleBarTheme.UseImmersiveDarkModeAttribute, 1),
                new AttributeCall(NativeWindowTitleBarTheme.UseImmersiveDarkModeLegacyAttribute, 1),
            ],
            api.Calls);
    }

    [Fact]
    public void ColorConversion_ProducesTheColorRefLayoutExpectedByDwm()
    {
        var color = Color.FromRgb(0x0F, 0x17, 0x2A);

        Assert.Equal(0x002A170Fu, NativeWindowTitleBarTheme.ToColorRef(color));
    }

    [Fact]
    public void ApplicationThemeSwitch_UpdatesTheThemeRememberedForFutureWindows()
    {
        using var service = new ApplicationThemeService(
            Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json"));

        service.Apply(ApplicationTheme.Dark);
        Assert.True(NativeWindowTitleBarTheme.IsDark);

        service.Apply(ApplicationTheme.Light);
        Assert.False(NativeWindowTitleBarTheme.IsDark);
    }

    private sealed class RecordingDwmApi : IDwmWindowAttributeApi
    {
        public List<AttributeCall> Calls { get; } = [];

        public int ImmersiveResult { get; init; }

        public int SetInt32(nint windowHandle, int attribute, int value)
        {
            Assert.Equal(WindowHandle, windowHandle);
            Calls.Add(new AttributeCall(attribute, unchecked((uint)value)));
            return attribute == NativeWindowTitleBarTheme.UseImmersiveDarkModeAttribute
                ? ImmersiveResult
                : 0;
        }

        public int SetUInt32(nint windowHandle, int attribute, uint value)
        {
            Assert.Equal(WindowHandle, windowHandle);
            Calls.Add(new AttributeCall(attribute, value));
            return 0;
        }
    }

    private sealed record AttributeCall(int Attribute, uint Value);
}