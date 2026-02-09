using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using ClaudeTokens.Services;
using ClaudeTokens.ViewModels;

namespace ClaudeTokens;

public partial class App : Application
{
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

            var tokenService = new TokenDataService();
            var watcherService = new FileWatcherService();

            _viewModel = new MenuBarViewModel(tokenService, watcherService, () => desktop.Shutdown());

            _trayIcon = new TrayIcon
            {
                ToolTipText = "Claude Code Tokens",
                Menu = _viewModel.Menu,
            };

            // Try to load icon, fall back to no icon (will show as empty space)
            try
            {
                var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "icon.png");
                if (File.Exists(iconPath))
                {
                    _trayIcon.Icon = new WindowIcon(new Bitmap(iconPath));
                }
            }
            catch
            {
                // Icon loading failed, tray icon will still work without an icon
            }

            _trayIcon.IsVisible = true;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
