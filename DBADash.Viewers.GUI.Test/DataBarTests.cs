using DBADashGUI.CustomReports;
using DBADashSharedGUI;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;
using System.Data;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;

namespace DBADash.Viewers.GUI.Test
{
    /// <summary>Data bars: the share a value fills, the colour of the bar, and bars drawn on a grid.</summary>
    [TestClass]
    public class DataBarTests
    {
        [TestMethod]
        public void Share_IsTheValuesPlaceInTheRange()
        {
            Assert.AreEqual(0.5, DataBarPainter.Share(50, 0, 100));
            Assert.AreEqual(0.25, DataBarPainter.Share(25m, 0, 100));
            Assert.AreEqual(0.5, DataBarPainter.Share(0, -10, 10));
            Assert.AreEqual(1, DataBarPainter.Share(500L, 0, 100), "Past the maximum fills the cell and no more");
        }

        [TestMethod]
        public void Share_IsNullWhereThereIsNothingToDraw()
        {
            Assert.IsNull(DataBarPainter.Share(null, 0, 100));
            Assert.IsNull(DataBarPainter.Share(DBNull.Value, 0, 100));
            Assert.IsNull(DataBarPainter.Share("not a number", 0, 100));
            Assert.IsNull(DataBarPainter.Share(0, 0, 100), "At the minimum");
            Assert.IsNull(DataBarPainter.Share(10, 0, 0), "No range");
        }

        [TestMethod]
        public void TrafficLight_TurnsAtTheThresholds()
        {
            var settings = new DataBarSettings { ColorMode = DataBarColorMode.TrafficLight, WarningThreshold = 20, CriticalThreshold = 50 };
            Assert.AreEqual(DashColors.Success, settings.ColorFor(0.1));
            Assert.AreEqual(DashColors.Warning, settings.ColorFor(0.2));
            Assert.AreEqual(DashColors.Fail, settings.ColorFor(0.9));

            // Where higher is better the short bars are the bad ones - the mirror image.
            settings.HigherIsBetter = true;
            Assert.AreEqual(DashColors.Success, settings.ColorFor(0.9));
            Assert.AreEqual(DashColors.Warning, settings.ColorFor(0.7));
            Assert.AreEqual(DashColors.Fail, settings.ColorFor(0.1));
        }

        [TestMethod]
        public void Gradient_RunsFromTheLowColourToTheHigh()
        {
            var settings = new DataBarSettings { ColorMode = DataBarColorMode.Gradient, Color = Color.FromArgb(0, 0, 0), GradientEndColor = Color.FromArgb(200, 100, 0) };
            Assert.AreEqual(Color.FromArgb(0, 0, 0).ToArgb(), settings.ColorFor(0).ToArgb());
            Assert.AreEqual(Color.FromArgb(100, 50, 0).ToArgb(), settings.ColorFor(0.5).ToArgb());
            Assert.AreEqual(Color.FromArgb(200, 100, 0).ToArgb(), settings.ColorFor(1).ToArgb());
        }

        [TestMethod]
        public void PositiveNegative_ColoursBySign()
        {
            var settings = new DataBarSettings { ColorMode = DataBarColorMode.PositiveNegative, HigherIsBetter = true };
            Assert.AreEqual(DashColors.Success, settings.ColorFor(0.5));
            Assert.AreEqual(DashColors.Fail, settings.ColorFor(0.5, negative: true));

            settings.HigherIsBetter = false;
            Assert.AreEqual(DashColors.Fail, settings.ColorFor(0.5));
            Assert.AreEqual(DashColors.Success, settings.ColorFor(0.5, negative: true));
        }

        /// <summary>
        /// With negative values the bars run out from a zero line: leftwards for negative, rightwards for positive,
        /// in the colour for their side.
        /// </summary>
        [TestMethod]
        public void Grid_DrawsNegativeBarsLeftOfZero()
        {
            var table = new DataTable();
            table.Columns.Add("n", typeof(decimal));
            table.Rows.Add(-50m);
            table.Rows.Add(100m);

            using var form = new Form { Width = 400, Height = 300, ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Left = -3000 };
            using var grid = new DBADashDataGridView { Dock = DockStyle.Fill, RowHeadersVisible = false, AllowUserToAddRows = false };
            form.Controls.Add(grid);
            grid.DataSource = new DataView(table);
            form.Show();
            grid.Columns[0].Width = 308; // 300px across -50 to 100: zero at 100px
            grid.SetDataBar("n", new DataBarSettings { ColorMode = DataBarColorMode.PositiveNegative, HigherIsBetter = true });
            Application.DoEvents();

            var cell = grid.GetCellDisplayRectangle(0, 0, false);
            var zero = cell.X + 4 + 100;

            var (negativeStart, negativeLength) = BarExtent(grid, 0, DashColors.Fail);
            Assert.AreEqual(100, negativeLength, 2, "-50 is a third of the scale");
            Assert.AreEqual(zero - 100, negativeStart, 2, "and runs left from zero");
            Assert.AreEqual(0, BarExtent(grid, 0, DashColors.Success).Length);

            var (positiveStart, positiveLength) = BarExtent(grid, 1, DashColors.Success);
            Assert.AreEqual(200, positiveLength, 2, "100 is two thirds");
            Assert.AreEqual(zero, positiveStart, 2, "and runs right from zero");
        }

        /// <summary>
        /// A traffic light reads the value's place in the scale, not its bar's length: on a scale of negatives the
        /// lowest value has the longest bar but is the least bad where higher is worse.
        /// </summary>
        [TestMethod]
        public void Grid_ColoursNegativeScaleByPlaceInScale()
        {
            var table = new DataTable();
            table.Columns.Add("n", typeof(decimal));
            table.Rows.Add(-100m);
            table.Rows.Add(-10m);

            using var form = new Form { Width = 400, Height = 300, ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Left = -3000 };
            using var grid = new DBADashDataGridView { Dock = DockStyle.Fill, RowHeadersVisible = false, AllowUserToAddRows = false };
            form.Controls.Add(grid);
            grid.DataSource = new DataView(table);
            form.Show();
            grid.SetDataBar("n", new DataBarSettings { ColorMode = DataBarColorMode.TrafficLight });
            Application.DoEvents();

            Assert.AreNotEqual(0, BarExtent(grid, 0, DashColors.Success).Length, "-100 is the bottom of the scale");
            Assert.AreNotEqual(0, BarExtent(grid, 1, DashColors.Fail).Length, "-10 is 90% of the way up");
        }

        /// <summary>
        /// The Data Bar menu is offered on numeric columns - but not on one whose owner paints its own bar, where a bar
        /// set from the menu would never show.
        /// </summary>
        [TestMethod]
        public void DataBarMenu_IsHiddenForColumnsWithTheirOwnBars()
        {
            var table = new DataTable();
            table.Columns.Add("text", typeof(string));
            table.Columns.Add("n", typeof(int));
            table.Columns.Add("own", typeof(int));
            table.Rows.Add("a", 1, 2);

            using var form = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Left = -3000 };
            using var grid = new DBADashDataGridView { Dock = DockStyle.Fill };
            form.Controls.Add(grid);
            grid.DataSource = new DataView(table);
            form.Show();
            grid.ColumnsWithOwnDataBars.Add("own");

            var menuItem = grid.ColumnContextMenu.Items.OfType<ToolStripMenuItem>().Single(i => i.Text == "Data Bar");
            bool VisibleFor(string column)
            {
                var index = grid.Columns[column]!.Index;
                grid.GetType().GetProperty(nameof(grid.ClickedColumnIndex))!.SetValue(grid, index);
                grid.ColumnContextMenuOpening?.Invoke(grid, new DataGridViewCellEventArgs(index, -1));
                return menuItem.Available;
            }

            Assert.IsFalse(VisibleFor("text"), "Not a number");
            Assert.IsTrue(VisibleFor("n"));
            Assert.IsFalse(VisibleFor("own"), "Draws its own bar");
        }

        /// <summary>Save Data Bars is on the Data Bar menu only where the host has somewhere to keep them.</summary>
        [TestMethod]
        public void SaveDataBars_IsOfferedOnlyWhereTheHostCanSave()
        {
            var table = new DataTable();
            table.Columns.Add("n", typeof(int));
            table.Rows.Add(1);

            using var form = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Left = -3000 };
            using var grid = new DBADashDataGridView { Dock = DockStyle.Fill };
            form.Controls.Add(grid);
            grid.DataSource = new DataView(table);
            form.Show();

            var menuItem = grid.ColumnContextMenu.Items.OfType<ToolStripMenuItem>().Single(i => i.Text == "Data Bar");
            var save = menuItem.DropDownItems.OfType<ToolStripMenuItem>().Single(i => i.Text == "Save Data Bars");
            void Open() => typeof(ToolStripDropDownItem)
                .GetMethod("OnDropDownShow", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .Invoke(menuItem, [EventArgs.Empty]);

            Open();
            Assert.IsFalse(save.Available, "Nowhere to save them");

            var saved = 0;
            grid.SaveDataBars = () => saved++;
            Open();
            Assert.IsTrue(save.Available);
            save.PerformClick();
            Assert.AreEqual(1, saved);
        }

        /// <summary>
        /// Copying a column's bar to the others gives every visible number column its look - but not its fixed scale,
        /// which belongs to the figures in the one column - and tells the host once.
        /// </summary>
        [TestMethod]
        public void CopyDataBarToAllColumns_CopiesTheLookNotTheScale()
        {
            var table = new DataTable();
            table.Columns.Add("source", typeof(int));
            table.Columns.Add("scaled", typeof(decimal));
            table.Columns.Add("plain", typeof(long));
            table.Columns.Add("text", typeof(string));
            table.Columns.Add("own", typeof(int));
            table.Columns.Add("hidden", typeof(int));
            table.Rows.Add(1, 2m, 3L, "a", 4, 5);

            using var form = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Left = -3000 };
            using var grid = new DBADashDataGridView { Dock = DockStyle.Fill };
            form.Controls.Add(grid);
            grid.DataSource = new DataView(table);
            form.Show();
            grid.Columns["hidden"]!.Visible = false;
            grid.ColumnsWithOwnDataBars.Add("own");
            grid.SetDataBar("source", new DataBarSettings { Style = DataBarStyle.Fill, ColorMode = DataBarColorMode.Gradient, Minimum = 0, Maximum = 100 });
            grid.SetDataBar("scaled", new DataBarSettings { Maximum = 5 });
            var changes = new List<string>();
            grid.DataBarChanged += (_, key) => changes.Add(key);

            Assert.AreEqual(2, grid.CopyDataBarToAllColumns(grid.Columns["source"]!));

            CollectionAssert.AreEquivalent(new[] { "source", "scaled", "plain" }, grid.DataBars.Keys.ToArray());
            foreach (var key in new[] { "scaled", "plain" })
            {
                Assert.AreEqual(DataBarStyle.Fill, grid.DataBars[key].Style);
                Assert.AreEqual(DataBarColorMode.Gradient, grid.DataBars[key].ColorMode);
                Assert.IsNull(grid.DataBars[key].Minimum);
            }
            Assert.AreEqual(5m, grid.DataBars["scaled"].Maximum, "Keeps its own scale");
            Assert.IsNull(grid.DataBars["plain"].Maximum, "Scales to its values");
            Assert.AreEqual(100m, grid.DataBars["source"].Maximum);

            CollectionAssert.AreEqual(new string[] { null }, changes, "One change, for several columns");
        }

        /// <summary>The plan viewer's ramp is the grid's more-is-worse traffic light, so the two can't drift apart.</summary>
        [TestMethod]
        public void MoreIsWorse_IsBlueThenAmberThenRed()
        {
            var settings = DataBarSettings.MoreIsWorse();
            Assert.AreEqual(DashColors.BlueLight, settings.ColorFor(0.19));
            Assert.AreEqual(DashColors.Warning, settings.ColorFor(0.2));
            Assert.AreEqual(DashColors.Fail, settings.ColorFor(0.5));
        }

        /// <summary>A custom report saves the settings with its column, so they have to come back as they went.</summary>
        [TestMethod]
        public void Settings_RoundTripThroughJson()
        {
            var settings = new DataBarSettings
            {
                Style = DataBarStyle.Fill,
                ColorMode = DataBarColorMode.Gradient,
                Color = Color.FromArgb(1, 2, 3),
                GradientEndColor = Color.FromArgb(4, 5, 6),
                HigherIsBetter = true,
                Minimum = 0,
                Maximum = 100,
                WarningThreshold = 30,
                CriticalThreshold = 60
            };
            var json = JsonConvert.SerializeObject(settings);
            StringAssert.Contains(json, "\"Fill\"");
            var copy = JsonConvert.DeserializeObject<DataBarSettings>(json)!;

            Assert.AreEqual(settings.Style, copy.Style);
            Assert.AreEqual(settings.ColorMode, copy.ColorMode);
            Assert.AreEqual(settings.Color.ToArgb(), copy.Color.ToArgb());
            Assert.AreEqual(settings.GradientEndColor.ToArgb(), copy.GradientEndColor.ToArgb());
            Assert.AreEqual(settings.HigherIsBetter, copy.HigherIsBetter);
            Assert.AreEqual(settings.Minimum, copy.Minimum);
            Assert.AreEqual(settings.Maximum, copy.Maximum);
            Assert.AreEqual(settings.WarningThreshold, copy.WarningThreshold);
            Assert.AreEqual(settings.CriticalThreshold, copy.CriticalThreshold);

            // Automatic limits are left out rather than saved as null.
            Assert.IsFalse(JsonConvert.SerializeObject(new DataBarSettings()).Contains("Minimum"));
        }

        /// <summary>
        /// A bar on a column is drawn to the largest value showing: full under the largest, none under zero - and a
        /// filter rescales it to what is left.
        /// </summary>
        [TestMethod]
        public void Grid_DrawsBarsScaledToTheValuesShowing()
        {
            var table = new DataTable();
            table.Columns.Add("n", typeof(int));
            table.Rows.Add(0);
            table.Rows.Add(50);
            table.Rows.Add(100);

            using var form = new Form { Width = 400, Height = 300, ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Left = -3000 };
            using var grid = new DBADashDataGridView { Dock = DockStyle.Fill, RowHeadersVisible = false, AllowUserToAddRows = false };
            form.Controls.Add(grid);
            grid.DataSource = new DataView(table);
            form.Show();
            grid.Columns[0].Width = 208; // a 200px bar at full length
            var bar = Color.FromArgb(255, 0, 0);
            grid.SetDataBar("n", new DataBarSettings { Color = bar });
            Application.DoEvents();

            Assert.AreEqual(0, BarLength(grid, 0, bar), "Zero draws no bar");
            Assert.AreEqual(100, BarLength(grid, 1, bar), 2, "Half the largest is half a bar");
            Assert.AreEqual(200, BarLength(grid, 2, bar), 2, "The largest is a full bar");

            grid.SetFilter("n <= 50");
            Application.DoEvents();
            Assert.AreEqual(200, BarLength(grid, 1, bar), 2, "Filtered, 50 is the largest showing");

            grid.SetDataBar("n", null);
            Application.DoEvents();
            Assert.AreEqual(0, BarLength(grid, 1, bar), "Removed");
        }

        /// <summary>The run of <paramref name="colour"/> pixels along the underline in a cell of the first column.</summary>
        private static int BarLength(DataGridView grid, int rowIndex, Color colour) => BarExtent(grid, rowIndex, colour).Length;

        /// <summary>Where the run of <paramref name="colour"/> pixels along the underline starts, and how long it is.</summary>
        private static (int Start, int Length) BarExtent(DataGridView grid, int rowIndex, Color colour)
        {
            using var bmp = new Bitmap(grid.Width, grid.Height);
            grid.DrawToBitmap(bmp, new Rectangle(0, 0, grid.Width, grid.Height));
            var cell = grid.GetCellDisplayRectangle(0, rowIndex, false);
            var y = cell.Bottom - 4;
            var start = -1;
            var length = 0;
            for (var x = cell.X; x < cell.Right; x++)
            {
                if (bmp.GetPixel(x, y).ToArgb() != colour.ToArgb()) continue;
                if (start < 0) start = x;
                length++;
            }
            return (start, length);
        }
    }
}
