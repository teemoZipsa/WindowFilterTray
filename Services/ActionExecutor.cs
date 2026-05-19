using WindowFilterTray.Interop;
using WindowFilterTray.Models;

namespace WindowFilterTray.Services;

public sealed class ActionExecutor
{
    public bool Execute(IntPtr hwnd, WindowActionType action)
    {
        return action switch
        {
            WindowActionType.CloseWindow => NativeMethods.PostMessage(hwnd, NativeMethods.WM_CLOSE, IntPtr.Zero, IntPtr.Zero),
            WindowActionType.HideWindow => NativeMethods.ShowWindow(hwnd, NativeMethods.SW_HIDE),
            WindowActionType.Minimize => NativeMethods.ShowWindow(hwnd, NativeMethods.SW_MINIMIZE),
            WindowActionType.Ignore => true,
            _ => false
        };
    }
}
