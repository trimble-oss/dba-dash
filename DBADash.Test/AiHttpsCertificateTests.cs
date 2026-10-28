using DBADash;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading.Tasks;

namespace DBADash.Test
{
    [TestClass]
    public class AiHttpsCertificateTests
    {
        [TestMethod]
        public void IsTrustedOrPinned_AcceptsATrustedCertificateOrTheRegisteredThumbprint()
        {
            Assert.IsTrue(AiHttpsCertificate.IsTrustedOrPinned(SslPolicyErrors.None, "aa", "bb"));
            Assert.IsTrue(AiHttpsCertificate.IsTrustedOrPinned(
                SslPolicyErrors.RemoteCertificateChainErrors,
                "ab cd",
                "ABCD"));
            Assert.IsFalse(AiHttpsCertificate.IsTrustedOrPinned(
                SslPolicyErrors.RemoteCertificateChainErrors,
                "AAAA",
                "BBBB"));
            Assert.IsFalse(AiHttpsCertificate.IsTrustedOrPinned(
                SslPolicyErrors.RemoteCertificateNameMismatch,
                "AAAA",
                null));
        }

        [TestMethod]
        public void LoadForService_ReusesTheCertificateUntilTheHostChanges()
        {
            var directory = Path.Combine(Path.GetTempPath(), "dbadash-ai-https-" + Guid.NewGuid().ToString("N"));
            try
            {
                using var first = AiHttpsCertificate.LoadForService(null, "https://alpha.example:5055", directory);
                using var second = AiHttpsCertificate.LoadForService(null, "https://alpha.example:5055", directory);
                Assert.AreEqual(first.Thumbprint, second.Thumbprint);
                Assert.IsTrue(AiHttpsCertificate.HostMatches(first, "alpha.example"));
                Assert.IsFalse(AiHttpsCertificate.ShouldRenew(first, "https://alpha.example", DateTimeOffset.UtcNow, AiHttpsCertificate.RenewWithin));
                Assert.IsTrue(AiHttpsCertificate.ShouldRenew(first, "https://beta.example", DateTimeOffset.UtcNow, AiHttpsCertificate.RenewWithin));
                Assert.IsTrue(AiHttpsCertificate.ShouldRenew(first, "https://alpha.example", DateTimeOffset.UtcNow.Add(AiHttpsCertificate.Lifetime), AiHttpsCertificate.RenewWithin));

                using var renewed = AiHttpsCertificate.LoadForService(null, "https://beta.example:5055", directory);
                Assert.AreNotEqual(first.Thumbprint, renewed.Thumbprint);
                Assert.IsTrue(AiHttpsCertificate.HostMatches(renewed, "beta.example"));

                var password = File.ReadAllText(Path.Combine(directory, AiHttpsCertificate.PasswordFileName));
                if (OperatingSystem.IsWindows())
                    StringAssert.StartsWith(password, EncryptText.DpApiPrefix);
            }
            finally
            {
                if (Directory.Exists(directory))
                    Directory.Delete(directory, true);
            }
        }

        [TestMethod]
        [DataRow("https://aiproxy:443", 5055, true)]
        [DataRow("https://aiproxy:8443/ai", 5055, true)]
        [DataRow("https://aiserver:5055", 5055, false)]
        [DataRow("https://aiserver", 5055, false)]
        [DataRow("http://aiserver:80", 5055, false)]
        [DataRow("", 5055, false)]
        public void IsTlsTerminatedUpstream_OnlyForHttpsOnAnotherExplicitPort(string serviceUrl, int listenPort, bool expected)
        {
            Assert.AreEqual(expected, AiHttpsCertificate.IsTlsTerminatedUpstream(serviceUrl, listenPort));
        }

        [TestMethod]
        public async Task LoadForService_SelfSignedCertificateCanServeTls()
        {
            // Windows TLS rejects certificates whose key is ephemeral, so loading alone is not enough.
            var directory = Path.Combine(Path.GetTempPath(), "dbadash-ai-https-" + Guid.NewGuid().ToString("N"));
            try
            {
                using var certificate = AiHttpsCertificate.LoadForService(null, "https://localhost:5055", directory);
                var listener = new TcpListener(IPAddress.Loopback, 0);
                listener.Start();
                try
                {
                    var port = ((IPEndPoint)listener.LocalEndpoint).Port;
                    var server = Task.Run(async () =>
                    {
                        using var accepted = await listener.AcceptTcpClientAsync();
                        await using var serverStream = new SslStream(accepted.GetStream());
                        await serverStream.AuthenticateAsServerAsync(new SslServerAuthenticationOptions { ServerCertificate = certificate });
                    });

                    using var client = new TcpClient();
                    await client.ConnectAsync(IPAddress.Loopback, port);
                    await using var clientStream = new SslStream(client.GetStream(), false,
                        (_, presented, _, errors) => AiHttpsCertificate.IsTrustedOrPinned(errors, presented?.GetCertHashString(), certificate.Thumbprint));
                    await clientStream.AuthenticateAsClientAsync("localhost");
                    await server;
                    Assert.IsTrue(clientStream.IsAuthenticated);
                }
                finally
                {
                    listener.Stop();
                }
            }
            finally
            {
                if (Directory.Exists(directory))
                    Directory.Delete(directory, true);
            }
        }

        [TestMethod]
        [SupportedOSPlatform("windows")]
        public void LoadForService_CertificateFolderExcludesOtherUsers()
        {
            if (!OperatingSystem.IsWindows())
                Assert.Inconclusive("Folder permissions are only set on Windows.");

            var directory = Path.Combine(Path.GetTempPath(), "dbadash-ai-https-" + Guid.NewGuid().ToString("N"));
            try
            {
                using var _ = AiHttpsCertificate.LoadForService(null, "https://localhost:5055", directory);
                var rules = new DirectoryInfo(directory).GetAccessControl()
                    .GetAccessRules(includeExplicit: true, includeInherited: true, typeof(SecurityIdentifier));
                var users = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);
                var everyone = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
                foreach (FileSystemAccessRule rule in rules)
                {
                    Assert.AreNotEqual(users, rule.IdentityReference);
                    Assert.AreNotEqual(everyone, rule.IdentityReference);
                }
            }
            finally
            {
                if (Directory.Exists(directory))
                    Directory.Delete(directory, true);
            }
        }

        [TestMethod]
        public void LoadFromLocalMachineStore_FailsForAnUnknownThumbprint()
        {
            if (!OperatingSystem.IsWindows())
                Assert.Inconclusive("The Local Computer store is only read on Windows.");

            var ex = Assert.ThrowsExactly<InvalidOperationException>(
                () => AiHttpsCertificate.LoadFromLocalMachineStore("00 00 00 00"));
            StringAssert.Contains(ex.Message, "was not found");
        }
    }
}
