using System.Windows.Interop;
using WindowFilterTray.Interop;

namespace WindowFilterTray.Services;

public sealed class HotkeyService : IDisposable
{
    private const int CaptureHotkeyId = 1001;
    private const int PauseHotkeyId = 1002;

    private readonly IntPtr _hwnd;
    private readonly HwndSource _source;
    private readonly HashSet<int> _registeredHotkeyIds = new();
    private bool _disposed;

    public HotkeyService(IntPtr hwnd)
    {
        _hwnd = hwnd;
        _source = HwndSource.FromHwnd(hwnd) ?? throw new InvalidOperationException("Unable to attach hotkey source.");
        _source.AddHook(WndProc);
    }

    public event EventHandler? CaptureRequested;
    public event EventHandler? PauseToggleRequested;

    public IReadOnlyList<string> RegisterDefaults()
    {
        var failures = new List<string>();
        if (!TryRegisterHotKey(CaptureHotkeyId, NativeMethods.MOD_CONTROL | NativeMethods.MOD_ALT, NativeMethods.VK_X))
        {
            failures.Add("Ctrl+Alt+X");
        }

        if (!TryRegisterHotKey(PauseHotkeyId, NativeMethods.MOD_CONTROL | NativeMethods.MOD_ALT, NativeMethods.VK_P))
        {
            failures.Add("Ctrl+Alt+P");
        }

        return failures;
    }

    private bool TryRegisterHotKey(int id, uint modifiers, uint key)
    {
        if (!NativeMethods.RegisterHotKey(_hwnd, id, modifiers, key))
        {
            return false;
        }

        _registeredHotkeyIds.Add(id);
        return true;
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
        foreach (var id in _registeredHotkeyIds)
        {
            NativeMethods.UnregisterHotKey(_hwnd, id);
        }

        _registeredHotkeyIds.Clear();
        _source.RemoveHook(WndProc);
    }
}
