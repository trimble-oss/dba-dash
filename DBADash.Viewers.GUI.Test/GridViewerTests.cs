using DBADashGUI.CustomReports;
using DBADashGUI.Grids;
using DBADashGUI.Viewers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Windows.Forms;

namespace DBADash.Viewers.GUI.Test
{
    /// <summary>
    /// Result sets in the viewer: which files open as a grid, and the file the SSMS extension hands over for a results
    /// grid - DataTable XML with its schema, gzipped, with the tab's name in the table's extended properties.
    /// </summary>
    [TestClass]
    public class GridViewerTests
    {
        private string _folder;

        [TestInitialize]
        public void Setup()
        {
            _folder = Path.Combine(Path.GetTempPath(), "DBADashGridViewerTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_folder);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(_folder)) Directory.Delete(_folder, recursive: true);
        }

        [TestMethod]
        [DataRow("plan.sqlplan", true)]
        [DataRow("graph.xdl", true)]
        [DataRow("either.xml", true)]
        [DataRow("grid.json", true)]
        [DataRow("grid.json.gz", true)]
        [DataRow("ssms_abc123.xml.gz", true)]
        [DataRow("notes.txt", false)]
        [DataRow("archive.zip", false)]
        [DataRow("archive.tar.gz", false)]
        [DataRow("grid.xml.gz", true)]
        public void CanOpen_ByExtension(string file, bool expected)
        {
            Assert.AreEqual(expected, ViewerApp.CanOpen(file));
        }

        /// <summary>
        /// The SSMS extension's hand-off, written the way it writes it (VisualizerLauncher.SaveGridToTemp in the
        /// net472 extension), reads back with its types, NULLs, title and SQL type names intact.
        /// </summary>
        [TestMethod]
        public void SsmsExtensionGridFile_RoundTrips()
        {
            var path = Path.Combine(_folder, "ssms_test.xml.gz");
            var written = SsmsStyleTable();
            using (var stream = File.Create(path))
            using (var gzip = new GZipStream(stream, CompressionLevel.Fastest))
            {
                written.WriteXml(gzip, XmlWriteMode.WriteSchema);
            }

            var read = GridSerializer.LoadDataTable(path);

            Assert.AreEqual("SQLQuery1.sql - Result 1", read.ExtendedProperties["Title"]);
            Assert.AreEqual(written.Columns.Count, read.Columns.Count);
            for (var i = 0; i < written.Columns.Count; i++)
            {
                Assert.AreEqual(written.Columns[i].ColumnName, read.Columns[i].ColumnName);
                Assert.AreEqual(written.Columns[i].DataType, read.Columns[i].DataType);
            }

            Assert.AreEqual("int", read.Columns["id"]!.ExtendedProperties["SqlType"]);
            Assert.AreEqual(12.5m, read.Rows[0]["amount"]);
            Assert.AreEqual(new DateTime(2026, 9, 28, 10, 30, 0), read.Rows[0]["created"]);
            Assert.AreEqual(TimeSpan.FromMinutes(3), read.Rows[0]["duration"]);
            Assert.IsTrue(read.Rows[1].IsNull("amount"));
        }

        [TestMethod]
        public void GridViewerControl_ShowsTitleAndSqlTypes()
        {
            using var form = new Form();
            using var control = new GridViewerControl(SsmsStyleTable(), "SQLQuery1.sql - Result 1") { Dock = DockStyle.Fill };
            form.Controls.Add(control);
            form.CreateControl();

            Assert.AreEqual("SQLQuery1.sql - Result 1", control.Title);
            StringAssert.Contains(control.TabToolTip, "2 rows, 5 columns");

            var grid = FindGrid(control);
            Assert.AreEqual(2, grid.Rows.Count);
            Assert.AreEqual("id (int)", grid.Columns["id"]!.ToolTipText);
        }

        [TestMethod]
        public void GridViewerControl_XmlAndJsonColumnsAreLinks()
        {
            var table = SsmsStyleTable();
            AddColumn(table, "query_plan", typeof(string), "xml");
            AddColumn(table, "doc", typeof(string), "json");

            using var form = new Form();
            using var control = new GridViewerControl(table, "Results") { Dock = DockStyle.Fill };
            form.Controls.Add(control);
            form.CreateControl();

            var grid = FindGrid(control);
            Assert.IsInstanceOfType<DataGridViewLinkColumn>(grid.Columns["query_plan"]);
            Assert.IsInstanceOfType<DataGridViewLinkColumn>(grid.Columns["doc"]);
            Assert.IsNotInstanceOfType<DataGridViewLinkColumn>(grid.Columns["name"]);
            Assert.AreEqual(5, grid.Columns["query_plan"]!.Index, "the link column keeps the column's place");
            Assert.AreEqual("query_plan (xml)", grid.Columns["query_plan"]!.ToolTipText);
        }

        /// <summary>
        /// Sized once shown, not while binding: then the grid has no width yet, and the width cap
        /// AutoResizeColumnsWithMaxColumnWidth works out from it left every column at its minimum.
        /// </summary>
        [TestMethod]
        public void GridViewerControl_SizesColumnsOnceShown()
        {
            var table = SsmsStyleTable();
            table.Rows[0]["name"] = new string('x', 60);

            using var form = new Form { Width = 1200, Height = 600, ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Left = -3000 };
            using var control = new GridViewerControl(table, "Results") { Dock = DockStyle.Fill };
            form.Controls.Add(control);
            form.Show();
            Application.DoEvents();

            var grid = FindGrid(control);
            foreach (DataGridViewColumn column in grid.Columns)
            {
                Assert.IsTrue(column.Width > column.MinimumWidth + 10, $"{column.Name} is {column.Width}px");
            }
            Assert.IsTrue(grid.Columns["name"]!.Width > grid.Columns["id"]!.Width, "sized to the content");
        }

        /// <summary>
        /// A DataSet as the DBA Dash service writes one (DestinationHandling.WriteFolderAsync) - what's left in its
        /// Failed folder - reads back with every table, and the SSMS extension's single table still reads as one.
        /// </summary>
        [TestMethod]
        public void LoadDataSet_ReadsServiceDataSetsAndSingleTables()
        {
            var path = Path.Combine(_folder, "DBADash_20260928_1030_00_SERVER_abc.xml");
            ServiceStyleDataSet().WriteXml(path, XmlWriteMode.WriteSchema);

            var read = GridSerializer.LoadDataSet(path);
            CollectionAssert.AreEqual(new[] { "DBADash", "Waits", "EmptyTable" },
                read.Tables.Cast<DataTable>().Select(t => t.TableName).ToArray());
            Assert.AreEqual(typeof(byte[]), read.Tables["Waits"]!.Columns["plan_handle"]!.DataType);

            var gridPath = Path.Combine(_folder, "ssms_test.xml.gz");
            using (var stream = File.Create(gridPath))
            using (var gzip = new GZipStream(stream, CompressionLevel.Fastest))
            {
                SsmsStyleTable().WriteXml(gzip, XmlWriteMode.WriteSchema);
            }

            var single = GridSerializer.LoadDataSet(gridPath);
            Assert.AreEqual(1, single.Tables.Count);
            Assert.AreEqual("SQLQuery1.sql - Result 1", single.Tables[0].ExtendedProperties["Title"]);
            Assert.AreEqual(typeof(TimeSpan), single.Tables[0].Columns["duration"]!.DataType);
        }

        [TestMethod]
        public void DataSetViewerControl_ListsAndFiltersTables()
        {
            using var form = new Form { Width = 1200, Height = 600, ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Left = -3000 };
            using var control = new DataSetViewerControl(ServiceStyleDataSet(), "failed.xml") { Dock = DockStyle.Fill };
            form.Controls.Add(control);
            form.Show();
            Application.DoEvents();

            var list = FindAll<DataGridView>(control).Single(g => g is not DBADashDataGridView);
            CollectionAssert.AreEqual(new[] { "DBADash", "Waits", "EmptyTable" }, ListedTables(list));
            Assert.AreEqual("DBADash", SelectedTableName(list), "the first table is shown to start with");
            Assert.IsFalse(list.EnableHeadersVisualStyles, "the header is themed like DBA Dash's grids");

            // Binary columns (plan handles and the like) show without a grid data error.
            list.Rows[1].Selected = true;
            Application.DoEvents();
            var waits = FindAll<DBADashDataGridView>(control).Single(g => g.Visible && g.Columns.Contains("plan_handle"));
            Assert.AreEqual("0x0102", waits.Rows[0].Cells["plan_handle"].FormattedValue);
            Assert.AreEqual(string.Empty, waits.Rows[0].Cells["plan_handle"].ErrorText);

            FindAll<CheckBox>(control).Single().Checked = true;
            CollectionAssert.AreEqual(new[] { "DBADash", "Waits" }, ListedTables(list));

            FindAll<TextBox>(control).Single().Text = "wai";
            CollectionAssert.AreEqual(new[] { "Waits" }, ListedTables(list));
            Assert.AreEqual("Waits", SelectedTableName(list), "the table picked stays picked");
        }

        private static string[] ListedTables(DataGridView list) =>
            list.Rows.Cast<DataGridViewRow>().Select(r => (string)r.Cells[0].Value).ToArray();

        private static string SelectedTableName(DataGridView list) => (string)list.SelectedRows[0].Cells[0].Value;

        private static IEnumerable<T> FindAll<T>(Control parent) where T : Control =>
            parent.Controls.Cast<Control>().SelectMany(c => (c is T t ? [t] : Enumerable.Empty<T>()).Concat(FindAll<T>(c)));

        private static DataSet ServiceStyleDataSet()
        {
            var ds = new DataSet("DBADash");
            var meta = ds.Tables.Add("DBADash");
            meta.Columns.Add("Instance", typeof(string));
            meta.Rows.Add("SERVER");

            var waits = ds.Tables.Add("Waits");
            waits.Columns.Add("wait_type", typeof(string));
            waits.Columns.Add("wait_time_ms", typeof(long));
            waits.Columns.Add("plan_handle", typeof(byte[]));
            waits.Rows.Add("PAGEIOLATCH_SH", 1234L, new byte[] { 1, 2 });

            ds.Tables.Add("EmptyTable").Columns.Add("x", typeof(int));
            return ds;
        }

        [TestMethod]
        public void GridViewerControl_HidesRowHeaders()
        {
            using var control = new GridViewerControl(SsmsStyleTable(), "Results");
            Assert.AreEqual(DBADashDataGridView.RowHeaderModes.Hidden, FindGrid(control).RowHeaderMode);
        }

        /// <summary>
        /// A grid file opens on a tab that says it's loading straight away, and the grid replaces it once the file has
        /// been read in the background - named from the table's Title, not the file.
        /// </summary>
        // STA: the viewer window registers as a drop target, which needs OLE - on an MTA thread creating its handle
        // throws, and WinForms puts up its unhandled exception dialog.
        [STATestMethod]
        public void OpenGridFile_ShowsLoadingThenTheGrid()
        {
            var path = Path.Combine(_folder, "ssms_test.xml.gz");
            using (var stream = File.Create(path))
            using (var gzip = new GZipStream(stream, CompressionLevel.Fastest))
            {
                SsmsStyleTable().WriteXml(gzip, XmlWriteMode.WriteSchema);
            }

            ViewerForm.OpenGridFile(path);
            var window = Application.OpenForms.OfType<ViewerForm>().Single();
            try
            {
                var page = FindAll<TabPage>(window).Single();
                Assert.AreEqual("ssms_test.xml.gz", page.Text, "named after the file while it loads");
                Assert.IsInstanceOfType<Label>(page.Controls[0]);

                var deadline = DateTime.UtcNow.AddSeconds(10);
                while (page.Controls[0] is not GridViewerControl && DateTime.UtcNow < deadline)
                {
                    Application.DoEvents();
                    Thread.Sleep(10);
                }

                Assert.IsInstanceOfType<GridViewerControl>(page.Controls[0]);
                Assert.AreEqual("SQLQuery1.sql - Result 1", page.Text);
                Assert.AreEqual(2, FindGrid(page.Controls[0]).Rows.Count);
            }
            finally
            {
                window.Close();
            }
        }

        [TestMethod]
        public void GridViewerControl_WithoutTitle_IsCalledResults()
        {
            using var control = new GridViewerControl(SsmsStyleTable(), null);
            Assert.AreEqual("Results", control.Title);
        }

        private static DBADashDataGridView FindGrid(Control parent) =>
            parent.Controls.OfType<DBADashDataGridView>().Single();

        private static DataTable SsmsStyleTable()
        {
            var table = new DataTable("Results");
            table.ExtendedProperties["Title"] = "SQLQuery1.sql - Result 1";
            AddColumn(table, "id", typeof(int), "int");
            AddColumn(table, "amount", typeof(decimal), "decimal(10,2)");
            AddColumn(table, "created", typeof(DateTime), "datetime2(7)").DateTimeMode = DataSetDateTime.Unspecified;
            AddColumn(table, "duration", typeof(TimeSpan), "time(7)");
            AddColumn(table, "name", typeof(string), "nvarchar(50)");

            table.Rows.Add(1, 12.5m, new DateTime(2026, 9, 28, 10, 30, 0), TimeSpan.FromMinutes(3), "first");
            table.Rows.Add(2, DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value);
            return table;
        }

        private static DataColumn AddColumn(DataTable table, string name, Type type, string sqlType)
        {
            var column = table.Columns.Add(name, type);
            column.ExtendedProperties["SqlType"] = sqlType;
            return column;
        }
    }
}
