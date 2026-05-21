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
        return key?.GetValue(_appName) is string value
            && !string.IsNullOrWhiteSpace(ExtractExecutablePath(value));
    }

    public void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
            ?? Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);

        if (enabled)
        {
            var exePath = Environment.ProcessPath;
            if (string.IsNullOrWhiteSpace(exePath))
            {
                throw new InvalidOperationException("현재 실행 파일 경로를 확인하지 못해 자동 시작을 설정할 수 없습니다.");
            }

            key.SetValue(_appName, $"\"{exePath}\"");
        }
        else
        {
            key.DeleteValue(_appName, throwOnMissingValue: false);
        }
    }

    private static string ExtractExecutablePath(string value)
    {
        value = value.Trim();
        if (value.Length == 0)
        {
            return string.Empty;
        }

        if (value[0] != '"')
        {
            return value.Split(' ', 2)[0];
        }

        var endQuote = value.IndexOf('"', 1);
        return endQuote <= 1 ? string.Empty : value[1..endQuote];
    }
}
