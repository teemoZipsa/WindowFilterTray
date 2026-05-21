using System.Collections.ObjectModel;
using System.Drawing;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using H.NotifyIcon;
using H.NotifyIcon.Core;
using WindowFilterTray.Models;
using WindowFilterTray.Services;
using WindowFilterTray.Views;

namespace WindowFilterTray;

public partial class App : System.Windows.Application
{
    private Mutex? _mutex;
    private TaskbarIcon? _trayIcon;
    private TrayPopoverControl? _trayPopover;
    private AppPaths _paths = null!;
    private StorageService _storage = null!;
    private WindowInspector _inspector = null!;
    private EventHookService _eventHook = null!;
    private HotkeyService? _hotkeys;
    private RuleEngine _ruleEngine = null!;
    private ActionExecutor _actionExecutor = null!;
    private ThumbnailService _thumbnailService = null!;
    private StartupService _startupService = null!;
    private MainWindow? _mainWindow;
    private ActionToastWindow? _toastWindow;

    public ObservableCollection<WindowRule> Rules { get; } = [];
    public ObservableCollection<WindowSnapshot> RecentWindows { get; } = [];
    public ObservableCollection<MatchLogEntry> Logs { get; } = [];
    public AppSettings Settings { get; private set; } = new();
    public Dictionary<string, RuleStats> Stats { get; private set; } = [];

    static App()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("windir")))
        {
            var windowsPath = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            if (!string.IsNullOrWhiteSpace(windowsPath))
            {
                Environment.SetEnvironmentVariable("windir", windowsPath, EnvironmentVariableTarget.Process);
            }
        }
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        _mutex = new Mutex(initiallyOwned: true, "WindowFilterTray.SingleInstance", out var created);
        if (!created)
        {
            System.Windows.MessageBox.Show("이미 실행 중입니다.", Branding.AppDisplayName, MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        base.OnStartup(e);

        _paths = new AppPaths();
        _storage = new StorageService(_paths);
        _inspector = new WindowInspector();
        _eventHook = new EventHookService(_inspector);
        _actionExecutor = new ActionExecutor();
        _thumbnailService = new ThumbnailService(_paths);
        _startupService = new StartupService(AppPaths.AppName);

        Settings = _storage.LoadSettings();
        Settings.AutoStart = _startupService.IsEnabled();
        Stats = _storage.LoadStats();
        foreach (var rule in _storage.LoadRules())
        {
            Rules.Add(rule);
        }

        foreach (var log in _storage.LoadLogs())
        {
            Logs.Add(log);
        }

        _ruleEngine = new RuleEngine(new SafetyPolicy(), Stats);
        _eventHook.WindowObserved += OnWindowObserved;
        _eventHook.Start();

        CreateTrayIcon();
        _mainWindow = new MainWindow(this);
        _mainWindow.SourceInitialized += (_, _) => RegisterHotkeys(_mainWindow);
        _mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _hotkeys?.Dispose();
        _eventHook?.Dispose();
        _trayIcon?.Dispose();
        _storage?.SaveRules(Rules);
        _storage?.SaveStats(Stats);
        _storage?.SaveSettings(Settings);
        _storage?.SaveLogs(Logs);
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
        base.OnExit(e);
    }

    public void SaveAll()
    {
        _storage.SaveRules(Rules);
        _storage.SaveStats(Stats);
        _storage.SaveSettings(Settings);
        _storage.SaveLogs(Logs);
        RefreshTrayState();
    }

    public void SetFilteringMode(FilteringMode mode)
    {
        Settings.FilteringMode = mode;
        SaveAll();
    }

    public void SetPaused(bool paused)
    {
        Settings.IsPaused = paused;
        SaveAll();
    }

    public void SetAutoStart(bool enabled)
    {
        _startupService.SetEnabled(enabled);
        Settings.AutoStart = enabled;
        SaveAll();
    }

    public void OpenRuleEditor(WindowSnapshot snapshot)
    {
        ShowMainWindow();
        _mainWindow?.OpenRuleEditor(snapshot);
    }

    public void EditRule(WindowRule rule)
    {
        ShowMainWindow();
        _mainWindow?.EditRule(rule);
    }

    public bool TryAddRuleFromEditor(WindowRule rule, WindowSnapshot? snapshot, out string error)
    {
        error = string.Empty;

        if (snapshot is not null)
        {
            try
            {
                rule.ThumbnailPath = _thumbnailService.Capture(snapshot, rule.Id);
            }
            catch
            {
                rule.ThumbnailPath = null;
            }
        }

        Rules.Add(rule);
        try
        {
            SaveAll();
            return true;
        }
        catch (Exception ex)
        {
            Rules.Remove(rule);
            error = $"규칙을 저장하지 못했습니다: {ex.Message}";
            return false;
        }
    }

    public bool TryUpdateRuleFromEditor(WindowRule rule, out string error)
    {
        error = string.Empty;
        var index = -1;
        for (var i = 0; i < Rules.Count; i++)
        {
            if (Rules[i].Id.Equals(rule.Id, StringComparison.Ordinal))
            {
                index = i;
                break;
            }
        }

        if (index < 0)
        {
            error = "편집 중이던 규칙이 이미 삭제되어 저장할 수 없습니다.";
            return false;
        }

        var previous = Rules[index];
        Rules[index] = rule;
        try
        {
            SaveAll();
            return true;
        }
        catch (Exception ex)
        {
            Rules[index] = previous;
            error = $"규칙을 저장하지 못했습니다: {ex.Message}";
            return false;
        }
    }

    public void DeleteRule(WindowRule rule)
    {
        Rules.Remove(rule);
        Stats.Remove(rule.Id);
        SaveAll();
    }

    public void StartPicker()
    {
        if (_mainWindow is not null && !_mainWindow.ConfirmDiscardEditorChanges())
        {
            return;
        }

        _mainWindow?.DiscardEditorChangesToRecent();

        var picker = new PickerOverlayWindow(_inspector)
        {
            Owner = _mainWindow
        };
        picker.WindowSelected += (_, snapshot) => OpenRuleEditor(snapshot);
        picker.Show();
    }

    public void CaptureFromCursor()
    {
        var snapshot = _inspector.CaptureFromCursor();
        if (snapshot is not null)
        {
            OpenRuleEditor(snapshot);
        }
    }

    public void ShowMainWindow()
    {
        if (_mainWindow is null)
        {
            return;
        }

        _mainWindow.Show();
        _mainWindow.WindowState = WindowState.Normal;
        _mainWindow.Activate();
    }

    public string GetThumbnailFullPath(WindowRule rule)
    {
        return string.IsNullOrWhiteSpace(rule.ThumbnailPath)
            ? string.Empty
            : Path.Combine(_paths.Root, rule.ThumbnailPath);
    }

    private void RegisterHotkeys(Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        _hotkeys = new HotkeyService(hwnd);
        _hotkeys.CaptureRequested += (_, _) => Dispatcher.Invoke(CaptureFromCursor);
        _hotkeys.PauseToggleRequested += (_, _) => Dispatcher.Invoke(() => SetPaused(!Settings.IsPaused));
        _hotkeys.RegisterDefaults();
    }

    private void OnWindowObserved(object? sender, WindowSnapshot snapshot)
    {
        Dispatcher.InvokeAsync(() =>
        {
            AddRecentWindow(snapshot);
            _ = ProcessWindowAsync(snapshot);
        });
    }

    private async Task ProcessWindowAsync(WindowSnapshot snapshot)
    {
        var decision = _ruleEngine.Evaluate(snapshot, Rules, Settings.FilteringMode, Settings.IsPaused);
        if (!decision.Matched || decision.Rule is null)
        {
            return;
        }

        var action = decision.Action;
        if (decision.GraceMs > 0)
        {
            await Task.Delay(decision.GraceMs);
        }

        var executed = _actionExecutor.Execute(snapshot.HWnd, action);
        if (executed && action != WindowActionType.Ignore)
        {
            _ruleEngine.MarkBlocked(decision.Rule);
            ShowActionToast(snapshot, action);
        }

        Logs.Add(new MatchLogEntry
        {
            RuleId = decision.Rule.Id,
            RuleName = decision.Rule.DisplayName,
            WindowTitle = snapshot.Title,
            ProcessName = snapshot.ProcessName,
            Score = decision.Score,
            Action = action.ToString(),
            Reason = decision.Reason
        });

        while (Logs.Count > 500)
        {
            Logs.RemoveAt(0);
        }

        SaveAll();
    }

    private void ShowActionToast(WindowSnapshot snapshot, WindowActionType action)
    {
        _toastWindow?.Close();
        Action? undo = action is WindowActionType.HideWindow or WindowActionType.Minimize
            ? () => _actionExecutor.Undo(snapshot.HWnd, action)
            : null;

        _toastWindow = new ActionToastWindow(
            snapshot.Title,
            snapshot.ProcessName,
            action,
            undo,
            ShowLogsSection);
        _toastWindow.Closed += (_, _) => _toastWindow = null;
        _toastWindow.Show();
    }

    private void ShowLogsSection()
    {
        ShowMainWindow();
        _mainWindow?.ShowLogsSection();
    }

    private void AddRecentWindow(WindowSnapshot snapshot)
    {
        var cutoff = DateTimeOffset.Now.AddMinutes(-30);
        for (var i = RecentWindows.Count - 1; i >= 0; i--)
        {
            if (RecentWindows[i].SeenAt < cutoff)
            {
                RecentWindows.RemoveAt(i);
            }
        }

        var duplicate = RecentWindows.FirstOrDefault(item =>
            item.HWnd == snapshot.HWnd
            || (item.ProcessName.Equals(snapshot.ProcessName, StringComparison.OrdinalIgnoreCase)
                && item.ClassName.Equals(snapshot.ClassName, StringComparison.OrdinalIgnoreCase)
                && item.Title.Equals(snapshot.Title, StringComparison.Ordinal)));

        if (duplicate is not null)
        {
            RecentWindows.Remove(duplicate);
        }

        RecentWindows.Insert(0, snapshot);
        while (RecentWindows.Count > 80)
        {
            RecentWindows.RemoveAt(RecentWindows.Count - 1);
        }
    }

    private void CreateTrayIcon()
    {
        _trayPopover = new TrayPopoverControl(
            ShowMainWindow,
            StartPicker,
            ShowLogsSection,
            () => SetPaused(!Settings.IsPaused),
            ExitApplication,
            CloseTrayPopup,
            OpenRuleEditor);

        _trayIcon = new TaskbarIcon
        {
            IconSource = LoadTrayIconSource(),
            PopupActivation = PopupActivationMode.LeftOrRightClick,
            TrayPopup = _trayPopover,
            NoLeftClickDelay = true
        };

        // Left and right click intentionally open the same WPF popover; no legacy context menu or double-click action is exposed.
        _trayIcon.TrayPopupOpen += (_, _) =>
        {
            _trayPopover.Update(CreateTrayPopoverSnapshot());
            _trayPopover.FocusInitialAction();
        };

        RefreshTrayState();
    }

    private void RefreshTrayState()
    {
        if (_trayIcon is null)
        {
            return;
        }

        _trayIcon.ToolTipText = CreateTrayToolTip();
    }

    private TrayPopoverSnapshot CreateTrayPopoverSnapshot()
    {
        var today = DateTimeOffset.Now.Date;
        var cleanedToday = Logs.Count(log =>
            log.At.LocalDateTime.Date == today
            && !string.Equals(log.Action, nameof(WindowActionType.Ignore), StringComparison.Ordinal));
        var isOff = Settings.FilteringMode == FilteringMode.Off;
        var isPaused = Settings.IsPaused;
        var statusText = isOff
            ? "꺼짐"
            : isPaused
                ? "잠시 멈춤"
                : "감시 중";
        var description = isOff
            ? "규칙 처리가 꺼져 있습니다."
            : isPaused
                ? "새 창을 기록하지만 정리하지 않습니다."
                : "새 창을 감지하고 규칙을 적용합니다.";

        return new TrayPopoverSnapshot
        {
            StatusText = statusText,
            StatusDescription = description,
            TodayCleanedCount = cleanedToday,
            ActiveRuleCount = Rules.Count(rule => rule.Enabled),
            IsPaused = isPaused,
            IsOff = isOff,
            RecentWindows = RecentWindows.Take(5).ToList()
        };
    }

    private string CreateTrayToolTip()
    {
        var snapshot = CreateTrayPopoverSnapshot();
        return $"{Branding.AppDisplayName} - {snapshot.StatusText}, 오늘 {snapshot.TodayCleanedCount}건";
    }

    private void CloseTrayPopup()
    {
        _trayIcon?.CloseTrayPopup();
    }

    private static ImageSource LoadTrayIconSource()
    {
        try
        {
            using var icon = Icon.ExtractAssociatedIcon(Environment.ProcessPath ?? string.Empty);
            if (icon is not null)
            {
                var source = Imaging.CreateBitmapSourceFromHIcon(
                    icon.Handle,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
                source.Freeze();
                return source;
            }
        }
        catch
        {
        }

        var pixels = new byte[16 * 16 * 4];
        for (var i = 0; i < pixels.Length; i += 4)
        {
            pixels[i] = 0xD3;
            pixels[i + 1] = 0x7D;
            pixels[i + 2] = 0x2F;
            pixels[i + 3] = 0xFF;
        }

        var fallback = BitmapSource.Create(16, 16, 96, 96, PixelFormats.Bgra32, null, pixels, 16 * 4);
        fallback.Freeze();
        return fallback;
    }

    private void ExitApplication()
    {
        if (_mainWindow is not null && !_mainWindow.ConfirmDiscardEditorChanges())
        {
            return;
        }

        if (_mainWindow is not null)
        {
            _mainWindow.AllowClose = true;
        }

        Shutdown();
    }
}
