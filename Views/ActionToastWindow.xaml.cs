using System.Windows;
using System.Windows.Threading;
using WindowFilterTray.Models;

namespace WindowFilterTray.Views;

public partial class ActionToastWindow : Window
{
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(8) };

    public ActionToastWindow(string windowTitle, WindowActionType action, Action? undoAction, Action openLogAction)
    {
        UndoAction = undoAction;
        OpenLogAction = openLogAction;
        InitializeComponent();

        DetailText.Text = string.IsNullOrWhiteSpace(windowTitle) ? "이름 없는 창" : windowTitle;
        UndoButton.Visibility = undoAction is null ? Visibility.Collapsed : Visibility.Visible;
        OpenLogButton.Content = undoAction is null ? "기록 보기" : "닫기";

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
        if (UndoAction is null)
        {
            OpenLogAction();
        }

        Close();
    }

    private void MoveToBottomRight()
    {
        Left = SystemParameters.WorkArea.Right - ActualWidth - 18;
        Top = SystemParameters.WorkArea.Bottom - ActualHeight - 18;
    }
}
