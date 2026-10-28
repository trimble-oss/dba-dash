#nullable enable
using Newtonsoft.Json.Linq;
using System;
using System.Linq;

namespace DBADash
{
    /// <summary>
    /// Reads the AI service's appsettings JSON the way the service decides its hosting at startup
    /// (DBADashAI AiHttpsHosting.IsCustomHostingConfigured). Kestrel endpoints or a listen URL mean
    /// the service keeps that hosting, ignores Use HTTPS and does not rewrite the service URL scheme.
    /// </summary>
    public static class AiHostingSettings
    {
        public static readonly string[] ListenUrlEnvironmentVariables = { "ASPNETCORE_URLS", "DOTNET_URLS" };

        public static bool IsCustomHostingConfigured(JObject settings, Func<string, string?>? getEnvironmentVariable = null)
        {
            if (Get(settings, "Urls") is JValue { Value: string urls } && !string.IsNullOrWhiteSpace(urls))
                return true;
            if (Get(Get(settings, "Kestrel") as JObject, "Endpoints") is JObject endpoints && endpoints.HasValues)
                return true;

            getEnvironmentVariable ??= name => Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Machine);
            return ListenUrlEnvironmentVariables.Any(name => !string.IsNullOrWhiteSpace(getEnvironmentVariable(name)));
        }

        // Configuration keys are case-insensitive.
        private static JToken? Get(JObject? parent, string name) =>
            parent?.GetValue(name, StringComparison.OrdinalIgnoreCase);
    }
}
