using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace DBADashAI.Services;

/// <summary>
/// Decides whether shared mode should install the automatic HTTPS binding.
/// A Kestrel endpoint or an explicit listen URL means the operator already chose how the service
/// is hosted, so that setup is left in place. A default Kestrel certificate on its own does not
/// say where to listen: the service still binds Registration:Port and serves that certificate.
/// </summary>
public static class AiHttpsHosting
{
    /// <summary>
    /// Set to "true" when the automatic HTTPS binding is in use, so registration rewrites the service URL to https.
    /// Absent otherwise: with existing hosting or with HTTPS off, the configured URL scheme is kept
    /// (an https:// URL in front of an HTTP listener is a reverse proxy).
    /// </summary>
    public const string ServiceUrlHttpsConfigurationKey = "Security:ServiceUrlHttps";

    private const string DefaultCertificateSection = "Kestrel:Certificates:Default";
    private const string ServerAuthenticationOid = "1.3.6.1.5.5.7.3.1";

    public static bool IsCustomHostingConfigured(IConfiguration configuration) =>
        !string.IsNullOrWhiteSpace(configuration["Urls"])
        || configuration.GetSection("Kestrel:Endpoints").GetChildren().Any();

    /// <summary>
    /// True when Kestrel:Certificates:Default names a certificate Kestrel can load (a file path or a store subject).
    /// </summary>
    public static bool HasDefaultCertificate(IConfiguration configuration) =>
        HasCertificateConfig(configuration.GetSection(DefaultCertificateSection));

    /// <summary>
    /// Thumbprint of a certificate already configured for Kestrel, when the file or store entry can be read.
    /// The certificate is not applied to Kestrel; the existing configuration still does that.
    /// With several HTTPS endpoints, the one serving the service URL's host and port is used.
    /// <paramref name="listenPort"/> is the port registration fills in when the service URL has none.
    /// </summary>
    public static string? TryReadConfiguredThumbprint(IConfiguration configuration, string? serviceUrl, int listenPort, string contentRoot)
    {
        var target = TryGetServiceEndpoint(serviceUrl, listenPort);
        string? hostMatch = null;
        string? wildcardMatch = null;
        var thumbprints = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var endpoint in configuration.GetSection("Kestrel:Endpoints").GetChildren())
        {
            if (!Uri.TryCreate(ToParsableUrl(endpoint["Url"]), UriKind.Absolute, out var endpointUri)
                || !endpointUri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
                continue;

            var certificate = endpoint.GetSection("Certificate");
            if (!HasCertificateConfig(certificate))
                certificate = configuration.GetSection(DefaultCertificateSection);
            var thumbprint = TryLoadThumbprint(certificate, contentRoot);
            if (thumbprint == null)
                continue;
            thumbprints.Add(thumbprint);

            if (target == null || endpointUri.Port != target.Value.Port)
                continue;
            if (IsWildcardHost(endpointUri.Host))
                wildcardMatch ??= thumbprint;
            else if (HostMatches(endpointUri, target.Value.Host))
                hostMatch ??= thumbprint;
        }

        if (hostMatch != null)
            return hostMatch;
        if (wildcardMatch != null)
            return wildcardMatch;
        // No endpoint serves the service URL (a proxy in front, for example). Only an unambiguous certificate is registered.
        if (thumbprints.Count == 1)
            return thumbprints.First();
        if (thumbprints.Count > 1)
            return null;

        return TryLoadThumbprint(configuration.GetSection(DefaultCertificateSection), contentRoot);
    }

    public static bool? ReadSchemeOverride(IConfiguration configuration)
    {
        var value = configuration[ServiceUrlHttpsConfigurationKey];
        if (string.IsNullOrWhiteSpace(value))
            return null;
        return bool.TryParse(value, out var useHttps) ? useHttps : null;
    }

    // These are the keys Kestrel reads to load a certificate. Kestrel has no thumbprint key.
    private static bool HasCertificateConfig(IConfiguration section) =>
        !string.IsNullOrWhiteSpace(section["Path"])
        || !string.IsNullOrWhiteSpace(section["Subject"]);

    private static string? TryLoadThumbprint(IConfiguration section, string contentRoot)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(section["Path"]))
            {
                var path = section["Path"]!;
                if (!Path.IsPathRooted(path))
                    path = Path.Combine(contentRoot, path);
                using var certificate = LoadFromFile(path, section["Password"]);
                return certificate.Thumbprint;
            }

            if (!string.IsNullOrWhiteSpace(section["Subject"]))
                return FindStoreThumbprint(section);
        }
        catch (Exception ex) when (ex is CryptographicException or IOException or InvalidOperationException or UnauthorizedAccessException)
        {
            return null;
        }

        return null;
    }

    private static X509Certificate2 LoadFromFile(string path, string? password)
    {
        try
        {
            return X509CertificateLoader.LoadPkcs12FromFile(path, password, X509KeyStorageFlags.EphemeralKeySet);
        }
        catch (CryptographicException)
        {
            return X509CertificateLoader.LoadCertificateFromFile(path);
        }
    }

    /// <summary>
    /// Picks the store certificate the way Kestrel does: CurrentUser and My unless set,
    /// valid certificates only unless AllowInvalid, then <see cref="SelectServerCertificate"/>.
    /// </summary>
    private static string? FindStoreThumbprint(IConfiguration section)
    {
        if (!OperatingSystem.IsWindows())
            return null;

        var location = StoreLocation.CurrentUser;
        if (!string.IsNullOrWhiteSpace(section["Location"]) && !Enum.TryParse(section["Location"], ignoreCase: true, out location))
            return null;
        var storeName = string.IsNullOrWhiteSpace(section["Store"]) ? "My" : section["Store"]!;
        var allowInvalid = bool.TryParse(section["AllowInvalid"], out var parsed) && parsed;

        using var store = new X509Store(storeName, location);
        store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
        var matches = store.Certificates.Find(X509FindType.FindBySubjectName, section["Subject"]!, validOnly: !allowInvalid);
        try
        {
            return SelectServerCertificate(matches)?.Thumbprint;
        }
        finally
        {
            foreach (var match in matches)
                match.Dispose();
        }
    }

    /// <summary>
    /// Kestrel's choice among store matches: server authentication allowed, a private key, then the latest expiry.
    /// </summary>
    public static X509Certificate2? SelectServerCertificate(IEnumerable<X509Certificate2> candidates) =>
        candidates
            .Where(IsAllowedForServerAuthentication)
            .Where(certificate => certificate.HasPrivateKey)
            .OrderByDescending(certificate => certificate.NotAfter)
            .FirstOrDefault();

    private static bool IsAllowedForServerAuthentication(X509Certificate2 certificate)
    {
        var hasEnhancedKeyUsage = false;
        foreach (var extension in certificate.Extensions.OfType<X509EnhancedKeyUsageExtension>())
        {
            hasEnhancedKeyUsage = true;
            foreach (var usage in extension.EnhancedKeyUsages)
            {
                if (usage.Value == ServerAuthenticationOid)
                    return true;
            }
        }

        return !hasEnhancedKeyUsage;
    }

    // Kestrel accepts * and + as "any host", which Uri cannot parse.
    private static string? ToParsableUrl(string? url) =>
        url?.Replace("://*", "://0.0.0.0", StringComparison.Ordinal)
            .Replace("://+", "://0.0.0.0", StringComparison.Ordinal);

    private static bool IsWildcardHost(string host) =>
        host is "0.0.0.0" or "[::]";

    private static bool HostMatches(Uri endpointUri, string host) =>
        string.Equals(endpointUri.IdnHost, host, StringComparison.OrdinalIgnoreCase)
        || string.Equals(endpointUri.Host, host, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Host and port GUI clients connect to. A service URL without a port gets the listen port,
    /// as registration does.
    /// </summary>
    private static (string Host, int Port)? TryGetServiceEndpoint(string? serviceUrl, int listenPort)
    {
        if (!Uri.TryCreate(serviceUrl?.Trim(), UriKind.Absolute, out var uri) || string.IsNullOrWhiteSpace(uri.Host))
            return null;
        var hasExplicitPort = serviceUrl!.Contains($":{uri.Port}", StringComparison.OrdinalIgnoreCase);
        return (uri.IdnHost, hasExplicitPort ? uri.Port : listenPort);
    }
}
