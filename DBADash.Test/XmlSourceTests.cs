using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DBADashAI.Mcp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DBADash.Test
{
    /// <summary>
    /// The files describe-deadlock-graph and describe-query-plan read by path.  The path can come from text the model
    /// read rather than from the user, so only plan and deadlock files are read, and that is decided before the file is
    /// looked at - the tools say nothing about whether another file exists.
    /// </summary>
    [TestClass]
    public class XmlSourceTests
    {
        private static readonly IReadOnlySet<string> Known = new HashSet<string> { "xml", "path" };

        private string _folder = null!;

        [TestInitialize]
        // Under DBA Dash's own folder, which the server always reads from: %TEMP% isn't inside the profile on every machine.
        public void Setup() => _folder = Directory.CreateDirectory(System.IO.Path.Combine(FileFolders.DbaDashFolder, "test-xmlsource-" + System.Guid.NewGuid().ToString("N"))).FullName;

        [TestCleanup]
        public void Cleanup()
        {
            try { Directory.Delete(_folder, recursive: true); } catch (IOException) { }
        }

        private static RepositoryMcpTool.Arguments Path(string path) =>
            new(new Dictionary<string, JsonElement> { ["path"] = JsonSerializer.SerializeToElement(path) }, Known);

        [TestMethod]
        [DataRow(@"C:\plans\a.sqlplan", true)]
        [DataRow(@"C:\plans\A.SQLPLAN", true)]
        [DataRow(@"C:\plans\a.xml", true)]
        [DataRow(@"plans\relative.sqlplan", true)]
        [DataRow(@"C:\plans\a.xdl", false)]
        [DataRow(@"C:\secrets\web.config", false)]
        [DataRow(@"C:\secrets\notes.txt", false)]
        [DataRow(@"C:\plans\a.sqlplan.txt", false)]
        [DataRow(@"C:\plans\noextension", false)]
        [DataRow(@"C:\plans\a.sqlplan:hidden", false)]
        [DataRow(@"C:\plans\a.txt:hidden.sqlplan", false)]
        [DataRow(@"C:\plans\a.sqlplan::$DATA", false)]
        public void Plans_AreReadFromPlanFilesOnly(string path, bool readable)
        {
            Assert.AreEqual(readable, XmlSource.IsReadable(path, XmlSource.PlanExtensions));
        }

        [TestMethod]
        public void Deadlocks_AreReadFromDeadlockFilesOnly()
        {
            Assert.IsTrue(XmlSource.IsReadable(@"C:\graphs\d.xdl", XmlSource.DeadlockExtensions));
            Assert.IsTrue(XmlSource.IsReadable(@"C:\graphs\d.xml", XmlSource.DeadlockExtensions));
            Assert.IsFalse(XmlSource.IsReadable(@"C:\graphs\d.sqlplan", XmlSource.DeadlockExtensions));
        }

        [TestMethod]
        public async Task AnotherFile_IsRefusedWithoutSayingWhetherItExists()
        {
            var there = System.IO.Path.Combine(_folder, "there.txt");
            File.WriteAllText(there, "password=Hunter2");
            var missing = System.IO.Path.Combine(_folder, "missing.txt");

            var refusedThere = await Assert.ThrowsExactlyAsync<McpToolException>(() => XmlSource.ReadAsync(Path(there), true, XmlSource.PlanExtensions, null, CancellationToken.None));
            var refusedMissing = await Assert.ThrowsExactlyAsync<McpToolException>(() => XmlSource.ReadAsync(Path(missing), true, XmlSource.PlanExtensions, null, CancellationToken.None));

            Assert.AreEqual(refusedMissing.Message, refusedThere.Message);
            Assert.DoesNotContain("Hunter2", refusedThere.Message);
        }

        [TestMethod]
        public async Task APlanFile_IsRead()
        {
            var plan = System.IO.Path.Combine(_folder, "p.sqlplan");
            File.WriteAllText(plan, "<ShowPlanXML/>");

            Assert.AreEqual("<ShowPlanXML/>", await XmlSource.ReadAsync(Path(plan), true, XmlSource.PlanExtensions, null, CancellationToken.None));
        }

        // ---------------------------------------------------------------- folders

        /// <summary>A folder outside the user's profile - the folders a client that gives no roots gets - or null when one can't be made.</summary>
        private static string? Outside()
        {
            try
            {
                return Directory.CreateDirectory(System.IO.Path.Combine(System.IO.Path.GetPathRoot(System.IO.Path.GetTempPath())!, "dbadash-outside-" + System.Guid.NewGuid().ToString("N"))).FullName;
            }
            catch (System.Exception ex) when (ex is IOException or System.UnauthorizedAccessException)
            {
                return null;
            }
        }

        [TestMethod]
        public async Task APlanFile_OutsideTheFolders_IsRefusedWithoutSayingWhetherItExists()
        {
            var outside = Outside();
            if (outside is null) Assert.Inconclusive("Can't make a folder outside the profile here.");
            try
            {
                var there = System.IO.Path.Combine(outside, "there.sqlplan");
                File.WriteAllText(there, "<ShowPlanXML/>");

                var refusedThere = await Assert.ThrowsExactlyAsync<McpToolException>(() => XmlSource.ReadAsync(Path(there), true, XmlSource.PlanExtensions, null, CancellationToken.None));
                var refusedMissing = await Assert.ThrowsExactlyAsync<McpToolException>(() => XmlSource.ReadAsync(Path(System.IO.Path.Combine(outside, "missing.sqlplan")), true, XmlSource.PlanExtensions, null, CancellationToken.None));

                StringAssert.Contains(refusedThere.Message, "isn't in a folder this server reads files from");
                Assert.AreEqual(refusedMissing.Message.Replace("missing", "there"), refusedThere.Message);
            }
            finally
            {
                Directory.Delete(outside, recursive: true);
            }
        }

        [TestMethod]
        public async Task ALinkLeadingOutOfTheFolders_IsRefused()
        {
            var outside = Outside();
            if (outside is null) Assert.Inconclusive("Can't make a folder outside the profile here.");
            var link = System.IO.Path.Combine(_folder, "link");
            try
            {
                File.WriteAllText(System.IO.Path.Combine(outside, "p.sqlplan"), "<ShowPlanXML/>");

                // A junction needs no privilege, unlike a symbolic link.
                using (var mklink = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("cmd.exe", $"/c mklink /J \"{link}\" \"{outside}\"") { CreateNoWindow = true, UseShellExecute = false }))
                {
                    mklink!.WaitForExit();
                }
                if (!Directory.Exists(link)) Assert.Inconclusive("Can't make a junction here.");

                var refused = await Assert.ThrowsExactlyAsync<McpToolException>(() => XmlSource.ReadAsync(Path(System.IO.Path.Combine(link, "p.sqlplan")), true, XmlSource.PlanExtensions, null, CancellationToken.None));
                StringAssert.Contains(refused.Message, "isn't in a folder this server reads files from");
            }
            finally
            {
                if (Directory.Exists(link)) Directory.Delete(link);
                Directory.Delete(outside, recursive: true);
            }
        }

        [TestMethod]
        [DataRow("file:///C:/work/plans", @"C:\work\plans")]
        [DataRow("file:///c%3A/work/plans", @"c:\work\plans")]
        [DataRow("file://server/share/plans", @"\\server\share\plans")]
        public void Roots_AreReadAsLocalFolders(string uri, string folder)
        {
            CollectionAssert.AreEqual(new[] { folder }, FileFolders.Local([uri]).ToArray());
        }

        [TestMethod]
        public void Roots_ThatArentFolders_AreIgnored()
        {
            Assert.IsEmpty(FileFolders.Local(["https://example.com/plans", "not a uri", null]));
        }

        [TestMethod]
        [DataRow(@"C:\work\plans\p.sqlplan", true)]
        [DataRow(@"C:\WORK\Plans\sub\p.sqlplan", true)]
        [DataRow(@"C:\work\plans-old\p.sqlplan", false)]
        [DataRow(@"C:\work\p.sqlplan", false)]
        [DataRow(@"C:\work\plans", false)]
        public void AFile_IsInAFolder_OnlyBelowIt(string path, bool inside)
        {
            Assert.AreEqual(inside, FileFolders.Contains([@"C:\work\plans"], path));
        }

        [TestMethod]
        [DataRow(@"\\?\C:\work\p.sqlplan", @"C:\work\p.sqlplan")]
        [DataRow(@"\\?\UNC\server\share\p.sqlplan", @"\\server\share\p.sqlplan")]
        [DataRow(@"C:\work\p.sqlplan", @"C:\work\p.sqlplan")]
        public void FinalPaths_LoseTheirPrefix(string final, string path)
        {
            Assert.AreEqual(path, FileFolders.WithoutPrefix(final));
        }
    }
}
