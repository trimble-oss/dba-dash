#nullable enable

namespace DBADashGUI.AI
{
    /// <summary>
    /// Whether a discovered AI service can be submitted to by this user, and what to say when it can't.
    ///
    /// Submitting is what spends tokens, so that is what access controls.  The AIUser role is what unmasks the
    /// service's API key in the repository; without it the key comes back masked and every request is turned
    /// away with a 401.  Saying so up front beats letting the reader press Submit and read an authentication
    /// error.  Previewing what would be sent spends nothing, so the viewers keep showing it either way.
    /// </summary>
    internal static class AIServiceAccess
    {
        internal const string NotConfigured = "No AI service is configured, so nothing can be submitted.";

        internal const string NoAccess = "You don't have access to the AI service (the AIUser role), so nothing can be submitted.";

        /// <summary>
        /// The service to submit to, or null with the reason there is none.  A local service on loopback needs no
        /// repository permission, so only a service registered in the repository is checked.
        /// </summary>
        internal static AIServiceDiscovery.ServiceInfo? Usable(AIServiceDiscovery.ServiceInfo? service, out string? reason)
        {
            if (service is null)
            {
                reason = NotConfigured;
                return null;
            }

            if (service is { Source: AIServiceDiscovery.ServiceSource.Repository, HasApiKeyAccess: false })
            {
                reason = NoAccess;
                return null;
            }

            reason = null;
            return service;
        }
    }
}
