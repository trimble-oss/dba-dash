using DBADashGUI.Theme;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace DBADashGUI.CollectionDates
{
    /// <summary>
    /// When a collection's runs count as a warning or critical - in the summary and the Collection Health report - for a
    /// collection on an instance, every collection on an instance, a collection on every instance or everything
    /// (dbo.CollectionHealthThresholds).  Each value can be set at the level or left to inherit from the level above.
    /// </summary>
    internal sealed class CollectionHealthThresholdsDialog : Form
    {
        private sealed record Scope(int InstanceID, string Reference, string Text)
        {
            public override string ToString() => Text;
        }

        /// <summary>A threshold that can be set at this level or inherited</summary>
        private sealed class Setting
        {
            public string Parameter { get; init; }
            public CheckBox Override { get; init; }
            public Control Value { get; init; }
            public Label Inherited { get; init; }

            public object GetValue() => Value switch
            {
                NumericUpDown num => num.DecimalPlaces == 0 ? (int)num.Value : num.Value,
                CheckBox chk => chk.Checked,
                _ => throw new InvalidOperationException()
            };

            public void SetValue(object value)
            {
                switch (Value)
                {
                    case NumericUpDown num:
                        num.Value = Math.Clamp(Convert.ToDecimal(value), num.Minimum, num.Maximum);
                        break;

                    case CheckBox chk:
                        chk.Checked = Convert.ToBoolean(value);
                        break;
                }
            }

            public static string Format(object value) => value switch
            {
                bool b => b ? "Yes" : "No",
                decimal d => d.ToString("0.##"),
                _ => Convert.ToString(value)
            };
        }

        private readonly ComboBox cboScope = new() { DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly List<Setting> settings = new();
        private bool loading;

        /// <param name="instanceID">The instance the row is for, or -1 for a row over every instance</param>
        /// <param name="instanceName">The instance's name for the scope list, or null</param>
        /// <param name="reference">The collection - not a step: steps use the collection's thresholds</param>
        public CollectionHealthThresholdsDialog(int instanceID, string instanceName, string reference)
        {
            Text = "Collection Health Thresholds";
            // Built in code, so nothing scales the sizes for the screen's DPI unless asked
            Size = LogicalToDeviceUnits(new Size(760, 640));
            MinimumSize = LogicalToDeviceUnits(new Size(560, 360));
            StartPosition = FormStartPosition.CenterParent;
            ShowIcon = false;
            ShowInTaskbar = false;
            MinimizeBox = false;
            MaximizeBox = false;

            var scopes = new List<Scope>();
            var hasInstance = instanceID > 0;
            var hasReference = !string.IsNullOrEmpty(reference);
            if (hasInstance && hasReference) scopes.Add(new Scope(instanceID, reference, $"{reference} on {instanceName}"));
            if (hasInstance) scopes.Add(new Scope(instanceID, "", $"All collections on {instanceName}"));
            if (hasReference) scopes.Add(new Scope(-1, reference, $"{reference} on all instances"));
            scopes.Add(new Scope(-1, "", "All collections on all instances"));
            cboScope.Items.AddRange(scopes.ToArray());

            // Everything but the buttons scrolls as one: the intro wraps to the width and the rows size to their text, so a
            // larger font or DPI makes it taller rather than cutting it off
            var lblIntro = new Label
            {
                AutoSize = true,
                Margin = new Padding(3, 3, 3, 8),
                Text = "A run of failures counts once it is long enough in runs and has gone on long enough since the collection last succeeded - so a collection that runs every minute has to fail for a while before it shows, while one that runs daily shows on its first failure.  " +
                       "The failure rate is over the summary's window (the last 24hrs, or since errors were acknowledged) or the report's range, once there are enough runs.  It is only critical while the latest run failed.\n" +
                       "Untick a value to inherit it from the level above."
            };

            var grid = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 4,
                Padding = new Padding(8)
            };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            var pnlContent = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            pnlContent.Controls.Add(grid);
            // A label only wraps up to its MaximumSize - the width there is, less the grid's padding and a scroll bar
            void WrapIntro() => lblIntro.MaximumSize = new Size(Math.Max(LogicalToDeviceUnits(200), pnlContent.ClientSize.Width - grid.Padding.Horizontal - lblIntro.Margin.Horizontal - SystemInformation.VerticalScrollBarWidth), 0);
            pnlContent.Resize += (_, _) => WrapIntro();

            AddRow(grid, lblIntro, 4);
            // The scope on a row of its own, so the combo sits next to its label rather than in the grid's columns
            var pnlScope = new TableLayoutPanel { AutoSize = true, ColumnCount = 2, RowCount = 1, Dock = DockStyle.Fill, Margin = Padding.Empty };
            pnlScope.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            pnlScope.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            pnlScope.Controls.Add(new Label { Text = "Apply to:", AutoSize = true, Margin = new Padding(3, 7, 3, 3) }, 0, 0);
            cboScope.Dock = DockStyle.Fill;
            pnlScope.Controls.Add(cboScope, 1, 0);
            AddRow(grid, pnlScope, 4);

            AddHeading(grid, "Warning - failures in a row");
            AddSetting(grid, "WarningConsecutiveFailures", "Failed runs in a row", Number(1, 100000, 0));
            AddSetting(grid, "WarningFailingMinutes", "And failing for at least (minutes)", Number(0, 1000000, 0));
            AddHeading(grid, "Critical - failures in a row");
            AddSetting(grid, "CriticalConsecutiveFailures", "Failed runs in a row", Number(1, 100000, 0));
            AddSetting(grid, "CriticalFailingMinutes", "And failing for at least (minutes)", Number(0, 1000000, 0));
            AddHeading(grid, "Failure rate");
            AddSetting(grid, "WarningFailedPct", "Warning: failed or partly failed %", Number(0, 100, 1));
            AddSetting(grid, "CriticalFailedPct", "Critical: failed %", Number(0, 100, 1));
            AddSetting(grid, "MinRunsForFailedPct", "Only with at least this many runs", Number(1, 100000, 0));
            AddHeading(grid, "Monitoring");
            AddSetting(grid, "Disabled", "Don't monitor (always N/A)", new CheckBox { AutoSize = true, Margin = new Padding(3, 6, 3, 3) });

            var bttnSave = MakeButton("Save");
            var bttnInheritAll = MakeButton("Inherit All");
            var bttnCancel = MakeButton("Close");
            bttnCancel.DialogResult = DialogResult.Cancel;
            var pnlButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Padding = new Padding(6)
            };
            pnlButtons.Controls.AddRange(new Control[] { bttnCancel, bttnSave, bttnInheritAll });
            AcceptButton = bttnSave;
            CancelButton = bttnCancel;

            Controls.Add(pnlContent);
            Controls.Add(pnlButtons);

            cboScope.SelectedIndexChanged += (_, _) => LoadScope();
            bttnSave.Click += (_, _) => Save();
            bttnInheritAll.Click += (_, _) =>
            {
                foreach (var setting in settings) setting.Override.Checked = false;
            };
            Load += (_, _) =>
            {
                this.ApplyTheme();
                WrapIntro();
                cboScope.SelectedIndex = 0;
            };
        }

        /// <summary>True once thresholds have been saved, so the caller can refresh</summary>
        public bool Saved { get; private set; }

        private NumericUpDown Number(decimal min, decimal max, int decimals) => new()
        {
            Minimum = min,
            Maximum = max,
            DecimalPlaces = decimals,
            Width = LogicalToDeviceUnits(100),
            TextAlign = HorizontalAlignment.Right
        };

        /// <summary>Sized to its text, so it isn't cut off at a larger font or DPI</summary>
        private Button MakeButton(string text) => new()
        {
            Text = text,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowOnly,
            MinimumSize = new Size(LogicalToDeviceUnits(90), 0),
            Padding = new Padding(6, 2, 6, 2)
        };

        private static void AddRow(TableLayoutPanel grid, Control control, int columnSpan)
        {
            var row = grid.RowCount++;
            grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            grid.Controls.Add(control, 0, row);
            grid.SetColumnSpan(control, columnSpan);
        }

        private static void AddHeading(TableLayoutPanel grid, string text) =>
            AddRow(grid, new Label { Text = text, AutoSize = true, Font = new Font(grid.Font, FontStyle.Bold), Margin = new Padding(3, 10, 3, 3) }, 4);

        private void AddSetting(TableLayoutPanel grid, string parameter, string text, Control value)
        {
            var row = grid.RowCount++;
            grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            var chk = new CheckBox { AutoSize = true, Margin = new Padding(3, 6, 3, 3) };
            var lblInherited = new Label { AutoSize = true, Margin = new Padding(3, 7, 3, 3) };
            grid.Controls.Add(new Label { Text = text, AutoSize = true, Margin = new Padding(3, 7, 3, 3) }, 0, row);
            grid.Controls.Add(chk, 1, row);
            grid.Controls.Add(value, 2, row);
            grid.Controls.Add(lblInherited, 3, row);
            var setting = new Setting { Parameter = parameter, Override = chk, Value = value, Inherited = lblInherited };
            chk.CheckedChanged += (_, _) => ShowOverride(setting);
            settings.Add(setting);
        }

        private Dictionary<string, object> inherited = new();

        /// <summary>Unticked shows the inherited value, read-only</summary>
        private void ShowOverride(Setting setting)
        {
            setting.Value.Enabled = setting.Override.Checked;
            if (!setting.Override.Checked && !loading && inherited.TryGetValue(setting.Parameter, out var value))
            {
                setting.SetValue(value);
            }
        }

        private Scope SelectedScope => (Scope)cboScope.SelectedItem;

        private void LoadScope()
        {
            var scope = SelectedScope;
            try
            {
                using var cn = new SqlConnection(Common.ConnectionString);
                using var cmd = new SqlCommand("dbo.CollectionHealthThresholds_Get", cn) { CommandType = CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("InstanceID", scope.InstanceID);
                cmd.Parameters.AddWithValue("Reference", scope.Reference);
                var dt = new DataTable();
                cn.Open();
                using (var rdr = cmd.ExecuteReader())
                {
                    dt.Load(rdr);
                }
                var row = dt.Rows[0];
                loading = true;
                inherited = new Dictionary<string, object>();
                foreach (var setting in settings)
                {
                    var inheritedValue = row["Inherited" + setting.Parameter];
                    inherited[setting.Parameter] = inheritedValue;
                    setting.Inherited.Text = $"Inherited: {Setting.Format(inheritedValue)}";
                    var configured = row[setting.Parameter];
                    setting.Override.Checked = configured != DBNull.Value;
                    setting.SetValue(configured == DBNull.Value ? inheritedValue : configured);
                    setting.Value.Enabled = setting.Override.Checked;
                }
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex, "Error loading collection health thresholds");
            }
            finally
            {
                loading = false;
            }
        }

        private void Save()
        {
            var scope = SelectedScope;
            try
            {
                using var cn = new SqlConnection(Common.ConnectionString);
                using var cmd = new SqlCommand("dbo.CollectionHealthThresholds_Upd", cn) { CommandType = CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("InstanceID", scope.InstanceID);
                cmd.Parameters.AddWithValue("Reference", scope.Reference);
                foreach (var setting in settings)
                {
                    cmd.Parameters.AddWithValue(setting.Parameter, setting.Override.Checked ? setting.GetValue() : DBNull.Value);
                }
                cn.Open();
                cmd.ExecuteNonQuery();
                Saved = true;
                MessageBox.Show($"Saved for {scope.Text}.  The summary picks it up when it next refreshes.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex, "Error saving collection health thresholds");
            }
        }
    }
}
