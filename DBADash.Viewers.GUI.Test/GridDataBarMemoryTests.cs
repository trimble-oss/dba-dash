using DBADashGUI.CustomReports;
using DBADashGUI.Grids;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace DBADash.Viewers.GUI.Test
{
    /// <summary>Data bars added to a result grid come back for the next result set with the same columns.</summary>
    [TestClass]
    public class GridDataBarMemoryTests
    {
        private string _original;

        [TestInitialize]
        public void Setup()
        {
            _original = GridDataBarMemory.Folder;
            GridDataBarMemory.Folder = Path.Combine(Path.GetTempPath(), "DBADashGridDataBarTests_" + Guid.NewGuid().ToString("N"));
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(GridDataBarMemory.Folder)) Directory.Delete(GridDataBarMemory.Folder, recursive: true);
            GridDataBarMemory.Folder = _original;
        }

        private static string[] CacheFiles() => Directory.Exists(GridDataBarMemory.Folder)
            ? Directory.GetFiles(GridDataBarMemory.Folder)
            : [];

        private static DataTable Table(params (string Name, Type Type, string SqlType)[] columns)
        {
            var table = new DataTable();
            foreach (var (name, type, sqlType) in columns)
            {
                var column = table.Columns.Add(name, type);
                if (sqlType != null) column.ExtendedProperties["SqlType"] = sqlType;
            }
            return table;
        }

        private static DataTable WaitsTable() => Table(("wait_type", typeof(string), "nvarchar(60)"), ("wait_time_ms", typeof(long), "bigint"));

        [TestMethod]
        public void Signature_IsTheColumnsNamesTypesAndOrder()
        {
            var signature = GridDataBarMemory.Signature(WaitsTable());
            Assert.AreEqual(signature, GridDataBarMemory.Signature(WaitsTable()), "Same columns");

            Assert.AreNotEqual(signature, GridDataBarMemory.Signature(
                Table(("wait_time_ms", typeof(long), "bigint"), ("wait_type", typeof(string), "nvarchar(60)"))), "Order");
            Assert.AreNotEqual(signature, GridDataBarMemory.Signature(
                Table(("wait_type", typeof(string), "nvarchar(60)"), ("wait_time", typeof(long), "bigint"))), "Name");
            Assert.AreNotEqual(signature, GridDataBarMemory.Signature(
                Table(("wait_type", typeof(string), "nvarchar(max)"), ("wait_time_ms", typeof(long), "bigint"))), "SQL type");
            Assert.AreNotEqual(signature, GridDataBarMemory.Signature(
                Table(("wait_type", typeof(string), null), ("wait_time_ms", typeof(int), null))), ".NET type, where there's no SQL type");
        }

        [TestMethod]
        public void RememberedBars_AreRecalledForTheSameShape()
        {
            GridDataBarMemory.Remember(WaitsTable(), new Dictionary<string, DataBarSettings>
            {
                ["wait_time_ms"] = new() { Style = DataBarStyle.Fill, ColorMode = DataBarColorMode.TrafficLight }
            });

            var recalled = GridDataBarMemory.Recall(WaitsTable());
            Assert.AreEqual(1, recalled.Count);
            Assert.AreEqual(DataBarStyle.Fill, recalled["wait_time_ms"].Style);
            Assert.AreEqual(DataBarColorMode.TrafficLight, recalled["wait_time_ms"].ColorMode);

            Assert.AreEqual(0, GridDataBarMemory.Recall(Table(("other", typeof(int), "int"))).Count, "Another shape has none");
        }

        [TestMethod]
        public void RemovingTheLastBar_ForgetsTheShape()
        {
            GridDataBarMemory.Remember(WaitsTable(), new Dictionary<string, DataBarSettings> { ["wait_time_ms"] = new() });
            GridDataBarMemory.Remember(WaitsTable(), new Dictionary<string, DataBarSettings>());

            Assert.AreEqual(0, GridDataBarMemory.Recall(WaitsTable()).Count);
            Assert.AreEqual(0, CacheFiles().Length, "Its file is deleted");
        }

        [TestMethod]
        public void EachShape_IsAFileOfItsOwn()
        {
            GridDataBarMemory.Remember(WaitsTable(), new Dictionary<string, DataBarSettings> { ["wait_time_ms"] = new() });
            GridDataBarMemory.Remember(Table(("n", typeof(int), "int")), new Dictionary<string, DataBarSettings> { ["n"] = new() });

            CollectionAssert.AreEquivalent(
                new[] { GridDataBarMemory.Signature(WaitsTable()) + ".json", GridDataBarMemory.Signature(Table(("n", typeof(int), "int"))) + ".json" },
                CacheFiles().Select(Path.GetFileName).ToArray());
        }

        /// <summary>
        /// Past the limit the least recently used shape goes - and opening a grid counts as using it, not only
        /// changing its bars.
        /// </summary>
        [TestMethod]
        public void OnlyTheMostRecentlyUsedShapesAreKept()
        {
            static DataTable Numbered(int i) => Table(("n" + i, typeof(int), "int"));

            // Written in a loop, many files can share a modified time, so they're given ones an hour apart: n0 oldest.
            var start = DateTime.UtcNow.AddDays(-1);
            for (var i = 0; i < GridDataBarMemory.MaxRemembered; i++)
            {
                GridDataBarMemory.Remember(Numbered(i), new Dictionary<string, DataBarSettings> { ["n" + i] = new() });
                File.SetLastWriteTimeUtc(Path.Combine(GridDataBarMemory.Folder, GridDataBarMemory.Signature(Numbered(i)) + ".json"), start.AddHours(i));
            }

            // Opened again, n0 is the most recently used, so n1 is now the oldest.
            Assert.AreEqual(1, GridDataBarMemory.Recall(Numbered(0)).Count);

            GridDataBarMemory.Remember(Numbered(GridDataBarMemory.MaxRemembered), new Dictionary<string, DataBarSettings> { ["new"] = new() });

            Assert.AreEqual(GridDataBarMemory.MaxRemembered, CacheFiles().Length);
            Assert.AreEqual(0, GridDataBarMemory.Recall(Numbered(1)).Count, "The least recently used is dropped");
            Assert.AreEqual(1, GridDataBarMemory.Recall(Numbered(0)).Count);
        }

        [TestMethod]
        public void AFileThatCantBeRead_GivesNoBars()
        {
            Directory.CreateDirectory(GridDataBarMemory.Folder);
            File.WriteAllText(Path.Combine(GridDataBarMemory.Folder, GridDataBarMemory.Signature(WaitsTable()) + ".json"), "not json");
            Assert.AreEqual(0, GridDataBarMemory.Recall(WaitsTable()).Count);
        }

        /// <summary>A temporary file left by a save that crashed is cleared up - once it's old enough not to be one being written now.</summary>
        [TestMethod]
        public void AbandonedTemporaryFiles_AreCleared()
        {
            Directory.CreateDirectory(GridDataBarMemory.Folder);
            var abandoned = Path.Combine(GridDataBarMemory.Folder, "abandoned.tmp");
            var current = Path.Combine(GridDataBarMemory.Folder, "current.tmp");
            File.WriteAllText(abandoned, "{");
            File.WriteAllText(current, "{");
            File.SetLastWriteTimeUtc(abandoned, DateTime.UtcNow.AddDays(-1));

            GridDataBarMemory.Remember(WaitsTable(), new Dictionary<string, DataBarSettings> { ["wait_time_ms"] = new() });

            Assert.IsFalse(File.Exists(abandoned));
            Assert.IsTrue(File.Exists(current));
        }

        /// <summary>A result grid opened on a result set of a remembered shape has its bars.</summary>
        [TestMethod]
        public void ResultGrid_OpensWithTheRememberedBars()
        {
            GridDataBarMemory.Remember(WaitsTable(), new Dictionary<string, DataBarSettings> { ["wait_time_ms"] = new() { Style = DataBarStyle.Fill } });

            var table = WaitsTable();
            table.Rows.Add("CXPACKET", 100L);
            using var form = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Left = -3000 };
            using var viewer = new GridViewerControl(table, "Waits") { Dock = DockStyle.Fill };
            form.Controls.Add(viewer);
            form.Show();
            Application.DoEvents();

            var grid = viewer.Controls.OfType<DBADashDataGridView>().Single();
            Assert.AreEqual(DataBarStyle.Fill, grid.DataBars["wait_time_ms"].Style);
        }
    }
}
