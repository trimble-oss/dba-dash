namespace DBADashAI.Services
{
    public class AiRunbookLinkService
    {
        private readonly string? _baseUrl;

        public AiRunbookLinkService(IConfiguration configuration)
        {
            _baseUrl = configuration["AI:RunbookBaseUrl"];
        }

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
}
