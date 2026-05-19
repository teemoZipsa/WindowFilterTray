using Microsoft.Win32;

namespace WindowFilterTray.Services;

public sealed class StartupService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private readonly string _appName;

    public StartupService(string appName)
    {
        _appName = appName;
    }

    public bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        return key?.GetValue(_appName) is string value && value.Length > 0;
    }

    public void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
            ?? Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);

        if (enabled)
        {
            var exePath = Environment.ProcessPath ?? string.Empty;
            key.SetValue(_appName, $"\"{exePath}\"");
        }
        else
        {
            key.DeleteValue(_appName, throwOnMissingValue: false);
        }
    }
}
