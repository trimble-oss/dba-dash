using DBADashAI.Configuration;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace DBADashAI.Services;

/// <summary>
/// Partitions the token-bucket rate limiter by remote IP address so each client
/// gets an independent bucket. Works correctly for both local (127.0.0.1) and
/// remote clients in shared mode.
/// </summary>
public sealed class AiRateLimitPartitioner : IRateLimiterPolicy<string>
{
    private readonly RateLimitingOptions _options;

    public AiRateLimitPartitioner(RateLimitingOptions options)
    {
        _options = options;
    }

    public RateLimitPartition<string> GetPartition(HttpContext httpContext)
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetTokenBucketLimiter(ip, _ => new TokenBucketRateLimiterOptions
        {
            TokenLimit = _options.TokenLimit,
            TokensPerPeriod = _options.TokensPerPeriod,
            ReplenishmentPeriod = TimeSpan.FromSeconds(_options.ReplenishmentPeriodSeconds),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = _options.QueueLimit,
            AutoReplenishment = true
        });
    }

    // No per-request rejection handling — global OnRejected in Program.cs handles it.
    public Func<OnRejectedContext, CancellationToken, ValueTask>? OnRejected => null;
}
