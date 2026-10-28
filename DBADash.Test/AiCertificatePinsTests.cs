using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Security;

namespace DBADash.Test
{
    [TestClass]
    public class AiCertificatePinsTests
    {
        [TestMethod]
        public void Pins_AreKeptPerServiceHostAndPort()
        {
            var pins = new AiCertificatePins();
            pins.Set("https://ai-one.example:5055", "AAAA");
            pins.Set("https://ai-two.example:5055", "BBBB");
            pins.Set("https://ai-one.example:8443", "CCCC");

            var one = new Uri("https://ai-one.example:5055/api/ai/chat");
            var two = new Uri("https://AI-TWO.example:5055/api/ai/chat");

            Assert.IsTrue(pins.IsTrustedOrPinned(one, SslPolicyErrors.RemoteCertificateChainErrors, "aaaa"));
            Assert.IsFalse(pins.IsTrustedOrPinned(one, SslPolicyErrors.RemoteCertificateChainErrors, "BBBB"));
            Assert.IsTrue(pins.IsTrustedOrPinned(two, SslPolicyErrors.RemoteCertificateChainErrors, "BBBB"));
            Assert.AreEqual("CCCC", pins.Get(new Uri("https://ai-one.example:8443/")));
        }

        [TestMethod]
        public void Pins_DefaultPortMatchesARequestWithoutAPort()
        {
            var pins = new AiCertificatePins();
            pins.Set("https://ai.example", "AAAA");

            Assert.AreEqual("AAAA", pins.Get(new Uri("https://ai.example:443/api")));
        }

        [TestMethod]
        public void Pins_EmptyThumbprintRemovesThePin()
        {
            var pins = new AiCertificatePins();
            pins.Set("https://ai.example:5055", "AAAA");
            pins.Set("https://ai.example:5055", null);

            Assert.IsNull(pins.Get(new Uri("https://ai.example:5055/")));
            Assert.IsFalse(pins.IsTrustedOrPinned(new Uri("https://ai.example:5055/"), SslPolicyErrors.RemoteCertificateChainErrors, "AAAA"));
            Assert.IsTrue(pins.IsTrustedOrPinned(new Uri("https://ai.example:5055/"), SslPolicyErrors.None, null));
        }

        [TestMethod]
        public void HostingSettings_EndpointsOrListenUrlsAreCustomHosting()
        {
            static string? NoEnvironment(string _) => null;

            Assert.IsFalse(AiHostingSettings.IsCustomHostingConfigured(new JObject(), NoEnvironment));
            Assert.IsFalse(AiHostingSettings.IsCustomHostingConfigured(
                JObject.Parse("""{"Kestrel":{"Certificates":{"Default":{"Path":"cert.pfx"}}}}"""), NoEnvironment));
            Assert.IsFalse(AiHostingSettings.IsCustomHostingConfigured(
                JObject.Parse("""{"Kestrel":{"Endpoints":{}}}"""), NoEnvironment));
            Assert.IsTrue(AiHostingSettings.IsCustomHostingConfigured(
                JObject.Parse("""{"kestrel":{"endpoints":{"Http":{"Url":"http://*:5055"}}}}"""), NoEnvironment));
            Assert.IsTrue(AiHostingSettings.IsCustomHostingConfigured(
                JObject.Parse("""{"urls":"http://*:5055"}"""), NoEnvironment));
            Assert.IsTrue(AiHostingSettings.IsCustomHostingConfigured(
                new JObject(), name => name == "ASPNETCORE_URLS" ? "http://*:5055" : null));
        }
    }
}
