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
            SeenAt = DateTimeOffset.UtcNow
        };
    }

    public WindowSnapshot? CaptureFromCursor()
    {
        return NativeMethods.GetCursorPos(out var point)
            ? Capture(NativeMethods.WindowFromPoint(point))
            : null;
    }

    public WindowSnapshot? CaptureFromPointExcluding(int x, int y, IntPtr excludedHwnd)
    {
        var point = new POINT(x, y);
        IntPtr best = IntPtr.Zero;
        NativeMethods.EnumWindows((hwnd, _) =>
        {
            if (hwnd == IntPtr.Zero || hwnd == excludedHwnd)
            {
                return true;
            }

            var root = NativeMethods.GetAncestor(hwnd, NativeMethods.GA_ROOT);
            if (root != IntPtr.Zero && root == excludedHwnd)
            {
                return true;
            }

            if (!NativeMethods.IsWindowVisible(hwnd) || !NativeMethods.GetWindowRect(hwnd, out var rect))
            {
                return true;
            }

            var windowRect = new WindowRect(rect.Left, rect.Top, rect.Right, rect.Bottom);
            if (windowRect.Width <= 0 || windowRect.Height <= 0)
            {
                return true;
            }

            if (point.X < rect.Left || point.X >= rect.Right || point.Y < rect.Top || point.Y >= rect.Bottom)
            {
                return true;
            }

            best = hwnd;
            return false;
        }, IntPtr.Zero);

        return best == IntPtr.Zero ? null : Capture(best);
    }

    private static string GetWindowText(IntPtr hwnd)
    {
        var capacity = Math.Max(256, NativeMethods.GetWindowTextLength(hwnd) + 1);
        var builder = new StringBuilder(capacity);
        NativeMethods.GetWindowText(hwnd, builder, builder.Capacity);
        return builder.ToString();
    }

    private static string GetClassName(IntPtr hwnd)
    {
        var capacity = 256;
        while (capacity <= 4096)
        {
            var builder = new StringBuilder(capacity);
            var length = NativeMethods.GetClassName(hwnd, builder, builder.Capacity);
            if (length < builder.Capacity - 1)
            {
                return builder.ToString();
            }

            capacity *= 2;
        }

        return string.Empty;
    }

    private static string GetProcessName(int processId)
    {
        if (processId <= 0)
        {
            return string.Empty;
        }

        var imageName = GetProcessImageName(processId);
        if (!string.IsNullOrWhiteSpace(imageName))
        {
            return EnsureExeExtension(imageName);
        }

        try
        {
            using var process = Process.GetProcessById(processId);
            return EnsureExeExtension(process.ProcessName);
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string GetProcessImageName(int processId)
    {
        var handle = NativeMethods.OpenProcess(
            NativeMethods.PROCESS_QUERY_LIMITED_INFORMATION,
            false,
            (uint)processId);

        if (handle == IntPtr.Zero)
        {
            return string.Empty;
        }

        try
        {
            var capacity = 1024;
            var builder = new StringBuilder(capacity);
            if (!NativeMethods.QueryFullProcessImageName(handle, 0, builder, ref capacity))
            {
                return string.Empty;
            }

            return Path.GetFileName(builder.ToString());
        }
        finally
        {
            NativeMethods.CloseHandle(handle);
        }
    }

    private static string EnsureExeExtension(string name)
    {
        return name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? name : $"{name}.exe";
    }
}
