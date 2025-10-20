using System.Data.Common;

namespace DBADashSharedGUI
{
    public static class ExtensionMethods
    {
        public static System.Windows.Media.Color ToMediaColor(this Color color) => System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B);

        private const float DefaultColumnWidthCapRatio = 0.15f;

        public static Color AdjustBasedOnLuminance(this Color color)
        {
            // Calculate luminance using a common formula
            var luminance = (0.299 * color.R + 0.587 * color.G + 0.114 * color.B) / 255;

            // Define adjustment value
            var adjustment = luminance < 0.5 ? 30 : -30;

            // Adjust and clamp color components
            int adjustComponent(int component) => Math.Clamp(component + adjustment, 0, 255);

            return Color.FromArgb(
                adjustComponent(color.R),
                adjustComponent(color.G),
                adjustComponent(color.B)
            );
        }

        public static Color ContrastColor(this Color value)
        {
            return ((value.R * 0.299) + (value.G * 0.587) + (value.B * 0.114)) > 186 ? Color.Black : Color.White;
        }

        public static Color ContrastColorTrimble(this Color value)
        {
            return ((value.R * 0.299) + (value.G * 0.587) + (value.B * 0.114)) > 186 ? DashColors.TrimbleBlueDark : DashColors.GrayLight;
        }

        public static string GetDataTypeString(this DbColumn col)
        {
            var dataTypeName = col.DataTypeName?.ToUpper() ?? "???";
            dataTypeName = dataTypeName == "DATETIME" ? "DATETIME2" : dataTypeName; // DATETIME2 has more precision for same storage size.  Also simplifies script generation.
            var typeDetails = dataTypeName switch
            {
                // Handle types with column size
                "VARCHAR" or "NVARCHAR" or "VARBINARY" or "CHAR" or "NCHAR" => col.ColumnSize == int.MaxValue ? "(MAX)" : $"({col.ColumnSize})",
                // Handle types with precision and scale
                "DECIMAL" or "NUMERIC" => $"({col.NumericPrecision},{col.NumericScale})",
                _ => ""
            };

            var nullability = (col.AllowDBNull ?? true) ? " NULL" : " NOT NULL";

            return $"{dataTypeName}{typeDetails}{nullability}";
        }


        /// <summary>
        /// Performs an auto-resize of DataGridView columns, but ensures that no column exceeds columnWidthCapRatio, unless there is sufficient space to accommodate all columns at their auto-sized widths.
        /// </summary>
        /// <param name="dgv">Grid</param>
        /// <param name="mode">Auto size mode used to get ideal sizing which is adjusted to ensure columns are not too large</param>
        /// <param name="columnWidthCapRatio">Defines the initial cap on column widths. e.g. 0.15f caps columns at 15% of the grid width, but allows them to grow larger if sufficient space is available</param>
        public static void AutoResizeColumnsWithMaxColumnWidth(this DataGridView dgv, DataGridViewAutoSizeColumnsMode mode = DataGridViewAutoSizeColumnsMode.DisplayedCells, float? columnWidthCapRatio = null)
        {
            var visibleColumns = dgv.Columns.Cast<DataGridViewColumn>().Where(c => c.Visible).ToList();
            if (visibleColumns.Count == 0) return;

            var colCount = visibleColumns.Count;
            var capRatio = Math.Max(columnWidthCapRatio ?? DefaultColumnWidthCapRatio, 1f / colCount);

            // First pass: get preferred widths
            dgv.AutoResizeColumns(mode);

            var availableWidth = dgv.ClientSize.Width;
            var capWidth = Convert.ToInt32(capRatio * availableWidth);
            var totalUsedWidth = 0;
            var columnWidths = new Dictionary<DataGridViewColumn, int>();

            // Store auto-sized widths and cap if needed
            foreach (var col in visibleColumns)
            {
                var autoWidth = col.Width;
                columnWidths[col] = autoWidth;
                col.Width = Math.Min(autoWidth, capWidth);
                totalUsedWidth += col.Width;
            }

            // Redistribute unused space only to columns that were capped and can still grow
            var unusedSpace = availableWidth - totalUsedWidth;
            if (unusedSpace > 0)
            {
                var cappedColumns = visibleColumns.Where(col => columnWidths[col] > capWidth).ToList();
                if (cappedColumns.Count > 0)
                {
                    var extraPerColumn = unusedSpace / cappedColumns.Count;
                    foreach (var col in cappedColumns)
                    {
                        // Only grow up to the original auto-sized width
                        var newWidth = Math.Min(col.Width + extraPerColumn, columnWidths[col]);
                        col.Width = newWidth;
                    }
                }
            }
        }

        public static string ToHexString(this Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";
    }
}