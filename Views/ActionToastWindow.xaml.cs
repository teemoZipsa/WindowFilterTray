using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using WindowFilterTray.Models;

namespace WindowFilterTray.Views;

public partial class ActionToastWindow : Window
{
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(8) };

    public ActionToastWindow(
        string windowTitle,
        string processName,
        WindowActionType action,
        Action? undoAction,
        Action openLogAction)
    {
        UndoAction = undoAction;
        OpenLogAction = openLogAction;
        InitializeComponent();

        DetailText.Text = string.IsNullOrWhiteSpace(windowTitle) ? "이름 없는 창" : windowTitle;
        MetaText.Text = string.IsNullOrWhiteSpace(processName) ? "앱 정보 없음" : processName;
        ConfigureAction(action);
        UndoButton.Visibility = undoAction is null ? Visibility.Collapsed : Visibility.Visible;

        Loaded += (_, _) => MoveToBottomRight();
        _timer.Tick += (_, _) => Close();
        _timer.Start();
    }

    private Action? UndoAction { get; }
    private Action OpenLogAction { get; }

    private void UndoButton_Click(object sender, RoutedEventArgs e)
    {
        UndoAction?.Invoke();
        Close();
    }

    private void OpenLogButton_Click(object sender, RoutedEventArgs e)
    {
        OpenLogAction();
        Close();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void ConfigureAction(WindowActionType action)
    {
        var (label, icon, bg, fg) = action switch
        {
            WindowActionType.HideWindow => ("숨기기", "H", "#FFF7E6", "#8A5A00"),
            WindowActionType.CloseWindow => ("닫기", "!", "#FFF0EE", "#C44538"),
            WindowActionType.Ignore => ("기록만", "L", "#EEF0F3", "#4B5260"),
            _ => ("작게 내리기", "M", "#EAF3FF", "#1E5FAA")
        };

        ActionText.Text = label;
        IconText.Text = icon;
        var background = (SolidColorBrush)new BrushConverter().ConvertFromString(bg)!;
        var foreground = (SolidColorBrush)new BrushConverter().ConvertFromString(fg)!;
        ActionBadge.Background = background;
        IconBadge.Background = background;
        ActionText.Foreground = foreground;
        IconText.Foreground = foreground;
    }

    private void MoveToBottomRight()
    {
        Left = SystemParameters.WorkArea.Right - ActualWidth - 18;
        Top = SystemParameters.WorkArea.Bottom - ActualHeight - 18;
    }
}
