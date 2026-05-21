using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WindowFilterTray.Models;

namespace WindowFilterTray.Views;

public partial class TrayPopoverControl : UserControl
{
    private readonly Action _openApp;
    private readonly Action _startPicker;
    private readonly Action _showLogs;
    private readonly Action _togglePause;
    private readonly Action _exitApp;
    private readonly Action _closePopup;
    private readonly Action<WindowSnapshot> _createRuleFromRecent;

    public TrayPopoverControl(
        Action openApp,
        Action startPicker,
        Action showLogs,
        Action togglePause,
        Action exitApp,
        Action closePopup,
        Action<WindowSnapshot> createRuleFromRecent)
    {
        InitializeComponent();
        _openApp = openApp;
        _startPicker = startPicker;
        _showLogs = showLogs;
        _togglePause = togglePause;
        _exitApp = exitApp;
        _closePopup = closePopup;
        _createRuleFromRecent = createRuleFromRecent;
    }

    public void Update(TrayPopoverSnapshot snapshot)
    {
        StatusText.Text = snapshot.StatusText;
        StatusDescriptionText.Text = snapshot.StatusDescription;
        TodayCountText.Text = snapshot.TodayCleanedCount.ToString();
        ActiveRuleCountText.Text = snapshot.ActiveRuleCount.ToString();
        PauseButton.Content = snapshot.IsPaused ? "다시 시작" : "잠시 멈춤";
        RecentItems.ItemsSource = snapshot.RecentWindows;
        RecentEmptyText.Visibility = snapshot.RecentWindows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        var isPaused = snapshot.IsPaused || snapshot.IsOff;
        StatusBadge.Background = isPaused
            ? new SolidColorBrush(Color.FromRgb(255, 247, 214))
            : new SolidColorBrush(Color.FromRgb(234, 243, 255));
        StatusText.Foreground = isPaused
            ? new SolidColorBrush(Color.FromRgb(184, 134, 11))
            : new SolidColorBrush(Color.FromRgb(30, 95, 170));
    }

    public void FocusInitialAction()
    {
        Dispatcher.BeginInvoke(() =>
        {
            Focus();
            Keyboard.Focus(OpenButton);
        });
    }

    private void OpenButton_Click(object sender, RoutedEventArgs e)
    {
        RunAndClose(_openApp);
    }

    private void PickerButton_Click(object sender, RoutedEventArgs e)
    {
        RunAndClose(_startPicker);
    }

    private void LogsButton_Click(object sender, RoutedEventArgs e)
    {
        RunAndClose(_showLogs);
    }

    private void PauseButton_Click(object sender, RoutedEventArgs e)
    {
        RunAndClose(_togglePause);
    }

    private void ExitButton_Click(object sender, RoutedEventArgs e)
    {
        RunAndClose(_exitApp);
    }

    private void RecentButton_Click(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).DataContext is WindowSnapshot snapshot)
        {
            RunAndClose(() => _createRuleFromRecent(snapshot));
        }
    }

    private void UserControl_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape)
        {
            return;
        }

        e.Handled = true;
        _closePopup();
    }

    private void RunAndClose(Action action)
    {
        _closePopup();
        action();
    }
}

public sealed class TrayPopoverSnapshot
{
    public string StatusText { get; init; } = string.Empty;
    public string StatusDescription { get; init; } = string.Empty;
    public int TodayCleanedCount { get; init; }
    public int ActiveRuleCount { get; init; }
    public bool IsPaused { get; init; }
    public bool IsOff { get; init; }
    public IReadOnlyList<WindowSnapshot> RecentWindows { get; init; } = [];
}
