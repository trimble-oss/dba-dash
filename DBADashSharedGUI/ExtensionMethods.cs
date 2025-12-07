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
            var tcs = new TaskCompletionSource<object?>();
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
        public static void ShowSingleInstance(this Form form, bool trackFormState = true)
        {
            if (form.InvokeRequired)
            {
                form.BeginInvoke(new Action(() =>
                {
                    var task = ShowSingleInstanceAsync(form, trackFormState);
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
                _ = ShowSingleInstanceAsync(form, trackFormState);
            }
        }

        /// <summary>
        /// Shows a single instance of the form, closing any existing instance.
        /// </summary>
        /// <param name="form"></param>
        public static async Task ShowSingleInstanceAsync(this Form form, bool trackFormState = true)
        {
            if (!ChildFormSingleInstance)
            {
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
    }
}