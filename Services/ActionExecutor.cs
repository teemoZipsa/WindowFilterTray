using WindowFilterTray.Interop;
using WindowFilterTray.Models;

namespace WindowFilterTray.Services;

public sealed class ActionExecutor
{
    public bool Execute(IntPtr hwnd, WindowActionType action)
    {
        if (hwnd == IntPtr.Zero || !NativeMethods.IsWindow(hwnd))
        {
            return false;
        }

        return action switch
        {
            WindowActionType.CloseWindow => NativeMethods.PostMessage(hwnd, NativeMethods.WM_CLOSE, IntPtr.Zero, IntPtr.Zero),
            WindowActionType.HideWindow => ShowWindowAndTreatAsAccepted(hwnd, NativeMethods.SW_HIDE),
            WindowActionType.Minimize => ShowWindowAndTreatAsAccepted(hwnd, NativeMethods.SW_MINIMIZE),
            WindowActionType.Ignore => true,
            _ => false
        };
    }

    public bool Undo(IntPtr hwnd, WindowActionType action)
    {
        if (hwnd == IntPtr.Zero || !NativeMethods.IsWindow(hwnd))
        {
            return false;
        }

        return action switch
        {
            WindowActionType.HideWindow => ShowWindowAndTreatAsAccepted(hwnd, NativeMethods.SW_SHOW),
            WindowActionType.Minimize => ShowWindowAndTreatAsAccepted(hwnd, NativeMethods.SW_RESTORE),
            _ => false
        };
    }

    private static bool ShowWindowAndTreatAsAccepted(IntPtr hwnd, int command)
    {
        NativeMethods.ShowWindow(hwnd, command);
        return true;
    }
}
