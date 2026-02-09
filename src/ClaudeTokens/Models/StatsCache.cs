namespace ClaudeTokens.Models;

public class StatsCache
{
    public int Version { get; set; }
    public string LastComputedDate { get; set; } = "";
    public List<DailyActivity> DailyActivity { get; set; } = [];
    public List<DailyModelTokens> DailyModelTokens { get; set; } = [];
    public Dictionary<string, ModelUsage> ModelUsage { get; set; } = new();
    public int TotalSessions { get; set; }
    public int TotalMessages { get; set; }
    public LongestSession? LongestSession { get; set; }
    public string? FirstSessionDate { get; set; }
    public Dictionary<string, int> HourCounts { get; set; } = new();
}

public class DailyActivity
{
    public string Date { get; set; } = "";
    public int MessageCount { get; set; }
    public int SessionCount { get; set; }
    public int ToolCallCount { get; set; }
}

public class DailyModelTokens
{
    public string Date { get; set; } = "";
    public Dictionary<string, long> TokensByModel { get; set; } = new();
}

public class ModelUsage
{
    public long InputTokens { get; set; }
    public long OutputTokens { get; set; }
    public long CacheReadInputTokens { get; set; }
    public long CacheCreationInputTokens { get; set; }
    public int WebSearchRequests { get; set; }
    public decimal CostUSD { get; set; }
    public int ContextWindow { get; set; }
    public int MaxOutputTokens { get; set; }
}

public class LongestSession
{
    public string SessionId { get; set; } = "";
    public long Duration { get; set; }
    public int MessageCount { get; set; }
    public string Timestamp { get; set; } = "";
}
