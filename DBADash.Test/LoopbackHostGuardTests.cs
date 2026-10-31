using DBADashAI.Services.Authentication;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DBADash.Test
{
    /// <summary>
    /// The AI service's local-mode /mcp has no authentication, so it only answers requests addressed to this machine
    /// by a loopback name.  Anything else is a hostname some page pointed at 127.0.0.1.
    /// </summary>
    [TestClass]
    public class LoopbackHostGuardTests
    {
        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("localhost")]
        [DataRow("LOCALHOST")]
        [DataRow("127.0.0.1")]
        [DataRow("127.1.2.3")]
        [DataRow("[::1]")]
        [DataRow("::1")]
        public void LoopbackNames_AreAllowed(string? host)
        {
            Assert.IsTrue(LoopbackHostGuard.IsAllowed(host));
        }

        [TestMethod]
        [DataRow("evil.example")]
        [DataRow("localhost.evil.example")]
        [DataRow("127.0.0.1.nip.io")]
        [DataRow("192.168.1.10")]
        [DataRow("0.0.0.0")]
        [DataRow("[::]")]
        [DataRow("[fe80::1]")]
        [DataRow("my-machine")]
        public void OtherNames_AreRefused(string host)
        {
            Assert.IsFalse(LoopbackHostGuard.IsAllowed(host));
        }
    }
}
