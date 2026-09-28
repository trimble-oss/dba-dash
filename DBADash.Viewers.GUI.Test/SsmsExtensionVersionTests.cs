using DBADashGUI.ShellIntegration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Text;

namespace DBADash.Viewers.GUI.Test
{
    /// <summary>
    /// Telling whether the SSMS extension that handed over a file is older than the one this build carries.
    /// </summary>
    [TestClass]
    public class SsmsExtensionVersionTests
    {
        [TestMethod]
        public void ReadManifestVersion_ReadsIdentityVersion()
        {
            const string manifest = """
                <?xml version="1.0" encoding="utf-8"?>
                <PackageManifest Version="2.0.0" xmlns="http://schemas.microsoft.com/developer/vsx-schema/2011">
                  <Metadata>
                    <Identity Id="DBADash.SSMSExtension.test" Version="1.2.3" Language="en-US" Publisher="DBA Dash" />
                  </Metadata>
                </PackageManifest>
                """;

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(manifest));
            Assert.AreEqual(new Version(1, 2, 3), SsmsExtensionInstaller.ReadManifestVersion(stream));
        }

        [TestMethod]
        public void IsFromExtension_OnlyForItsTempFiles()
        {
            Assert.IsTrue(SsmsExtensionInstaller.IsFromExtension(Path.Combine(Path.GetTempPath(), "ssms_abc123.xml.gz")));
            Assert.IsTrue(SsmsExtensionInstaller.IsFromExtension(Path.Combine(Path.GetTempPath(), "SSMS_abc123.sqlplan")));
            Assert.IsFalse(SsmsExtensionInstaller.IsFromExtension(Path.Combine(Path.GetTempPath(), "plan.sqlplan")));
            Assert.IsFalse(SsmsExtensionInstaller.IsFromExtension(Path.Combine(Path.GetTempPath(), "sub", "ssms_abc123.xdl")));
            Assert.IsFalse(SsmsExtensionInstaller.IsFromExtension(@"C:\Plans\ssms_abc123.sqlplan"));
        }

        /// <summary>
        /// Against the extension embedded in this build: older or unknown (an extension from before versions were
        /// recorded) is offered the update; the same or newer (e.g. from the SSMS gallery) isn't.
        /// </summary>
        [TestMethod]
        public void IsNewerThan_ComparesWithTheEmbeddedExtension()
        {
            var embedded = SsmsExtensionInstaller.EmbeddedVersion;
            if (embedded == null) Assert.Inconclusive("This build doesn't have the SSMS extension embedded.");

            Assert.IsTrue(SsmsExtensionInstaller.IsNewerThan(null));
            Assert.IsTrue(SsmsExtensionInstaller.IsNewerThan(new Version(1, 0, 10)));
            Assert.IsFalse(SsmsExtensionInstaller.IsNewerThan(embedded));
            Assert.IsFalse(SsmsExtensionInstaller.IsNewerThan(new Version(embedded.Major + 1, 0)));
        }
    }
}
