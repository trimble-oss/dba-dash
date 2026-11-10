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

        /// <summary>
        /// Results SSMS saved as XML (Save Results As... XML): names decoded, xsi:nil read as NULL, and each column's
        /// type inferred from its values - including sp_WhoIsActive's padded, comma-separated counts.
        /// </summary>
        [TestMethod]
        public void LoadDataSet_ReadsSsmsResultsXml()
        {
            var path = Path.Combine(_folder, "Results.xml");
            File.WriteAllText(path, SsmsResultsXmlText);

            var table = GridSerializer.LoadDataSet(path).Tables.Cast<DataTable>().Single();

            CollectionAssert.AreEqual(
                new[] { "dd hh:mm:ss.mss", "session_id", "sql_text", "CPU", "blocking_session_id", "start_time", "code", "list", "code (2)" },
                table.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToArray());
            Assert.AreEqual(2, table.Rows.Count);

            Assert.AreEqual(typeof(string), table.Columns["dd hh:mm:ss.mss"]!.DataType);
            Assert.AreEqual(typeof(long), table.Columns["session_id"]!.DataType);
            Assert.AreEqual(typeof(long), table.Columns["CPU"]!.DataType);
            Assert.AreEqual(9688L, table.Rows[0]["CPU"]);
            Assert.AreEqual(typeof(DateTime), table.Columns["start_time"]!.DataType);
            Assert.AreEqual(new DateTime(2026, 9, 29, 23, 33, 58, 330), table.Rows[0]["start_time"]);
            Assert.AreEqual(typeof(string), table.Columns["blocking_session_id"]!.DataType, "all NULL");
            Assert.IsTrue(table.Rows[0].IsNull("blocking_session_id"));
            Assert.AreEqual(typeof(string), table.Columns["code"]!.DataType, "a leading zero is a code, not a number");
            Assert.AreEqual(typeof(string), table.Columns["list"]!.DataType, "\"1,2\" isn't 12");
            StringAssert.StartsWith((string)table.Rows[0]["sql_text"], "<?query --");
            Assert.IsTrue(table.Rows[0].IsNull("code (2)"), "a column a row leaves out is NULL in it");
            Assert.AreEqual("x", table.Rows[1]["code (2)"]);
        }

        /// <summary>
        /// The name a repeated column is given can be another column's real name - a, a (2), a - and each keeps its own
        /// column and value rather than two sharing one.
        /// </summary>
        [TestMethod]
        public void LoadDataSet_SsmsResultsXml_DuplicateNamesDontCollide()
        {
            var path = Path.Combine(_folder, "Duplicates.xml");
            File.WriteAllText(path,
                "<Data><Row><a>first</a><a_x0020__x0028_2_x0029_>real</a_x0020__x0028_2_x0029_><a>third</a><A>case</A></Row></Data>");

            var table = GridSerializer.LoadDataSet(path).Tables.Cast<DataTable>().Single();

            // Told apart from the others ignoring case, as the grid does.
            CollectionAssert.AreEqual(new[] { "a", "a (2)", "a (3)", "A (4)" },
                table.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToArray());
            CollectionAssert.AreEqual(new object[] { "first", "real", "third", "case" }, table.Rows[0].ItemArray);
        }

        [TestMethod]
        [DataRow("<?xml version=\"1.0\"?><Data><Row><a>1</a></Row></Data>", true)]
        [DataRow("<Data />", true)]
        [DataRow("<Data><Row><a>1</a></Row>", true)] // just the start of a file
        [DataRow("<NewDataSet><xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\" /></NewDataSet>", false)]
        [DataRow("<ShowPlanXML><Row /></ShowPlanXML>", false)]
        [DataRow("<Data><Other /></Data>", false)]
        [DataRow("not xml", false)]
        public void SsmsResultsXml_IsResultsXml(string xml, bool expected)
        {
            Assert.AreEqual(expected, SsmsResultsXml.IsResultsXml(new StringReader(xml)));
        }

        private const string SsmsResultsXmlText = """
            <?xml version="1.0" encoding="utf-8"?>
            <Data xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
              <Row>
                <dd_x0020_hh_x003A_mm_x003A_ss.mss>00 12:00:08.900</dd_x0020_hh_x003A_mm_x003A_ss.mss>
                <session_id>131</session_id>
                <sql_text>&lt;?query --
            waitfor delay @waittime
            --?&gt;</sql_text>
                <CPU>              9,688</CPU>
                <blocking_session_id xsi:nil="true" />
                <start_time>2026-09-29 23:33:58.330</start_time>
                <code>007</code>
                <list>1,2</list>
              </Row>
              <Row>
                <dd_x0020_hh_x003A_mm_x003A_ss.mss>00 00:06:06.564</dd_x0020_hh_x003A_mm_x003A_ss.mss>
                <session_id>79</session_id>
                <sql_text>WAITFOR DELAY '00:10:00'</sql_text>
                <CPU>                     0</CPU>
                <blocking_session_id xsi:nil="true" />
                <start_time>2026-09-30 11:28:00.667</start_time>
                <code>12</code>
                <list>3</list>
                <code>x</code>
              </Row>
            </Data>
            """;

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

        /// <summary>
        /// All of a query's result sets from the SSMS extension (OpenAllResultsCommand): a DataSet of a table per result
        /// set, opened on one tab named from the DataSet's Title, each table keeping its name, title and types.
        /// </summary>
        [STATestMethod]
        public void OpenGridFile_AllResults_OpensAsOneDataSetTab()
        {
            var path = Path.Combine(_folder, "ssms_all.xml.gz");
            using (var stream = File.Create(path))
            using (var gzip = new GZipStream(stream, CompressionLevel.Fastest))
            {
                SsmsStyleDataSet().WriteXml(gzip, XmlWriteMode.WriteSchema);
            }

            var read = GridSerializer.LoadDataSet(path);
            Assert.AreEqual("SQLQuery1.sql - All results", read.ExtendedProperties["Title"]);
            CollectionAssert.AreEqual(new[] { "Result 1", "Result 2" },
                read.Tables.Cast<DataTable>().Select(t => t.TableName).ToArray());
            Assert.AreEqual("SQLQuery1.sql - Result 2", read.Tables[1].ExtendedProperties["Title"]);
            Assert.AreEqual(typeof(TimeSpan), read.Tables[1].Columns["duration"]!.DataType);
            Assert.AreEqual("time(7)", read.Tables[1].Columns["duration"]!.ExtendedProperties["SqlType"]);

            ViewerForm.OpenGridFile(path);
            var window = Application.OpenForms.OfType<ViewerForm>().Single();
            try
            {
                var page = FindAll<TabPage>(window).Single();
                var deadline = DateTime.UtcNow.AddSeconds(10);
                while (page.Controls[0] is not DataSetViewerControl && DateTime.UtcNow < deadline)
                {
                    Application.DoEvents();
                    Thread.Sleep(10);
                }

                Assert.IsInstanceOfType<DataSetViewerControl>(page.Controls[0]);
                Assert.AreEqual("SQLQuery1.sql - All results", page.Text);
                var grid = FindAll<GridViewerControl>(page.Controls[0]).Single();
                Assert.AreEqual("SQLQuery1.sql - Result 1", grid.Title, "each result set keeps its own title");
            }
            finally
            {
                window.Close();
            }
        }

        /// <summary>
        /// A whole DataSet saved from the viewer reads back with every table: from XML with the titles and types intact,
        /// from JSON with the tables and values.
        /// </summary>
        [TestMethod]
        [DataRow("saved.xml.gz", true)]
        [DataRow("saved.xml", true)]
        [DataRow("saved.json", false)]
        [DataRow("saved.json.gz", false)]
        public void SaveDataSet_RoundTrips(string file, bool keepsTypes)
        {
            var path = Path.Combine(_folder, file);
            GridSerializer.SaveDataSet(SsmsStyleDataSet(), path);

            var read = GridSerializer.LoadDataSet(path);
            CollectionAssert.AreEqual(new[] { "Result 1", "Result 2" },
                read.Tables.Cast<DataTable>().Select(t => t.TableName).ToArray());
            Assert.AreEqual(2, read.Tables[1].Rows.Count);
            Assert.AreEqual("first", read.Tables[1].Rows[0]["name"]);

            if (!keepsTypes) return;
            Assert.AreEqual("SQLQuery1.sql - All results", read.ExtendedProperties["Title"]);
            Assert.AreEqual("SQLQuery1.sql - Result 2", read.Tables[1].ExtendedProperties["Title"]);
            Assert.AreEqual(typeof(TimeSpan), read.Tables[1].Columns["duration"]!.DataType);
        }

        [TestMethod]
        public void DataSetViewerControl_HasSaveAll()
        {
            using var control = new DataSetViewerControl(SsmsStyleDataSet(), "SQLQuery1.sql - All results");
            var saveAll = FindAll<ToolStrip>(control).SelectMany(t => t.Items.OfType<ToolStripDropDownButton>())
                .Single(i => i.Text == "Save All");
            CollectionAssert.AreEqual(new[] { "Data Set File...", "Excel..." },
                saveAll.DropDownItems.Cast<ToolStripItem>().Select(i => i.Text).ToArray());
            Assert.IsTrue(saveAll.DropDownItems.Cast<ToolStripItem>().All(i => i.Image != null), "each item has an icon");
        }

        /// <summary>A sheet per table, named after it, with its header and rows - and an empty table still gets a sheet.</summary>
        [TestMethod]
        public void SaveDataSetToXLSX_SheetPerTable()
        {
            var dataSet = SsmsStyleDataSet();
            dataSet.Tables.Add("Empty");
            var path = Path.Combine(_folder, "all.xlsx");
            DBADashSharedGUI.CommonShared.SaveDataSetToXLSX(dataSet, path);

            using var workbook = new ClosedXML.Excel.XLWorkbook(path);
            CollectionAssert.AreEqual(new[] { "Result 1", "Result 2", "Empty" },
                workbook.Worksheets.Select(w => w.Name).ToArray());
            var sheet = workbook.Worksheet("Result 2");
            Assert.AreEqual("id", sheet.Cell(1, 1).GetString());
            Assert.AreEqual("first", sheet.Cell(2, 5).GetString());
            Assert.AreEqual(12.5, sheet.Cell(2, 2).GetDouble());
        }

        [TestMethod]
        [DataRow("Result 1", "Result 1")]
        [DataRow("a/b:c*d?e[f]g\\h", "a_b_c_d_e_f_g_h")]
        [DataRow("'quoted'", "quoted")]
        [DataRow("", "Sheet3")]
        [DataRow("A name that is much longer than thirty-one characters", "A name that is much longer than")]
        [DataRow("A name much longer than 31 cha's", "A name much longer than 31 cha")] // the 31st character is '
        [DataRow("'''", "Sheet3")]
        public void ExcelSheetName_IsValid(string name, string expected)
        {
            Assert.AreEqual(expected, DBADashSharedGUI.CommonShared.ExcelSheetName(name, 3, new HashSet<string>()));
        }

        [TestMethod]
        public void ExcelSheetName_IsUnique()
        {
            var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var longName = new string('x', 40);
            var names = new[] { "Results", "results", longName, longName }
                .Select((n, i) => DBADashSharedGUI.CommonShared.ExcelSheetName(n, i + 1, used)).ToArray();

            CollectionAssert.AreEqual(new[] { "Results", "results (2)", new string('x', 31), new string('x', 27) + " (2)" }, names);
        }

        /// <summary>
        /// One result set with the query's messages, as the SSMS extension sends them (OpenCommand): the result set opens
        /// on a Results tab, with the messages and the statistics read from them on tabs beside it - not as a second
        /// table of a data set.
        /// </summary>
        [STATestMethod]
        public void OpenGridFile_WithMessages_ShowsResultsMessagesAndStatisticsTabs()
        {
            var path = Path.Combine(_folder, "ssms_messages.xml.gz");
            var dataSet = new DataSet("Results");
            dataSet.ExtendedProperties["Title"] = "SQLQuery1.sql - Result 1";
            dataSet.Tables.Add(SsmsStyleTable());
            var messages = dataSet.Tables.Add("Messages");
            messages.ExtendedProperties["Kind"] = "Messages";
            messages.Columns.Add("Message", typeof(string));
            foreach (var line in new[]
                     {
                         "SQL Server parse and compile time: ", "   CPU time = 0 ms, elapsed time = 4 ms.", "",
                         "(2 rows affected)",
                         "Table 'Orders'. Scan count 1, logical reads 120, physical reads 3, read-ahead reads 0.", "",
                         " SQL Server Execution Times:", "   CPU time = 16 ms,  elapsed time = 40 ms."
                     })
            {
                messages.Rows.Add(line);
            }

            using (var stream = File.Create(path))
            using (var gzip = new GZipStream(stream, CompressionLevel.Fastest))
            {
                dataSet.WriteXml(gzip, XmlWriteMode.WriteSchema);
            }

            ViewerForm.OpenGridFile(path);
            var window = Application.OpenForms.OfType<ViewerForm>().Single();
            try
            {
                var page = FindAll<TabPage>(window).First();
                var deadline = DateTime.UtcNow.AddSeconds(10);
                while (page.Controls[0] is Label && DateTime.UtcNow < deadline)
                {
                    Application.DoEvents();
                    Thread.Sleep(10);
                }

                var viewer = (QueryResultsViewerControl)page.Controls[0];
                Assert.AreEqual("SQLQuery1.sql - Result 1", page.Text);
                CollectionAssert.AreEqual(new[] { "Results", "Messages", "Statistics" }, viewer.TabNames);
                Assert.IsInstanceOfType<GridViewerControl>(viewer.Results, "one result set is a grid, not a data set");
                Assert.AreEqual(120L, viewer.Statistics.Counter(StatisticsMessages.LogicalReads));
                Assert.AreEqual(16L, viewer.Statistics.CpuMs);

                Assert.IsTrue(viewer.SelectTab("Statistics"));
                Application.DoEvents();
                Assert.AreEqual(4, FindAll<GridViewerControl>(viewer).Count(), "the result set, table I/O by table and by statement, and statements");
            }
            finally
            {
                window.Close();
            }
        }

        /// <summary>
        /// The Messages tab opened on its own (OpenMessagesCommand), for a query with no results: no Results tab, and it opens
        /// on the statistics it was opened for, named for the messages rather than as a grid.
        /// </summary>
        [STATestMethod]
        public void OpenGridFile_MessagesOnly_OpensOnStatistics()
        {
            var path = Path.Combine(_folder, "ssms_messages_only.xml.gz");
            var dataSet = new DataSet("Results");
            dataSet.ExtendedProperties["Title"] = "SQLQuery1.sql - Messages";
            var messages = dataSet.Tables.Add("Messages");
            messages.ExtendedProperties["Kind"] = "Messages";
            messages.ExtendedProperties["QueryText"] = "SET STATISTICS IO, TIME ON;\r\nUPDATE dbo.T SET a = 1;";
            messages.Columns.Add("Message", typeof(string));
            foreach (var line in new[]
                     {
                         "Table 'T'. Scan count 1, logical reads 30, physical reads 0, read-ahead reads 0.", "",
                         " SQL Server Execution Times:", "   CPU time = 15 ms,  elapsed time = 12 ms.", "",
                         "(4 rows affected)"
                     })
            {
                messages.Rows.Add(line);
            }

            using (var stream = File.Create(path))
            using (var gzip = new GZipStream(stream, CompressionLevel.Fastest))
            {
                dataSet.WriteXml(gzip, XmlWriteMode.WriteSchema);
            }

            ViewerForm.OpenGridFile(path);
            var window = Application.OpenForms.OfType<ViewerForm>().Single();
            try
            {
                var page = FindAll<TabPage>(window).First();
                var deadline = DateTime.UtcNow.AddSeconds(10);
                while (page.Controls[0] is Label && DateTime.UtcNow < deadline)
                {
                    Application.DoEvents();
                    Thread.Sleep(10);
                }

                var viewer = (QueryResultsViewerControl)page.Controls[0];
                Assert.AreEqual("SQLQuery1.sql - Messages", page.Text);
                Assert.AreEqual("SQLQuery1.sql - Messages", window.Text, "not called a grid");
                Assert.IsNull(viewer.Results);
                CollectionAssert.AreEqual(new[] { "Messages", "Statistics" }, viewer.TabNames);
                Assert.AreEqual("Statistics", viewer.SelectedTabName);

                // In the code editor, for its find bar - which Ctrl+F opens on the Messages tab.
                StringAssert.Contains(viewer.MessagesText, "Table 'T'. Scan count 1, logical reads 30");
                viewer.FindInMessages();
                Assert.AreEqual("Messages", viewer.SelectedTabName);
                Assert.AreEqual(15L, viewer.Statistics.CpuMs);
                Assert.AreEqual("UPDATE dbo.T SET a = 1;", viewer.Statistics.SingleStatement);
            }
            finally
            {
                window.Close();
            }
        }

        /// <summary>
        /// The Messages tab of a query split at PRINT 'DBADash-Compare' opens on its parts compared; and any tab with
        /// statistics offers the others' runs to compare with, opening the comparison on a tab of its own.
        /// </summary>
        [STATestMethod]
        public void OpenGridFile_SplitMessages_ComparesTheParts_AndTabsCompareWithEachOther()
        {
            string Write(string name, string fixture)
            {
                var path = Path.Combine(_folder, name + ".xml.gz");
                var dataSet = new DataSet("Results");
                dataSet.ExtendedProperties["Title"] = name;
                var messages = dataSet.Tables.Add("Messages");
                messages.ExtendedProperties["Kind"] = "Messages";
                messages.ExtendedProperties["QueryText"] = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Statistics", fixture + ".sql"));
                messages.Columns.Add("Message", typeof(string));
                foreach (var line in File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "Statistics", fixture + ".txt")))
                {
                    messages.Rows.Add(line);
                }

                using var stream = File.Create(path);
                using var gzip = new GZipStream(stream, CompressionLevel.Fastest);
                dataSet.WriteXml(gzip, XmlWriteMode.WriteSchema);
                return path;
            }

            QueryResultsViewerControl Loaded(TabPage page)
            {
                var deadline = DateTime.UtcNow.AddSeconds(10);
                while (page.Controls[0] is Label && DateTime.UtcNow < deadline)
                {
                    Application.DoEvents();
                    Thread.Sleep(10);
                }
                return (QueryResultsViewerControl)page.Controls[0];
            }

            ViewerForm.OpenGridFile(Write("Split", "compare_calls"));
            var window = Application.OpenForms.OfType<ViewerForm>().Single();
            try
            {
                ViewerForm.OpenGridFile(Write("Single", "sp_WhoIsActive"));
                var pages = FindAll<TabPage>(window).Where(p => p.Parent is DBADashGUI.Controls.DocumentTabControl).ToList();
                var split = Loaded(pages[0]);
                var single = Loaded(pages[1]);

                Assert.IsTrue(split.IsSplit);
                Assert.AreEqual("Statistics", split.SelectedTabName);
                var compare = FindAll<StatisticsCompareControl>(split).Single();
                CollectionAssert.AreEqual(new[] { "Comparison", "(1) EXEC dbo.StatsInner;", "(2) EXEC dbo.StatsOuter;" }, compare.TabNames);

                Assert.IsFalse(single.IsSplit);
                Assert.AreEqual(1, single.Runs.Count);

                // The single query's tab offers each part of the split one.
                var menu = window.CompareStatisticsMenu(pages[1]);
                CollectionAssert.AreEqual(new[] { "Split - EXEC dbo.StatsInner;", "Split - EXEC dbo.StatsOuter;" },
                    menu.DropDownItems.Cast<ToolStripItem>().Select(i => i.Text).ToArray());

                menu.DropDownItems[1].PerformClick();
                var comparison = (StatisticsCompareControl)FindAll<TabPage>(window)
                    .Where(p => p.Parent is DBADashGUI.Controls.DocumentTabControl).Last().Controls[0];
                Assert.AreEqual(2, comparison.Runs.Count);
                Assert.AreEqual(188L, comparison.Runs[0].Report.CpuMs, "sp_WhoIsActive's own time");
                Assert.AreEqual("EXEC dbo.StatsOuter;", comparison.Runs[1].Label);
                StringAssert.StartsWith(window.Text, "Statistics Comparison");

                // The comparison's own tab isn't one to compare from.
                Assert.IsNull(window.CompareStatisticsMenu(FindAll<TabPage>(window)
                    .Where(p => p.Parent is DBADashGUI.Controls.DocumentTabControl).Last()));
            }
            finally
            {
                window.Close();
            }
        }

        /// <summary>
        /// Results and messages saved from the tab come back as they were opened - a grid, its messages, and the statistics
        /// read again from them - and saving leaves the DataSet on show as it was.
        /// </summary>
        [STATestMethod]
        public void SaveResultsAndMessages_OpensBackWithStatistics()
        {
            var path = Path.Combine(_folder, "with_messages.xml.gz");
            var dataSet = new DataSet("Results");
            dataSet.ExtendedProperties["Title"] = "SQLQuery1.sql - Result 1";
            var results = SsmsStyleTable();
            results.TableName = "Result 1";
            results.ExtendedProperties["Title"] = "SQLQuery1.sql - Result 1";
            dataSet.Tables.Add(results);
            var messages = dataSet.Tables.Add("Messages");
            messages.ExtendedProperties["Kind"] = "Messages";
            messages.ExtendedProperties["QueryText"] = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Statistics", "sp_WhoIsActive.sql"));
            messages.ExtendedProperties["QueryTextSource"] = "Executed";
            messages.Columns.Add("Message", typeof(string));
            foreach (var line in File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "Statistics", "sp_WhoIsActive.txt"))) messages.Rows.Add(line);
            using (var stream = File.Create(path))
            using (var gzip = new GZipStream(stream, CompressionLevel.Fastest))
            {
                dataSet.WriteXml(gzip, XmlWriteMode.WriteSchema);
            }

            QueryResultsViewerControl Loaded(ViewerForm window, int index)
            {
                var page = FindAll<TabPage>(window).Where(p => p.Parent is DBADashGUI.Controls.DocumentTabControl).ElementAt(index);
                var deadline = DateTime.UtcNow.AddSeconds(10);
                while (page.Controls[0] is Label && DateTime.UtcNow < deadline)
                {
                    Application.DoEvents();
                    Thread.Sleep(10);
                }
                return (QueryResultsViewerControl)page.Controls[0];
            }

            ViewerForm.OpenGridFile(path);
            var window = Application.OpenForms.OfType<ViewerForm>().Single();
            try
            {
                var original = Loaded(window, 0);
                // On the Results tab's Export menu too, after the grid's own.
                var exports = ((GridViewerControl)original.Results).ExportItemNames;
                Assert.AreEqual("Save Results and Messages...", exports[^1], string.Join(", ", exports));
                CollectionAssert.Contains(exports, "Export File");

                var saved = Path.Combine(_folder, "saved.xml.gz");
                original.Save(saved);
                // Again: the messages were taken back out after the first, or the second would add them twice.
                original.Save(saved);

                var reloaded = DBADashGUI.CustomReports.GridSerializer.LoadDataSet(saved);
                Assert.AreEqual(2, reloaded.Tables.Count, "the result set and the messages");
                var reread = QueryResultsViewerControl.TakeMessages(reloaded);
                Assert.IsTrue(reread.QueryTextExecuted);
                StringAssert.Contains(reread.QueryText, "sp_WhoIsActive");

                ViewerForm.OpenGridFile(saved);
                var reopened = Loaded(window, 1);
                CollectionAssert.AreEqual(new[] { "Results", "Messages", "Statistics" }, reopened.TabNames);
                Assert.AreEqual(original.Statistics.CpuMs, reopened.Statistics.CpuMs);
                Assert.AreEqual(188L, reopened.Statistics.CpuMs, "sp_WhoIsActive's own time, as the text came back too");
                Assert.IsFalse(reopened.Statistics.QueryMayHaveChanged);
            }
            finally
            {
                window.Close();
            }
        }

        /// <summary>The messages table written for a save reads back as the messages it was made from.</summary>
        [TestMethod]
        public void MessagesTable_RoundTripsThroughTakeMessages()
        {
            foreach (var messages in new[]
                     {
                         new SsmsMessages("line 1\nline 2\n\n", QueryText: "EXEC dbo.P", QueryTextExecuted: true),
                         new SsmsMessages("only\n", QueryText: "SELECT 1"),
                         new SsmsMessages("", Error: "The Messages tab wasn't found")
                     })
            {
                var dataSet = new DataSet();
                dataSet.Tables.Add(QueryResultsViewerControl.MessagesTable(messages));
                Assert.AreEqual(messages, QueryResultsViewerControl.TakeMessages(dataSet));
            }
        }

        private static DataSet SsmsStyleDataSet()
        {
            var ds = new DataSet("Results");
            ds.ExtendedProperties["Title"] = "SQLQuery1.sql - All results";
            for (var i = 1; i <= 2; i++)
            {
                var table = SsmsStyleTable();
                table.TableName = $"Result {i}";
                table.ExtendedProperties["Title"] = $"SQLQuery1.sql - Result {i}";
                ds.Tables.Add(table);
            }
            return ds;
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
