using System.ComponentModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WindowFilterTray.Models;

namespace WindowFilterTray;

public partial class MainWindow : Window
{
    private readonly App _app;
    private bool _initializing = true;

    public bool AllowClose { get; set; }

    public MainWindow(App app)
    {
        _app = app;
        InitializeComponent();

        RulesList.ItemsSource = _app.Rules;
        RecentList.ItemsSource = _app.RecentWindows;
        LogList.ItemsSource = _app.Logs;
        ModeSlider.Value = (int)_app.Settings.FilteringMode;
        PauseCheckBox.IsChecked = _app.Settings.IsPaused;
        AutoStartCheckBox.IsChecked = _app.Settings.AutoStart;
        UpdateModeDescription();
        UpdateEmptyStates();
        _app.Rules.CollectionChanged += AppCollection_Changed;
        _app.RecentWindows.CollectionChanged += AppCollection_Changed;
        _app.Logs.CollectionChanged += AppCollection_Changed;
        _initializing = false;
    }

    protected override void OnClosing(CancelEventArgs e)
    {
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
        UpdateModeLabels(mode);
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

    private void AppCollection_Changed(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateEmptyStates();
    }

    private void UpdateEmptyStates()
    {
        RulesEmptyText.Visibility = _app.Rules.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        RecentEmptyText.Visibility = _app.RecentWindows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        LogsEmptyText.Visibility = _app.Logs.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }
}
