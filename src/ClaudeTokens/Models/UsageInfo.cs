namespace ClaudeTokens.Models;

public class UsageInfo
{
    public long TokensLimit { get; set; }
    public long TokensRemaining { get; set; }
    public DateTime TokensReset { get; set; }
    public long RequestsLimit { get; set; }
    public long RequestsRemaining { get; set; }
    public DateTime RequestsReset { get; set; }

    public double RemainingPercent =>
        TokensLimit > 0 ? (double)TokensRemaining / TokensLimit * 100 : 0;
}
