using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using WindowFilterTray.Models;
using WindowFilterTray.Views.Pages;

namespace WindowFilterTray;

public partial class MainWindow : Window, IMainShell
{
    private readonly App _app;
    private readonly DashboardPage _dashboardPage;
    private RuleEditorPage? _editorPage;
    private RuleEditorReturnTarget _editorReturnTarget = RuleEditorReturnTarget.Dashboard;
    private bool _suppressNavigation;
    private bool _initializing = true;

    public MainWindow(App app)
    {
        _app = app;
        InitializeComponent();

        _dashboardPage = new DashboardPage(app, this);
        PageHost.Content = _dashboardPage;
        AutoStartCheckBox.IsChecked = _app.Settings.AutoStart;
        PauseCheckBox.IsChecked = _app.Settings.IsPaused;
        RefreshShellState();

        _app.Logs.CollectionChanged += AppCollection_Changed;
        _app.Rules.CollectionChanged += AppCollection_Changed;
        _initializing = false;
    }

    public bool AllowClose { get; set; }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!ConfirmDiscardEditorChanges())
        {
            e.Cancel = true;
            return;
        }

        DiscardEditorChanges(RuleEditorReturnTarget.Dashboard);

        if (AllowClose)
        {
            return;
        }

        e.Cancel = true;
        Hide();
    }

    public void StartPicker()
    {
        _app.StartPicker();
    }

    public void OpenRuleEditor(WindowSnapshot snapshot)
    {
        if (!ConfirmDiscardEditorChanges())
        {
            return;
        }

        ShowRuleEditor(new RuleEditorPage(snapshot, _app.Settings.FilteringMode), RuleEditorReturnTarget.Recent);
    }

    public void EditRule(WindowRule rule)
    {
        if (!ConfirmDiscardEditorChanges())
        {
            return;
        }

        ShowRuleEditor(new RuleEditorPage(rule), RuleEditorReturnTarget.Rules);
    }

    public void DeleteRule(WindowRule rule)
    {
        _app.DeleteRule(rule);
        RefreshShellState();
    }

    public void SetFilteringMode(FilteringMode mode)
    {
        _app.SetFilteringMode(mode);
        RefreshShellState();
    }

    public void SetAutoStart(bool enabled)
    {
        _app.SetAutoStart(enabled);
    }

    public bool TrySetRuleEnabled(WindowRule rule, bool enabled, out string error)
    {
        error = string.Empty;
        var previous = !enabled;
        rule.Enabled = enabled;

        try
        {
            _app.SaveAll();
            RefreshShellState();
            return true;
        }
        catch (Exception ex)
        {
            rule.Enabled = previous;
            error = $"규칙 상태를 저장하지 못했습니다: {ex.Message}";
            return false;
        }
    }

    public bool ConfirmDiscardEditorChanges()
    {
        if (_editorPage is null || !_editorPage.IsDirty)
        {
            return true;
        }

        var result = System.Windows.MessageBox.Show(
            "변경사항을 버리시겠습니까?",
            "편집 중인 규칙",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        return result == MessageBoxResult.Yes;
    }

    public void DiscardEditorChangesToRecent()
    {
        DiscardEditorChanges(RuleEditorReturnTarget.Recent);
    }

    public void ShowLogsSection()
    {
        if (!ConfirmDiscardEditorChanges())
        {
            return;
        }

        ShowDashboardContent();
        NavigateToReturnTarget(RuleEditorReturnTarget.Logs);
    }

    public void RefreshShellState()
    {
        StatusModeText.Text = _app.Settings.FilteringMode switch
        {
            FilteringMode.Off => "구경만",
            FilteringMode.Low => "조심",
            FilteringMode.Optimal => "적당",
            FilteringMode.Strong => "적극",
            _ => string.Empty
        };

        var today = DateTimeOffset.Now.Date;
        var cleanedToday = _app.Logs.Count(log =>
            log.At.LocalDateTime.Date == today
            && !string.Equals(log.Action, nameof(WindowActionType.Ignore), StringComparison.Ordinal));

        TodayCleanedText.Text = cleanedToday.ToString();
        StatusValueText.Text = _app.Settings.IsPaused ? "일시정지됨" : "감시 중";
        StatusDot.Fill = _app.Settings.IsPaused
            ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(184, 134, 11))
            : new SolidColorBrush(System.Windows.Media.Color.FromRgb(47, 154, 104));
        _dashboardPage.RefreshDashboardState();
    }

    private void PauseCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_initializing)
        {
            return;
        }

        _app.SetPaused(PauseCheckBox.IsChecked == true);
        RefreshShellState();
    }

    private void AutoStartCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_initializing)
        {
            return;
        }

        SetAutoStart(AutoStartCheckBox.IsChecked == true);
    }

    private void Picker_Click(object sender, RoutedEventArgs e)
    {
        StartPicker();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void NavDashboard_Checked(object sender, RoutedEventArgs e)
    {
        if (_suppressNavigation || !PrepareDashboardNavigation())
        {
            return;
        }

        _dashboardPage.ScrollToDashboard();
    }

    private void NavRules_Checked(object sender, RoutedEventArgs e)
    {
        if (_suppressNavigation || !PrepareDashboardNavigation())
        {
            return;
        }

        _dashboardPage.ShowRulesSection();
    }

    private void NavRecent_Checked(object sender, RoutedEventArgs e)
    {
        if (_suppressNavigation || !PrepareDashboardNavigation())
        {
            return;
        }

        _dashboardPage.ShowRecentSection();
    }

    private void NavLogs_Checked(object sender, RoutedEventArgs e)
    {
        if (_suppressNavigation || !PrepareDashboardNavigation())
        {
            return;
        }

        _dashboardPage.ShowLogsSection();
    }

    private void NavSettings_Checked(object sender, RoutedEventArgs e)
    {
        if (_suppressNavigation || !PrepareDashboardNavigation())
        {
            return;
        }

        _dashboardPage.ShowSettingsSection();
    }

    private bool PrepareDashboardNavigation()
    {
        if (!ConfirmDiscardEditorChanges())
        {
            ClearNavSelection();
            return false;
        }

        DiscardEditorChanges(RuleEditorReturnTarget.Dashboard);
        ShowDashboardContent();
        return true;
    }

    private void ShowRuleEditor(RuleEditorPage page, RuleEditorReturnTarget returnTarget)
    {
        DisconnectEditor();
        _editorPage = page;
        _editorReturnTarget = returnTarget;
        page.SaveRequested += RuleEditor_SaveRequested;
        page.CancelRequested += RuleEditor_CancelRequested;
        PageHost.Content = page;
        ClearNavSelection();
        page.Focus();
    }

    private void RuleEditor_SaveRequested(object? sender, RuleEditorSaveRequestedEventArgs e)
    {
        var success = e.IsEdit
            ? _app.TryUpdateRuleFromEditor(e.Rule, out var error)
            : _app.TryAddRuleFromEditor(e.Rule, e.Snapshot, out error);

        if (!success)
        {
            System.Windows.MessageBox.Show(error, "규칙 저장", MessageBoxButton.OK, MessageBoxImage.Warning);
            if (e.IsEdit && error.Contains("삭제", StringComparison.Ordinal))
            {
                CloseEditorAndReturn(RuleEditorReturnTarget.Rules);
            }

            return;
        }

        CloseEditorAndReturn(_editorReturnTarget);
    }

    private void RuleEditor_CancelRequested(object? sender, EventArgs e)
    {
        if (!ConfirmDiscardEditorChanges())
        {
            return;
        }

        CloseEditorAndReturn(_editorReturnTarget);
    }

    private void CloseEditorAndReturn(RuleEditorReturnTarget target)
    {
        DisconnectEditor();
        ShowDashboardContent();
        NavigateToReturnTarget(target);
    }

    private void DiscardEditorChanges(RuleEditorReturnTarget target)
    {
        if (_editorPage is null)
        {
            return;
        }

        DisconnectEditor();
        ShowDashboardContent();
        NavigateToReturnTarget(target);
    }

    private void DisconnectEditor()
    {
        if (_editorPage is null)
        {
            return;
        }

        _editorPage.SaveRequested -= RuleEditor_SaveRequested;
        _editorPage.CancelRequested -= RuleEditor_CancelRequested;
        _editorPage = null;
    }

    private void ShowDashboardContent()
    {
        if (PageHost.Content != _dashboardPage)
        {
            PageHost.Content = _dashboardPage;
        }
    }

    private void NavigateToReturnTarget(RuleEditorReturnTarget target)
    {
        RefreshShellState();
        switch (target)
        {
            case RuleEditorReturnTarget.Rules:
                SetNavSelection(RulesNavButton);
                _dashboardPage.ShowRulesSection();
                break;
            case RuleEditorReturnTarget.Recent:
                SetNavSelection(RecentNavButton);
                _dashboardPage.ShowRecentSection();
                break;
            case RuleEditorReturnTarget.Settings:
                SetNavSelection(SettingsNavButton);
                _dashboardPage.ShowSettingsSection();
                break;
            case RuleEditorReturnTarget.Logs:
                SetNavSelection(LogsNavButton);
                _dashboardPage.ShowLogsSection();
                break;
            default:
                SetNavSelection(DashboardNavButton);
                _dashboardPage.ScrollToDashboard();
                break;
        }
    }

    private void ClearNavSelection()
    {
        _suppressNavigation = true;
        DashboardNavButton.IsChecked = false;
        RulesNavButton.IsChecked = false;
        RecentNavButton.IsChecked = false;
        LogsNavButton.IsChecked = false;
        SettingsNavButton.IsChecked = false;
        _suppressNavigation = false;
    }

    private void SetNavSelection(System.Windows.Controls.RadioButton button)
    {
        _suppressNavigation = true;
        DashboardNavButton.IsChecked = button == DashboardNavButton;
        RulesNavButton.IsChecked = button == RulesNavButton;
        RecentNavButton.IsChecked = button == RecentNavButton;
        LogsNavButton.IsChecked = button == LogsNavButton;
        SettingsNavButton.IsChecked = button == SettingsNavButton;
        _suppressNavigation = false;
    }

    private void AppCollection_Changed(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RefreshShellState();
    }

    private enum RuleEditorReturnTarget
    {
        Dashboard,
        Rules,
        Recent,
        Logs,
        Settings
    }
}
