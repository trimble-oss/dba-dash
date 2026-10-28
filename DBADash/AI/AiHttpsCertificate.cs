#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;

namespace DBADash
{
    /// <summary>
    /// HTTPS certificate for the shared AI service.
    ///
    /// With no certificate selected, the service creates a self-signed certificate and keeps it next
    /// to the application. GUI clients accept that certificate when its thumbprint matches the value
    /// registered in the repository. A certificate chosen from the Local Computer Personal store is
    /// used instead, and clients also accept it when Windows trusts it and the name matches.
    /// </summary>
    public static class AiHttpsCertificate
    {
        public const string PfxFileName = "ai-https.pfx";
        public const string PasswordFileName = "ai-https.pfx.password";

        /// <summary>
        /// In-memory configuration key set once the process has chosen the certificate it is serving.
        /// Registration writes this thumbprint to the repository.
        /// </summary>
        public const string ActiveThumbprintConfigurationKey = "Security:ActiveCertificateThumbprint";

        public static readonly TimeSpan Lifetime = TimeSpan.FromDays(730);
        public static readonly TimeSpan RenewWithin = TimeSpan.FromDays(30);

        // Windows TLS (SChannel) cannot serve a certificate whose key is ephemeral, so the key is
        // loaded into the machine key store for the life of the process. It is not persisted:
        // the key file is removed when the certificate is disposed.
        private const X509KeyStorageFlags ServerKeyStorageFlags = X509KeyStorageFlags.MachineKeySet;

        public static string NormalizeThumbprint(string thumbprint) =>
            thumbprint.Replace(" ", "", StringComparison.Ordinal).ToUpperInvariant();

        /// <summary>
        /// Accepts a certificate Windows already trusts, including a matching host name.
        /// Otherwise accepts only the certificate whose thumbprint was registered for this service.
        /// </summary>
        public static bool IsTrustedOrPinned(SslPolicyErrors errors, string? presentedThumbprint, string? pinnedThumbprint)
        {
            if (errors == SslPolicyErrors.None)
                return true;
            if (string.IsNullOrWhiteSpace(presentedThumbprint) || string.IsNullOrWhiteSpace(pinnedThumbprint))
                return false;
            return string.Equals(
                NormalizeThumbprint(presentedThumbprint),
                NormalizeThumbprint(pinnedThumbprint),
                StringComparison.OrdinalIgnoreCase);
        }

        public static bool HostMatches(X509Certificate2 certificate, string host)
        {
            if (certificate == null || string.IsNullOrWhiteSpace(host))
                return false;

            try
            {
                if (IPAddress.TryParse(host, out var ip))
                {
                    foreach (var extension in certificate.Extensions)
                    {
                        if (extension is not X509SubjectAlternativeNameExtension san)
                            continue;
                        foreach (var listed in san.EnumerateIPAddresses())
                        {
                            if (listed.Equals(ip))
                                return true;
                        }
                    }
                }

                return certificate.MatchesHostname(host);
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        /// <summary>
        /// True when <paramref name="serviceUrl"/> is HTTPS on an explicit port other than the port this
        /// service listens on. Before shared mode defaulted to HTTPS, that meant TLS was terminated by
        /// a reverse proxy in front of an HTTP listener. Used only when HTTPS is not set explicitly.
        /// </summary>
        public static bool IsTlsTerminatedUpstream(string? serviceUrl, int listenPort)
        {
            if (!Uri.TryCreate(serviceUrl?.Trim(), UriKind.Absolute, out var uri)
                || !uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
                return false;
            // Registration replaces a missing port with the listen port, so only an explicit port can differ.
            var hasExplicitPort = serviceUrl!.Contains($":{uri.Port}", StringComparison.OrdinalIgnoreCase);
            return hasExplicitPort && uri.Port != listenPort;
        }

        public static bool ShouldRenew(X509Certificate2 certificate, string serviceUrl, DateTimeOffset utcNow, TimeSpan renewWithin)
        {
            if (certificate.NotAfter.ToUniversalTime() <= utcNow.UtcDateTime.Add(renewWithin))
                return true;
            return TryGetServiceHost(serviceUrl, out var host) && !HostMatches(certificate, host);
        }

        /// <summary>
        /// Loads the configured store certificate, or the self-signed certificate for <paramref name="serviceUrl"/>.
        /// A configured thumbprint that cannot be used fails rather than silently substituting another certificate.
        /// </summary>
        public static X509Certificate2 LoadForService(string? configuredThumbprint, string serviceUrl, string storageDirectory)
        {
            if (!string.IsNullOrWhiteSpace(configuredThumbprint))
                return LoadFromLocalMachineStore(configuredThumbprint);
            return LoadOrCreateSelfSigned(serviceUrl, storageDirectory, DateTimeOffset.UtcNow);
        }

        public static X509Certificate2 LoadFromLocalMachineStore(string thumbprint)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                throw new InvalidOperationException(
                    "A certificate thumbprint was configured, and reading it from the Local Computer Personal store is only supported on Windows.");
            }

            var normalized = NormalizeThumbprint(thumbprint);
            // Certificates returned from a store stay usable after the store is closed (Kestrel loads
            // store certificates the same way), so only the matching certificate is kept.
            using var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
            var certificates = store.Certificates;
            X509Certificate2? match = null;
            try
            {
                foreach (var cert in certificates)
                {
                    if (string.Equals(NormalizeThumbprint(cert.Thumbprint), normalized, StringComparison.OrdinalIgnoreCase))
                    {
                        match = cert;
                        break;
                    }
                }

                if (match == null)
                {
                    throw new InvalidOperationException(
                        $"HTTPS certificate {normalized} was not found in the Local Computer Personal store.");
                }

                if (match.NotAfter.ToUniversalTime() <= DateTime.UtcNow)
                {
                    throw new InvalidOperationException(
                        $"HTTPS certificate {normalized} expired on {match.NotAfter:u}. Choose another certificate, or clear the thumbprint to use a self-signed certificate.");
                }

                if (!match.HasPrivateKey)
                {
                    throw new InvalidOperationException(
                        $"HTTPS certificate {normalized} is in the Local Computer Personal store but has no private key.");
                }

                EnsurePrivateKeyAccess(match, normalized);
                return match;
            }
            finally
            {
                foreach (var cert in certificates)
                {
                    if (!ReferenceEquals(cert, match))
                        cert.Dispose();
                }
            }
        }

        /// <summary>
        /// HasPrivateKey only says a key is associated with the certificate. Opening it is what fails when
        /// the service account has no permission, so check here rather than at the first TLS handshake.
        /// </summary>
        private static void EnsurePrivateKeyAccess(X509Certificate2 certificate, string thumbprint)
        {
            try
            {
                using AsymmetricAlgorithm? key = (AsymmetricAlgorithm?)certificate.GetRSAPrivateKey()
                                                 ?? certificate.GetECDsaPrivateKey();
                if (key == null)
                {
                    throw new InvalidOperationException(
                        $"HTTPS certificate {thumbprint} has a private key type that cannot be used for HTTPS. Use an RSA or ECDSA certificate.");
                }
            }
            catch (CryptographicException ex)
            {
                throw new InvalidOperationException(
                    $"HTTPS certificate {thumbprint} is in the Local Computer Personal store, but its private key could not be opened. Grant the AI service account permission to use the key (Manage Private Keys in the Certificates console).",
                    ex);
            }
        }

        private static X509Certificate2 LoadOrCreateSelfSigned(string serviceUrl, string storageDirectory, DateTimeOffset utcNow)
        {
            var existing = TryLoadSelfSigned(serviceUrl, storageDirectory, utcNow);
            if (existing != null)
                return existing;

            var password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            var pfx = CreateSelfSignedPfx(serviceUrl, password, utcNow);
            Directory.CreateDirectory(storageDirectory);
            RestrictAccess(storageDirectory);
            File.WriteAllBytes(Path.Combine(storageDirectory, PfxFileName), pfx);
            File.WriteAllText(
                Path.Combine(storageDirectory, PasswordFileName),
                password.ToMachineProtectedConfigValue());
            return X509CertificateLoader.LoadPkcs12(pfx, password, ServerKeyStorageFlags);
        }

        /// <summary>
        /// The password is protected with machine-scope DPAPI, which any local process can decrypt,
        /// so the folder itself must keep other users out. ProgramData grants Users read by default.
        /// Access is limited to SYSTEM, Administrators and the account running the service.
        /// </summary>
        private static void RestrictAccess(string directory)
        {
            if (!OperatingSystem.IsWindows())
                return;

            var security = new DirectorySecurity();
            security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
            var inheritance = InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit;
            void Allow(IdentityReference identity) =>
                security.AddAccessRule(new FileSystemAccessRule(identity, FileSystemRights.FullControl, inheritance, PropagationFlags.None, AccessControlType.Allow));
            Allow(new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null));
            Allow(new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null));
            using (var identity = WindowsIdentity.GetCurrent())
                Allow(identity.User!);
            new DirectoryInfo(directory).SetAccessControl(security);
        }

        private static X509Certificate2? TryLoadSelfSigned(string serviceUrl, string storageDirectory, DateTimeOffset utcNow)
        {
            var pfxPath = Path.Combine(storageDirectory, PfxFileName);
            var passwordPath = Path.Combine(storageDirectory, PasswordFileName);
            if (!File.Exists(pfxPath) || !File.Exists(passwordPath))
                return null;

            try
            {
                var password = File.ReadAllText(passwordPath).FromMachineProtectedConfigValue().Trim();
                var cert = X509CertificateLoader.LoadPkcs12FromFile(pfxPath, password, ServerKeyStorageFlags);
                if (!cert.HasPrivateKey || ShouldRenew(cert, serviceUrl, utcNow, RenewWithin))
                {
                    cert.Dispose();
                    return null;
                }

                return cert;
            }
            catch (Exception ex) when (ex is CryptographicException or InvalidDataException or FormatException or IOException)
            {
                return null;
            }
        }

        private static byte[] CreateSelfSignedPfx(string serviceUrl, string password, DateTimeOffset utcNow)
        {
            using var rsa = RSA.Create(2048);
            var request = new CertificateRequest(
                new X500DistinguishedName("CN=DBA Dash AI"),
                rsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);
            request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
            request.CertificateExtensions.Add(new X509KeyUsageExtension(
                X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,
                critical: true));
            var serverAuth = new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") };
            request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(serverAuth, critical: false));

            var san = new SubjectAlternativeNameBuilder();
            foreach (var host in GetHostNames(serviceUrl))
            {
                if (IPAddress.TryParse(host, out var ip))
                    san.AddIpAddress(ip);
                else
                    san.AddDnsName(host);
            }

            request.CertificateExtensions.Add(san.Build());
            using var created = request.CreateSelfSigned(utcNow.AddDays(-1), utcNow.Add(Lifetime));
            return created.Export(X509ContentType.Pfx, password);
        }

        private static List<string> GetHostNames(string serviceUrl)
        {
            var names = new List<string>();
            if (TryGetServiceHost(serviceUrl, out var host))
                AddHost(names, host);
            AddHost(names, "localhost");
            try
            {
                AddHost(names, Dns.GetHostName());
            }
            catch (Exception ex) when (ex is ArgumentException or System.Net.Sockets.SocketException)
            {
                // The certificate still has the service URL host and localhost.
            }

            AddHost(names, Environment.MachineName);
            return names;
        }

        private static void AddHost(List<string> names, string? host)
        {
            if (string.IsNullOrWhiteSpace(host))
                return;
            foreach (var existing in names)
            {
                if (string.Equals(existing, host, StringComparison.OrdinalIgnoreCase))
                    return;
            }

            names.Add(host);
        }

        private static bool TryGetServiceHost(string serviceUrl, out string host)
        {
            host = "";
            if (!Uri.TryCreate(serviceUrl, UriKind.Absolute, out var uri) || string.IsNullOrWhiteSpace(uri.Host))
                return false;
            host = uri.IdnHost;
            return true;
        }
    }
}
