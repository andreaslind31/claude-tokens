using System.Text.Json;

namespace ClaudeTokens.Models;

public class ProjectConfig
{
    public decimal? LastCost { get; set; }
    public long? LastTotalInputTokens { get; set; }
    public long? LastTotalOutputTokens { get; set; }
    public long? LastTotalCacheCreationInputTokens { get; set; }
    public long? LastTotalCacheReadInputTokens { get; set; }
    public Dictionary<string, ProjectModelUsage>? LastModelUsage { get; set; }
    public string? LastSessionId { get; set; }
}

public class ProjectModelUsage
{
    public long InputTokens { get; set; }
    public long OutputTokens { get; set; }
    public long CacheReadInputTokens { get; set; }
    public long CacheCreationInputTokens { get; set; }
    public int WebSearchRequests { get; set; }
    public decimal CostUSD { get; set; }
}

public static class ClaudeConfigParser
{
    public static Dictionary<string, ProjectConfig> ParseProjects(string json)
    {
        var result = new Dictionary<string, ProjectConfig>();
        using var doc = JsonDocument.Parse(json);

        if (!doc.RootElement.TryGetProperty("projects", out var projectsElement))
            return result;

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        foreach (var prop in projectsElement.EnumerateObject())
        {
            // Only include projects that have cost data
            if (prop.Value.TryGetProperty("lastCost", out _))
            {
                var config = JsonSerializer.Deserialize<ProjectConfig>(prop.Value.GetRawText(), options);
                if (config != null)
                    result[prop.Name] = config;
            }
        }

        return result;
    }
}
