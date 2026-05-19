using System.Windows;
using WindowFilterTray.Models;

namespace WindowFilterTray.Views;

public partial class RuleEditorWindow : Window
{
    private readonly WindowSnapshot? _snapshot;
    private readonly WindowRule? _originalRule;

    public RuleEditorWindow(WindowSnapshot snapshot, FilteringMode mode)
    {
        _snapshot = snapshot;
        InitializeComponent();
        PopulateCombos();
        LoadFromSnapshot(snapshot, mode);
    }

    public RuleEditorWindow(WindowRule rule)
    {
        _originalRule = rule;
        InitializeComponent();
        PopulateCombos();
        LoadFromRule(rule);
    }

    public WindowRule? Rule { get; private set; }

    private void PopulateCombos()
    {
        ActionComboBox.ItemsSource = Enum.GetValues<WindowActionType>();
        FrequencyModeComboBox.ItemsSource = Enum.GetValues<FrequencyCapMode>();
    }

    private void LoadFromSnapshot(WindowSnapshot snapshot, FilteringMode mode)
    {
        DisplayNameBox.Text = string.IsNullOrWhiteSpace(snapshot.Title)
            ? $"{snapshot.ProcessName} 창"
            : snapshot.Title;
        CapturedTitleText.Text = snapshot.Title;
        CapturedProcessText.Text = snapshot.ProcessName;
        CapturedClassText.Text = snapshot.ClassName;
        CapturedRectText.Text = $"{snapshot.Rect.Width}x{snapshot.Rect.Height} @ {snapshot.Rect.Left},{snapshot.Rect.Top}";

        UseProcessCheckBox.IsChecked = true;
        UseClassCheckBox.IsChecked = true;
        UseTitleCheckBox.IsChecked = false;
        UseSizeCheckBox.IsChecked = false;
        UsePositionCheckBox.IsChecked = false;

        ProcessNameBox.Text = snapshot.ProcessName;
        WindowClassBox.Text = snapshot.ClassName;
        TitleContainsBox.Text = snapshot.Title;
        ActionComboBox.SelectedItem = WindowActionType.CloseWindow;

        var frequency = mode == FilteringMode.Strong ? FrequencyCapMode.None : FrequencyCapMode.PerDay;
        FrequencyModeComboBox.SelectedItem = frequency;
        MaxImpressionsBox.Text = "1";
        DetectOnlyCheckBox.IsChecked = true;
    }

    private void LoadFromRule(WindowRule rule)
    {
        DisplayNameBox.Text = rule.DisplayName;
        CapturedTitleText.Text = rule.Matcher.TitleContains ?? string.Empty;
        CapturedProcessText.Text = rule.Matcher.ProcessName;
        CapturedClassText.Text = rule.Matcher.WindowClass;
        CapturedRectText.Text = rule.Matcher.SizeHint is null
            ? string.Empty
            : $"{rule.Matcher.SizeHint.W}x{rule.Matcher.SizeHint.H}";

        UseProcessCheckBox.IsChecked = rule.Matcher.UseProcessName;
        UseClassCheckBox.IsChecked = rule.Matcher.UseWindowClass;
        UseTitleCheckBox.IsChecked = rule.Matcher.UseTitle;
        UseSizeCheckBox.IsChecked = rule.Matcher.UseSize;
        UsePositionCheckBox.IsChecked = rule.Matcher.UsePosition;
        ProcessNameBox.Text = rule.Matcher.ProcessName;
        WindowClassBox.Text = rule.Matcher.WindowClass;
        TitleContainsBox.Text = rule.Matcher.TitleContains ?? string.Empty;
        ActionComboBox.SelectedItem = rule.Action;
        FrequencyModeComboBox.SelectedItem = rule.FrequencyCap.Mode;
        MaxImpressionsBox.Text = rule.FrequencyCap.MaxImpressions.ToString();
        DetectOnlyCheckBox.IsChecked = rule.Action == WindowActionType.Ignore;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(MaxImpressionsBox.Text, out var maxImpressions) || maxImpressions < 0)
        {
            MessageBox.Show("허용 노출 수는 0 이상의 숫자여야 합니다.", "입력 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var action = (WindowActionType)(ActionComboBox.SelectedItem ?? WindowActionType.CloseWindow);
        if (DetectOnlyCheckBox.IsChecked == true)
        {
            action = WindowActionType.Ignore;
        }

        var rect = _snapshot?.Rect;
        Rule = new WindowRule
        {
            Id = _originalRule?.Id ?? $"rule-{Guid.NewGuid():N}",
            CreatedAt = _originalRule?.CreatedAt ?? DateTimeOffset.UtcNow,
            DisplayName = string.IsNullOrWhiteSpace(DisplayNameBox.Text) ? "사용자 규칙" : DisplayNameBox.Text.Trim(),
            Enabled = _originalRule?.Enabled ?? true,
            Action = action,
            GraceMs = _originalRule?.GraceMs ?? 1000,
            ThumbnailPath = _originalRule?.ThumbnailPath,
            FrequencyCap = new FrequencyCap
            {
                Mode = (FrequencyCapMode)(FrequencyModeComboBox.SelectedItem ?? FrequencyCapMode.PerDay),
                MaxImpressions = maxImpressions
            },
            Matcher = new RuleMatcher
            {
                ProcessName = ProcessNameBox.Text.Trim(),
                WindowClass = WindowClassBox.Text.Trim(),
                TitleContains = string.IsNullOrWhiteSpace(TitleContainsBox.Text) ? null : TitleContainsBox.Text.Trim(),
                UseProcessName = UseProcessCheckBox.IsChecked == true,
                UseWindowClass = UseClassCheckBox.IsChecked == true,
                UseTitle = UseTitleCheckBox.IsChecked == true,
                UseSize = UseSizeCheckBox.IsChecked == true,
                UsePosition = UsePositionCheckBox.IsChecked == true,
                SizeHint = rect is null ? _originalRule?.Matcher.SizeHint : new SizeHint { W = rect.Value.Width, H = rect.Value.Height },
                PositionHint = rect is null ? _originalRule?.Matcher.PositionHint : new PositionHint { X = rect.Value.Left, Y = rect.Value.Top },
                MinScore = _originalRule?.Matcher.MinScore ?? 60
            }
        };

        DialogResult = true;
    }
}
