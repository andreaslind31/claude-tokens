namespace ClaudeTokens.Models;

public class AppSettings
{
    public string? ApiKey { get; set; }
    public int PollIntervalSeconds { get; set; } = 30;
}
