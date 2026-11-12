using System.Data;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DBADash.Test
{
    /// <summary>
    /// Tests for the gzip-compressed (.xml.gz) format.  Files are written compressed when
    /// <see cref="CollectionConfig.CompressionLevel"/> is anything other than NoCompression, and the importer reads both formats.
    /// </summary>
    [TestClass]
    public class CompressedFileTests
    {
        private string _folder = string.Empty;

        [TestInitialize]
        public void TestInitialize()
        {
            _folder = Path.Combine(Path.GetTempPath(), "DBADashCompressedTest_" + Path.GetRandomFileName());
            Directory.CreateDirectory(_folder);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            try
            {
                Directory.Delete(_folder, true);
            }
            catch (IOException)
            {
                // Leave it to the temp folder cleanup
            }
        }

        private static DataSet GetTestDataSet(int rowCount)
        {
            var ds = new DataSet("Test");
            var dt = new DataTable("TestTable");
            dt.Columns.Add("Value", typeof(string));
            for (var i = 0; i < rowCount; i++)
            {
                dt.Rows.Add("Row " + i);
            }
            ds.Tables.Add(dt);
            return ds;
        }

        private static CollectionConfig Config(CompressionLevel level) => new() { CompressionLevel = level };

        private static readonly string PlainName = DestinationHandling.FileNamePrefix + "Test" + DestinationHandling.FileExtension;

        [TestMethod]
        public void CompressionLevel_DefaultsToNoCompression()
        {
            Assert.AreEqual(CompressionLevel.NoCompression, new CollectionConfig().CompressionLevel);
        }

        [TestMethod]
        [DataRow(CompressionLevel.Fastest)]
        [DataRow(CompressionLevel.Optimal)]
        [DataRow(CompressionLevel.SmallestSize)]
        public async Task WriteFolderAsync_CompressedLevel_WritesReadableGzipFileWithCompressedExtension(CompressionLevel level)
        {
            var path = await DestinationHandling.WriteFolderAsync(GetTestDataSet(200), _folder, PlainName, Config(level));

            Assert.AreEqual(Path.Combine(_folder, "DBADash_Test.xml.gz"), path);
            CollectionAssert.AreEqual(new[] { "DBADash_Test.xml.gz" }, Directory.GetFiles(_folder).Select(Path.GetFileName).ToArray());

            // gzip magic number, so it's really compressed and not just renamed
            var bytes = File.ReadAllBytes(path);
            Assert.AreEqual(0x1f, bytes[0]);
            Assert.AreEqual(0x8b, bytes[1]);
            Assert.AreEqual(200, DataSetSerialization.DeserializeFromFile(path).Tables["TestTable"]!.Rows.Count);
        }

        [TestMethod]
        public async Task WriteFolderAsync_NoCompression_WritesPlainXmlEvenWhenNamedCompressed()
        {
            // NoCompression is the off switch: plain XML, not a gzip container with nothing compressed in it.
            // The caller's name decides nothing - the setting does.
            var path = await DestinationHandling.WriteFolderAsync(GetTestDataSet(3), _folder, "DBADash_Test.xml.gz", Config(CompressionLevel.NoCompression));

            Assert.AreEqual(Path.Combine(_folder, "DBADash_Test.xml"), path);
            Assert.IsTrue(File.Exists(path));
            Assert.AreEqual((byte)'<', File.ReadAllBytes(path)[0]);
        }

        [TestMethod]
        public async Task CompressedAndPlainFiles_ReadBackToTheSameData()
        {
            var plainPath = await DestinationHandling.WriteFolderAsync(GetTestDataSet(50), _folder, PlainName, Config(CompressionLevel.NoCompression));
            var gzPath = await DestinationHandling.WriteFolderAsync(GetTestDataSet(50), _folder, PlainName, Config(CompressionLevel.Optimal));

            var plain = DataSetSerialization.DeserializeFromFile(plainPath);
            var compressed = DataSetSerialization.DeserializeFromFile(gzPath);

            Assert.AreEqual(50, plain.Tables["TestTable"]!.Rows.Count);
            Assert.AreEqual(50, compressed.Tables["TestTable"]!.Rows.Count);
            Assert.AreEqual("Row 49", compressed.Tables["TestTable"]!.Rows[49]["Value"]);
        }

        [TestMethod]
        public async Task WriteFolderAsync_CompressedFile_IsSmallerThanPlain()
        {
            // Both writes use the same name, so each length is read before the next write replaces the file.
            var plainPath = await DestinationHandling.WriteFolderAsync(GetTestDataSet(2000), _folder, PlainName, Config(CompressionLevel.NoCompression));
            var plainLength = new FileInfo(plainPath).Length;
            var gzPath = await DestinationHandling.WriteFolderAsync(GetTestDataSet(2000), _folder, PlainName, Config(CompressionLevel.Optimal));
            var gzLength = new FileInfo(gzPath).Length;

            Assert.IsTrue(gzLength < plainLength);
        }

        [TestMethod]
        public async Task WriteFolderAsync_Compressed_DoesNotLeaveTempFileBehind()
        {
            await DestinationHandling.WriteFolderAsync(GetTestDataSet(3), _folder, PlainName, Config(CompressionLevel.Optimal));

            Assert.IsFalse(Directory.GetFiles(_folder).Any(f => f.EndsWith(DestinationHandling.TempFileExtension)));
        }

        [TestMethod]
        public async Task WriteFolderAsync_Compressed_OverwritingExistingFileReplacesIt()
        {
            await DestinationHandling.WriteFolderAsync(GetTestDataSet(100), _folder, PlainName, Config(CompressionLevel.Optimal));
            var path = await DestinationHandling.WriteFolderAsync(GetTestDataSet(1), _folder, PlainName, Config(CompressionLevel.Optimal));

            Assert.AreEqual(1, DataSetSerialization.DeserializeFromFile(path).Tables["TestTable"]!.Rows.Count);
        }

        [TestMethod]
        public void ToFileFormat_ConvertsBetweenFormats()
        {
            Assert.AreEqual("DBADash_x.xml.gz", DestinationHandling.ToFileFormat("DBADash_x.xml", true));
            Assert.AreEqual("DBADash_x.xml", DestinationHandling.ToFileFormat("DBADash_x.xml", false));
            Assert.AreEqual("DBADash_x.xml.gz", DestinationHandling.ToFileFormat("DBADash_x.xml.gz", true));
            Assert.AreEqual("DBADash_x.xml", DestinationHandling.ToFileFormat("DBADash_x.xml.gz", false));
        }

        /// <summary>
        /// SQS replies carry the exact S3 path of their .message payload, and the reader expects plain XML, so
        /// those payloads must keep their name and stay uncompressed whatever the compression setting.
        /// </summary>
        [TestMethod]
        public void MessagePayloads_AreNeverCompressed()
        {
            var name = "6f1c2a3e-0000-0000-0000-000000000000.message";

            Assert.AreEqual(name, DestinationHandling.ToFileFormat(name, true));
            Assert.AreEqual(CompressionLevel.NoCompression, DestinationHandling.CompressionLevelFor(name, CompressionLevel.Optimal));
            Assert.AreEqual(CompressionLevel.Optimal, DestinationHandling.CompressionLevelFor(PlainName, CompressionLevel.Optimal));
        }

        [TestMethod]
        [DataRow("DBADash_x.xml", true)]
        [DataRow("DBADash_x.xml.gz", true)]
        [DataRow("DBADash_x.xml.tmp", false)]
        [DataRow("DBADash_x.xml.gz.tmp", false)]
        [DataRow("DBADash_x.gz", false)]
        [DataRow("DBADash_x.json", false)]
        public void IsCompletedFileName_OnlyCompletedXmlFormats(string name, bool expected)
        {
            // The importer lists both formats but never a temp file, which is still being written.
            Assert.AreEqual(expected, DestinationHandling.IsCompletedFileName(name));
        }

        [TestMethod]
        public void ReadFromStream_CompressedAndPlain_ReadTheSameData()
        {
            var ds = GetTestDataSet(5);
            using var plain = new MemoryStream();
            DestinationHandling.WriteDataSet(ds, plain, CompressionLevel.NoCompression);
            using var gz = new MemoryStream();
            DestinationHandling.WriteDataSet(ds, gz, CompressionLevel.Optimal);

            plain.Position = 0;
            gz.Position = 0;
            Assert.AreEqual(5, DataSetSerialization.ReadFromStream(plain, compressed: false).Tables["TestTable"]!.Rows.Count);
            Assert.AreEqual(5, DataSetSerialization.ReadFromStream(gz, compressed: true).Tables["TestTable"]!.Rows.Count);
        }

        [TestMethod]
        public void FailedMessageFolder_ManagesCompressedFiles()
        {
            // The failed message folder's cap and cleanup must count and remove .xml.gz files too.  Path.GetExtension
            // would report ".gz" for them, so this is matched on the whole ending.
            File.WriteAllText(Path.Combine(_folder, "dbadash_1.xml.gz"), "x");
            var info = new FileInfo(Path.Combine(_folder, "dbadash_1.xml.gz"));

            Assert.IsTrue(FailedMessageFolder.IsManagedFile(info));
        }
    }
}
