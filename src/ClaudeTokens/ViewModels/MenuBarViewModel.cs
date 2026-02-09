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
    private readonly NativeMenu _menu;
    private readonly string? _apiKey;
    private Timer? _apiPollTimer;
    private UsageInfo? _lastUsage;

    public NativeMenu Menu => _menu;
    public event Action<int>? PercentChanged;

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
        _menu = new NativeMenu();

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
        RebuildMenu(summary);
    }

    private void RebuildMenu(TokenSummary summary)
    {
        _menu.Items.Clear();

        // Rate limit section (if API data available)
        if (_lastUsage != null)
        {
            var pct = (int)Math.Round(_lastUsage.RemainingPercent);
            AddDisabledItem($"Remaining: {pct}%");
            AddDisabledItem($"  {TokenDataService.FormatTokenCount(_lastUsage.TokensRemaining)} / {TokenDataService.FormatTokenCount(_lastUsage.TokensLimit)} tokens");
            if (_lastUsage.TokensReset > DateTime.MinValue)
            {
                var resetIn = _lastUsage.TokensReset - DateTime.UtcNow;
                if (resetIn.TotalSeconds > 0)
                    AddDisabledItem($"  Resets in {FormatTimeSpan(resetIn)}");
            }
            AddSeparator();
        }
        else if (!string.IsNullOrWhiteSpace(_apiKey))
        {
            AddDisabledItem("Remaining: loading...");
            AddSeparator();
        }

        // Today header
        AddDisabledItem($"Today ({summary.DisplayDate})");
        AddSeparator();

        // Today's stats
        AddDisabledItem($"  Cost: ~${summary.TodayEstimatedCostUSD:F2}");
        AddDisabledItem($"  Messages: {summary.TodayMessageCount:N0}");
        AddDisabledItem($"  Sessions: {summary.TodaySessionCount}");
        AddDisabledItem($"  Tool Calls: {summary.TodayToolCallCount:N0}");
        AddSeparator();

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
            _menu.Items.Add(modelsItem);
            AddSeparator();
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
            _menu.Items.Add(projectsItem);
            AddSeparator();
        }

        // All-time totals
        AddDisabledItem("All Time");
        AddDisabledItem($"  Cost: ~${summary.TotalEstimatedCostUSD:F2}");
        AddDisabledItem($"  Messages: {summary.TotalMessages:N0}");
        AddDisabledItem($"  Sessions: {summary.TotalSessions}");
        AddSeparator();

        // Actions
        var refreshItem = new NativeMenuItem("Refresh");
        refreshItem.Click += (_, _) =>
        {
            Refresh();
            if (!string.IsNullOrWhiteSpace(_apiKey))
                _ = PollApiAsync();
        };
        _menu.Items.Add(refreshItem);

        var quitItem = new NativeMenuItem("Quit");
        quitItem.Click += (_, _) => _quitAction();
        _menu.Items.Add(quitItem);
    }

    private static string FormatTimeSpan(TimeSpan ts)
    {
        if (ts.TotalHours >= 1)
            return $"{(int)ts.TotalHours}h {ts.Minutes}m";
        if (ts.TotalMinutes >= 1)
            return $"{(int)ts.TotalMinutes}m";
        return $"{(int)ts.TotalSeconds}s";
    }

    private void AddDisabledItem(string header)
    {
        AddDisabledItemTo(_menu, header);
    }

    private static void AddDisabledItemTo(NativeMenu menu, string header)
    {
        menu.Items.Add(new NativeMenuItem(header) { IsEnabled = false });
    }

    private void AddSeparator()
    {
        _menu.Items.Add(new NativeMenuItemSeparator());
    }

    public void Dispose()
    {
        _watcherService.Dispose();
        _apiService.Dispose();
        _apiPollTimer?.Dispose();
    }
}
