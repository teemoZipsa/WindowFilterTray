using System.Diagnostics;
using System.Text;
using WindowFilterTray.Interop;
using WindowFilterTray.Models;

namespace WindowFilterTray.Services;

public sealed class WindowInspector
{
    public WindowSnapshot? Capture(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero)
        {
            return null;
        }

        var root = NativeMethods.GetAncestor(hwnd, NativeMethods.GA_ROOT);
        if (root != IntPtr.Zero)
        {
            hwnd = root;
        }

        if (!NativeMethods.IsWindowVisible(hwnd) || !NativeMethods.GetWindowRect(hwnd, out var rect))
        {
            return null;
        }

        var windowRect = new WindowRect(rect.Left, rect.Top, rect.Right, rect.Bottom);
        if (windowRect.Width <= 0 || windowRect.Height <= 0)
        {
            return null;
        }

        NativeMethods.GetWindowThreadProcessId(hwnd, out var processId);
        var processName = GetProcessName((int)processId);
        var title = GetWindowText(hwnd);
        var className = GetClassName(hwnd);

        if (string.IsNullOrWhiteSpace(processName) && string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(className))
        {
            return null;
        }

        return new WindowSnapshot
        {
            HWnd = hwnd,
            Title = title,
            ProcessId = (int)processId,
            ProcessName = processName,
            ClassName = className,
            Rect = windowRect,
            SeenAt = DateTimeOffset.Now
        };
    }

    public WindowSnapshot? CaptureFromCursor()
    {
        return NativeMethods.GetCursorPos(out var point)
            ? Capture(NativeMethods.WindowFromPoint(point))
            : null;
    }

    private static string GetWindowText(IntPtr hwnd)
    {
        var builder = new StringBuilder(512);
        NativeMethods.GetWindowText(hwnd, builder, builder.Capacity);
        return builder.ToString();
    }

    private static string GetClassName(IntPtr hwnd)
    {
        var builder = new StringBuilder(256);
        NativeMethods.GetClassName(hwnd, builder, builder.Capacity);
        return builder.ToString();
    }

    private static string GetProcessName(int processId)
    {
        if (processId <= 0)
        {
            return string.Empty;
        }

        try
        {
            using var process = Process.GetProcessById(processId);
            var name = process.ProcessName;
            return name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? name : $"{name}.exe";
        }
        catch
        {
            return string.Empty;
        }
    }
}
