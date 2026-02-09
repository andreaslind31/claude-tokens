namespace ClaudeTokens.Models;

public class TokenSummary
{
    // Today (or most recent day)
    public string DisplayDate { get; set; } = "";
    public int TodayMessageCount { get; set; }
    public int TodaySessionCount { get; set; }
    public int TodayToolCallCount { get; set; }
    public decimal TodayEstimatedCostUSD { get; set; }

    // All-time totals
    public int TotalMessages { get; set; }
    public int TotalSessions { get; set; }
    public decimal TotalEstimatedCostUSD { get; set; }

    // Per-model breakdown (all time)
    public List<ModelSummary> Models { get; set; } = [];

    // Per-project (last session costs)
    public List<ProjectSummary> Projects { get; set; } = [];
}

public class ModelSummary
{
    public string ModelName { get; set; } = "";
    public string ShortName { get; set; } = "";
    public long InputTokens { get; set; }
    public long OutputTokens { get; set; }
    public long CacheReadTokens { get; set; }
    public long CacheCreationTokens { get; set; }
    public decimal EstimatedCostUSD { get; set; }
}

public class ProjectSummary
{
    public string FullPath { get; set; } = "";
    public string ShortName { get; set; } = "";
    public decimal LastCost { get; set; }
}
