namespace DBADashAI.Configuration;

/// <summary>
/// Configuration for the per-IP token-bucket rate limiter applied to AI endpoints.
/// Bind from the "RateLimiting" section of appsettings.json.
/// </summary>
public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    /// <summary>Whether rate limiting is enabled. Defaults to true.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Maximum number of tokens (requests) each IP can hold in the bucket at one time.
    /// This is also the maximum burst size.
    /// </summary>
    public int TokenLimit { get; set; } = 20;

    /// <summary>Number of tokens added to each bucket per replenishment period.</summary>
    public int TokensPerPeriod { get; set; } = 10;

    /// <summary>How often the bucket is replenished, in seconds.</summary>
    public int ReplenishmentPeriodSeconds { get; set; } = 60;

    /// <summary>
    /// Maximum number of requests that can queue while waiting for a token.
    /// Set to 0 to reject immediately when the bucket is empty.
    /// </summary>
    public int QueueLimit { get; set; } = 2;
}
