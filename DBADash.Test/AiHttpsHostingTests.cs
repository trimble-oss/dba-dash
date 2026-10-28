using DBADashAI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace DBADash.Test
{
    [TestClass]
    public class AiHttpsHostingTests
    {
        [TestMethod]
        public void IsCustomHostingConfigured_IsFalseUntilHostingIsSet()
        {
            Assert.IsFalse(AiHttpsHosting.IsCustomHostingConfigured(Config()));
            Assert.IsTrue(AiHttpsHosting.IsCustomHostingConfigured(Config(("Urls", "https://*:443"))));
            Assert.IsTrue(AiHttpsHosting.IsCustomHostingConfigured(Config(("Kestrel:Endpoints:Https:Url", "https://aiserver:443"))));
            Assert.IsFalse(AiHttpsHosting.IsCustomHostingConfigured(Config(("Security:CertificateThumbprint", "ABCDEF"))));
        }

        [TestMethod]
        public void DefaultCertificate_IsNotCustomHostingSoTheServicePortIsStillBound()
        {
            var path = Config(("Kestrel:Certificates:Default:Path", "cert.pfx"));
            var subject = Config(("Kestrel:Certificates:Default:Subject", "aiserver.example"));

            Assert.IsFalse(AiHttpsHosting.IsCustomHostingConfigured(path));
            Assert.IsFalse(AiHttpsHosting.IsCustomHostingConfigured(subject));
            Assert.IsTrue(AiHttpsHosting.HasDefaultCertificate(path));
            Assert.IsTrue(AiHttpsHosting.HasDefaultCertificate(subject));
        }

        [TestMethod]
        public void ThumbprintKey_IsNotAKestrelCertificateSetting()
        {
            var configuration = Config(("Kestrel:Certificates:Default:Thumbprint", "ABCDEF"));

            Assert.IsFalse(AiHttpsHosting.IsCustomHostingConfigured(configuration));
            Assert.IsFalse(AiHttpsHosting.HasDefaultCertificate(configuration));
            Assert.IsNull(AiHttpsHosting.TryReadConfiguredThumbprint(configuration, "https://aiserver.example", 5055, Path.GetTempPath()));
        }

        [TestMethod]
        public void TryReadConfiguredThumbprint_ReadsTheEndpointCertificateThatMatchesTheServiceHost()
        {
            var directory = Path.Combine(Path.GetTempPath(), "dbadash-ai-hosting-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                var other = WriteCertificate(directory, "other.pfx", "other.example");
                var match = WriteCertificate(directory, "match.pfx", "aiserver.example");
                var configuration = Config(
                    ("Kestrel:Endpoints:Other:Url", "https://other.example:443"),
                    ("Kestrel:Endpoints:Other:Certificate:Path", "other.pfx"),
                    ("Kestrel:Endpoints:Other:Certificate:Password", "secret"),
                    ("Kestrel:Endpoints:Match:Url", "https://aiserver.example:443"),
                    ("Kestrel:Endpoints:Match:Certificate:Path", "match.pfx"),
                    ("Kestrel:Endpoints:Match:Certificate:Password", "secret"));

                var thumbprint = AiHttpsHosting.TryReadConfiguredThumbprint(configuration, "https://aiserver.example", 443, directory);

                Assert.AreEqual(match, thumbprint);
                Assert.AreNotEqual(other, thumbprint);
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        [TestMethod]
        public void TryReadConfiguredThumbprint_ReadsTheDefaultCertificateWhenTheEndpointHasNone()
        {
            var directory = Path.Combine(Path.GetTempPath(), "dbadash-ai-hosting-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                var thumbprint = WriteCertificate(directory, "default.pfx", "aiserver.example");
                var configuration = Config(
                    ("Kestrel:Endpoints:Https:Url", "https://*:443"),
                    ("Kestrel:Certificates:Default:Path", "default.pfx"),
                    ("Kestrel:Certificates:Default:Password", "secret"));

                Assert.AreEqual(thumbprint, AiHttpsHosting.TryReadConfiguredThumbprint(configuration, "https://aiserver.example", 443, directory));
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        [TestMethod]
        public void TryReadConfiguredThumbprint_MatchesTheServicePort()
        {
            WithDirectory(directory =>
            {
                var standard = WriteCertificate(directory, "standard.pfx", "aiserver.example");
                var alternate = WriteCertificate(directory, "alternate.pfx", "aiserver.example");
                var configuration = Config(
                    ("Kestrel:Endpoints:Standard:Url", "https://aiserver.example:443"),
                    ("Kestrel:Endpoints:Standard:Certificate:Path", "standard.pfx"),
                    ("Kestrel:Endpoints:Standard:Certificate:Password", "secret"),
                    ("Kestrel:Endpoints:Alternate:Url", "https://aiserver.example:8443"),
                    ("Kestrel:Endpoints:Alternate:Certificate:Path", "alternate.pfx"),
                    ("Kestrel:Endpoints:Alternate:Certificate:Password", "secret"));

                Assert.AreEqual(alternate, AiHttpsHosting.TryReadConfiguredThumbprint(configuration, "https://aiserver.example:8443", 5055, directory));
                Assert.AreEqual(standard, AiHttpsHosting.TryReadConfiguredThumbprint(configuration, "https://aiserver.example:443", 5055, directory));
            });
        }

        [TestMethod]
        public void TryReadConfiguredThumbprint_MatchesAWildcardEndpointOnTheListenPortWhenTheUrlHasNoPort()
        {
            WithDirectory(directory =>
            {
                WriteCertificate(directory, "other.pfx", "aiserver.example");
                var service = WriteCertificate(directory, "service.pfx", "aiserver.example");
                var configuration = Config(
                    ("Kestrel:Endpoints:Other:Url", "https://*:443"),
                    ("Kestrel:Endpoints:Other:Certificate:Path", "other.pfx"),
                    ("Kestrel:Endpoints:Other:Certificate:Password", "secret"),
                    ("Kestrel:Endpoints:Service:Url", "https://*:5055"),
                    ("Kestrel:Endpoints:Service:Certificate:Path", "service.pfx"),
                    ("Kestrel:Endpoints:Service:Certificate:Password", "secret"));

                Assert.AreEqual(service, AiHttpsHosting.TryReadConfiguredThumbprint(configuration, "https://aiserver.example", 5055, directory));
            });
        }

        [TestMethod]
        public void TryReadConfiguredThumbprint_RegistersNothingWhenNoEndpointServesTheUrlAndCertificatesDiffer()
        {
            WithDirectory(directory =>
            {
                WriteCertificate(directory, "one.pfx", "aiserver.example");
                WriteCertificate(directory, "two.pfx", "aiserver.example");
                var configuration = Config(
                    ("Kestrel:Endpoints:One:Url", "https://*:443"),
                    ("Kestrel:Endpoints:One:Certificate:Path", "one.pfx"),
                    ("Kestrel:Endpoints:One:Certificate:Password", "secret"),
                    ("Kestrel:Endpoints:Two:Url", "https://*:8443"),
                    ("Kestrel:Endpoints:Two:Certificate:Path", "two.pfx"),
                    ("Kestrel:Endpoints:Two:Certificate:Password", "secret"));

                Assert.IsNull(AiHttpsHosting.TryReadConfiguredThumbprint(configuration, "https://aiproxy:9443", 5055, directory));
            });
        }

        [TestMethod]
        public void TryReadConfiguredThumbprint_DoesNotTreatAnHttpEndpointAsServingTheDefaultCertificate()
        {
            WithDirectory(directory =>
            {
                var standard = WriteCertificate(directory, "standard.pfx", "aiserver.example");
                WriteCertificate(directory, "default.pfx", "aiserver.example");
                var configuration = Config(
                    ("Kestrel:Endpoints:Http:Url", "http://*:5055"),
                    ("Kestrel:Endpoints:Https:Url", "https://*:443"),
                    ("Kestrel:Endpoints:Https:Certificate:Path", "standard.pfx"),
                    ("Kestrel:Endpoints:Https:Certificate:Password", "secret"),
                    ("Kestrel:Certificates:Default:Path", "default.pfx"),
                    ("Kestrel:Certificates:Default:Password", "secret"));

                // The HTTP endpoint on the service port uses no certificate, so the only HTTPS endpoint's certificate is registered.
                Assert.AreEqual(standard, AiHttpsHosting.TryReadConfiguredThumbprint(configuration, "https://aiserver.example", 5055, directory));
            });
        }

        [TestMethod]
        public void SelectServerCertificate_SkipsCertificatesKestrelWouldNotServe()
        {
            using var usable = CreateCertificate("aiserver.example", DateTimeOffset.UtcNow.AddDays(30), serverAuth: true);
            using var newerClientOnly = CreateCertificate("aiserver.example", DateTimeOffset.UtcNow.AddDays(60), serverAuth: false);
            using var newerWithKey = CreateCertificate("aiserver.example", DateTimeOffset.UtcNow.AddDays(90), serverAuth: true);
            using var newerPublicOnly = X509CertificateLoader.LoadCertificate(newerWithKey.RawData);

            Assert.AreEqual(usable.Thumbprint, AiHttpsHosting.SelectServerCertificate(new[] { usable, newerClientOnly, newerPublicOnly })?.Thumbprint);
            Assert.AreEqual(newerWithKey.Thumbprint, AiHttpsHosting.SelectServerCertificate(new[] { usable, newerWithKey })?.Thumbprint);
        }

        [TestMethod]
        public void ReadSchemeOverride_IsUnsetUntilStartupChoosesAScheme()
        {
            Assert.IsNull(AiHttpsHosting.ReadSchemeOverride(Config()));
            Assert.IsTrue(AiHttpsHosting.ReadSchemeOverride(Config((AiHttpsHosting.ServiceUrlHttpsConfigurationKey, "true"))));
            Assert.IsFalse(AiHttpsHosting.ReadSchemeOverride(Config((AiHttpsHosting.ServiceUrlHttpsConfigurationKey, "false"))));
        }

        private static string WriteCertificate(string directory, string fileName, string subject)
        {
            using var rsa = RSA.Create(2048);
            var request = new CertificateRequest($"CN={subject}", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            using var created = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(30));
            File.WriteAllBytes(Path.Combine(directory, fileName), created.Export(X509ContentType.Pfx, "secret"));
            return created.Thumbprint;
        }

        private static void WithDirectory(Action<string> test)
        {
            var directory = Path.Combine(Path.GetTempPath(), "dbadash-ai-hosting-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                test(directory);
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        private static X509Certificate2 CreateCertificate(string subject, DateTimeOffset notAfter, bool serverAuth)
        {
            using var rsa = RSA.Create(2048);
            var request = new CertificateRequest($"CN={subject}", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            var usage = new OidCollection { new Oid(serverAuth ? "1.3.6.1.5.5.7.3.1" : "1.3.6.1.5.5.7.3.2") };
            request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(usage, false));
            return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), notAfter);
        }

        private static IConfiguration Config(params (string Key, string Value)[] values)
        {
            var data = new Dictionary<string, string?>();
            foreach (var (key, value) in values)
                data[key] = value;
            return new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        }
    }
}
