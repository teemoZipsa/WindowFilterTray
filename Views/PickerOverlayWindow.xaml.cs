using System.Windows;
using System.Windows.Input;
using WindowFilterTray.Interop;
using WindowFilterTray.Models;
using WindowFilterTray.Services;

namespace WindowFilterTray.Views;

public partial class PickerOverlayWindow : Window
{
    private readonly WindowInspector _inspector;

    public PickerOverlayWindow(WindowInspector inspector)
    {
        _inspector = inspector;
        InitializeComponent();
        Left = SystemParameters.VirtualScreenLeft;
        Top = SystemParameters.VirtualScreenTop;
        Width = SystemParameters.VirtualScreenWidth;
        Height = SystemParameters.VirtualScreenHeight;
    }

    public event EventHandler<WindowSnapshot>? WindowSelected;

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var point = PointToScreen(e.GetPosition(this));
        Hide();
        var snapshot = _inspector.Capture(NativeMethods.WindowFromPoint(new POINT((int)point.X, (int)point.Y)));
        Close();

        if (snapshot is not null)
        {
            WindowSelected?.Invoke(this, snapshot);
        }
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
        }
    }
}
