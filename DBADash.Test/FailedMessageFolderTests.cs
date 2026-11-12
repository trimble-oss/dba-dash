using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DBADash.Test
{
    /// <summary>
    /// Tests for the cap on the failed message folder.  A repeating failure writes a file per collection, so
    /// without the cap the folder grew to 300K files / 100GB.
    /// </summary>
    [TestClass]
    public class FailedMessageFolderTests
    {
        private string _folder = string.Empty;
        private int _originalMaxFiles;
        private DateTime _baseTime;

        [TestInitialize]
        public void TestInitialize()
        {
            _folder = Path.Combine(Path.GetTempPath(), "DBADashFailedTest_" + Path.GetRandomFileName());
            Directory.CreateDirectory(_folder);
            _originalMaxFiles = FailedMessageFolder.MaxFiles;
            FailedMessageFolder.MaxFiles = 3;
            _baseTime = DateTime.UtcNow.AddDays(-1);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            FailedMessageFolder.MaxFiles = _originalMaxFiles;
            try
            {
                Directory.Delete(_folder, true);
            }
            catch (IOException)
            {
                // Leave it to the temp folder cleanup
            }
        }

        private static DataSet GetTestDataSet()
        {
            var ds = new DataSet("Test");
            var dt = new DataTable("TestTable");
            dt.Columns.Add("Value", typeof(string));
            dt.Rows.Add("Row");
            ds.Tables.Add(dt);
            return ds;
        }

        /// <summary>Creates a file with a write time that orders it: higher index = newer.</summary>
        private string CreateFile(string name, int index)
        {
            var path = Path.Combine(_folder, name);
            File.WriteAllText(path, "x");
            File.SetLastWriteTimeUtc(path, _baseTime.AddMinutes(index));
            return path;
        }

        private string[] FileNames() => Directory.GetFiles(_folder).Select(Path.GetFileName).OrderBy(n => n).ToArray()!;

        [TestMethod]
        public void MakeRoom_FolderUnderCap_LeavesFilesUntouched()
        {
            CreateFile("dbadash_1.xml", 1);
            CreateFile("dbadash_2.xml", 2);

            FailedMessageFolder.MakeRoom(_folder);

            CollectionAssert.AreEqual(new[] { "dbadash_1.xml", "dbadash_2.xml" }, FileNames());
        }

        [TestMethod]
        public void MakeRoom_FolderAtCap_DeletesOldestToLeaveRoomForOneMore()
        {
            CreateFile("dbadash_1.xml", 1);
            CreateFile("dbadash_2.xml", 2);
            CreateFile("dbadash_3.xml", 3);

            FailedMessageFolder.MakeRoom(_folder);

            CollectionAssert.AreEqual(new[] { "dbadash_2.xml", "dbadash_3.xml" }, FileNames());
        }

        [TestMethod]
        public void MakeRoom_OldestFileLocked_DeletesNextOldestInstead()
        {
            if (!OperatingSystem.IsWindows())
            {
                Assert.Inconclusive("Relies on an open handle blocking deletion, which only happens on Windows");
            }
            var locked = CreateFile("dbadash_1.xml", 1);
            CreateFile("dbadash_2.xml", 2);
            CreateFile("dbadash_3.xml", 3);

            using (new FileStream(locked, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                FailedMessageFolder.MakeRoom(_folder);
            }

            CollectionAssert.AreEqual(new[] { "dbadash_1.xml", "dbadash_3.xml" }, FileNames());
        }

        [TestMethod]
        public async Task WriteAsync_OverCap_KeepsNewestFilesAndNeverExceedsCap()
        {
            // Simulates a repeating failure: every write adds one file.  Write times are set explicitly so the
            // eviction order doesn't depend on the clock resolution.
            for (var i = 1; i <= 10; i++)
            {
                var name = DestinationHandling.FileNamePrefix + "Run" + i.ToString("D3") + DestinationHandling.FileExtension;
                await FailedMessageFolder.WriteAsync(GetTestDataSet(), _folder, name, new CollectionConfig());
                File.SetLastWriteTimeUtc(Path.Combine(_folder, name), _baseTime.AddMinutes(i));
                Assert.IsTrue(Directory.GetFiles(_folder).Length <= FailedMessageFolder.MaxFiles);
            }

            CollectionAssert.AreEqual(
                new[] { "DBADash_Run008.xml", "DBADash_Run009.xml", "DBADash_Run010.xml" },
                FileNames());
        }

        [TestMethod]
        public void MakeRoom_IgnoresFilesItDoesNotWrite()
        {
            // Anything else in the folder - including the user's own files - is never deleted, however old.
            CreateFile("notes.txt", 0);
            CreateFile("dbadash_1.xml", 1);
            CreateFile("dbadash_2.xml", 2);
            CreateFile("dbadash_3.xml", 3);

            FailedMessageFolder.MakeRoom(_folder);

            CollectionAssert.AreEqual(new[] { "dbadash_2.xml", "dbadash_3.xml", "notes.txt" }, FileNames());
        }

        [TestMethod]
        public void MakeRoom_CountsInterruptedTempFilesAgainstCap()
        {
            CreateFile("dbadash_1.xml.tmp", 1);
            CreateFile("dbadash_2.xml", 2);
            CreateFile("dbadash_3.xml", 3);

            FailedMessageFolder.MakeRoom(_folder);

            CollectionAssert.AreEqual(new[] { "dbadash_2.xml", "dbadash_3.xml" }, FileNames());
        }

        [TestMethod]
        public void Copy_OverCap_CopiesFileAndKeepsCap()
        {
            // The source is a file in an import folder, kept out of the failed message folder itself.
            var sourceDir = Directory.CreateDirectory(Path.Combine(_folder, "import")).FullName;
            var source = Path.Combine(sourceDir, "dbadash_import.xml");
            File.WriteAllText(source, "x");
            CreateFile("dbadash_1.xml", 1);
            CreateFile("dbadash_2.xml", 2);
            CreateFile("dbadash_3.xml", 3);

            FailedMessageFolder.Copy(source, _folder, "dbadash_source.xml");

            Assert.IsTrue(File.Exists(source), "The source file must be left in place for the caller to remove");
            Assert.IsTrue(File.Exists(Path.Combine(_folder, "dbadash_source.xml")));
            Assert.AreEqual(3, Directory.GetFiles(_folder).Length);
            Assert.IsFalse(File.Exists(Path.Combine(_folder, "dbadash_1.xml")));
        }

        [TestMethod]
        public void MakeRoom_MissingFolder_DoesNotThrow()
        {
            FailedMessageFolder.MakeRoom(Path.Combine(_folder, "does_not_exist"));
        }
    }
}
