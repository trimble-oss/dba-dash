using DBADashGUI.Theme;
using DBADashGUI.Pickers;
using DBADashGUI;
using System.Data;
using System.Xml;
using AsyncKeyedLock;
using System.Collections.Concurrent;
using System.Data.Common;
using System.Security.Cryptography;
using System.Text;

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

        public static Task InvokeAsync(this Control control, Func<Task> func)
        {
            var tcs = new TaskCompletionSource<object>();
            control.BeginInvoke(new Action(async () =>
            {
                try
                {
                    await func();
                    tcs.SetResult(null);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            }));
            return tcs.Task;
        }

        public static string ToHexString(this Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";

        private static readonly ConcurrentDictionary<string, FormState> SingleInstanceFormStates = new();
        private static readonly ConcurrentDictionary<string, Form> SingleInstanceForm = new();
        private static readonly AsyncNonKeyedLocker SingleInstanceLocker = new();
        public static bool ChildFormSingleInstance { get; set; } = true;

        /// <summary>
        /// Get a key to identity the type of form.  Uses the type name of the form, unless the type is "Form" in which case it computes a hash of the controls.
        /// </summary>
        /// <param name="form"></param>
        /// <returns>A unique string</returns>
        private static string GetSingleInstanceKey(Form form)
        {
            var type = form.GetType();
            var typeName = type.FullName ?? typeof(Form).FullName;

            // If it's a plain Form type, use title and a hash of the control tree to distinguish instances
            if (typeName == typeof(Form).FullName)
            {
                var controlsHash = ComputeControlsHash(form);
                return $"{typeName}|{controlsHash}";
            }

            return typeName;
        }

        /// <summary>
        /// Creates a hash of controls based on type, name, size and position.  For forms defined programmatically, this provides a way to distinguish between them
        /// </summary>
        /// <param name="root"></param>
        /// <returns></returns>
        private static string ComputeControlsHash(Control root)
        {
            // Create a deterministic string representation of the control hierarchy
            var sb = new StringBuilder();
            void Visit(Control c)
            {
                sb.Append(c.GetType().FullName);
                sb.Append('|');
                sb.Append(c.Name);
                sb.Append('|');
                sb.Append(c.Bounds.X);
                sb.Append(',');
                sb.Append(c.Bounds.Y);
                sb.Append(',');
                sb.Append(c.Bounds.Width);
                sb.Append(',');
                sb.Append(c.Bounds.Height);
                sb.Append(';');

                // Sort children by name/type to get stable order
                foreach (Control child in c.Controls.Cast<Control>().OrderBy(x => x.GetType().FullName).ThenBy(x => x.Name))
                {
                    Visit(child);
                }
            }

            Visit(root);
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            var hash = sha.ComputeHash(bytes);
            return Convert.ToHexString(hash);
        }

        /// <summary>
        /// Shows a single instance of the form, closing any existing instance.
        /// </summary>
        /// <param name="form"></param>
        /// <param name="trackFormState">Persist and restore the form size/position.</param>
        /// <param name="forceNewInstance">When true, always open a new copy of the form instead of reusing/closing the existing single instance (e.g. Ctrl+click).</param>
        public static void ShowSingleInstance(this Form form, bool trackFormState = true, bool forceNewInstance = false)
        {
            if (form.InvokeRequired)
            {
                form.BeginInvoke(new Action(() =>
                {
                    var task = ShowSingleInstanceAsync(form, trackFormState, forceNewInstance);
                    task.ContinueWith(t =>
                    {
                        try
                        {
                            // Observe exception to avoid crashing due to async void behavior
                            _ = t.Exception;
                        }
                        catch
                        {
                            // Swallow if AggregateException is not present
                        }
                    }, TaskContinuationOptions.OnlyOnFaulted);
                }));
            }
            else
            {
                _ = ShowSingleInstanceAsync(form, trackFormState, forceNewInstance);
            }
        }

        /// <summary>
        /// Shows a single instance of the form, closing any existing instance.
        /// </summary>
        /// <param name="form"></param>
        /// <param name="trackFormState">Persist and restore the form size/position.</param>
        /// <param name="forceNewInstance">When true, always open a new copy of the form instead of reusing/closing the existing single instance (e.g. Ctrl+click).</param>
        public static async Task ShowSingleInstanceAsync(this Form form, bool trackFormState = true, bool forceNewInstance = false)
        {
            if (!ChildFormSingleInstance || forceNewInstance)
            {
                // Open a new, independent copy without touching the single-instance tracking.
                form.Show();
                return;
            }
            using var locker = await SingleInstanceLocker.LockAsync();

            var key = GetSingleInstanceKey(form);
            SingleInstanceForm.TryGetValue(key, out var inst);
            try
            {
                inst?.Close();
            }
            catch
            {
                // Ignore exceptions when closing existing form
            }
            SingleInstanceForm[key] = form;
            form.FormClosed += (s, e) =>
            {
                SingleInstanceForm.TryRemove(key, out _);
            };

            if (trackFormState)
            {
                SingleInstanceFormStates.TryGetValue(key, out var formState);
                formState ??= new FormState();
                FormState.ApplyFormState(form, formState);
                FormState.TrackFormState(form, formState);
                SingleInstanceFormStates[key] = formState;
            }

            form.Show();
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

        public static string StripInvalidXmlChars(this string text)
        {
            var validXml = new StringBuilder();
            foreach (var c in text.Where(XmlConvert.IsXmlChar))
            {
                validXml.Append(c);
            }

            return validXml.ToString();
        }

        public static List<ISelectable> ToSelectableList(this DataGridViewColumnCollection columns) => columns
            .Cast<DataGridViewColumn>()
            .Select(column => new SelectableColumn(column) as ISelectable)
            .ToList();

        public static void ApplyVisibility(this DataGridViewColumnCollection columns, List<ISelectable> selectables)
        {
            // Columns are keyed by HeaderText, which is not guaranteed unique (e.g. a visible column aliased to the
            // same header as a hidden backing column).  Keep the first occurrence rather than throwing on duplicates.
            var columnDict = new Dictionary<string, DataGridViewColumn>();
            foreach (var c in columns.Cast<DataGridViewColumn>())
            {
                columnDict.TryAdd(c.HeaderText, c);
            }

            foreach (var selectable in selectables)
            {
                if (columnDict.TryGetValue(selectable.Name, out var column))
                {
                    column.Visible = selectable.IsVisible;
                }
            }
        }

        public static DialogResult PromptColumnSelection(this DataGridView dgv)
        {
            using var frm = new SelectColumns() { Items = dgv.Columns.ToSelectableList() };
            frm.ApplyTheme(ThemeExtensions.CurrentTheme);
            frm.ShowDialog(dgv);
            if (frm.DialogResult == DialogResult.OK)
            {
                dgv.Columns.ApplyVisibility(frm.Items);
            }

            return frm.DialogResult;
        }

        public static List<ISelectable> ToSelectableList(this List<string> list)
        {
            return list.Select(s => new SelectableString(s) as ISelectable).ToList();
        }

        public static object DBNullToNull(this object obj)
        {
            return obj == DBNull.Value ? null : obj;
        }

        /// <summary>
        /// Infers the data type of a DataGridViewColumn based on the first non-null value in the column, or uses ValueType of the column where available.
        /// </summary>
        /// <param name="column">The DataGridViewColumn to infer the type for.</param>
        /// <returns>The inferred data type, or typeof(string) if the column is empty or the DataGridView is not set.</returns>
        public static Type InferColumnType(this DataGridViewColumn column)
        {
            if (column.ValueType != null)
            {
                return column.ValueType;
            }
            var dgv = column.DataGridView;
            if (dgv == null) return typeof(string);
            var firstNonNullValue = dgv.Rows.Cast<DataGridViewRow>()
                .Select(row => row.Cells[column.Name].Value)
                .FirstOrDefault(value => value != null);

            return firstNonNullValue?.GetType() ?? typeof(string);
        }
    }
}