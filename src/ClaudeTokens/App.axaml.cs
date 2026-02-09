using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using ClaudeTokens.Models;
using ClaudeTokens.Services;
using ClaudeTokens.ViewModels;

namespace ClaudeTokens;

public partial class App : Application
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".claude-tokens", "settings.json");

    private MenuBarViewModel? _viewModel;
    private TrayIcon? _trayIcon;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            desktop.MainWindow = null;

            var settings = LoadSettings();
            var tokenService = new TokenDataService();
            var watcherService = new FileWatcherService();
            var apiService = new ApiUsageService();

            _viewModel = new MenuBarViewModel(
                tokenService, watcherService, apiService,
                settings.ApiKey, settings.PollIntervalSeconds,
                () => desktop.Shutdown());

            _trayIcon = new TrayIcon
            {
                ToolTipText = "Claude Code Tokens",
                Menu = _viewModel.Menu,
            };

            // Set initial icon
            try
            {
                var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "icon.png");
                if (File.Exists(iconPath))
                    _trayIcon.Icon = new WindowIcon(new Bitmap(iconPath));
            }
            catch { }

            // Update icon with percentage when API data arrives
            _viewModel.PercentChanged += percent =>
            {
                try
                {
                    _trayIcon.Icon = IconRenderer.RenderPercentageIcon(percent);
                }
                catch { }
            };

            _trayIcon.IsVisible = true;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static AppSettings LoadSettings()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize<AppSettings>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new AppSettings();
            }
        }
        catch { }

        // Create template settings file if it doesn't exist
        try
        {
            var dir = Path.GetDirectoryName(SettingsPath)!;
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            if (!File.Exists(SettingsPath))
            {
                var template = new AppSettings { ApiKey = "your-api-key-here", PollIntervalSeconds = 30 };
                File.WriteAllText(SettingsPath,
                    JsonSerializer.Serialize(template, new JsonSerializerOptions { WriteIndented = true }));
            }
        }
        catch { }

        return new AppSettings();
    }
}
