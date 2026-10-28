#nullable enable
using System;
using System.Collections.Concurrent;
using System.Net.Security;

namespace DBADash
{
    /// <summary>
    /// Registered certificate thumbprints, one per AI service host and port.
    /// A handshake is checked against the pin of the service it connects to, so requests to
    /// different services, or a window holding an older service's details, cannot replace
    /// the pin another service relies on.
    /// </summary>
    public sealed class AiCertificatePins
    {
        private readonly ConcurrentDictionary<string, string> _pins = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Records the thumbprint registered for <paramref name="serviceUrl"/>. An empty thumbprint removes the pin.
        /// </summary>
        public void Set(string? serviceUrl, string? thumbprint)
        {
            if (!TryGetKey(serviceUrl, out var key))
                return;
            if (string.IsNullOrWhiteSpace(thumbprint))
                _pins.TryRemove(key, out _);
            else
                _pins[key] = AiHttpsCertificate.NormalizeThumbprint(thumbprint);
        }

        public string? Get(Uri? requestUri) =>
            requestUri != null && _pins.TryGetValue(Key(requestUri), out var thumbprint) ? thumbprint : null;

        public bool IsTrustedOrPinned(Uri? requestUri, SslPolicyErrors errors, string? presentedThumbprint) =>
            AiHttpsCertificate.IsTrustedOrPinned(errors, presentedThumbprint, Get(requestUri));

        private static bool TryGetKey(string? serviceUrl, out string key)
        {
            key = string.Empty;
            if (!Uri.TryCreate(serviceUrl?.Trim(), UriKind.Absolute, out var uri) || string.IsNullOrWhiteSpace(uri.Host))
                return false;
            key = Key(uri);
            return true;
        }

        private static string Key(Uri uri) => $"{uri.IdnHost}:{uri.Port}";
    }
}
