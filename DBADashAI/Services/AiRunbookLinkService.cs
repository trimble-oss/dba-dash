namespace DBADashAI.Services;

public sealed class AiRunbookLinkService(IConfiguration configuration)
{
    private readonly string? _baseUrl = configuration["AI:RunbookBaseUrl"];

    public string? Resolve(string category)
    {
        if (string.IsNullOrWhiteSpace(_baseUrl))
        {
            return null;
        }

        var key = category.ToLowerInvariant().Replace(" ", "-");
        return _baseUrl.TrimEnd('/') + "/" + key;
    }
}
