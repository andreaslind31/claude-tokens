namespace ClaudeTokens.Services;

public class FileWatcherService : IDisposable
{
    private FileSystemWatcher? _statsCacheWatcher;
    private FileSystemWatcher? _configWatcher;
    private Timer? _debounceTimer;
    private Timer? _pollTimer;
    private readonly TimeSpan _debounceInterval = TimeSpan.FromMilliseconds(500);

    public event Action? DataChanged;

    public void Start()
    {
        var homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var claudeDir = Path.Combine(homePath, ".claude");

        if (Directory.Exists(claudeDir))
        {
            _statsCacheWatcher = new FileSystemWatcher(claudeDir, "stats-cache.json")
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
            };
            _statsCacheWatcher.Changed += OnFileChanged;
            _statsCacheWatcher.EnableRaisingEvents = true;
        }

        if (Directory.Exists(homePath))
        {
            _configWatcher = new FileSystemWatcher(homePath, ".claude.json")
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
            };
            _configWatcher.Changed += OnFileChanged;
            _configWatcher.EnableRaisingEvents = true;
        }

        // Fallback polling every 60 seconds
        _pollTimer = new Timer(_ => DataChanged?.Invoke(), null,
            TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(60));
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        _debounceTimer?.Dispose();
        _debounceTimer = new Timer(_ => DataChanged?.Invoke(),
            null, _debounceInterval, Timeout.InfiniteTimeSpan);
    }

    public void Dispose()
    {
        _statsCacheWatcher?.Dispose();
        _configWatcher?.Dispose();
        _debounceTimer?.Dispose();
        _pollTimer?.Dispose();
    }
}
