using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WindowFilterTray.Models;

namespace WindowFilterTray.Views.Pages;

public partial class RuleEditorPage : System.Windows.Controls.UserControl
{
    private readonly WindowSnapshot? _snapshot;
    private readonly WindowRule? _originalRule;
    private FrequencyCapMode _newRuleFrequencyMode = FrequencyCapMode.PerDay;
    private readonly string _initialSignature;
    private bool _loading = true;

    public RuleEditorPage(WindowSnapshot snapshot, FilteringMode mode)
    {
        _snapshot = snapshot;
        InitializeComponent();
        ActionItemsControl.ItemsSource = ActionOptions;
        LoadFromSnapshot(snapshot, mode);
        _initialSignature = BuildSignature();
        _loading = false;
        RefreshState();
    }

    public RuleEditorPage(WindowRule rule)
    {
        _originalRule = rule;
        InitializeComponent();
        ActionItemsControl.ItemsSource = ActionOptions;
        LoadFromRule(rule);
        _initialSignature = BuildSignature();
        _loading = false;
        RefreshState();
    }

    public event EventHandler<RuleEditorSaveRequestedEventArgs>? SaveRequested;
    public event EventHandler? CancelRequested;
    public event EventHandler? DirtyStateChanged;

    public ObservableCollection<ActionOption> ActionOptions { get; } =
    [
        new("M", "작게 내리기", "작업 표시줄로 보냅니다. 가장 안전합니다.", WindowActionType.Minimize),
        new("H", "숨기기", "화면에서 숨겨두고 기록에 남깁니다.", WindowActionType.HideWindow),
        new("L", "기록만", "아무것도 하지 않고 기록만 남깁니다.", WindowActionType.Ignore),
        new("!", "닫기", "창을 닫습니다. 되돌릴 수 없습니다.", WindowActionType.CloseWindow)
    ];

    public bool IsDirty => !_loading && !string.Equals(BuildSignature(), _initialSignature, StringComparison.Ordinal);

    public string OriginalRuleId => _originalRule?.Id ?? string.Empty;

    private void LoadFromSnapshot(WindowSnapshot snapshot, FilteringMode mode)
    {
        PageTitleText.Text = "새 규칙 만들기";
        BreadcrumbCurrentText.Text = "새 규칙 만들기";
        PageSubtitleText.Text = "감지된 창에서 시작하면 매칭 기준이 자동으로 채워집니다.";

        DisplayNameBox.Text = string.IsNullOrWhiteSpace(snapshot.Title)
            ? $"{snapshot.ProcessName} 창"
            : snapshot.Title;
        ProcessNameBox.Text = snapshot.ProcessName;
        WindowClassBox.Text = snapshot.ClassName;
        TitleContainsBox.Text = snapshot.Title;
        UseProcessCheckBox.IsChecked = true;
        UseClassCheckBox.IsChecked = true;
        UseTitleCheckBox.IsChecked = false;
        UseSizeCheckBox.IsChecked = false;
        UsePositionCheckBox.IsChecked = false;
        _newRuleFrequencyMode = mode == FilteringMode.Strong ? FrequencyCapMode.None : FrequencyCapMode.PerDay;
        MaxImpressionsBox.Text = "1";
        SelectAction(WindowActionType.Minimize);
        LoadSnapshotText(snapshot);
        ModeHintText.Text = mode == FilteringMode.Strong ? "적극 모드" : "감지됨";
    }

    private void LoadFromRule(WindowRule rule)
    {
        PageTitleText.Text = "규칙 편집";
        BreadcrumbCurrentText.Text = "규칙 편집";
        PageSubtitleText.Text = "기존 규칙의 조건과 처리 방식을 조정합니다.";

        DisplayNameBox.Text = rule.DisplayName;
        ProcessNameBox.Text = rule.Matcher.ProcessName;
        WindowClassBox.Text = rule.Matcher.WindowClass;
        TitleContainsBox.Text = rule.Matcher.TitleContains ?? string.Empty;
        UseProcessCheckBox.IsChecked = rule.Matcher.UseProcessName;
        UseClassCheckBox.IsChecked = rule.Matcher.UseWindowClass;
        UseTitleCheckBox.IsChecked = rule.Matcher.UseTitle;
        UseSizeCheckBox.IsChecked = rule.Matcher.UseSize;
        UsePositionCheckBox.IsChecked = rule.Matcher.UsePosition;
        MaxImpressionsBox.Text = rule.FrequencyCap.MaxImpressions.ToString();
        SelectAction(rule.Action);

        var rect = new WindowRect(
            rule.Matcher.Position?.X ?? 0,
            rule.Matcher.Position?.Y ?? 0,
            (rule.Matcher.Position?.X ?? 0) + (rule.Matcher.Size?.W ?? 0),
            (rule.Matcher.Position?.Y ?? 0) + (rule.Matcher.Size?.H ?? 0));
        LoadSnapshotText(new WindowSnapshot
        {
            Title = rule.Matcher.TitleContains ?? rule.DisplayName,
            ProcessName = rule.Matcher.ProcessName,
            ClassName = rule.Matcher.WindowClass,
            Rect = rect
        });
        ModeHintText.Text = "저장된 규칙";
    }

    private void LoadSnapshotText(WindowSnapshot snapshot)
    {
        CapturedTitleText.Text = string.IsNullOrWhiteSpace(snapshot.Title) ? "이름 없는 창" : snapshot.Title;
        CapturedProcessRun.Text = string.IsNullOrWhiteSpace(snapshot.ProcessName) ? "앱 정보 없음" : snapshot.ProcessName;
        CapturedClassRun.Text = string.IsNullOrWhiteSpace(snapshot.ClassName) ? "클래스 없음" : snapshot.ClassName;
        CapturedRectRun.Text = $"{snapshot.Rect.Width}x{snapshot.Rect.Height} @ {snapshot.Rect.Left},{snapshot.Rect.Top}";

        SizeText.Text = snapshot.Rect.Width > 0 && snapshot.Rect.Height > 0
            ? $"{snapshot.Rect.Width} x {snapshot.Rect.Height}"
            : "저장된 크기 정보 없음";
        PositionText.Text = snapshot.Rect.Width > 0 && snapshot.Rect.Height > 0
            ? $"{snapshot.Rect.Left}, {snapshot.Rect.Top}"
            : "저장된 위치 정보 없음";

        SummaryTitleText.Text = CapturedTitleText.Text;
        SummaryProcessText.Text = $"앱: {CapturedProcessRun.Text}";
        SummaryClassText.Text = $"클래스: {CapturedClassRun.Text}";
        SummaryRectText.Text = $"크기/위치: {CapturedRectRun.Text}";
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (!TryBuildRule(out var rule, out var error))
        {
            ValidationText.Text = error;
            return;
        }

        SaveRequested?.Invoke(this, new RuleEditorSaveRequestedEventArgs(rule, _snapshot, _originalRule is not null));
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        CancelRequested?.Invoke(this, EventArgs.Empty);
    }

    private void EditorInput_Changed(object sender, RoutedEventArgs e)
    {
        if (_loading)
        {
            return;
        }

        RefreshState();
        DirtyStateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ActionRadio_Checked(object sender, RoutedEventArgs e)
    {
        if (_loading)
        {
            return;
        }

        RefreshState();
        DirtyStateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void UserControl_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.S)
        {
            Save_Click(this, new RoutedEventArgs());
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape)
        {
            Cancel_Click(this, new RoutedEventArgs());
            e.Handled = true;
        }
    }

    private void SelectAction(WindowActionType action)
    {
        foreach (var option in ActionOptions)
        {
            option.IsSelected = option.Action == action;
        }
    }

    private WindowActionType SelectedAction()
    {
        return ActionOptions.FirstOrDefault(option => option.IsSelected)?.Action ?? WindowActionType.Minimize;
    }

    private void RefreshState()
    {
        var valid = TryBuildRule(out _, out var error);
        SaveButton.IsEnabled = valid;
        ValidationText.Text = valid ? string.Empty : error;
        RefreshPreview();
    }

    private void RefreshPreview()
    {
        PreviewNameText.Text = string.IsNullOrWhiteSpace(DisplayNameBox.Text)
            ? "이름 없는 규칙"
            : DisplayNameBox.Text.Trim();
        PreviewActionText.Text = ActionOptions.FirstOrDefault(option => option.Action == SelectedAction())?.Label ?? "작게 내리기";

        if (int.TryParse(MaxImpressionsBox.Text, out var maxImpressions))
        {
            PreviewFrequencyText.Text = maxImpressions == 0
                ? "처음부터 바로 처리합니다."
                : $"처음 {maxImpressions}회는 그냥 보여둡니다.";
        }
        else
        {
            PreviewFrequencyText.Text = "처음 보여둘 횟수를 확인해 주세요.";
        }

        var chips = new List<string>();
        if (UseTitleCheckBox.IsChecked == true)
        {
            chips.Add($"제목: {TitleContainsBox.Text.Trim()}");
        }

        if (UseProcessCheckBox.IsChecked == true)
        {
            chips.Add($"앱: {ProcessNameBox.Text.Trim()}");
        }

        if (UseClassCheckBox.IsChecked == true)
        {
            chips.Add($"클래스: {WindowClassBox.Text.Trim()}");
        }

        if (UseSizeCheckBox.IsChecked == true)
        {
            chips.Add($"크기: {SizeText.Text}");
        }

        if (UsePositionCheckBox.IsChecked == true)
        {
            chips.Add($"위치: {PositionText.Text}");
        }

        ConditionChipsControl.ItemsSource = chips.Count == 0 ? ["조건 없음"] : chips;
    }

    private bool TryBuildRule(out WindowRule rule, out string error)
    {
        rule = new WindowRule();
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(DisplayNameBox.Text))
        {
            error = "규칙 이름을 입력해 주세요.";
            return false;
        }

        var useTitle = UseTitleCheckBox.IsChecked == true;
        var useProcess = UseProcessCheckBox.IsChecked == true;
        var useClass = UseClassCheckBox.IsChecked == true;
        var useSize = UseSizeCheckBox.IsChecked == true;
        var usePosition = UsePositionCheckBox.IsChecked == true;

        if (!useTitle && !useProcess && !useClass && !useSize && !usePosition)
        {
            error = "적용 조건을 하나 이상 켜 주세요.";
            return false;
        }

        if (useTitle && string.IsNullOrWhiteSpace(TitleContainsBox.Text))
        {
            error = "제목 조건을 켰다면 제목 일부를 입력해 주세요.";
            return false;
        }

        if (useProcess && string.IsNullOrWhiteSpace(ProcessNameBox.Text))
        {
            error = "앱 조건을 켰다면 프로세스명을 입력해 주세요.";
            return false;
        }

        if (useClass && string.IsNullOrWhiteSpace(WindowClassBox.Text))
        {
            error = "창 클래스 조건을 켰다면 클래스명을 입력해 주세요.";
            return false;
        }

        if (!int.TryParse(MaxImpressionsBox.Text, out var maxImpressions) || maxImpressions < 0)
        {
            error = "처음 몇 번 보여둘지는 0 이상의 숫자로 입력해 주세요.";
            return false;
        }

        WindowRect? rect = null;
        if (_snapshot is not null)
        {
            rect = _snapshot.Rect;
        }
        else if (_originalRule?.Matcher.Size is not null)
        {
            var x = _originalRule.Matcher.Position?.X ?? 0;
            var y = _originalRule.Matcher.Position?.Y ?? 0;
            rect = new WindowRect(
                x,
                y,
                x + _originalRule.Matcher.Size.W,
                y + _originalRule.Matcher.Size.H);
        }

        rule = new WindowRule
        {
            Id = _originalRule?.Id ?? $"rule-{Guid.NewGuid():N}",
            CreatedAt = _originalRule?.CreatedAt ?? DateTimeOffset.UtcNow,
            DisplayName = DisplayNameBox.Text.Trim(),
            Enabled = _originalRule?.Enabled ?? true,
            Action = SelectedAction(),
            GraceMs = _originalRule?.GraceMs ?? 1000,
            ThumbnailPath = _originalRule?.ThumbnailPath,
            FrequencyCap = new FrequencyCap
            {
                Mode = _originalRule?.FrequencyCap.Mode ?? _newRuleFrequencyMode,
                MaxImpressions = maxImpressions
            },
            Matcher = new RuleMatcher
            {
                ProcessName = ProcessNameBox.Text.Trim(),
                WindowClass = WindowClassBox.Text.Trim(),
                TitleContains = string.IsNullOrWhiteSpace(TitleContainsBox.Text) ? string.Empty : TitleContainsBox.Text.Trim(),
                UseProcessName = useProcess,
                UseWindowClass = useClass,
                UseTitle = useTitle,
                UseSize = useSize,
                UsePosition = usePosition,
                Size = rect is null ? _originalRule?.Matcher.Size : new SizeHint { W = rect.Value.Width, H = rect.Value.Height },
                Position = rect is null ? _originalRule?.Matcher.Position : new PositionHint { X = rect.Value.Left, Y = rect.Value.Top },
                MinScore = _originalRule?.Matcher.MinScore ?? 60
            }
        };

        return true;
    }

    private string BuildSignature()
    {
        return string.Join("|",
            DisplayNameBox.Text.Trim(),
            UseTitleCheckBox.IsChecked == true,
            TitleContainsBox.Text.Trim(),
            UseProcessCheckBox.IsChecked == true,
            ProcessNameBox.Text.Trim(),
            UseClassCheckBox.IsChecked == true,
            WindowClassBox.Text.Trim(),
            UseSizeCheckBox.IsChecked == true,
            UsePositionCheckBox.IsChecked == true,
            SelectedAction(),
            MaxImpressionsBox.Text.Trim());
    }

    public sealed class ActionOption : INotifyPropertyChanged
    {
        private bool _isSelected;

        public ActionOption(string icon, string label, string description, WindowActionType action)
        {
            Icon = icon;
            Label = label;
            Description = description;
            Action = action;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public string Icon { get; }
        public string Label { get; }
        public string Description { get; }
        public WindowActionType Action { get; }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value)
                {
                    return;
                }

                _isSelected = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
            }
        }
    }
}

public sealed class RuleEditorSaveRequestedEventArgs : EventArgs
{
    public RuleEditorSaveRequestedEventArgs(WindowRule rule, WindowSnapshot? snapshot, bool isEdit)
    {
        Rule = rule;
        Snapshot = snapshot;
        IsEdit = isEdit;
    }

    public WindowRule Rule { get; }
    public WindowSnapshot? Snapshot { get; }
    public bool IsEdit { get; }
}
