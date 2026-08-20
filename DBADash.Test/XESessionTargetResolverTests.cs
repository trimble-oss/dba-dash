using DBADash.XE;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DBADash.Test
{
    /// <summary>
    /// Tests for <see cref="XESessionTargetResolver.ResolveEventFileReadPath"/> - the pure logic that turns a running
    /// event_file target's <c>&lt;File name="..."&gt;</c> into a wildcard read path so rollover files are followed.
    /// Shared by the live watch and the one-shot "view existing data" read.
    /// </summary>
    [TestClass]
    public class XESessionTargetResolverTests
    {
        private static string Target(string fileName) =>
            $"<EventFileTarget><File name=\"{fileName}\" /></EventFileTarget>";

        [TestMethod]
        public void RollverSuffix_ReplacedWithWildcard()
        {
            var xml = Target(@"C:\Logs\MySession_0_133456789012345678.xel");

            var path = XESessionTargetResolver.ResolveEventFileReadPath(xml);

            Assert.AreEqual(@"C:\Logs\MySession*.xel", path);
        }

        [TestMethod]
        public void NameWithoutRolloverSuffix_LeftAsIs()
        {
            var xml = Target(@"C:\Logs\MySession.xel");

            var path = XESessionTargetResolver.ResolveEventFileReadPath(xml);

            Assert.AreEqual(@"C:\Logs\MySession.xel", path);
        }

        [TestMethod]
        public void RootFileElement_IsHandled()
        {
            var xml = "<File name=\"D:\\XE\\health_0_133456789012345678.xel\" />";

            var path = XESessionTargetResolver.ResolveEventFileReadPath(xml);

            Assert.AreEqual(@"D:\XE\health*.xel", path);
        }

        [TestMethod]
        public void UnderscoresInName_OnlyTrailingPairStripped()
        {
            // Only the trailing _<targetId>_<timestamp> pair before .xel is the rollover suffix.
            var xml = Target(@"C:\Logs\my_session_name_0_133456789012345678.xel");

            var path = XESessionTargetResolver.ResolveEventFileReadPath(xml);

            Assert.AreEqual(@"C:\Logs\my_session_name*.xel", path);
        }

        [TestMethod]
        public void NullEmptyOrMissingName_ReturnsNull()
        {
            Assert.IsNull(XESessionTargetResolver.ResolveEventFileReadPath(null));
            Assert.IsNull(XESessionTargetResolver.ResolveEventFileReadPath(""));
            Assert.IsNull(XESessionTargetResolver.ResolveEventFileReadPath("<EventFileTarget><File /></EventFileTarget>"));
        }

        [TestMethod]
        public void InvalidXml_ReturnsNull()
        {
            Assert.IsNull(XESessionTargetResolver.ResolveEventFileReadPath("<not-valid"));
        }
    }
}
