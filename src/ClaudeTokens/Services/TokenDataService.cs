using System.Text.Json;
using ClaudeTokens.Models;

namespace ClaudeTokens.Services;

public class TokenDataService
{
    private static readonly string HomePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    private static readonly string StatsCachePath = Path.Combine(HomePath, ".claude", "stats-cache.json");
    private static readonly string ClaudeConfigPath = Path.Combine(HomePath, ".claude.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // Pricing per token (not per million)
    private static readonly Dictionary<string, (decimal input, decimal output, decimal cacheRead, decimal cacheWrite)> Pricing = new()
    {
        ["claude-opus-4-6"] = (15m / 1_000_000, 75m / 1_000_000, 1.5m / 1_000_000, 18.75m / 1_000_000),
        ["claude-opus-4-5-20251101"] = (15m / 1_000_000, 75m / 1_000_000, 1.5m / 1_000_000, 18.75m / 1_000_000),
        ["claude-sonnet-4-5-20250929"] = (3m / 1_000_000, 15m / 1_000_000, 0.3m / 1_000_000, 3.75m / 1_000_000),
        ["claude-haiku-4-5-20251001"] = (0.8m / 1_000_000, 4m / 1_000_000, 0.08m / 1_000_000, 1m / 1_000_000),
    };

    // Fallback pricing for unknown models (assume Sonnet-level)
    private static readonly (decimal input, decimal output, decimal cacheRead, decimal cacheWrite) FallbackPricing =
        (3m / 1_000_000, 15m / 1_000_000, 0.3m / 1_000_000, 3.75m / 1_000_000);

    public TokenSummary LoadSummary()
    {
        var stats = LoadStatsCache();
        var projects = LoadProjectConfigs();
        return BuildSummary(stats, projects);
    }

    private StatsCache? LoadStatsCache()
    {
        try
        {
            var json = ReadFileShared(StatsCachePath);
            if (json == null) return null;
            return JsonSerializer.Deserialize<StatsCache>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    private Dictionary<string, ProjectConfig> LoadProjectConfigs()
    {
        try
        {
            var json = ReadFileShared(ClaudeConfigPath);
            if (json == null) return new();
            return ClaudeConfigParser.ParseProjects(json);
        }
        catch
        {
            return new();
        }
    }

    private static string? ReadFileShared(string path)
    {
        if (!File.Exists(path)) return null;
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private TokenSummary BuildSummary(StatsCache? stats, Dictionary<string, ProjectConfig> projects)
    {
        var summary = new TokenSummary();

        if (stats != null)
        {
            // Always show today's actual date
            summary.DisplayDate = DateTime.Now.ToString("MMM d");

            var todayStr = DateTime.Now.ToString("yyyy-MM-dd");
            var todayActivity = stats.DailyActivity.FirstOrDefault(d => d.Date == todayStr);

            if (todayActivity != null)
            {
                summary.TodayMessageCount = todayActivity.MessageCount;
                summary.TodaySessionCount = todayActivity.SessionCount;
                summary.TodayToolCallCount = todayActivity.ToolCallCount;
            }
            // else: defaults to 0 for all counts

            var displayActivity = todayActivity ?? stats.DailyActivity.LastOrDefault();

            summary.TotalMessages = stats.TotalMessages;
            summary.TotalSessions = stats.TotalSessions;

            // Build per-model breakdown and all-time cost
            decimal totalCost = 0;

            foreach (var (modelId, usage) in stats.ModelUsage)
            {
                var cost = EstimateCost(modelId, usage);
                totalCost += cost;

                summary.Models.Add(new ModelSummary
                {
                    ModelName = modelId,
                    ShortName = GetShortModelName(modelId),
                    InputTokens = usage.InputTokens,
                    OutputTokens = usage.OutputTokens,
                    CacheReadTokens = usage.CacheReadInputTokens,
                    CacheCreationTokens = usage.CacheCreationInputTokens,
                    EstimatedCostUSD = cost,
                });
            }

            summary.TotalEstimatedCostUSD = totalCost;

            // Estimate displayed day's cost by proportioning all-time cost
            // by message count, since daily data only has output tokens
            // and the real cost is dominated by cache tokens.
            if (displayActivity != null && stats.TotalMessages > 0 && totalCost > 0)
            {
                var dayShare = (decimal)displayActivity.MessageCount / stats.TotalMessages;
                summary.TodayEstimatedCostUSD = totalCost * dayShare;
            }

            // Sort models by cost descending
            summary.Models.Sort((a, b) => b.EstimatedCostUSD.CompareTo(a.EstimatedCostUSD));
        }
        else
        {
            summary.DisplayDate = DateTime.Now.ToString("MMM d");
        }

        // Build project summaries
        foreach (var (path, config) in projects)
        {
            if (config.LastCost.HasValue)
            {
                summary.Projects.Add(new ProjectSummary
                {
                    FullPath = path,
                    ShortName = Path.GetFileName(path.TrimEnd('/')),
                    LastCost = config.LastCost.Value,
                });
            }
        }

        summary.Projects.Sort((a, b) => b.LastCost.CompareTo(a.LastCost));

        return summary;
    }

    private static decimal EstimateCost(string modelId, ModelUsage usage)
    {
        var pricing = GetPricing(modelId);
        return usage.InputTokens * pricing.input
             + usage.OutputTokens * pricing.output
             + usage.CacheReadInputTokens * pricing.cacheRead
             + usage.CacheCreationInputTokens * pricing.cacheWrite;
    }

    private static (decimal input, decimal output, decimal cacheRead, decimal cacheWrite) GetPricing(string modelId)
    {
        if (Pricing.TryGetValue(modelId, out var pricing))
            return pricing;

        // Try partial match for model families
        foreach (var (key, value) in Pricing)
        {
            if (modelId.Contains(key, StringComparison.OrdinalIgnoreCase) ||
                key.Contains(modelId, StringComparison.OrdinalIgnoreCase))
                return value;
        }

        return FallbackPricing;
    }

    private static string GetShortModelName(string modelId)
    {
        if (modelId.Contains("opus-4-6")) return "Opus 4.6";
        if (modelId.Contains("opus-4-5")) return "Opus 4.5";
        if (modelId.Contains("sonnet-4-5")) return "Sonnet 4.5";
        if (modelId.Contains("haiku-4-5")) return "Haiku 4.5";
        if (modelId.Contains("opus")) return "Opus";
        if (modelId.Contains("sonnet")) return "Sonnet";
        if (modelId.Contains("haiku")) return "Haiku";
        return modelId;
    }

    public static string FormatTokenCount(long count)
    {
        if (count >= 1_000_000_000) return $"{count / 1_000_000_000.0:F1}B";
        if (count >= 1_000_000) return $"{count / 1_000_000.0:F1}M";
        if (count >= 1_000) return $"{count / 1_000.0:F1}K";
        return count.ToString("N0");
    }
}
