using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using WindowFilterTray.Models;

namespace WindowFilterTray.Views.Pages;

public partial class DashboardPage : System.Windows.Controls.UserControl
{
    private readonly App _app;
    private readonly IMainShell _shell;
    private readonly ICollectionView _rulesView;
    private readonly ICollectionView _logsView;
    private readonly DispatcherTimer _rulesSearchTimer = new() { Interval = TimeSpan.FromMilliseconds(200) };
    private readonly DispatcherTimer _logsSearchTimer = new() { Interval = TimeSpan.FromMilliseconds(200) };
    private bool _initializing = true;

    public DashboardPage(App app, IMainShell shell)
    {
        _app = app;
        _shell = shell;
        InitializeComponent();

        _rulesView = CollectionViewSource.GetDefaultView(_app.Rules);
        _logsView = CollectionViewSource.GetDefaultView(_app.Logs);
        _rulesView.Filter = FilterRule;
        _logsView.Filter = FilterLog;

        RulesList.ItemsSource = _rulesView;
        RecentList.ItemsSource = _app.RecentWindows;
        LogList.ItemsSource = _logsView;
        ModeSlider.Value = (int)_app.Settings.FilteringMode;

        _rulesSearchTimer.Tick += (_, _) =>
        {
            _rulesSearchTimer.Stop();
            RefreshRulesView();
        };
        _logsSearchTimer.Tick += (_, _) =>
        {
            _logsSearchTimer.Stop();
            RefreshLogsView();
        };

        _app.Rules.CollectionChanged += AppCollection_Changed;
        _app.RecentWindows.CollectionChanged += AppCollection_Changed;
        _app.Logs.CollectionChanged += AppCollection_Changed;
        UpdateModeDescription();
        UpdateEmptyStates();
        UpdateDashboardMetrics();
        _initializing = false;
    }

    public void ScrollToDashboard()
    {
        MainScrollViewer.ScrollToTop();
    }

    public void ShowRulesSection()
    {
        RulesSection.BringIntoView();
    }

    public void ShowRecentSection()
    {
        RecentSection.BringIntoView();
    }

    public void ShowLogsSection()
    {
        LogsSection.BringIntoView();
    }

    public void ShowSettingsSection()
    {
        SettingsSection.BringIntoView();
    }

    public void RefreshDashboardState()
    {
        RefreshRulesView();
        RefreshLogsView();
        UpdateDashboardMetrics();
        UpdateEmptyStates();
    }

    private void CreateRuleFromRecent_Click(object sender, RoutedEventArgs e)
    {
        if (RecentList.SelectedItem is WindowSnapshot snapshot)
        {
            _shell.OpenRuleEditor(snapshot);
        }
    }

    private void Picker_Click(object sender, RoutedEventArgs e)
    {
        _shell.StartPicker();
    }

    private void EditRule_Click(object sender, RoutedEventArgs e)
    {
        if (RulesList.SelectedItem is WindowRule rule)
        {
            _shell.EditRule(rule);
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
            _shell.DeleteRule(rule);
        }
    }

    private void RuleEnabledCheckBox_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.CheckBox { DataContext: WindowRule rule } checkBox)
        {
            return;
        }

        var next = checkBox.IsChecked == true;
        var previous = !next;
        if (!_shell.TrySetRuleEnabled(rule, next, out var error))
        {
            rule.Enabled = previous;
            checkBox.IsChecked = previous;
            System.Windows.MessageBox.Show(error, "규칙 저장", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        RefreshRulesView();
        _shell.RefreshShellState();
    }

    private void ModeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_initializing)
        {
            return;
        }

        _shell.SetFilteringMode((FilteringMode)(int)ModeSlider.Value);
        UpdateModeDescription();
        _shell.RefreshShellState();
    }

    private void RulesSearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _rulesSearchTimer.Stop();
        _rulesSearchTimer.Start();
    }

    private void LogsSearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _logsSearchTimer.Stop();
        _logsSearchTimer.Start();
    }

    private void SearchBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key != Key.Escape || sender is not System.Windows.Controls.TextBox textBox)
        {
            return;
        }

        textBox.Clear();
        e.Handled = true;
    }

    private void ClearRulesSearch_Click(object sender, RoutedEventArgs e)
    {
        RulesSearchBox.Clear();
    }

    private void ClearLogsSearch_Click(object sender, RoutedEventArgs e)
    {
        LogsSearchBox.Clear();
    }

    private bool FilterRule(object item)
    {
        if (item is not WindowRule rule)
        {
            return false;
        }

        var query = RulesSearchBox?.Text?.Trim();
        if (string.IsNullOrWhiteSpace(query))
        {
            return true;
        }

        return Contains(rule.DisplayName, query)
            || Contains(rule.DisplayAction, query)
            || Contains(rule.DisplayBasis, query);
    }

    private bool FilterLog(object item)
    {
        if (item is not MatchLogEntry log)
        {
            return false;
        }

        var query = LogsSearchBox?.Text?.Trim();
        if (string.IsNullOrWhiteSpace(query))
        {
            return true;
        }

        return Contains(log.WindowTitle, query)
            || Contains(log.ProcessName, query)
            || Contains(log.RuleName, query)
            || Contains(log.DisplayAction, query)
            || Contains(log.Reason, query);
    }

    private static bool Contains(string value, string query)
    {
        return value.Contains(query, StringComparison.OrdinalIgnoreCase);
    }

    private void RefreshRulesView()
    {
        _rulesView.Refresh();
        UpdateEmptyStates();
    }

    private void RefreshLogsView()
    {
        _logsView.Refresh();
        UpdateEmptyStates();
    }

    private void AppCollection_Changed(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RefreshRulesView();
        RefreshLogsView();
        UpdateDashboardMetrics();
        UpdateEmptyStates();
    }

    private void UpdateEmptyStates()
    {
        var rulesFiltered = RulesList.Items.Count;
        var logsFiltered = LogList.Items.Count;

        RulesEmptyText.Visibility = _app.Rules.Count == 0 || rulesFiltered == 0 ? Visibility.Visible : Visibility.Collapsed;
        RulesEmptyText.Text = _app.Rules.Count == 0
            ? "아직 정리할 창이 없습니다. 불편한 창이 뜨면 창 찍어서 규칙을 추가하세요."
            : "검색 결과가 없습니다. 검색어를 지우거나 다른 단어로 찾아보세요.";

        RecentEmptyText.Visibility = _app.RecentWindows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        LogsEmptyText.Visibility = _app.Logs.Count == 0 || logsFiltered == 0 ? Visibility.Visible : Visibility.Collapsed;
        LogsEmptyText.Text = _app.Logs.Count == 0
            ? "정리한 기록이 아직 없습니다."
            : "검색 결과가 없습니다. 검색어를 지우거나 다른 단어로 찾아보세요.";
    }

    private void UpdateDashboardMetrics()
    {
        RulesCountText.Text = _app.Rules.Count(rule => rule.Enabled).ToString();
        RecentCountText.Text = _app.RecentWindows.Count.ToString();
        LogCountText.Text = _app.Logs.Count.ToString();
    }

    private void UpdateModeDescription()
    {
        var mode = (FilteringMode)(int)ModeSlider.Value;
        ModeDescriptionText.Text = mode switch
        {
            FilteringMode.Off => "구경만 - 아무 창도 닫지 않고 기록만 남깁니다",
            FilteringMode.Low => "조심 - 확실히 같은 창일 때만 정리합니다",
            FilteringMode.Optimal => "적당 - 권장. 대부분의 경우에 알맞게 정리합니다",
            FilteringMode.Strong => "적극 - 비슷한 창도 더 빠르게 정리합니다",
            _ => string.Empty
        };
        UpdateModeLabels(mode);
        UpdateModeTrack(mode);
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
}
