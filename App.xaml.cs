using System.Collections.ObjectModel;
using System.Drawing;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using WindowFilterTray.Models;
using WindowFilterTray.Services;
using WindowFilterTray.Views;
using Forms = System.Windows.Forms;

namespace WindowFilterTray;

public partial class App : System.Windows.Application
{
    private Mutex? _mutex;
    private Forms.NotifyIcon? _trayIcon;
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
        RefreshTrayIcon();
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
        var dialog = new RuleEditorWindow(snapshot, Settings.FilteringMode)
        {
            Owner = _mainWindow
        };

        if (dialog.ShowDialog() != true || dialog.Rule is null)
        {
            return;
        }

        dialog.Rule.ThumbnailPath = _thumbnailService.Capture(snapshot, dialog.Rule.Id);
        Rules.Add(dialog.Rule);
        SaveAll();
    }

    public void EditRule(WindowRule rule)
    {
        var dialog = new RuleEditorWindow(rule)
        {
            Owner = _mainWindow
        };

        if (dialog.ShowDialog() != true || dialog.Rule is null)
        {
            return;
        }

        var index = Rules.IndexOf(rule);
        if (index >= 0)
        {
            Rules[index] = dialog.Rule;
            SaveAll();
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
            action,
            undo,
            ShowMainWindow);
        _toastWindow.Closed += (_, _) => _toastWindow = null;
        _toastWindow.Show();
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
        _trayIcon = new Forms.NotifyIcon
        {
            Text = Branding.RunningText,
            Icon = LoadTrayIcon(),
            Visible = true
        };
        _trayIcon.DoubleClick += (_, _) => Dispatcher.Invoke(ShowMainWindow);
        RefreshTrayIcon();
    }

    private void RefreshTrayIcon()
    {
        if (_trayIcon is null)
        {
            return;
        }

        _trayIcon.Text = Settings.IsPaused || Settings.FilteringMode == FilteringMode.Off
            ? Branding.PausedText
            : Branding.RunningText;
        _trayIcon.ContextMenuStrip = BuildTrayMenu();
    }

    private Forms.ContextMenuStrip BuildTrayMenu()
    {
        var menu = new Forms.ContextMenuStrip();

        var open = new Forms.ToolStripMenuItem("열기");
        open.Click += (_, _) => Dispatcher.Invoke(ShowMainWindow);
        menu.Items.Add(open);

        var pause = new Forms.ToolStripMenuItem(Settings.IsPaused ? "다시 시작" : "잠시 멈춤");
        pause.Click += (_, _) => Dispatcher.Invoke(() => SetPaused(!Settings.IsPaused));
        menu.Items.Add(pause);

        var picker = new Forms.ToolStripMenuItem("창 고르기");
        picker.Click += (_, _) => Dispatcher.Invoke(StartPicker);
        menu.Items.Add(picker);

        var recent = new Forms.ToolStripMenuItem("최근 뜬 창");
        foreach (var snapshot in RecentWindows.Take(10))
        {
            var item = new Forms.ToolStripMenuItem(snapshot.Summary) { Tag = snapshot };
            item.Click += (_, _) => Dispatcher.Invoke(() => OpenRuleEditor(snapshot));
            recent.DropDownItems.Add(item);
        }

        if (recent.DropDownItems.Count == 0)
        {
            recent.DropDownItems.Add(new Forms.ToolStripMenuItem("없음") { Enabled = false });
        }

        menu.Items.Add(recent);
        menu.Items.Add(new Forms.ToolStripSeparator());

        var exit = new Forms.ToolStripMenuItem("종료");
        exit.Click += (_, _) => Dispatcher.Invoke(ExitApplication);
        menu.Items.Add(exit);

        return menu;
    }

    private static Icon LoadTrayIcon()
    {
        try
        {
            return Icon.ExtractAssociatedIcon(Environment.ProcessPath ?? string.Empty) ?? SystemIcons.Application;
        }
        catch
        {
            return SystemIcons.Application;
        }
    }

    private void ExitApplication()
    {
        if (_mainWindow is not null)
        {
            _mainWindow.AllowClose = true;
        }

        Shutdown();
    }
}
