using WindowFilterTray.Interop;
using WindowFilterTray.Models;

namespace WindowFilterTray.Services;

public sealed class EventHookService : IDisposable
{
    private readonly WindowInspector _inspector;
    private readonly NativeMethods.WinEventDelegate _callback;
    private readonly List<IntPtr> _hooks = [];
    private bool _disposed;

    public EventHookService(WindowInspector inspector)
    {
        _inspector = inspector;
        _callback = HandleWinEvent;
    }

    public event EventHandler<WindowSnapshot>? WindowObserved;

    public void Start()
    {
        AddHook(NativeMethods.EVENT_OBJECT_SHOW, NativeMethods.EVENT_OBJECT_SHOW);
        AddHook(NativeMethods.EVENT_SYSTEM_FOREGROUND, NativeMethods.EVENT_SYSTEM_FOREGROUND);
        AddHook(NativeMethods.EVENT_OBJECT_CREATE, NativeMethods.EVENT_OBJECT_CREATE);
    }

    private void AddHook(uint minEvent, uint maxEvent)
    {
        var hook = NativeMethods.SetWinEventHook(
            minEvent,
            maxEvent,
            IntPtr.Zero,
            _callback,
            0,
            0,
            NativeMethods.WINEVENT_OUTOFCONTEXT);

        if (hook != IntPtr.Zero)
        {
            _hooks.Add(hook);
        }
    }

    private void HandleWinEvent(
        IntPtr hWinEventHook,
        uint eventType,
        IntPtr hwnd,
        int idObject,
        int idChild,
        uint dwEventThread,
        uint dwmsEventTime)
    {
        if (_disposed || hwnd == IntPtr.Zero || idObject != NativeMethods.OBJID_WINDOW)
        {
            return;
        }

        var snapshot = _inspector.Capture(hwnd);
        if (snapshot is not null)
        {
            WindowObserved?.Invoke(this, snapshot);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        foreach (var hook in _hooks)
        {
            NativeMethods.UnhookWinEvent(hook);
        }

        _hooks.Clear();
    }
}
