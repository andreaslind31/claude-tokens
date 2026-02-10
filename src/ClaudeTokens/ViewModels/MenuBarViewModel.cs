using Avalonia.Controls;
using Avalonia.Threading;
using ClaudeTokens.Models;
using ClaudeTokens.Services;

namespace ClaudeTokens.ViewModels;

public class MenuBarViewModel : IDisposable
{
    private readonly TokenDataService _tokenService;
    private readonly FileWatcherService _watcherService;
    private readonly ApiUsageService _apiService;
    private readonly Action _quitAction;
    private readonly string? _apiKey;
    private Timer? _apiPollTimer;
    private UsageInfo? _lastUsage;
    private NativeMenu _currentMenu;

    public event Action<int>? PercentChanged;
    public event Action<NativeMenu>? MenuChanged;

    public NativeMenu Menu => _currentMenu;

    public MenuBarViewModel(
        TokenDataService tokenService,
        FileWatcherService watcherService,
        ApiUsageService apiService,
        string? apiKey,
        int pollIntervalSeconds,
        Action quitAction)
    {
        _tokenService = tokenService;
        _watcherService = watcherService;
        _apiService = apiService;
        _apiKey = apiKey;
        _quitAction = quitAction;
        _currentMenu = new NativeMenu();

        _watcherService.DataChanged += () => Dispatcher.UIThread.Post(Refresh);
        _watcherService.Start();

        Refresh();

        // Start API polling if we have a key
        if (!string.IsNullOrWhiteSpace(_apiKey))
        {
            var interval = TimeSpan.FromSeconds(Math.Max(pollIntervalSeconds, 10));
            _apiPollTimer = new Timer(_ => _ = PollApiAsync(), null, TimeSpan.Zero, interval);
        }
    }

    private async Task PollApiAsync()
    {
        if (string.IsNullOrWhiteSpace(_apiKey)) return;

        var usage = await _apiService.GetUsageAsync(_apiKey);
        if (usage != null)
        {
            _lastUsage = usage;
            var percent = (int)Math.Round(usage.RemainingPercent);
            Dispatcher.UIThread.Post(() =>
            {
                PercentChanged?.Invoke(percent);
                Refresh();
            });
        }
    }

    public void Refresh()
    {
        var summary = _tokenService.LoadSummary();
        var menu = BuildMenu(summary);
        _currentMenu = menu;
        MenuChanged?.Invoke(menu);
    }

    private NativeMenu BuildMenu(TokenSummary summary)
    {
        var menu = new NativeMenu();

        // Rate limit section (if API data available)
        if (_lastUsage != null)
        {
            var pct = (int)Math.Round(_lastUsage.RemainingPercent);
            AddDisabledItemTo(menu, $"Remaining: {pct}%");
            AddDisabledItemTo(menu, $"  {TokenDataService.FormatTokenCount(_lastUsage.TokensRemaining)} / {TokenDataService.FormatTokenCount(_lastUsage.TokensLimit)} tokens");
            if (_lastUsage.TokensReset > DateTime.MinValue)
            {
                var resetIn = _lastUsage.TokensReset - DateTime.UtcNow;
                if (resetIn.TotalSeconds > 0)
                    AddDisabledItemTo(menu, $"  Resets in {FormatTimeSpan(resetIn)}");
            }
            menu.Items.Add(new NativeMenuItemSeparator());
        }
        else if (!string.IsNullOrWhiteSpace(_apiKey))
        {
            AddDisabledItemTo(menu, "Remaining: loading...");
            menu.Items.Add(new NativeMenuItemSeparator());
        }

        // Today header
        AddDisabledItemTo(menu, $"Today ({summary.DisplayDate})");
        menu.Items.Add(new NativeMenuItemSeparator());

        // Today's stats
        AddDisabledItemTo(menu, $"  Cost: ~${summary.TodayEstimatedCostUSD:F2}");
        AddDisabledItemTo(menu, $"  Messages: {summary.TodayMessageCount:N0}");
        AddDisabledItemTo(menu, $"  Sessions: {summary.TodaySessionCount}");
        AddDisabledItemTo(menu, $"  Tool Calls: {summary.TodayToolCallCount:N0}");
        menu.Items.Add(new NativeMenuItemSeparator());

        // Models submenu
        if (summary.Models.Count > 0)
        {
            var modelsItem = new NativeMenuItem("Models");
            var modelsMenu = new NativeMenu();

            foreach (var model in summary.Models)
            {
                var modelItem = new NativeMenuItem(model.ShortName);
                var modelMenu = new NativeMenu();

                AddDisabledItemTo(modelMenu, $"In: {TokenDataService.FormatTokenCount(model.InputTokens)}  Out: {TokenDataService.FormatTokenCount(model.OutputTokens)}");
                AddDisabledItemTo(modelMenu, $"Cache R: {TokenDataService.FormatTokenCount(model.CacheReadTokens)}  W: {TokenDataService.FormatTokenCount(model.CacheCreationTokens)}");
                AddDisabledItemTo(modelMenu, $"~${model.EstimatedCostUSD:F2}");

                modelItem.Menu = modelMenu;
                modelsMenu.Items.Add(modelItem);
            }

            modelsItem.Menu = modelsMenu;
            menu.Items.Add(modelsItem);
            menu.Items.Add(new NativeMenuItemSeparator());
        }

        // Projects submenu
        if (summary.Projects.Count > 0)
        {
            var projectsItem = new NativeMenuItem("Projects");
            var projectsMenu = new NativeMenu();

            foreach (var project in summary.Projects)
            {
                AddDisabledItemTo(projectsMenu, $"{project.ShortName}  ${project.LastCost:F2}");
            }

            projectsItem.Menu = projectsMenu;
            menu.Items.Add(projectsItem);
            menu.Items.Add(new NativeMenuItemSeparator());
        }

        // All-time totals
        AddDisabledItemTo(menu, "All Time");
        AddDisabledItemTo(menu, $"  Cost: ~${summary.TotalEstimatedCostUSD:F2}");
        AddDisabledItemTo(menu, $"  Messages: {summary.TotalMessages:N0}");
        AddDisabledItemTo(menu, $"  Sessions: {summary.TotalSessions}");
        menu.Items.Add(new NativeMenuItemSeparator());

        // Actions
        var refreshItem = new NativeMenuItem("Refresh");
        refreshItem.Click += (_, _) =>
        {
            Refresh();
            if (!string.IsNullOrWhiteSpace(_apiKey))
                _ = PollApiAsync();
        };
        menu.Items.Add(refreshItem);

        var quitItem = new NativeMenuItem("Quit");
        quitItem.Click += (_, _) => _quitAction();
        menu.Items.Add(quitItem);

        return menu;
    }

    private static string FormatTimeSpan(TimeSpan ts)
    {
        if (ts.TotalHours >= 1)
            return $"{(int)ts.TotalHours}h {ts.Minutes}m";
        if (ts.TotalMinutes >= 1)
            return $"{(int)ts.TotalMinutes}m";
        return $"{(int)ts.TotalSeconds}s";
    }

    private static void AddDisabledItemTo(NativeMenu menu, string header)
    {
        menu.Items.Add(new NativeMenuItem(header) { IsEnabled = false });
    }

    public void Dispose()
    {
        _watcherService.Dispose();
        _apiService.Dispose();
        _apiPollTimer?.Dispose();
    }
}
