using WindowFilterTray.Models;

namespace WindowFilterTray.Services;

public sealed class KnownPopupTemplateService
{
    private static readonly string[] PopupTitleKeywords =
    [
        "광고",
        "AD",
        "이벤트",
        "쇼핑",
        "추천",
        "특가",
        "프로모션",
        "혜택",
        "쿠폰"
    ];

    private static readonly TemplateTarget[] Targets =
    [
        new("nateon", "네이트온", ["NATEON.exe", "NateOnMain.exe"]),
        new("potplayer", "팟플레이어", ["PotPlayerMini.exe", "PotPlayerMini64.exe", "PotPlayer.exe", "PotPlayer64.exe"]),
        new("alzip", "알집", ["ALZip.exe", "ALZipCon.exe"]),
        new("alyac", "알약", ["ALYac.exe", "ALYac64.exe", "AYAgent.exe", "AYLaunch.exe"]),
        new("gomplayer", "곰플레이어", ["GOM.exe", "GOM64.exe", "GOMPlayer.exe"]),
        new("bandizip", "반디집", ["Bandizip.exe", "Bandizip32.exe", "Bandizip64.exe"]),
        new("honeyview", "꿀뷰", ["Honeyview.exe"])
    ];

    private readonly Dictionary<WindowActionType, IReadOnlyList<WindowRule>> _rulesByAction = [];

    public IReadOnlyList<WindowRule> GetRules(WindowActionType action)
    {
        if (_rulesByAction.TryGetValue(action, out var rules))
        {
            return rules;
        }

        rules = BuildRules(action);
        _rulesByAction[action] = rules;
        return rules;
    }

    private static IReadOnlyList<WindowRule> BuildRules(WindowActionType action)
    {
        var rules = new List<WindowRule>();
        foreach (var target in Targets)
        {
            foreach (var processName in target.ProcessNames)
            {
                for (var keywordIndex = 0; keywordIndex < PopupTitleKeywords.Length; keywordIndex++)
                {
                    var keyword = PopupTitleKeywords[keywordIndex];
                    rules.Add(new WindowRule
                    {
                        Id = $"known-popup-{target.Id}-{NormalizeIdPart(processName)}-{keywordIndex}",
                        DisplayName = $"알려진 반복 팝업: {target.DisplayName}",
                        Enabled = true,
                        CreatedAt = DateTimeOffset.UnixEpoch,
                        Action = action,
                        GraceMs = 500,
                        FrequencyCap = new FrequencyCap
                        {
                            Mode = FrequencyCapMode.None,
                            MaxImpressions = 0
                        },
                        Matcher = new RuleMatcher
                        {
                            ProcessName = processName,
                            TitleContains = keyword,
                            UseProcessName = true,
                            UseTitle = true,
                            UseWindowClass = false,
                            UseSize = false,
                            UsePosition = false,
                            MinScore = 70
                        }
                    });
                }
            }
        }

        return rules;
    }

    private static string NormalizeIdPart(string value)
    {
        return new string(value
            .Where(char.IsLetterOrDigit)
            .Select(char.ToLowerInvariant)
            .ToArray());
    }

    private sealed record TemplateTarget(string Id, string DisplayName, string[] ProcessNames);
}
