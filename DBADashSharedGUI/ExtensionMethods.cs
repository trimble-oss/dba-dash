using System.Data.Common;

namespace DBADashSharedGUI
{
    public static class ExtensionMethods
    {
        public static System.Windows.Media.Color ToMediaColor(this Color color) => System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B);

        private const float DefaultMaxColumnWidthPercent = 0.15f;

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

        public static void AutoResizeColumnsWithMaxColumnWidth(this DataGridView dgv, DataGridViewAutoSizeColumnsMode mode = DataGridViewAutoSizeColumnsMode.DisplayedCells, float maxPercentWidth = DefaultMaxColumnWidthPercent)
        {
            dgv.AutoResizeColumns(mode);
            foreach (var col in dgv.Columns.Cast<DataGridViewColumn>().Where(c => c.Width > maxPercentWidth * dgv.Width))
            {
                col.Width = Convert.ToInt32(maxPercentWidth * dgv.Width);
            }
        }

        public static string ToHexString(this Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";
    }
}