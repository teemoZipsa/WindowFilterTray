using WindowFilterTray.Models;

namespace WindowFilterTray.Views.Pages;

public interface IMainShell
{
    void StartPicker();
    void OpenRuleEditor(WindowSnapshot snapshot);
    void EditRule(WindowRule rule);
    void DeleteRule(WindowRule rule);
    void SetFilteringMode(FilteringMode mode);
    void SetAutoStart(bool enabled);
    bool TrySetRuleEnabled(WindowRule rule, bool enabled, out string error);
    void RefreshShellState();
}
