using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using WindowFilterTray.Interop;
using WindowFilterTray.Models;
using WindowFilterTray.Services;

namespace WindowFilterTray.Views;

public partial class PickerOverlayWindow : Window
{
    private readonly WindowInspector _inspector;
    private WindowSnapshot? _currentSnapshot;
    private IntPtr _overlayHwnd;

    public PickerOverlayWindow(WindowInspector inspector)
    {
        _inspector = inspector;
        InitializeComponent();
        Left = SystemParameters.VirtualScreenLeft;
        Top = SystemParameters.VirtualScreenTop;
        Width = SystemParameters.VirtualScreenWidth;
        Height = SystemParameters.VirtualScreenHeight;
        Loaded += Window_Loaded;
    }

    public event EventHandler<WindowSnapshot>? WindowSelected;

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        _overlayHwnd = new WindowInteropHelper(this).Handle;
        System.Windows.Controls.Canvas.SetLeft(GuidePanel, Math.Max(24, (ActualWidth - GuidePanel.Width) / 2));
        Focus();
    }

    private void Window_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        UpdateTarget(PointToScreen(e.GetPosition(this)));
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        UpdateTarget(PointToScreen(e.GetPosition(this)));
        if (_currentSnapshot is null)
        {
            return;
        }

        var selected = _currentSnapshot;
        Close();
        WindowSelected?.Invoke(this, selected);
    }

    private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
        }
    }

    private void UpdateTarget(System.Windows.Point screenPoint)
    {
        if (IsPointInsideGuidePanel(PointFromScreen(screenPoint)))
        {
            _currentSnapshot = null;
            HighlightBorder.Visibility = Visibility.Collapsed;
            InfoPanel.Visibility = Visibility.Collapsed;
            return;
        }

        var snapshot = _inspector.CaptureFromPointExcluding(
            (int)screenPoint.X,
            (int)screenPoint.Y,
            _overlayHwnd);

        if (snapshot is null)
        {
            _currentSnapshot = null;
            HighlightBorder.Visibility = Visibility.Collapsed;
            InfoPanel.Visibility = Visibility.Collapsed;
            return;
        }

        if (IsShellSurface(snapshot))
        {
            _currentSnapshot = null;
            HighlightBorder.Visibility = Visibility.Collapsed;
            InfoPanel.Visibility = Visibility.Collapsed;
            return;
        }

        if (_currentSnapshot?.HWnd == snapshot.HWnd)
        {
            return;
        }

        _currentSnapshot = snapshot;
        UpdateHighlight(snapshot);
    }

    private void UpdateHighlight(WindowSnapshot snapshot)
    {
        var rect = snapshot.Rect;
        var topLeft = PointFromScreen(new System.Windows.Point(rect.Left, rect.Top));
        var bottomRight = PointFromScreen(new System.Windows.Point(rect.Right, rect.Bottom));
        var left = topLeft.X;
        var top = topLeft.Y;
        var width = Math.Max(0, bottomRight.X - topLeft.X);
        var height = Math.Max(0, bottomRight.Y - topLeft.Y);

        System.Windows.Controls.Canvas.SetLeft(HighlightBorder, left);
        System.Windows.Controls.Canvas.SetTop(HighlightBorder, top);
        HighlightBorder.Width = width;
        HighlightBorder.Height = height;
        HighlightBorder.Visibility = Visibility.Visible;

        InfoTitleText.Text = string.IsNullOrWhiteSpace(snapshot.Title) ? "이름 없는 창" : snapshot.Title;
        InfoProcessText.Text = $"{snapshot.ProcessName} · {Math.Round(width)}x{Math.Round(height)}";
        InfoClassText.Text = snapshot.ClassName;

        var panelLeft = left;
        var panelTop = top + height + 10;
        if (panelTop + 96 > ActualHeight)
        {
            panelTop = Math.Max(10, top - 106);
        }

        panelLeft = Math.Max(10, Math.Min(panelLeft, ActualWidth - InfoPanel.Width - 10));
        System.Windows.Controls.Canvas.SetLeft(InfoPanel, panelLeft);
        System.Windows.Controls.Canvas.SetTop(InfoPanel, panelTop);
        InfoPanel.Visibility = Visibility.Visible;
    }

    private static bool IsShellSurface(WindowSnapshot snapshot)
    {
        return snapshot.ClassName.Equals("Shell_TrayWnd", StringComparison.OrdinalIgnoreCase)
            || snapshot.ClassName.Equals("Progman", StringComparison.OrdinalIgnoreCase)
            || snapshot.ClassName.Equals("WorkerW", StringComparison.OrdinalIgnoreCase);
    }

    private bool IsPointInsideGuidePanel(System.Windows.Point point)
    {
        var left = System.Windows.Controls.Canvas.GetLeft(GuidePanel);
        var top = System.Windows.Controls.Canvas.GetTop(GuidePanel);
        return point.X >= left
            && point.X <= left + GuidePanel.ActualWidth
            && point.Y >= top
            && point.Y <= top + GuidePanel.ActualHeight;
    }
}
