using System.ComponentModel;
using System.Windows;
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

        var result = MessageBox.Show($"'{rule.DisplayName}' 규칙을 삭제할까요?", "규칙 삭제", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result == MessageBoxResult.Yes)
        {
            _app.DeleteRule(rule);
        }
    }

    private void UpdateModeDescription()
    {
        ModeDescriptionText.Text = ((FilteringMode)(int)ModeSlider.Value) switch
        {
            FilteringMode.Off => "꺼짐 — 차단하지 않고 감지만 합니다",
            FilteringMode.Low => "약함 — 확실한 경우에만 차단합니다",
            FilteringMode.Optimal => "최적 — 권장. 균형 잡힌 차단",
            FilteringMode.Strong => "강함 — 의심되는 창을 적극적으로 차단합니다",
            _ => string.Empty
        };
    }
}
