using DBADashGUI.CustomReports;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data;
using System.Windows.Forms;

namespace DBADash.Viewers.GUI.Test
{
    /// <summary>DBADashDataGridView's toolbar items and row header modes.</summary>
    [TestClass]
    public class DBADashDataGridViewTests
    {
        /// <summary>
        /// The grid's toolbar items come from the same factories as its context menus: the grid-wide actions, without
        /// the ones that need a right-clicked row or cell.
        /// </summary>
        [TestMethod]
        public void ToolbarItems_AreTheGridWideActions()
        {
            using var grid = new DBADashDataGridView();
            var items = grid.CreateToolbarItems();

            CollectionAssert.AreEqual(
                new[] { "Copy", "Export", "Transpose", "Group By", "Columns", "Auto Resize Columns", "Clear Filters" },
                items.Where(i => i is not ToolStripSeparator).Select(i => i.Text).ToArray());

            var copy = (ToolStripDropDownButton)items.Single(i => i.Text == "Copy");
            CollectionAssert.AreEqual(new[] { "Grid", "Selected", "As Markdown", "As JSON" },
                copy.DropDownItems.Cast<ToolStripItem>().Select(i => i.Text).ToArray());

            var transpose = (ToolStripDropDownButton)items.Single(i => i.Text == "Transpose");
            CollectionAssert.AreEqual(new[] { "Selected Rows", "Grid" },
                transpose.DropDownItems.Cast<ToolStripItem>().Select(i => i.Text).ToArray());

            // The context menu keeps the right-clicked row.
            var menuTranspose = grid.CellContextMenu.Items.OfType<ToolStripMenuItem>().Single(i => i.Text == "Transpose");
            Assert.AreEqual(3, menuTranspose.DropDownItems.Count);
        }

        /// <summary>
        /// Row headers can be hidden, shown, or shown numbered from either context menu, and numbered ones are sized to
        /// the largest number.
        /// </summary>
        [TestMethod]
        public void RowHeaderModes()
        {
            var table = new DataTable();
            table.Columns.Add("n", typeof(int));
            for (var i = 0; i < 12345; i++) table.Rows.Add(i);

            using var form = new Form { Width = 800, Height = 600, ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Left = -3000 };
            using var grid = new DBADashDataGridView { Dock = DockStyle.Fill, RowHeadersVisible = false };
            form.Controls.Add(grid);
            grid.DataSource = table;
            form.Show();
            Application.DoEvents();

            foreach (var menu in new[] { grid.CellContextMenu, grid.ColumnContextMenu })
            {
                var rowHeaders = menu.Items.OfType<ToolStripMenuItem>().Single(i => i.Text == "Row Headers");
                CollectionAssert.AreEqual(new[] { "Hide", "Show", "Show with Row Numbers" },
                    rowHeaders.DropDownItems.Cast<ToolStripItem>().Select(i => i.Text).ToArray());
            }

            grid.RowHeaderMode = DBADashDataGridView.RowHeaderModes.Visible;
            Assert.IsTrue(grid.RowHeadersVisible);
            // Themed as the column headers, not left in the system's grey.
            Assert.AreEqual(grid.ColumnHeadersDefaultCellStyle.BackColor, grid.RowHeadersDefaultCellStyle.BackColor);
            Assert.AreEqual(grid.ColumnHeadersDefaultCellStyle.ForeColor, grid.RowHeadersDefaultCellStyle.ForeColor);
            var plainWidth = grid.RowHeadersWidth;

            grid.RowHeaderMode = DBADashDataGridView.RowHeaderModes.Numbered;
            Assert.AreEqual(DBADashDataGridView.RowHeaderModes.Numbered, grid.RowHeaderMode);
            var fiveDigits = TextRenderer.MeasureText("99999", grid.RowHeadersDefaultCellStyle.Font ?? grid.Font).Width;
            Assert.IsTrue(grid.RowHeadersWidth > fiveDigits, $"{grid.RowHeadersWidth}px fits 12,345");
            Assert.AreNotEqual(plainWidth, grid.RowHeadersWidth);

            grid.RowHeaderMode = DBADashDataGridView.RowHeaderModes.Hidden;
            Assert.IsFalse(grid.RowHeadersVisible);
        }
    }
}
