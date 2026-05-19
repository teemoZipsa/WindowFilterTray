using System.Collections.ObjectModel;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using H.NotifyIcon;
using WindowFilterTray.Models;
using WindowFilterTray.Services;
using WindowFilterTray.Views;

namespace WindowFilterTray;

public partial class App : Application
{
    private Mutex? _mutex;
    private TaskbarIcon? _trayIcon;
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

    public ObservableCollection<WindowRule> Rules { get; } = [];
    public ObservableCollection<WindowSnapshot> RecentWindows { get; } = [];
    public ObservableCollection<MatchLogEntry> Logs { get; } = [];
    public AppSettings Settings { get; private set; } = new();
    public Dictionary<string, RuleStats> Stats { get; private set; } = [];

    protected override void OnStartup(StartupEventArgs e)
    {
        _mutex = new Mutex(initiallyOwned: true, "WindowFilterTray.SingleInstance", out var created);
        if (!created)
        {
            MessageBox.Show("이미 실행 중입니다.", "Window Filter Tray", MessageBoxButton.OK, MessageBoxImage.Information);
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
        _trayIcon = new TaskbarIcon
        {
            ToolTipText = "Window Filter Tray",
            IconSource = CreateTrayImage(paused: Settings.IsPaused || Settings.FilteringMode == FilteringMode.Off)
        };
        RefreshTrayIcon();
    }

    private void RefreshTrayIcon()
    {
        if (_trayIcon is null)
        {
            return;
        }

        _trayIcon.IconSource = CreateTrayImage(Settings.IsPaused || Settings.FilteringMode == FilteringMode.Off);
        _trayIcon.ContextMenu = BuildTrayMenu();
    }

    private ContextMenu BuildTrayMenu()
    {
        var menu = new ContextMenu();

        var open = new MenuItem { Header = "열기" };
        open.Click += (_, _) => ShowMainWindow();
        menu.Items.Add(open);

        var pause = new MenuItem { Header = Settings.IsPaused ? "전체 차단 켜기" : "전체 차단 끄기" };
        pause.Click += (_, _) => SetPaused(!Settings.IsPaused);
        menu.Items.Add(pause);

        var picker = new MenuItem { Header = "창 선택" };
        picker.Click += (_, _) => StartPicker();
        menu.Items.Add(picker);

        var recent = new MenuItem { Header = "최근 감지된 창" };
        foreach (var snapshot in RecentWindows.Take(10))
        {
            var item = new MenuItem { Header = snapshot.Summary, Tag = snapshot };
            item.Click += (_, _) => OpenRuleEditor(snapshot);
            recent.Items.Add(item);
        }

        if (recent.Items.Count == 0)
        {
            recent.Items.Add(new MenuItem { Header = "없음", IsEnabled = false });
        }

        menu.Items.Add(recent);
        menu.Items.Add(new Separator());

        var exit = new MenuItem { Header = "종료" };
        exit.Click += (_, _) => ExitApplication();
        menu.Items.Add(exit);

        return menu;
    }

    private static ImageSource CreateTrayImage(bool paused)
    {
        var brush = new SolidColorBrush(paused ? Color.FromRgb(150, 150, 150) : Color.FromRgb(28, 142, 96));
        brush.Freeze();

        var pen = new Pen(Brushes.White, 1.4);
        pen.Freeze();

        var group = new DrawingGroup();
        group.Children.Add(new GeometryDrawing(brush, null, new EllipseGeometry(new Point(8, 8), 7, 7)));
        if (paused)
        {
            group.Children.Add(new GeometryDrawing(null, pen, Geometry.Parse("M 5,5 L 11,11 M 11,5 L 5,11")));
        }
        else
        {
            group.Children.Add(new GeometryDrawing(null, pen, Geometry.Parse("M 4.5,8 L 7,10.5 L 12,5")));
        }

        group.Freeze();
        var image = new DrawingImage(group);
        image.Freeze();
        return image;
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
