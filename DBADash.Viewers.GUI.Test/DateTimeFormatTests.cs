using DBADashGUI.CustomReports;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace DBADash.Viewers.GUI.Test
{
    /// <summary>Date and time cells: seconds and milliseconds by default, and the format chosen from the menu.</summary>
    [TestClass]
    public class DateTimeFormatTests
    {
        [TestInitialize]
        public void Reset() => DBADashDataGridView.DateTimeFormat = DBADashDataGridView.DefaultDateTimeFormat;

        [TestCleanup]
        public void Restore() => DBADashDataGridView.DateTimeFormat = DBADashDataGridView.DefaultDateTimeFormat;

        /// <summary>A grid bound to the table, on a form: a grid binds its rows only once it has a handle.</summary>
        private static DBADashDataGridView BoundGrid(DataTable table)
        {
            var grid = new DBADashDataGridView { Dock = DockStyle.Fill };
            var form = new Form { Controls = { grid } };
            _ = form.Handle;
            grid.DataSource = table;
            return grid;
        }

        private static DataTable TableWithDate(DateTime value)
        {
            var table = new DataTable();
            table.Columns.Add("When", typeof(DateTime));
            table.Rows.Add(value);
            return table;
        }

        [TestMethod]
        public void DefaultFormat_ShowsSecondsAndMilliseconds()
        {
            using var grid = BoundGrid(TableWithDate(new DateTime(2026, 11, 25, 14, 3, 7, 123)));
            Assert.AreEqual("2026-11-25 14:03:07.123", grid.Rows[0].Cells[0].FormattedValue);
        }

        [TestMethod]
        public void ChosenFormat_IsUsedForEveryGrid()
        {
            using var first = BoundGrid(TableWithDate(new DateTime(2026, 11, 25, 14, 3, 7, 123)));
            using var second = BoundGrid(TableWithDate(new DateTime(2026, 11, 25, 14, 3, 7, 123)));

            DBADashDataGridView.DateTimeFormat = "yyyy-MM-dd HH:mm";

            Assert.AreEqual("2026-11-25 14:03", first.Rows[0].Cells[0].FormattedValue);
            Assert.AreEqual("2026-11-25 14:03", second.Rows[0].Cells[0].FormattedValue);
        }

        [TestMethod]
        public void ColumnWithOwnFormat_KeepsIt()
        {
            var table = TableWithDate(new DateTime(2026, 11, 25, 14, 3, 7, 123));
            using var grid = BoundGrid(table);
            grid.Columns[0].DefaultCellStyle.Format = "yyyy-MM-dd";

            Assert.AreEqual("2026-11-25", grid.Rows[0].Cells[0].FormattedValue);
        }

        [TestMethod]
        public void BlankFormat_PutsBackTheDefault()
        {
            DBADashDataGridView.DateTimeFormat = "yyyy";
            DBADashDataGridView.DateTimeFormat = " ";
            Assert.AreEqual(DBADashDataGridView.DefaultDateTimeFormat, DBADashDataGridView.DateTimeFormat);
        }

        [TestMethod]
        public void IsValidDateTimeFormat_RejectsWhatNetWontFormat()
        {
            Assert.IsTrue(DBADashDataGridView.IsValidDateTimeFormat("dd/MM/yyyy HH:mm:ss"));
            Assert.IsFalse(DBADashDataGridView.IsValidDateTimeFormat("%"));
        }

        [TestMethod]
        public void Menus_OfferTheFormats()
        {
            using var grid = new DBADashDataGridView();
            foreach (var menu in new[] { grid.CellContextMenu, grid.ColumnContextMenu })
            {
                var item = menu.Items.OfType<ToolStripMenuItem>().Single(i => i.Text == "Date and Time Format");
                Assert.AreEqual(DBADashDataGridView.DateTimeFormats.Length + 1 + 1, item.DropDownItems.Count);
            }
        }
    }
}
