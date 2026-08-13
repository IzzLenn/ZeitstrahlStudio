using System.ComponentModel;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Xml.Linq;

namespace ZeitstrahlStudio.IntegrationTests;

public sealed class CheckBoxThemeTests
{
    [Fact]
    public void SharedCheckBoxTemplate_KeepsEveryStateVisibleWithoutHoverOrFocusSurfaceReplacement()
    {
        var path = Path.Combine(FindAppRoot(), "Themes", "ControlStyles.xaml");
        var document = XDocument.Load(path);
        var checkBoxStyle = document.Root!
            .Elements()
            .Single(element =>
                element.Name.LocalName == "Style" &&
                string.Equals((string?)element.Attribute("TargetType"), "CheckBox", StringComparison.Ordinal));
        var template = checkBoxStyle
            .Descendants()
            .Single(element =>
                element.Name.LocalName == "ControlTemplate" &&
                string.Equals((string?)element.Attribute("TargetType"), "CheckBox", StringComparison.Ordinal));

        Assert.NotNull(FindNamedElement(template, "CheckBoxBorder"));
        Assert.NotNull(FindNamedElement(template, "CheckMark"));
        Assert.NotNull(FindNamedElement(template, "IndeterminateMark"));
        Assert.NotNull(FindTrigger(template, "IsChecked", "True"));
        Assert.NotNull(FindTrigger(template, "IsChecked", "{x:Null}"));

        foreach (var property in new[] { "IsMouseOver", "IsKeyboardFocused" })
        {
            var trigger = FindTrigger(template, property, "True");
            Assert.DoesNotContain(
                trigger.Elements().Where(element => element.Name.LocalName == "Setter"),
                setter => string.Equals((string?)setter.Attribute("Property"), "Background", StringComparison.Ordinal));
        }

        Assert.DoesNotContain(
            checkBoxStyle.Elements().Where(element => element.Name.LocalName == "Setter"),
            setter => string.Equals((string?)setter.Attribute("Property"), "Focusable", StringComparison.Ordinal));
    }

    [Fact]
    public async Task CheckBoxTemplate_DarkThemeRendersEveryStateAndKeepsTwoWayToggleBinding()
    {
        await RunOnStaThread(() =>
        {
            var state = new CheckBoxState { Value = false };
            var uncheckedBox = new CheckBox { Content = "Nicht ausgewählt" };
            uncheckedBox.SetBinding(
                ToggleButton.IsCheckedProperty,
                new Binding(nameof(CheckBoxState.Value))
                {
                    Source = state,
                    Mode = BindingMode.TwoWay,
                });
            var checkedBox = new CheckBox
            {
                Content = "Ausgewählt",
                IsChecked = true,
            };
            var indeterminateBox = new CheckBox
            {
                Content = "Teilweise ausgewählt",
                IsChecked = null,
                IsThreeState = true,
            };
            var disabledCheckedBox = new CheckBox
            {
                Content = "Deaktiviert ausgewählt",
                IsChecked = true,
                IsEnabled = false,
            };
            var host = CreateDarkThemeHost(uncheckedBox, checkedBox, indeterminateBox, disabledCheckedBox);
            Layout(host, 320, 140);

            Assert.True(uncheckedBox.Focusable);
            Assert.True(uncheckedBox.IsTabStop);
            AssertCheckBoxState(
                uncheckedBox,
                expectedBackground: Color.FromRgb(0x0F, 0x17, 0x2A),
                checkVisibility: Visibility.Collapsed,
                indeterminateVisibility: Visibility.Collapsed);
            AssertCheckBoxState(
                checkedBox,
                expectedBackground: Color.FromRgb(0x3B, 0x82, 0xF6),
                checkVisibility: Visibility.Visible,
                indeterminateVisibility: Visibility.Collapsed);
            AssertCheckBoxState(
                indeterminateBox,
                expectedBackground: Color.FromRgb(0x3B, 0x82, 0xF6),
                checkVisibility: Visibility.Collapsed,
                indeterminateVisibility: Visibility.Visible);
            AssertCheckBoxState(
                disabledCheckedBox,
                expectedBackground: Color.FromRgb(0x33, 0x41, 0x55),
                checkVisibility: Visibility.Visible,
                indeterminateVisibility: Visibility.Collapsed);

            var automationPeer = new CheckBoxAutomationPeer(uncheckedBox);
            var toggleProvider = Assert.IsAssignableFrom<IToggleProvider>(
                automationPeer.GetPattern(PatternInterface.Toggle));
            toggleProvider.Toggle();
            uncheckedBox.UpdateLayout();

            Assert.True(uncheckedBox.IsChecked);
            Assert.Equal(true, state.Value);
            AssertCheckBoxState(
                uncheckedBox,
                expectedBackground: Color.FromRgb(0x3B, 0x82, 0xF6),
                checkVisibility: Visibility.Visible,
                indeterminateVisibility: Visibility.Collapsed);
        });
    }

    private static Border CreateDarkThemeHost(params CheckBox[] checkBoxes)
    {
        var host = new Border();
        host.Resources.MergedDictionaries.Add(CreateDictionary("Theme.Dark.xaml"));
        host.Resources.MergedDictionaries.Add(CreateDictionary("Typography.xaml"));
        host.Resources.MergedDictionaries.Add(CreateDictionary("ControlStyles.xaml"));

        var panel = new StackPanel();
        foreach (var checkBox in checkBoxes)
        {
            panel.Children.Add(checkBox);
        }

        host.Child = panel;
        return host;
    }

    private static ResourceDictionary CreateDictionary(string fileName) =>
        new()
        {
            Source = new Uri(
                $"/ZeitstrahlStudio.App;component/Themes/{fileName}",
                UriKind.Relative),
        };

    private static void AssertCheckBoxState(
        CheckBox checkBox,
        Color expectedBackground,
        Visibility checkVisibility,
        Visibility indeterminateVisibility)
    {
        _ = checkBox.ApplyTemplate();
        checkBox.UpdateLayout();
        var border = Assert.IsType<Border>(checkBox.Template.FindName("CheckBoxBorder", checkBox));
        var checkMark = Assert.IsType<System.Windows.Shapes.Path>(
            checkBox.Template.FindName("CheckMark", checkBox));
        var indeterminateMark = Assert.IsType<System.Windows.Shapes.Rectangle>(
            checkBox.Template.FindName("IndeterminateMark", checkBox));

        var backgroundColor = Assert.IsType<SolidColorBrush>(border.Background).Color;
        Assert.Equal(expectedBackground, backgroundColor);
        Assert.NotEqual(Colors.White, backgroundColor);
        Assert.Equal(checkVisibility, checkMark.Visibility);
        Assert.Equal(indeterminateVisibility, indeterminateMark.Visibility);

        if (checkVisibility == Visibility.Visible)
        {
            var checkColor = Assert.IsType<SolidColorBrush>(checkMark.Stroke).Color;
            Assert.NotEqual(Colors.Transparent, checkColor);
            Assert.NotEqual(backgroundColor, checkColor);
        }

        if (indeterminateVisibility == Visibility.Visible)
        {
            var indeterminateColor = Assert.IsType<SolidColorBrush>(indeterminateMark.Fill).Color;
            Assert.NotEqual(Colors.Transparent, indeterminateColor);
            Assert.NotEqual(backgroundColor, indeterminateColor);
        }
    }

    private static XElement FindNamedElement(XElement root, string name)
    {
        XNamespace xaml = "http://schemas.microsoft.com/winfx/2006/xaml";
        return root.Descendants().Single(element =>
            string.Equals((string?)element.Attribute(xaml + "Name"), name, StringComparison.Ordinal));
    }

    private static XElement FindTrigger(XElement root, string property, string value) =>
        root.Descendants().Single(element =>
            element.Name.LocalName == "Trigger" &&
            string.Equals((string?)element.Attribute("Property"), property, StringComparison.Ordinal) &&
            string.Equals((string?)element.Attribute("Value"), value, StringComparison.Ordinal));

    private static string FindAppRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "ZeitstrahlStudio.sln")))
        {
            directory = directory.Parent;
        }

        if (directory is null)
        {
            throw new DirectoryNotFoundException("Der Repository-Stamm konnte nicht ermittelt werden.");
        }

        return Path.Combine(directory.FullName, "src", "ZeitstrahlStudio.App");
    }

    private static void Layout(FrameworkElement element, double width, double height)
    {
        element.Measure(new Size(width, height));
        element.Arrange(new Rect(0, 0, width, height));
        element.UpdateLayout();
    }

    private static Task RunOnStaThread(Action action)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try
            {
                action();
                completion.SetResult();
            }
            catch (Exception exception)
            {
                completion.SetException(exception);
            }
            finally
            {
                System.Windows.Threading.Dispatcher.CurrentDispatcher.InvokeShutdown();
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return AwaitCompletionAsync(completion.Task, thread);
    }

    private static async Task AwaitCompletionAsync(Task completion, Thread thread)
    {
        await completion.WaitAsync(TimeSpan.FromSeconds(30));
        thread.Join(TimeSpan.FromSeconds(5));
    }

    private sealed class CheckBoxState : INotifyPropertyChanged
    {
        private bool? value;

        public event PropertyChangedEventHandler? PropertyChanged;

        public bool? Value
        {
            get => value;
            set
            {
                if (value == this.value)
                {
                    return;
                }

                this.value = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
            }
        }
    }
}