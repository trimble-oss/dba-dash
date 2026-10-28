#nullable enable
using DBADash;
using System;
using System.Net.Http;

namespace DBADashGUI.AI
{
    /// <summary>
    /// Trusts an AI service certificate when Windows trusts it, or when its thumbprint is the one
    /// stored in the repository for that service. The pin is what makes the shared-mode self-signed
    /// certificate usable without turning certificate checks off. Pins are kept per service host and port.
    /// </summary>
    internal static class AiServerCertificateValidator
    {
        private static readonly AiCertificatePins Pins = new();

        public static void SetPinnedThumbprint(string? serviceUrl, string? thumbprint) =>
            Pins.Set(serviceUrl, thumbprint);

        public static void SetPinnedThumbprint(AIServiceDiscovery.ServiceInfo? service)
        {
            if (service != null)
                Pins.Set(service.ServiceUrl, service.CertificateThumbprint);
        }

        public static HttpClient CreateClient(TimeSpan timeout)
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (request, certificate, _, errors) =>
                    Pins.IsTrustedOrPinned(request.RequestUri, errors, certificate?.Thumbprint)
            };
            return new HttpClient(handler) { Timeout = timeout };
        }
    }
}
