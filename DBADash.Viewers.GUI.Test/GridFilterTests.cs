using DBADashGUI.CustomReports;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data;
using System.Windows.Forms;

namespace DBADash.Viewers.GUI.Test
{
    /// <summary>Putting back a grid filter the user set earlier, as a custom report does when it rebuilds its grids.</summary>
    [TestClass]
    public class GridFilterTests
    {
        private static DataTable Table()
        {
            var table = new DataTable();
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("n", typeof(int));
            table.Rows.Add("a", 1);
            table.Rows.Add("b", 2);
            table.Rows.Add("c", 3);
            return table;
        }

        [TestMethod]
        public void TrySetFilter_AppliesTheFilterAndRaisesGridFilterChanged()
        {
            using var grid = new DBADashDataGridView();
            grid.DataSource = new DataView(Table());
            var raised = 0;
            grid.GridFilterChanged += (_, _) => raised++;

            Assert.IsTrue(grid.TrySetFilter("n > 1"));
            Assert.AreEqual("n > 1", grid.RowFilter);
            Assert.AreEqual(2, ((DataView)grid.DataSource).Count);
            Assert.AreEqual(1, raised);
        }

        /// <summary>A filter that names a column the results no longer have is dropped without an error dialog.</summary>
        [TestMethod]
        public void TrySetFilter_KeepsThePreviousFilterWhenItNoLongerApplies()
        {
            using var grid = new DBADashDataGridView();
            grid.DataSource = new DataView(Table());
            Assert.IsTrue(grid.TrySetFilter("n > 2"));

            Assert.IsFalse(grid.TrySetFilter("[Gone] = 1"));
            Assert.AreEqual("n > 2", grid.RowFilter);
            Assert.AreEqual(1, ((DataView)grid.DataSource).Count);
        }

        [TestMethod]
        public void TrySetFilter_ReturnsFalseWithoutADataView()
        {
            using var grid = new DBADashDataGridView();
            Assert.IsFalse(grid.TrySetFilter("n > 1"));
        }
    }
}
