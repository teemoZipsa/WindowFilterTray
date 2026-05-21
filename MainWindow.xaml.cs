using System.ComponentModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WindowFilterTray.Models;
using WindowFilterTray.Views.Pages;

namespace WindowFilterTray;

public partial class MainWindow : Window
{
    private readonly App _app;
    private object? _dashboardContent;
    private RuleEditorPage? _editorPage;
    private RuleEditorReturnTarget _editorReturnTarget = RuleEditorReturnTarget.Dashboard;
    private bool _suppressNavigation;
    private bool _initializing = true;

    public bool AllowClose { get; set; }

    public MainWindow(App app)
    {
        _app = app;
        InitializeComponent();
        _dashboardContent = MainScrollViewer;

        RulesList.ItemsSource = _app.Rules;
        RecentList.ItemsSource = _app.RecentWindows;
        LogList.ItemsSource = _app.Logs;
        ModeSlider.Value = (int)_app.Settings.FilteringMode;
        PauseCheckBox.IsChecked = _app.Settings.IsPaused;
        AutoStartCheckBox.IsChecked = _app.Settings.AutoStart;
        UpdateModeDescription();
        UpdateEmptyStates();
        UpdateDashboardMetrics();
        _app.Rules.CollectionChanged += AppCollection_Changed;
        _app.RecentWindows.CollectionChanged += AppCollection_Changed;
        _app.Logs.CollectionChanged += AppCollection_Changed;
        _initializing = false;
    }

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

    private void ModeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_initializing)
        {
            return;
        }

        _app.SetFilteringMode((FilteringMode)(int)ModeSlider.Value);
        UpdateModeDescription();
    }

    private void PauseCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_initializing)
        {
            return;
        }

        _app.SetPaused(PauseCheckBox.IsChecked == true);
    }

    private void AutoStartCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_initializing)
        {
            return;
        }

        _app.SetAutoStart(AutoStartCheckBox.IsChecked == true);
    }

    private void CreateRuleFromRecent_Click(object sender, RoutedEventArgs e)
    {
        if (RecentList.SelectedItem is WindowSnapshot snapshot)
        {
            _app.OpenRuleEditor(snapshot);
        }
    }

    private void Picker_Click(object sender, RoutedEventArgs e)
    {
        _app.StartPicker();
    }

    private void EditRule_Click(object sender, RoutedEventArgs e)
    {
        if (RulesList.SelectedItem is WindowRule rule)
        {
            _app.EditRule(rule);
        }
    }

    private void DeleteRule_Click(object sender, RoutedEventArgs e)
    {
        if (RulesList.SelectedItem is not WindowRule rule)
        {
            return;
        }

        var result = System.Windows.MessageBox.Show($"'{rule.DisplayName}' 항목을 삭제할까요?", "삭제", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result == MessageBoxResult.Yes)
        {
            _app.DeleteRule(rule);
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
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

    private void NavDashboard_Checked(object sender, RoutedEventArgs e)
    {
        if (_suppressNavigation)
        {
            return;
        }

        if (!PrepareDashboardNavigation())
        {
            return;
        }

        MainScrollViewer?.ScrollToTop();
    }

    private void NavRules_Checked(object sender, RoutedEventArgs e)
    {
        if (_suppressNavigation)
        {
            return;
        }

        if (!PrepareDashboardNavigation())
        {
            return;
        }

        RulesSection?.BringIntoView();
    }

    private void NavRecent_Checked(object sender, RoutedEventArgs e)
    {
        if (_suppressNavigation)
        {
            return;
        }

        if (!PrepareDashboardNavigation())
        {
            return;
        }

        RecentSection?.BringIntoView();
    }

    private void NavLogs_Checked(object sender, RoutedEventArgs e)
    {
        if (_suppressNavigation)
        {
            return;
        }

        if (!PrepareDashboardNavigation())
        {
            return;
        }

        LogsSection?.BringIntoView();
    }

    private void NavSettings_Checked(object sender, RoutedEventArgs e)
    {
        if (_suppressNavigation)
        {
            return;
        }

        if (!PrepareDashboardNavigation())
        {
            return;
        }

        SettingsSection?.BringIntoView();
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
        if (_editorPage is not null)
        {
            _editorPage.SaveRequested -= RuleEditor_SaveRequested;
            _editorPage.CancelRequested -= RuleEditor_CancelRequested;
        }

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
        if (PageHost.Content != _dashboardContent)
        {
            PageHost.Content = _dashboardContent;
        }
    }

    private void NavigateToReturnTarget(RuleEditorReturnTarget target)
    {
        switch (target)
        {
            case RuleEditorReturnTarget.Rules:
                SetNavSelection(RulesNavButton);
                RulesSection?.BringIntoView();
                break;
            case RuleEditorReturnTarget.Recent:
                SetNavSelection(RecentNavButton);
                RecentSection?.BringIntoView();
                break;
            case RuleEditorReturnTarget.Settings:
                SetNavSelection(SettingsNavButton);
                SettingsSection?.BringIntoView();
                break;
            case RuleEditorReturnTarget.Logs:
                SetNavSelection(LogsNavButton);
                LogsSection?.BringIntoView();
                break;
            default:
                SetNavSelection(DashboardNavButton);
                MainScrollViewer?.ScrollToTop();
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

    private void UpdateModeDescription()
    {
        var mode = (FilteringMode)(int)ModeSlider.Value;
        ModeDescriptionText.Text = mode switch
        {
            FilteringMode.Off => "구경만 — 아무 창도 닫지 않고 기록만 남깁니다",
            FilteringMode.Low => "조심 — 확실히 같은 창일 때만 정리합니다",
            FilteringMode.Optimal => "적당 — 권장. 대부분의 경우에 알맞게 정리합니다",
            FilteringMode.Strong => "적극 — 비슷한 창도 더 빠르게 정리합니다",
            _ => string.Empty
        };
        StatusModeText.Text = mode switch
        {
            FilteringMode.Off => "구경만",
            FilteringMode.Low => "조심",
            FilteringMode.Optimal => "적당",
            FilteringMode.Strong => "적극",
            _ => string.Empty
        };
        UpdateModeLabels(mode);
        UpdateModeTrack(mode);
        UpdateDashboardMetrics();
    }

    private void UpdateModeLabels(FilteringMode mode)
    {
        SetModeLabel(ModeLabelOff, mode == FilteringMode.Off);
        SetModeLabel(ModeLabelLow, mode == FilteringMode.Low);
        SetModeLabel(ModeLabelOptimal, mode == FilteringMode.Optimal);
        SetModeLabel(ModeLabelStrong, mode == FilteringMode.Strong);
    }

    private static void SetModeLabel(TextBlock label, bool selected)
    {
        label.FontWeight = selected ? FontWeights.Bold : FontWeights.Normal;
        label.Foreground = selected
            ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 95, 170))
            : new SolidColorBrush(System.Windows.Media.Color.FromRgb(85, 85, 85));
    }

    private void UpdateModeTrack(FilteringMode mode)
    {
        const double start = 8;
        const double end = 412;
        const double step = (end - start) / 3;
        var value = (int)mode;
        ModeFillBar.Width = Math.Max(0, value * step);

        SetMarker(ModeMarkerOff, value >= 0, mode == FilteringMode.Off);
        SetMarker(ModeMarkerLow, value >= 1, mode == FilteringMode.Low);
        SetMarker(ModeMarkerOptimal, value >= 2, mode == FilteringMode.Optimal);
        SetMarker(ModeMarkerStrong, value >= 3, mode == FilteringMode.Strong);
    }

    private static void SetMarker(System.Windows.Shapes.Rectangle marker, bool filled, bool selected)
    {
        marker.Fill = selected
            ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 95, 170))
            : new SolidColorBrush(filled
                ? System.Windows.Media.Color.FromRgb(47, 125, 211)
                : System.Windows.Media.Color.FromRgb(139, 149, 165));
        marker.Height = selected ? 16 : 12;
        Canvas.SetTop(marker, selected ? 8 : 10);
    }

    private void AppCollection_Changed(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateEmptyStates();
        UpdateDashboardMetrics();
    }

    private void UpdateEmptyStates()
    {
        RulesEmptyText.Visibility = _app.Rules.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        RecentEmptyText.Visibility = _app.RecentWindows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        LogsEmptyText.Visibility = _app.Logs.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdateDashboardMetrics()
    {
        var today = DateTimeOffset.Now.Date;
        var cleanedToday = _app.Logs.Count(log =>
            log.At.LocalDateTime.Date == today
            && !string.Equals(log.Action, nameof(WindowActionType.Ignore), StringComparison.Ordinal));

        RulesCountText.Text = _app.Rules.Count(rule => rule.Enabled).ToString();
        RecentCountText.Text = _app.RecentWindows.Count.ToString();
        LogCountText.Text = _app.Logs.Count.ToString();
        TodayCleanedText.Text = cleanedToday.ToString();

        var paused = _app.Settings.IsPaused;
        StatusValueText.Text = paused ? "일시정지됨" : "감시 중";
        StatusDot.Fill = paused
            ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(184, 134, 11))
            : new SolidColorBrush(System.Windows.Media.Color.FromRgb(47, 154, 104));
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
