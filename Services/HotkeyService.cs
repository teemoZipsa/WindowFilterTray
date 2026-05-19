using System.Windows.Interop;
using WindowFilterTray.Interop;

namespace WindowFilterTray.Services;

public sealed class HotkeyService : IDisposable
{
    private const int CaptureHotkeyId = 1001;
    private const int PauseHotkeyId = 1002;

    private readonly IntPtr _hwnd;
    private readonly HwndSource _source;
    private bool _disposed;

    public HotkeyService(IntPtr hwnd)
    {
        _hwnd = hwnd;
        _source = HwndSource.FromHwnd(hwnd) ?? throw new InvalidOperationException("Unable to attach hotkey source.");
        _source.AddHook(WndProc);
    }

    public event EventHandler? CaptureRequested;
    public event EventHandler? PauseToggleRequested;

    public void RegisterDefaults()
    {
        NativeMethods.RegisterHotKey(_hwnd, CaptureHotkeyId, NativeMethods.MOD_CONTROL | NativeMethods.MOD_ALT, NativeMethods.VK_X);
        NativeMethods.RegisterHotKey(_hwnd, PauseHotkeyId, NativeMethods.MOD_CONTROL | NativeMethods.MOD_ALT, NativeMethods.VK_P);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg != NativeMethods.WM_HOTKEY)
        {
            return IntPtr.Zero;
        }

        var id = wParam.ToInt32();
        if (id == CaptureHotkeyId)
        {
            CaptureRequested?.Invoke(this, EventArgs.Empty);
            handled = true;
        }
        else if (id == PauseHotkeyId)
        {
            PauseToggleRequested?.Invoke(this, EventArgs.Empty);
            handled = true;
        }

        return IntPtr.Zero;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        NativeMethods.UnregisterHotKey(_hwnd, CaptureHotkeyId);
        NativeMethods.UnregisterHotKey(_hwnd, PauseHotkeyId);
        _source.RemoveHook(WndProc);
    }
}
