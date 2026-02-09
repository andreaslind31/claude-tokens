using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ClaudeTokens.Models;

namespace ClaudeTokens.Services;

public class ApiUsageService : IDisposable
{
    private readonly HttpClient _http = new();
    private const string BaseUrl = "https://api.anthropic.com/v1/messages";

    public async Task<UsageInfo?> GetUsageAsync(string apiKey)
    {
        try
        {
            // Make a minimal API call to get rate limit headers.
            // Use max_tokens=1 and a tiny prompt to minimize cost.
            var request = new HttpRequestMessage(HttpMethod.Post, BaseUrl);
            request.Headers.Add("x-api-key", apiKey);
            request.Headers.Add("anthropic-version", "2023-06-01");
            request.Content = new StringContent(
                JsonSerializer.Serialize(new
                {
                    model = "claude-haiku-4-5-20251001",
                    max_tokens = 1,
                    messages = new[] { new { role = "user", content = "." } }
                }),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _http.SendAsync(request);
            return ParseRateLimitHeaders(response.Headers);
        }
        catch
        {
            return null;
        }
    }

    private static UsageInfo? ParseRateLimitHeaders(HttpResponseHeaders headers)
    {
        var info = new UsageInfo();
        bool hasData = false;

        if (TryGetHeaderLong(headers, "anthropic-ratelimit-tokens-limit", out var tokensLimit))
        {
            info.TokensLimit = tokensLimit;
            hasData = true;
        }
        if (TryGetHeaderLong(headers, "anthropic-ratelimit-tokens-remaining", out var tokensRemaining))
        {
            info.TokensRemaining = tokensRemaining;
            hasData = true;
        }
        if (TryGetHeaderDateTime(headers, "anthropic-ratelimit-tokens-reset", out var tokensReset))
        {
            info.TokensReset = tokensReset;
        }
        if (TryGetHeaderLong(headers, "anthropic-ratelimit-requests-limit", out var requestsLimit))
        {
            info.RequestsLimit = requestsLimit;
        }
        if (TryGetHeaderLong(headers, "anthropic-ratelimit-requests-remaining", out var requestsRemaining))
        {
            info.RequestsRemaining = requestsRemaining;
        }
        if (TryGetHeaderDateTime(headers, "anthropic-ratelimit-requests-reset", out var requestsReset))
        {
            info.RequestsReset = requestsReset;
        }

        return hasData ? info : null;
    }

    private static bool TryGetHeaderLong(HttpResponseHeaders headers, string name, out long value)
    {
        value = 0;
        if (headers.TryGetValues(name, out var values))
        {
            var str = values.FirstOrDefault();
            return str != null && long.TryParse(str, out value);
        }
        return false;
    }

    private static bool TryGetHeaderDateTime(HttpResponseHeaders headers, string name, out DateTime value)
    {
        value = DateTime.MinValue;
        if (headers.TryGetValues(name, out var values))
        {
            var str = values.FirstOrDefault();
            return str != null && DateTime.TryParse(str, CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind, out value);
        }
        return false;
    }

    public void Dispose()
    {
        _http.Dispose();
    }
}
