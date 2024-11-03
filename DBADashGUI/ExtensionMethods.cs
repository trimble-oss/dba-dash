using DBADashGUI.CustomReports;
using DBADashGUI.Pickers;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using DBADashGUI.Theme;
using static DBADashGUI.DBADashStatus;
using LiveChartsCore.SkiaSharpView.SKCharts;
using SkiaSharp;
using System.IO;
using LiveChartsCore.SkiaSharpView.WinForms;

namespace DBADashGUI
{
    public static class ExtensionMethods
    {
        public static string Truncate(this string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            return value[..Math.Min(value.Length, maxLength)];
        }

        private static readonly HashSet<Type> NumericTypes = new()
        {
            typeof(int), typeof(double), typeof(decimal),
            typeof(long), typeof(short), typeof(sbyte),
            typeof(byte), typeof(ulong), typeof(ushort),
            typeof(uint), typeof(float)
        };

        public static bool IsNumeric(this Type myType)
        {
            return NumericTypes.Contains(Nullable.GetUnderlyingType(myType) ?? myType);
        }

        public static DataTable AsDataTable(this IEnumerable<int> list)
        {
            var dt = new DataTable();
            dt.Columns.Add("ID", typeof(int));
            foreach (int i in list)
            {
                var r = dt.NewRow();
                r["ID"] = i;
                dt.Rows.Add(r);
            }

            return dt;
        }

        public static Color GetBackColor(this DBADashStatusEnum value)
        {
            return GetStatusBackColor(value);
        }

        public static Color GetForeColor(this DBADashStatusEnum value)
        {
            return GetStatusForeColor(value);
        }

        public static void SetStatusColor(this DataGridViewCellStyle value, DBADashStatusEnum Status)
        {
            value.BackColor = Status.GetBackColor();
            value.ForeColor = Status.GetForeColor();
        }

        public static void SetColor(this DataGridViewCell cell, Color backColor, Color foreColor,
            Color selectionBackColor, Color selectionForeColor)
        {
            cell.Style.BackColor = backColor;
            cell.Style.ForeColor = foreColor;
            if (cell.GetType() == typeof(DataGridViewLinkCell))
            {
                ((DataGridViewLinkCell)cell).LinkColor = foreColor;
                ((DataGridViewLinkCell)cell).VisitedLinkColor = foreColor;
                ((DataGridViewLinkCell)cell).ActiveLinkColor = selectionForeColor;
            }

            cell.Style.SelectionForeColor = selectionForeColor;
            cell.Style.SelectionBackColor = selectionBackColor;
        }

        public static void SetColor(this DataGridViewCell cell, Color backColor, Color foreColor)
        {
            SetColor(cell, backColor, foreColor, backColor.AdjustBasedOnLuminance(), foreColor);
        }

        public static void SetColor(this DataGridViewCell cell, Color backColor)
        {
            SetColor(cell, backColor, backColor.ContrastColor());
        }

        public static void SetStatusColor(this DataGridViewCell cell, DBADashStatusEnum Status)
        {
            cell.SetColor(Status.GetBackColor(), Status.GetForeColor(), Status.GetBackColor().AdjustBasedOnLuminance(),
                Status.GetForeColor());
        }

        public static string ToHexString(this Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";

        /// <summary>
        /// Returns structure with column layout - size, position & visibility
        /// </summary>
        internal static List<KeyValuePair<string, PersistedColumnLayout>> GetColumnLayout(this DataGridView dgv)
        {
            return dgv.Columns.Cast<DataGridViewColumn>()
                .Select(c => new KeyValuePair<string, PersistedColumnLayout>(c.Name,
                    new PersistedColumnLayout()
                    { Visible = c.Visible, Width = c.Width, DisplayIndex = c.DisplayIndex }))
                .ToList();
        }

        /// <summary>
        /// Loads a saved column layout to the grid.  Size, position & visibility of columns
        /// </summary>
        internal static void LoadColumnLayout(this DataGridView dgv,
            List<KeyValuePair<string, PersistedColumnLayout>> savedCols)
        {
            if (savedCols == null)
            {
                return;
            }

            foreach (DataGridViewColumn col in dgv.Columns)
            {
                if (savedCols.Count(savedCol => savedCol.Key == col.Name) == 1)
                {
                    var savedCol = savedCols.First(savedCol => savedCol.Key == col.Name);
                    col.Visible = savedCol.Value.Visible;
                    col.Width = savedCol.Value.Width;
                    if (savedCol.Value.DisplayIndex >= 0 && savedCol.Value.DisplayIndex < dgv.Columns.Count)
                    {
                        col.DisplayIndex = savedCol.Value.DisplayIndex;
                    }
                }
                else
                {
                    col.Visible = false;
                }
            }
        }

        internal static SQLTreeItem SelectedSQLTreeItem(this TreeView value)
        {
            return value.SelectedNode.AsSQLTreeItem();
        }

        internal static SQLTreeItem AsSQLTreeItem(this TreeNode value)
        {
            return (SQLTreeItem)value;
        }

        /// <summary>
        /// Add Guid SqlParameter to the collection only if parameter value is not empty
        /// </summary>
        /// <param name="p"></param>
        /// <param name="parameterName">Name of parameter</param>
        /// <param name="value">Parameter value</param>
        internal static SqlParameter AddGuidIfNotEmpty(this SqlParameterCollection p, string parameterName, Guid value)
        {
            if (value != Guid.Empty)
            {
                return p.AddWithValue(parameterName, value);
            }

            return null;
        }

        /// <summary>
        /// Add Guid SqlParameter to the collection only if parameter value is not null or empty
        /// </summary>
        /// <param name="p"></param>
        /// <param name="parameterName">Name of parameter</param>
        /// <param name="value">Parameter value</param>
        internal static SqlParameter AddStringIfNotNullOrEmpty(this SqlParameterCollection p, string parameterName,
            string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                return p.AddWithValue(parameterName, value);
            }

            return null;
        }

        /// <summary>
        /// Add parameter with value, passing DBNull.value in place of null
        /// </summary>
        /// <param name="p"></param>
        /// <param name="parameterName">Name of parameter</param>
        /// <param name="value">Parameter value</param>
        internal static SqlParameter AddWithNullableValue(this SqlParameterCollection p, string parameterName,
            object value)
        {
            if (value == null)
                return p.AddWithValue(parameterName, DBNull.Value);
            else
                return p.AddWithValue(parameterName, value);
        }

        /// <summary>
        /// Add parameter with value if value is greater than zero
        /// </summary>
        /// <param name="p"></param>
        /// <param name="parameterName">Name of parameter</param>
        /// <param name="value">Parameter value</param>
        internal static SqlParameter AddIfGreaterThanZero(this SqlParameterCollection p, string parameterName,
            int value)
        {
            if (value > 0)
                return p.AddWithValue(parameterName, value);
            else
                return null;
        }

        /// <summary>
        /// Add parameter with value if value is greater than zero
        /// </summary>
        /// <param name="p"></param>
        /// <param name="parameterName">Name of parameter</param>
        /// <param name="value">Parameter value</param>
        internal static SqlParameter AddIfGreaterThanZero(this SqlParameterCollection p, string parameterName,
            long value)
        {
            if (value > 0)
                return p.AddWithValue(parameterName, value);
            else
                return null;
        }

        /// <summary>
        /// Add parameter with value if value is less than max value
        /// </summary>
        /// <param name="p"></param>
        /// <param name="parameterName">Name of parameter</param>
        /// <param name="value">Parameter value</param>
        internal static SqlParameter AddIfLessThanMaxValue(this SqlParameterCollection p, string parameterName,
            long value)
        {
            if (value != long.MaxValue)
                return p.AddWithValue(parameterName, value);
            else
                return null;
        }

        /// <summary>
        /// Check a single ToolStripMenuItem.  Other menu items to be unchecked
        /// </summary>
        /// <param name="dropdown"></param>
        /// <param name="checkedItem">Item to be checked.  Other drop down items will be unchecked</param>
        internal static void CheckSingleItem(this ToolStripDropDownButton dropdown, ToolStripMenuItem checkedItem)
        {
            foreach (ToolStripMenuItem mnu in dropdown.DropDownItems.OfType<ToolStripMenuItem>())
            {
                mnu.Checked = mnu == checkedItem;
            }
        }

        internal static void OpenAsTextFile(this string value)
        {
            string path = Common.GetTempFilePath(".txt");
            System.IO.File.WriteAllText(path, value);
            ProcessStartInfo psi = new() { FileName = path, UseShellExecute = true };
            Process.Start(psi);
        }

        public static List<ISelectable> ToSelectableList(this DataGridViewColumnCollection columns) => columns
            .Cast<DataGridViewColumn>()
            .Select(column => new SelectableColumn(column) as ISelectable)
            .ToList();

        public static void ApplyVisibility(this DataGridViewColumnCollection columns, List<ISelectable> selectables)
        {
            var columnDict = columns.Cast<DataGridViewColumn>()
                .ToDictionary(c => c.HeaderText, c => c);

            foreach (var selectable in selectables)
            {
                if (columnDict.TryGetValue(selectable.Name, out var column))
                {
                    column.Visible = selectable.IsVisible;
                }
            }
        }

        public static void ApplyVisibility(this Dictionary<string, ColumnMetaData> metrics,
            List<ISelectable> selectables) => selectables.Where(s => metrics.ContainsKey(s.Name))
            .ToList()
            .ForEach(s => metrics[s.Name].IsVisible = s.IsVisible);

        public static DialogResult PromptColumnSelection(this DataGridView dgv)
        {
            using var frm = new SelectColumns() { Items = dgv.Columns.ToSelectableList() };
            frm.ApplyTheme(DBADashUser.SelectedTheme);
            frm.ShowDialog(dgv);
            if (frm.DialogResult == DialogResult.OK)
            {
                dgv.Columns.ApplyVisibility(frm.Items);
            }

            return frm.DialogResult;
        }

        public static double RoundUpToSignificantFigures(this double num, int n = 1)
        {
            if (num == 0) return 0;

            double d = Math.Ceiling(Math.Log10(Math.Abs(num)));
            int power = n - (int)d;

            double magnitude = Math.Pow(10, power);
            long shifted = Convert.ToInt64(Math.Ceiling(num * magnitude));
            return shifted / magnitude;
        }

        public static List<ISelectable> ToSelectableList(this List<string> list)
        {
            return list.Select(s => new SelectableString(s) as ISelectable).ToList();
        }

        public static object DBNullToNull(this object obj)
        {
            return obj == DBNull.Value ? null : obj;
        }

        public static int GetValueAsInt(this Dictionary<string, object> dict, string key, int defaultValue)
        {
            // Check if the key exists
            if (dict.TryGetValue(key, out object value))
            {
                // Try to cast the value to an integer
                if (value is int intValue)
                {
                    return intValue;
                }
            }

            // If the key does not exist or the value is not an integer, return the default value
            return defaultValue;
        }

        public static int? GetValueAsNullableInt(this Dictionary<string, object> dict, string key)
        {
            // Check if the key exists
            if (dict.TryGetValue(key, out object value))
            {
                // Try to cast the value to an integer
                if (value is int intValue)
                {
                    return intValue;
                }
            }

            // If the key does not exist or the value is not an integer, return null value
            return null;
        }

        public static double GetValueAsDouble(this Dictionary<string, object> dict, string key, double defaultValue)
        {
            // Check if the key exists
            if (dict.TryGetValue(key, out object value))
            {
                // Try to cast the value to a double
                if (double.TryParse(value.ToString(), out double dValue))
                {
                    return dValue;
                }
            }

            // If the key does not exist or the value is not a double, return the default value
            return defaultValue;
        }

        public static bool IsNumericType(this Type type)
        {
            return Type.GetTypeCode(type) switch
            {
                TypeCode.Byte => true,
                TypeCode.Decimal => true,
                TypeCode.Double => true,
                TypeCode.Int16 => true,
                TypeCode.Int32 => true,
                TypeCode.Int64 => true,
                TypeCode.SByte => true,
                TypeCode.Single => true,
                TypeCode.UInt16 => true,
                TypeCode.UInt32 => true,
                TypeCode.UInt64 => true,
                _ => false
            };
        }

        public static SqlParameter Clone(this SqlParameter original)
        {
            return new SqlParameter
            {
                ParameterName = original.ParameterName,
                SqlDbType = original.SqlDbType,
                Direction = original.Direction,
                Size = original.Size,
                Value = original.Value,
                IsNullable = original.IsNullable,
                Precision = original.Precision,
                Scale = original.Scale,
                SourceColumn = original.SourceColumn,
                SourceVersion = original.SourceVersion,
                UdtTypeName = original.UdtTypeName
            };
        }

        /// <summary>
        /// Replace single ' quote with two single quotes ''.  Only to be used where input can't be parameterized
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string SqlSingleQuote(this string value) => value.Replace("'", "''");

        public static int ToWin32(this Color color) => color.R | (color.G << 8) | (color.B << 16);

        public static string GetDescription(this Font font)
        {
            return font.Name + ", " + font.Size + " " + font.Unit + ", " + font.Style;
        }

        /// <summary>
        /// Changes the brightness of a color by adjusting its RGB values.
        /// </summary>
        /// <param name="color">The original color.</param>
        /// <param name="correctionFactor">
        /// The factor to adjust brightness. Positive values make the color lighter,
        /// negative values make it darker. The range is typically between -1.0 and 1.0.
        /// </param>
        /// <returns>The adjusted color.</returns>
        public static Color ChangeColorBrightness(this Color color, float correctionFactor)
        {
            // Convert RGB to HSL
            color.ToHsl(out var hue, out var saturation, out var lightness);

            // Adjust lightness
            lightness += correctionFactor;
            lightness = Math.Max(0, Math.Min(1, lightness)); // Clamp lightness between 0 and 1

            // Convert back to RGB
            return FromHsl(hue, saturation, lightness);
        }

        public static void ToHsl(this Color color, out double h, out double s, out double l)
        {
            var tolerance = 1e-6;
            // Convert RGB to a 0-1 range.
            var r = color.R / 255.0;
            var g = color.G / 255.0;
            var b = color.B / 255.0;

            var max = Math.Max(r, Math.Max(g, b));
            var min = Math.Min(r, Math.Min(g, b));

            // Lightness is the average of the largest and smallest color components.
            l = (max + min) / 2;

            if (Math.Abs(max - min) < tolerance)
            {
                // Achromatic color (gray scale).
                h = s = 0;
            }
            else
            {
                var delta = max - min;

                // Saturation calculation.
                s = l > 0.5 ? delta / (2 - max - min) : delta / (max + min);

                // Hue calculation.
                if (Math.Abs(max - r) < tolerance)
                {
                    h = (g - b) / delta + (g < b ? 6 : 0);
                }
                else if (Math.Abs(max - g) < tolerance)
                {
                    h = (b - r) / delta + 2;
                }
                else // if (Math.Abs(max - b) < Tolerance)
                {
                    h = (r - g) / delta + 4;
                }

                h /= 6;
            }

            // Convert hue to degrees.
            h *= 360;
        }

        public static Color FromHsl(double h, double s, double l)
        {
            double r, g, b;

            if (s == 0)
            {
                // Achromatic color (gray scale).
                r = g = b = l;
            }
            else
            {
                double HueToRgb(double p, double q, double t)
                {
                    if (t < 0) t += 1;
                    if (t > 1) t -= 1;
                    return t switch
                    {
                        < 1.0 / 6 => p + (q - p) * 6 * t,
                        < 1.0 / 2 => q,
                        < 2.0 / 3 => p + (q - p) * (2.0 / 3 - t) * 6,
                        _ => p
                    };
                }

                var q = l < 0.5 ? l * (1 + s) : l + s - l * s;
                var p = 2 * l - q;

                h /= 360; // Convert hue to [0,1] range
                r = HueToRgb(p, q, h + 1.0 / 3);
                g = HueToRgb(p, q, h);
                b = HueToRgb(p, q, h - 1.0 / 3);
            }

            return Color.FromArgb((int)(r * 255), (int)(g * 255), (int)(b * 255));
        }

        public static string StripInvalidXmlChars(this string text)
        {
            var validXml = new StringBuilder();
            foreach (var c in text.Where(XmlConvert.IsXmlChar))
            {
                validXml.Append(c);
            }

            return validXml.ToString();
        }

        public static string RemoveHexPrefix(this string hexString)
        {
            return hexString.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? hexString[2..] : hexString;
        }

        public static bool IsHex(this string hexString)
        {
            if (!hexString.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            var hex = hexString[2..];
            return hex.Length % 2 == 0 && hex.All(c => "0123456789ABCDEFabcdef".Contains(c));
        }

        public static byte[] HexStringToByteArray(this string hex)
        {
            if (!hex.IsHex())
            {
                return null;
            }
            var h = hex.RemoveHexPrefix();
            return Enumerable.Range(0, h.Length)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(h.Substring(x, 2), 16))
                .ToArray();
        }

        public static void InvokeSetStatus(this ToolStripStatusLabel label, string message, string tooltip, Color color)
        {
            label.Owner?.Invoke(() =>
            {
                label.Visible = true;
                label.Text = message.Length > 200 ? message[..200] + "..." : message;
                label.ToolTipText = tooltip;
                label.IsLink = !string.IsNullOrEmpty(tooltip);
                label.ForeColor = DBADashUser.SelectedTheme.ThemeIdentifier == ThemeType.Dark ? DBADashUser.SelectedTheme.ForegroundColor : color;
                label.LinkColor = DBADashUser.SelectedTheme.ThemeIdentifier == ThemeType.Dark ? DBADashUser.SelectedTheme.ForegroundColor : color;
                label.Click -= StatusLabel_Click;
                label.Click += StatusLabel_Click;
            });
        }

        private static void StatusLabel_Click(object sender, EventArgs e)
        {
            if (sender is ToolStripStatusLabel clickedLabel && !string.IsNullOrEmpty(clickedLabel.ToolTipText))
            {
                MessageBox.Show(clickedLabel.ToolTipText, "Error Details", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static void InvokeSetStatus(this ToolStripLabel label, string message, string tooltip, Color color)
        {
            const int maxLength = 200;
            label.Owner?.Invoke(() =>
            {
                label.Visible = true;
                label.Text = message.Length > maxLength ? message[..maxLength] + "..." : message;
                label.ToolTipText = tooltip;
                label.IsLink = !string.IsNullOrEmpty(tooltip);
                label.ForeColor = color;
                label.LinkColor = color;
            });
        }

        /// <summary>
        /// Add columns to data grid view based on the columns in the data table and user preferences for column alias, cell format string and link columns
        /// </summary>
        /// <param name="dt">DataTable - columns will be added from here</param>
        /// <param name="customReportResult">Types for each column</param>
        public static void AddColumns(this DataGridView dgv, DataTable dt, CustomReportResult customReportResult)
        {
            foreach (DataColumn dataColumn in dt.Columns)
            {
                DataGridViewColumn column;

                if (customReportResult.LinkColumns.ContainsKey(dataColumn.ColumnName))
                {
                    column = new DataGridViewLinkColumn();
                }
                else if (dataColumn.DataType == typeof(bool))
                {
                    column = new DataGridViewCheckBoxColumn();
                }
                else
                {
                    column = new DataGridViewTextBoxColumn();
                }

                column.SortMode = DataGridViewColumnSortMode.Automatic;
                column.DefaultCellStyle.Format =
                    customReportResult.CellFormatString.TryGetValue(dataColumn.ColumnName, out var value)
                        ? value
                        : "";

                column.DataPropertyName = dataColumn.ColumnName;
                column.Name = dataColumn.ColumnName;
                column.HeaderText =
                    customReportResult.ColumnAlias.TryGetValue(column.DataPropertyName, out var alias)
                        ? alias
                        : dataColumn.Caption;
                column.ValueType = dataColumn.DataType;
                dgv.Columns.Add(column);
            }
        }

        public static DateTime RoundDownToPreviousHour(this DateTime dateTime)
        {
            return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, 0, 0, dateTime.Kind);
        }

        public static void SaveChartAs(this CartesianChart chart, string fileName = "chart.png")
        {
            var sfd = new SaveFileDialog
            {
                Filter = @"PNG Files|*.png|SVG Files|*.svg",
                Title = @"Save Chart Image",
                FileName = fileName
            };
            if (sfd.ShowDialog() != DialogResult.OK) return;
            File.Delete(sfd.FileName);
            var skChart = new SKCartesianChart(chart) { Width = chart.Width, Height = chart.Height, };
            if (string.Equals(System.IO.Path.GetExtension(sfd.FileName), ".svg", StringComparison.InvariantCultureIgnoreCase))
            {
                using var stream = new MemoryStream();
                var svgCanvas = SKSvgCanvas.Create(SKRect.Create(skChart.Width, skChart.Height), stream);
                skChart.DrawOnCanvas(svgCanvas);
                svgCanvas.Dispose(); // <- dispose it before using the stream, otherwise the svg could not be completed.

                stream.Position = 0;
                using var fs = new FileStream(sfd.FileName, FileMode.OpenOrCreate);
                stream.CopyTo(fs);
            }
            else
            {
                skChart.SaveImage(sfd.FileName);
            }
        }

        public static void CopyImage(this CartesianChart chart)
        {
            var skChart = new SKCartesianChart(chart) { Width = chart.Width, Height = chart.Height, };
            using var stream = new MemoryStream();
            skChart.GetImage().Encode(SKEncodedImageFormat.Png, 80).SaveTo(stream);
            Clipboard.SetImage(new Bitmap(stream));
        }

        public static Type ToClrType(this SqlDbType sqlDbType)
        {
            return sqlDbType switch
            {
                SqlDbType.BigInt => typeof(long),
                SqlDbType.Binary => typeof(byte[]),
                SqlDbType.Bit => typeof(bool),
                SqlDbType.Char => typeof(string),
                SqlDbType.Date => typeof(DateTime),
                SqlDbType.DateTime => typeof(DateTime),
                SqlDbType.DateTime2 => typeof(DateTime),
                SqlDbType.DateTimeOffset => typeof(DateTimeOffset),
                SqlDbType.Decimal => typeof(decimal),
                SqlDbType.Float => typeof(double),
                SqlDbType.Image => typeof(byte[]),
                SqlDbType.Int => typeof(int),
                SqlDbType.Money => typeof(decimal),
                SqlDbType.NChar => typeof(string),
                SqlDbType.NText => typeof(string),
                SqlDbType.NVarChar => typeof(string),
                SqlDbType.Real => typeof(float),
                SqlDbType.SmallDateTime => typeof(DateTime),
                SqlDbType.SmallInt => typeof(short),
                SqlDbType.SmallMoney => typeof(decimal),
                SqlDbType.Structured => typeof(DataTable),
                SqlDbType.Text => typeof(string),
                SqlDbType.Time => typeof(TimeSpan),
                SqlDbType.Timestamp => typeof(byte[]),
                SqlDbType.TinyInt => typeof(byte),
                SqlDbType.Udt => typeof(object), // User-defined type; you might need a more specific mapping here.
                SqlDbType.UniqueIdentifier => typeof(Guid),
                SqlDbType.VarBinary => typeof(byte[]),
                SqlDbType.VarChar => typeof(string),
                SqlDbType.Variant => typeof(object),
                SqlDbType.Xml => typeof(string), // For XML data, string is a common choice, but you might want to use XmlDocument or XDocument in some cases.
                _ => throw new ArgumentOutOfRangeException(nameof(sqlDbType), $"Unsupported SqlDbType: {sqlDbType}")
            };
        }
    }
}