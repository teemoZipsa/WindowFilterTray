using WindowFilterTray.Models;
using System.Diagnostics;

namespace WindowFilterTray.Services;

public sealed class SafetyPolicy
{
    private static readonly HashSet<string> AlwaysExcludedProcesses = new(StringComparer.OrdinalIgnoreCase)
    {
        "svchost.exe",
        "dwm.exe",
        "lsass.exe",
        "winlogon.exe",
        "csrss.exe",
        "SecurityHealthSystray.exe",
        "taskmgr.exe",
        "consent.exe",
        "LogonUI.exe"
    };

    private static readonly string CurrentProcessName = GetCurrentProcessName();

    private static readonly HashSet<string> AlwaysExcludedClasses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Shell_TrayWnd",
        "Progman",
        "WorkerW"
    };

    private static readonly string[] NegativeKeywords =
    [
        "업데이트",
        "보안",
        "로그인",
        "인증",
        "결제",
        "암호"
    ];

    private static readonly string[] ProtectedTitlePhrases =
    [
        "Windows 보안",
        "Windows 업데이트",
        "Windows Security",
        "Windows Update",
        "Windows",
        "Microsoft",
        "Microsoft Defender"
    ];

    public bool IsExcluded(WindowSnapshot snapshot, out string reason)
    {
        if (AlwaysExcludedProcesses.Contains(snapshot.ProcessName))
        {
            reason = $"강제 예외 프로세스: {snapshot.ProcessName}";
            return true;
        }

        if (snapshot.ProcessName.Equals(CurrentProcessName, StringComparison.OrdinalIgnoreCase))
        {
            reason = "현재 앱 창";
            return true;
        }

        if (snapshot.ProcessName.Equals("explorer.exe", StringComparison.OrdinalIgnoreCase)
            && AlwaysExcludedClasses.Contains(snapshot.ClassName))
        {
            reason = "Explorer 셸 창";
            return true;
        }

        if (AlwaysExcludedClasses.Contains(snapshot.ClassName))
        {
            reason = $"강제 예외 창 클래스: {snapshot.ClassName}";
            return true;
        }

        var title = snapshot.Title ?? string.Empty;
        var phrase = ProtectedTitlePhrases.FirstOrDefault(k => title.Contains(k, StringComparison.OrdinalIgnoreCase));
        if (phrase is not null)
        {
            reason = $"보호 창 제목: {phrase}";
            return true;
        }

        var keyword = NegativeKeywords.FirstOrDefault(k => title.Contains(k, StringComparison.OrdinalIgnoreCase));
        if (keyword is not null)
        {
            reason = $"안전 키워드: {keyword}";
            return true;
        }

        reason = string.Empty;
        return false;
    }

    private static string GetCurrentProcessName()
    {
        using var process = Process.GetCurrentProcess();
        return process.ProcessName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            ? process.ProcessName
            : $"{process.ProcessName}.exe";
    }
}
