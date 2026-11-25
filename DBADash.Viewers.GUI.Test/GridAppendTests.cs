using DBADashGUI.CustomReports;
using DBADashGUI.Grids;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Data;

namespace DBADash.Viewers.GUI.Test
{
    /// <summary>A grid read from a growing file takes the new rows, and keeps the rows it has.</summary>
    [TestClass]
    public class GridAppendTests
    {
        private static DataTable Table(params (int Id, string Text)[] rows)
        {
            var table = new DataTable();
            table.Columns.Add("Id", typeof(int));
            table.Columns.Add("Text", typeof(string));
            foreach (var (id, text) in rows) table.Rows.Add(id, text);
            return table;
        }

        [TestMethod]
        public void NewRows_AreAddedAndOldRowsKept()
        {
            var shown = Table((1, "a"), (2, "b"));
            using var grid = new GridViewerControl(shown, "log");

            Assert.IsTrue(grid.TryAppendRows(Table((1, "a"), (2, "b"), (3, "c"))));

            Assert.AreEqual(3, shown.Rows.Count);
            Assert.AreEqual("c", shown.Rows[2]["Text"]);
        }

        [TestMethod]
        public void SameRows_AddNothing()
        {
            var shown = Table((1, "a"));
            using var grid = new GridViewerControl(shown, "log");

            Assert.IsTrue(grid.TryAppendRows(Table((1, "a"))));
            Assert.AreEqual(1, shown.Rows.Count);
        }

        [TestMethod]
        public void ChangedRowInTheMiddle_IsNotAppended()
        {
            var shown = Table((1, "a"), (2, "b"));
            using var grid = new GridViewerControl(shown, "log");

            Assert.IsFalse(grid.TryAppendRows(Table((1, "changed"), (2, "b"), (3, "c"))));
            Assert.AreEqual(2, shown.Rows.Count, "left as it was");
        }

        [TestMethod]
        public void DifferentColumns_AreNotAppended()
        {
            var shown = Table((1, "a"));
            using var grid = new GridViewerControl(shown, "log");

            var other = new DataTable();
            other.Columns.Add("Id", typeof(int));
            other.Rows.Add(1);
            Assert.IsFalse(grid.TryAppendRows(other));
        }

        [TestMethod]
        public void RecordStillBeingWritten_IsReplacedByTheFullRecord()
        {
            var shown = Table((1, "a"));
            shown.Columns.Add(JsonRecords.UnreadableColumnName, typeof(string));
            shown.Rows.Add(2, DBNull.Value, "{\"Id\":2,\"Te");
            using var grid = new GridViewerControl(shown, "log");

            var fresh = Table((1, "a"), (2, "b"), (3, "c"));
            fresh.Columns.Add(JsonRecords.UnreadableColumnName, typeof(string));
            foreach (DataRow row in fresh.Rows) row[JsonRecords.UnreadableColumnName] = DBNull.Value;

            // The partial record is replaced by the same record read in full.
            Assert.IsTrue(grid.TryAppendRows(fresh));
            Assert.AreEqual(3, shown.Rows.Count);
            Assert.AreEqual("b", shown.Rows[1]["Text"]);
            Assert.AreEqual(DBNull.Value, shown.Rows[1][JsonRecords.UnreadableColumnName]);
        }

        [TestMethod]
        public void PartialRecordAtTheEnd_AddsTheColumn_WithoutRebuilding()
        {
            var shown = Table((1, "a"));
            using var grid = new GridViewerControl(shown, "log");
            Assert.IsNull(grid.Notice);

            var fresh = Table((1, "a"), (2, "b"));
            fresh.Columns.Add(JsonRecords.UnreadableColumnName, typeof(string));
            fresh.Rows.Add(3, DBNull.Value, "{\"Id\":3,\"Te");

            Assert.IsTrue(grid.TryAppendRows(fresh));
            Assert.AreEqual(3, shown.Rows.Count);
            Assert.IsTrue(shown.Columns.Contains(JsonRecords.UnreadableColumnName));
            Assert.IsNull(grid.Notice, "still being written - status bar, not above the grid");
            Assert.AreEqual(1, grid.PendingRecordCount);
        }

        [TestMethod]
        public void PartialRecordCompleted_ClearsTheWarning()
        {
            var shown = Table((1, "a"));
            shown.Columns.Add(JsonRecords.UnreadableColumnName, typeof(string));
            shown.Rows.Add(2, DBNull.Value, "{\"Id\":2,\"Te");
            using var grid = new GridViewerControl(shown, "log");

            var fresh = Table((1, "a"), (2, "b"));
            Assert.IsTrue(grid.TryAppendRows(fresh));
            Assert.AreEqual(2, shown.Rows.Count);
            Assert.AreEqual("b", shown.Rows[1]["Text"]);
            Assert.IsNull(grid.Notice);
        }

        [TestMethod]
        public void UnreadablePartBeforeTheEnd_IsShownAboveTheGrid()
        {
            var shown = Table((1, "a"));
            shown.Columns.Add(JsonRecords.UnreadableColumnName, typeof(string));
            shown.Rows.Add(2, DBNull.Value, "{\"Id\":2,\"Te");
            shown.Rows.Add(3, "c", DBNull.Value);
            using var grid = new GridViewerControl(shown, "log");

            StringAssert.StartsWith(grid.Notice, "1 part of the file couldn't be read as JSON");
            Assert.AreEqual(0, grid.PendingRecordCount);
        }
    }
}
