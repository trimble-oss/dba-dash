using DBADashGUI;
using DBADashGUI.CustomReports;
using DBADashGUI.Grids;
using DBADashGUI.Viewers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Windows.Forms;

namespace DBADash.Viewers.GUI.Test
{
    /// <summary>
    /// JSON inside a grid's cells - nested in a JSON file's records, or held as text - shown as links, and opened as a
    /// grid of its own; and the text of the file a grid was read from.
    /// </summary>
    [TestClass]
    public class NestedJsonTests
    {
        private string _folder;

        [TestInitialize]
        public void Setup()
        {
            _folder = Path.Combine(Path.GetTempPath(), "DBADashNestedJsonTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_folder);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(_folder)) Directory.Delete(_folder, recursive: true);
        }

        [TestMethod]
        [DataRow("{\"a\":1}", true)]
        [DataRow("  [1, 2]\r\n", true)]
        [DataRow("[]", true)]
        [DataRow("{}", true)]
        [DataRow("[INFO] started ]", false)]
        [DataRow("{not json}", false)]
        [DataRow("plain text", false)]
        [DataRow("42", false)]
        [DataRow("\"text\"", false)]
        [DataRow("{", false)]
        [DataRow("", false)]
        [DataRow(null, false)]
        public void IsJson_OnlyObjectsAndArrays(string text, bool expected)
        {
            Assert.AreEqual(expected, NestedJson.IsJson(text));
        }

        [TestMethod]
        public void IsJsonColumn_EveryValueJson_NullsAside()
        {
            var table = new DataTable();
            var json = table.Columns.Add("json", typeof(string));
            var mixed = table.Columns.Add("mixed", typeof(string));
            var empty = table.Columns.Add("empty", typeof(string));
            var brackets = table.Columns.Add("brackets", typeof(string));
            var number = table.Columns.Add("number", typeof(long));
            table.Rows.Add("{\"a\":1}", "{\"a\":1}", DBNull.Value, "[INFO] one ]", 1L);
            table.Rows.Add(DBNull.Value, "text", DBNull.Value, "[INFO] two ]", 2L);
            table.Rows.Add("[1,2]", DBNull.Value, DBNull.Value, DBNull.Value, 3L);

            Assert.IsTrue(NestedJson.IsJsonColumn(table, json));
            Assert.IsFalse(NestedJson.IsJsonColumn(table, mixed), "a value that isn't JSON");
            Assert.IsFalse(NestedJson.IsJsonColumn(table, empty), "nothing but NULL");
            Assert.IsFalse(NestedJson.IsJsonColumn(table, brackets), "looks like it, but doesn't parse");
            Assert.IsFalse(NestedJson.IsJsonColumn(table, number), "not text");
        }

        /// <summary>
        /// A JSON file's nested objects and arrays, and a property whose value is JSON as a string, are links - not the
        /// record's plain values.
        /// </summary>
        [TestMethod]
        public void GridViewerControl_NestedJsonColumnsAreLinks()
        {
            var table = LoadRecords(
                "{\"id\":1,\"name\":\"a\",\"props\":{\"x\":1},\"items\":[{\"n\":1},{\"n\":2}],\"payload\":\"{\\\"k\\\":\\\"v\\\"}\"}\n" +
                "{\"id\":2,\"name\":\"b\",\"props\":{\"x\":2},\"items\":[],\"payload\":\"[1]\"}\n");

            using var form = new Form();
            using var control = new GridViewerControl(table, "records.jsonl") { Dock = DockStyle.Fill };
            form.Controls.Add(control);
            form.CreateControl();

            var grid = FindGrid(control);
            Assert.IsInstanceOfType<DataGridViewLinkColumn>(grid.Columns["props"]);
            Assert.IsInstanceOfType<DataGridViewLinkColumn>(grid.Columns["items"]);
            Assert.IsInstanceOfType<DataGridViewLinkColumn>(grid.Columns["payload"], "JSON held as text");
            Assert.IsNotInstanceOfType<DataGridViewLinkColumn>(grid.Columns["name"]);
            Assert.IsNotInstanceOfType<DataGridViewLinkColumn>(grid.Columns["id"]);
            Assert.AreEqual("props (JSON) - Ctrl+click a value to view it as a grid", grid.Columns["props"]!.ToolTipText);
        }

        /// <summary>View JSON and View as Grid are on a JSON cell's menu, and only there.</summary>
        [TestMethod]
        public void CellMenu_OffersJsonItemsOnJsonCells()
        {
            var table = LoadRecords("{\"name\":\"a\",\"props\":{\"x\":1}}\n");

            using var form = new Form();
            using var control = new GridViewerControl(table, "records.jsonl") { Dock = DockStyle.Fill };
            form.Controls.Add(control);
            form.CreateControl();

            var grid = FindGrid(control);
            var viewJson = grid.CellContextMenu.Items.Cast<ToolStripItem>().Single(i => i.Text == "View JSON");
            var viewAsGrid = grid.CellContextMenu.Items.Cast<ToolStripItem>().Single(i => i.Text == "View as Grid");

            OpenCellMenu(grid, grid.Columns["props"]!.Index, 0);
            Assert.IsTrue(viewJson.Available);
            Assert.IsTrue(viewAsGrid.Available);

            OpenCellMenu(grid, grid.Columns["name"]!.Index, 0);
            Assert.IsFalse(viewJson.Available);
            Assert.IsFalse(viewAsGrid.Available);
        }

        [TestMethod]
        public void ToDataSet_ReadsAnArrayOfObjectsAsRows()
        {
            var dataSet = NestedJson.ToDataSet("[{\"n\":1,\"s\":\"a\"},{\"n\":2,\"s\":\"b\",\"extra\":true}]");

            var table = dataSet.Tables.Cast<DataTable>().Single();
            Assert.AreEqual(2, table.Rows.Count);
            CollectionAssert.AreEqual(new[] { "n", "s", "extra" }, table.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToArray());
            Assert.AreEqual(2L, table.Rows[1]["n"]);
        }

        /// <summary>A cell's JSON opened as a grid goes on a new tab of the window: one table as a grid, several as a data set.</summary>
        [STATestMethod]
        public void OpenGrid_AddsATab()
        {
            ViewerForm.OpenGrid(NestedJson.ToDataSet("[{\"n\":1},{\"n\":2}]"), "log.json - items (row 1)");
            var window = Application.OpenForms.OfType<ViewerForm>().Single();
            try
            {
                var page = FindAll<TabPage>(window).Single();
                Assert.AreEqual("log.json - items (row 1)", page.Text);
                var grid = (GridViewerControl)page.Controls[0];
                Assert.AreEqual(2, FindGrid(grid).Rows.Count);
                Assert.IsNull(grid.SourcePath, "not read from a file, so no Reload, Watch or file text");

                ViewerForm.OpenGrid(NestedJson.ToDataSet("{\"a\":[{\"n\":1}],\"b\":[{\"m\":2}]}"), "several", window);
                Assert.AreEqual(2, FindAll<TabPage>(window).Count());
                Assert.IsInstanceOfType<DataSetViewerControl>(FindAll<TabPage>(window).Last().Controls[0]);
            }
            finally
            {
                window.Close();
            }
        }

        [TestMethod]
        [DataRow("grid.json", false)]
        [DataRow("grid.json.gz", true)]
        [DataRow("log.clef", false)]
        public void ReadText_IsTheFileAsWritten(string file, bool compressed)
        {
            const string text = "{\"@t\":\"2026-11-25T10:00:00Z\",\"@mt\":\"Hello\"}\n";
            var path = Path.Combine(_folder, file);
            using (var stream = File.Create(path))
            using (var output = compressed ? new GZipStream(stream, CompressionLevel.Fastest) : (Stream)stream)
            {
                output.Write(Encoding.UTF8.GetBytes(text));
            }

            Assert.AreEqual(text, GridSerializer.ReadText(path));
        }

        /// <summary>
        /// View File Text reads up to its limit before asking about the rest - counted in text, so a .gz is judged by what
        /// it decompresses to.
        /// </summary>
        [TestMethod]
        [DataRow(10, 20, false)]
        [DataRow(20, 20, false)]
        [DataRow(21, 20, true)]
        [DataRow(200000, 100000, true)]
        public void ReadUpTo_StopsAtTheLimit(int length, int limit, bool more)
        {
            var text = new string('x', length);

            var read = FileTextViewer.ReadUpTo(new StringReader(text), limit);

            Assert.AreEqual(Math.Min(length, limit), read.Text.Length);
            Assert.AreEqual(more, read.More);
        }

        [TestMethod]
        public void ReadUpTo_GzipCountsDecompressedText()
        {
            var path = Path.Combine(_folder, "big.json.gz");
            using (var stream = File.Create(path))
            using (var gzip = new GZipStream(stream, CompressionLevel.Optimal))
            {
                gzip.Write(Encoding.UTF8.GetBytes(new string(' ', 100000)));
            }

            Assert.IsTrue(new FileInfo(path).Length < 1000, "compresses to almost nothing");
            using var reader = GridSerializer.OpenText(path);
            Assert.IsTrue(FileTextViewer.ReadUpTo(reader, 50000).More);
        }

        [TestMethod]
        [DataRow("grid.xml", true)]
        [DataRow("grid.xml.gz", true)]
        [DataRow("grid.json", false)]
        [DataRow("log.jsonl", false)]
        public void IsXmlText_ByExtension(string file, bool expected)
        {
            Assert.AreEqual(expected, GridSerializer.IsXmlText(file));
        }

        /// <summary>View File Text is only for a grid read from a file.</summary>
        [TestMethod]
        public void FileTextButton_EnabledOnceTheGridHasASource()
        {
            var path = Path.Combine(_folder, "records.jsonl");
            File.WriteAllText(path, "{\"n\":1}\n");

            using var control = new GridViewerControl(GridSerializer.LoadDataSet(path).Tables[0], "records.jsonl");
            var button = FindAll<ToolStrip>(control).SelectMany(t => t.Items.OfType<ToolStripButton>())
                .Single(b => b.ToolTipText?.StartsWith("View File Text") == true);
            Assert.IsFalse(button.Enabled);

            control.SetSource(path, GridViewerControl.FileStamp(path)!.Value);
            Assert.IsTrue(button.Enabled);
        }

        [TestMethod]
        public void DataSetViewerControl_FileTextButtonShownWithSourcePath()
        {
            using var control = new DataSetViewerControl(NestedJson.ToDataSet("{\"a\":[{\"n\":1}],\"b\":[{\"m\":2}]}"), "several");
            // The data set's own toolbar, with Save All - not the one of the grid it shows.
            var button = FindAll<ToolStrip>(control).Where(t => t.Items.OfType<ToolStripDropDownButton>().Any(i => i.Text == "Save All"))
                .SelectMany(t => t.Items.OfType<ToolStripButton>())
                .Single(b => b.ToolTipText?.StartsWith("View File Text") == true);
            Assert.IsFalse(button.Available);

            control.SourcePath = Path.Combine(_folder, "several.json");
            Assert.IsTrue(button.Available);
        }

        /// <summary>What a right-click on a cell does before showing its menu: the grid's own handlers read where it was.</summary>
        private static void OpenCellMenu(DBADashDataGridView grid, int columnIndex, int rowIndex)
        {
            typeof(DBADashDataGridView).GetProperty(nameof(DBADashDataGridView.ClickedColumnIndex))!.SetValue(grid, columnIndex);
            typeof(DBADashDataGridView).GetProperty(nameof(DBADashDataGridView.ClickedRowIndex))!.SetValue(grid, rowIndex);
            grid.CellContextMenuOpening(grid, new DataGridViewCellEventArgs(columnIndex, rowIndex));
        }

        private DataTable LoadRecords(string jsonLines)
        {
            var path = Path.Combine(_folder, "records.jsonl");
            File.WriteAllText(path, jsonLines);
            return GridSerializer.LoadDataSet(path).Tables.Cast<DataTable>().Single();
        }

        private static IEnumerable<T> FindAll<T>(Control parent) where T : Control =>
            parent.Controls.Cast<Control>().SelectMany(c => (c is T t ? [t] : Enumerable.Empty<T>()).Concat(FindAll<T>(c)));

        private static DBADashDataGridView FindGrid(Control parent) =>
            parent.Controls.OfType<DBADashDataGridView>().Single();
    }
}
