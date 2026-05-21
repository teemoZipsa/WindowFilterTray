using WindowFilterTray.Interop;
using WindowFilterTray.Models;

namespace WindowFilterTray.Services;

public sealed class EventHookService : IDisposable
{
    private static readonly TimeSpan DuplicateWindowEventWindow = TimeSpan.FromMilliseconds(250);

    private readonly WindowInspector _inspector;
    private readonly NativeMethods.WinEventDelegate _callback;
    private readonly List<IntPtr> _hooks = [];
    private readonly Dictionary<IntPtr, DateTimeOffset> _lastObservedAt = [];
    private bool _disposed;

    public EventHookService(WindowInspector inspector)
    {
        _inspector = inspector;
        _callback = HandleWinEvent;
    }

    public event EventHandler<WindowSnapshot>? WindowObserved;

    public void Start()
    {
        // WINEVENT_OUTOFCONTEXT callbacks are expected to marshal back to the WPF UI thread that starts the hooks.
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

        if (IsDuplicateWindowEvent(hwnd))
        {
            return;
        }

        var snapshot = _inspector.Capture(hwnd);
        if (snapshot is not null)
        {
            WindowObserved?.Invoke(this, snapshot);
        }
    }

    private bool IsDuplicateWindowEvent(IntPtr hwnd)
    {
        var now = DateTimeOffset.UtcNow;
        if (_lastObservedAt.TryGetValue(hwnd, out var lastSeen)
            && now - lastSeen < DuplicateWindowEventWindow)
        {
            return true;
        }

        _lastObservedAt[hwnd] = now;
        foreach (var stale in _lastObservedAt
            .Where(item => now - item.Value > TimeSpan.FromSeconds(5))
            .Select(item => item.Key)
            .ToList())
        {
            _lastObservedAt.Remove(stale);
        }

        return false;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _lastObservedAt.Clear();
        foreach (var hook in _hooks)
        {
            NativeMethods.UnhookWinEvent(hook);
        }

        _hooks.Clear();
    }
}
