using DBADash;
using DBADashGUI.Theme;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBADashServiceConfig
{
    /// <summary>
    /// Picker for OS-level (perfmon) performance counters collected via WMI.  Used for both the global
    /// counter list (<see cref="CollectionConfig.PerfmonCounters"/>) and per-instance overrides
    /// (<see cref="DBADashSource.PerfmonCounters"/>).  Available counters are discovered live from a host
    /// via <see cref="PerfmonCounterDiscovery"/>.
    /// </summary>
    public class ManagePerfmonCounters : Form
    {
        /// <summary>The list being edited.  Read back after DialogResult.OK.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public List<PerfmonCounter> Counters { get; set; } = new();

        /// <summary>Global list, supplied in per-instance mode to enable the "Load global" button.  Null in global mode.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public List<PerfmonCounter> GlobalCounters { get; set; }

        /// <summary>Default host to discover counters from (e.g. the instance's computer name).</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string ComputerName { get; set; }

        private TextBox txtHost;
        private TextBox txtSearch;
        private DataGridView dgvAvailable;
        private DataGridView dgvSelected;
        private ComboBox cboInstance;
        private ComboBox cboObjectFilter;
        private CheckBox chkAllInstances;
        private Button bttnAdd;
        private Button bttnGlobal;
        private Button bttnDefaults;
        private Button bttnClear;
        private Button bttnDiscover;
        private RadioButton rbInherit;
        private RadioButton rbCustom;
        private RadioButton rbDisabled;
        private FlowLayoutPanel pnlMode;

        private DataTable dtAvailable;
        private DataTable dtSelected;
        private string lastInstanceClass;

        private enum PerfMode { Inherit, Custom, Disabled }

        private PerfMode lastMode;
        private List<PerfmonCounter> customWorking; // preserves custom-mode edits across mode switches
        private bool suppressModeEvents;

        private const string AllObjects = "(All objects)";

        public ManagePerfmonCounters()
        {
            Text = "Perfmon Counters";
            // AutoScaleMode.None stops the form re-scaling (and growing) on the first real layout pass
            // after Discover populates the grid, which was pushing the top controls off-screen.
            AutoScaleMode = AutoScaleMode.None;
            Width = 1000;
            Height = 700;
            MinimumSize = new Size(700, 500);
            StartPosition = FormStartPosition.CenterParent;
            BuildLayout();
            Load += ManagePerfmonCounters_Load;
        }

        private void BuildLayout()
        {
            var tlp = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 6,
                Padding = new Padding(8)
            };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            tlp.RowStyles.Add(new RowStyle(SizeType.AutoSize));          // 0 mode (per-instance only)
            tlp.RowStyles.Add(new RowStyle(SizeType.AutoSize));          // 1 discover/search
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 50));       // 2 available grid
            tlp.RowStyles.Add(new RowStyle(SizeType.AutoSize));          // 3 add controls
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 50));       // 4 selected grid
            tlp.RowStyles.Add(new RowStyle(SizeType.AutoSize));          // 5 buttons

            // Row 0 - mode selector (only shown in per-instance mode; visibility set at Load)
            pnlMode = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = false, Visible = false };
            rbInherit = new RadioButton { Text = "Inherit global list", AutoSize = true, Margin = new Padding(3, 5, 15, 3) };
            rbCustom = new RadioButton { Text = "Custom counters", AutoSize = true, Margin = new Padding(3, 5, 15, 3) };
            rbDisabled = new RadioButton { Text = "Disabled (collect none)", AutoSize = true, Margin = new Padding(3, 5, 3, 3) };
            rbInherit.CheckedChanged += ModeChanged;
            rbCustom.CheckedChanged += ModeChanged;
            rbDisabled.CheckedChanged += ModeChanged;
            pnlMode.Controls.Add(rbInherit);
            pnlMode.Controls.Add(rbCustom);
            pnlMode.Controls.Add(rbDisabled);
            tlp.Controls.Add(pnlMode, 0, 0);

            // Row 1 - host + discover + search
            // WrapContents so the row flows onto a second line rather than pushing controls off-screen.
            var pnlTop = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true };
            pnlTop.Controls.Add(new Label { Text = "Host:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(3, 8, 3, 3) });
            txtHost = new TextBox { Width = 200, Margin = new Padding(3, 5, 3, 3) };
            pnlTop.Controls.Add(txtHost);
            bttnDiscover = new Button { Text = "Discover", AutoSize = true, Margin = new Padding(3, 3, 15, 3) };
            bttnDiscover.Click += Discover_Click;
            pnlTop.Controls.Add(bttnDiscover);
            pnlTop.Controls.Add(new Label { Text = "Object:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(3, 8, 3, 3) });
            cboObjectFilter = new ComboBox { Width = 170, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(3, 5, 3, 3) };
            cboObjectFilter.SelectedIndexChanged += (_, _) => ApplyAvailableFilter();
            pnlTop.Controls.Add(cboObjectFilter);
            pnlTop.Controls.Add(new Label { Text = "Search:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(3, 8, 3, 3) });
            txtSearch = new TextBox { Width = 180, Margin = new Padding(3, 5, 3, 3) };
            txtSearch.TextChanged += (_, _) => ApplyAvailableFilter();
            pnlTop.Controls.Add(txtSearch);
            tlp.Controls.Add(pnlTop, 0, 1);

            // Row 2 - available counters grid
            dgvAvailable = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            dgvAvailable.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Object", DataPropertyName = "ObjectName" });
            dgvAvailable.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Counter", DataPropertyName = "CounterName" });
            dgvAvailable.SelectionChanged += Available_SelectionChanged;
            tlp.Controls.Add(dgvAvailable, 0, 2);

            // Row 3 - add controls
            var pnlAdd = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = false };
            pnlAdd.Controls.Add(new Label { Text = "Instance:", AutoSize = true, Margin = new Padding(3, 8, 3, 3) });
            cboInstance = new ComboBox { Width = 200, DropDownStyle = ComboBoxStyle.DropDown, Margin = new Padding(3, 5, 3, 3) };
            pnlAdd.Controls.Add(cboInstance);
            chkAllInstances = new CheckBox { Text = "All instances (*)", AutoSize = true, Margin = new Padding(3, 7, 3, 3) };
            chkAllInstances.CheckedChanged += (_, _) => cboInstance.Enabled = !chkAllInstances.Checked;
            pnlAdd.Controls.Add(chkAllInstances);
            bttnAdd = new Button { Text = "Add ↓", AutoSize = true, Margin = new Padding(15, 3, 3, 3) };
            bttnAdd.Click += Add_Click;
            pnlAdd.Controls.Add(bttnAdd);
            tlp.Controls.Add(pnlAdd, 0, 3);

            // Row 4 - selected counters grid
            dgvSelected = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            dgvSelected.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Object", DataPropertyName = "ObjectName" });
            dgvSelected.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Counter", DataPropertyName = "CounterName" });
            dgvSelected.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Instance", DataPropertyName = "InstanceName" });
            dgvSelected.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "WMI Class", DataPropertyName = "WmiClass", AutoSizeMode = DataGridViewAutoSizeColumnMode.None, Width = 220 });
            dgvSelected.Columns.Add(new DataGridViewButtonColumn { HeaderText = "", Text = "Remove", UseColumnTextForButtonValue = true, Name = "Remove", AutoSizeMode = DataGridViewAutoSizeColumnMode.None, Width = 80 });
            dgvSelected.CellContentClick += Selected_CellContentClick;
            tlp.Controls.Add(dgvSelected, 0, 4);

            // Row 5 - buttons
            var pnlButtons = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, FlowDirection = FlowDirection.RightToLeft };
            var bttnCancel = new Button { Text = "Cancel", AutoSize = true, Margin = new Padding(3) };
            bttnCancel.Click += (_, _) => DialogResult = DialogResult.Cancel;
            pnlButtons.Controls.Add(bttnCancel);
            var bttnSave = new Button { Text = "Save", AutoSize = true, Margin = new Padding(3) };
            bttnSave.Click += Save_Click;
            pnlButtons.Controls.Add(bttnSave);
            bttnDefaults = new Button { Text = "Load defaults", AutoSize = true, Margin = new Padding(3) };
            bttnDefaults.Click += (_, _) => AddRange(PerfmonCounter.DefaultCounters());
            pnlButtons.Controls.Add(bttnDefaults);
            bttnClear = new Button { Text = "Clear all", AutoSize = true, Margin = new Padding(3) };
            bttnClear.Click += (_, _) => dtSelected.Clear();
            pnlButtons.Controls.Add(bttnClear);
            // Visibility set at Load, since GlobalCounters is assigned by the caller after construction.
            bttnGlobal = new Button { Text = "Load global", AutoSize = true, Margin = new Padding(3), Visible = false };
            bttnGlobal.Click += (_, _) => AddRange(GlobalCounters ?? new List<PerfmonCounter>());
            pnlButtons.Controls.Add(bttnGlobal);
            tlp.Controls.Add(pnlButtons, 0, 5);

            Controls.Add(tlp);
            this.ApplyTheme();
        }

        private void ManagePerfmonCounters_Load(object sender, EventArgs e)
        {
            txtHost.Text = ComputerName ?? string.Empty;
            var perInstance = GlobalCounters != null;
            pnlMode.Visible = perInstance;
            bttnGlobal.Visible = perInstance;

            dtSelected = BuildSelectedTable();
            dgvSelected.DataSource = new DataView(dtSelected);

            // Remember any originally-specified custom counters as the base for Custom mode.
            customWorking = Counters is { Count: > 0 } ? new List<PerfmonCounter>(Counters) : null;

            if (perInstance)
            {
                // null=inherit, empty=disabled, populated=custom.  Set the radio without firing the
                // transition handler, then seed the grid for the initial mode.
                lastMode = Counters == null ? PerfMode.Inherit
                    : Counters.Count == 0 ? PerfMode.Disabled
                    : PerfMode.Custom;
                suppressModeEvents = true;
                (lastMode == PerfMode.Inherit ? rbInherit : lastMode == PerfMode.Disabled ? rbDisabled : rbCustom).Checked = true;
                suppressModeEvents = false;
                SeedGridForMode(lastMode);
            }
            else
            {
                LoadGrid(Counters); // global mode: edit the global list directly
            }
            UpdateModeUI();
        }

        private void ModeChanged(object sender, EventArgs e)
        {
            if (suppressModeEvents) return;
            if (sender is RadioButton { Checked: false }) return; // act only on the newly-selected radio
            var newMode = CurrentMode();
            if (newMode == lastMode) return;
            if (lastMode == PerfMode.Custom) customWorking = ExtractGrid(); // preserve edits before overwriting
            lastMode = newMode;
            SeedGridForMode(newMode);
            UpdateModeUI();
        }

        private PerfMode CurrentMode() =>
            rbCustom.Checked ? PerfMode.Custom : rbDisabled.Checked ? PerfMode.Disabled : PerfMode.Inherit;

        private void SeedGridForMode(PerfMode mode)
        {
            switch (mode)
            {
                case PerfMode.Inherit: LoadGrid(GlobalCounters); break;                    // show the global list
                case PerfMode.Disabled: LoadGrid(null); break;                             // clear the grid
                case PerfMode.Custom: LoadGrid(customWorking ?? GlobalCounters); break;    // custom, else global base
            }
        }

        private void LoadGrid(List<PerfmonCounter> counters)
        {
            dtSelected.Clear();
            foreach (var c in counters ?? new List<PerfmonCounter>())
            {
                AddSelectedRow(c);
            }
        }

        private List<PerfmonCounter> ExtractGrid() =>
            dtSelected.Rows.Cast<DataRow>()
                .Where(r => r.RowState != DataRowState.Deleted)
                .Select(r => new PerfmonCounter
                {
                    ObjectName = (string)r["ObjectName"],
                    CounterName = (string)r["CounterName"],
                    InstanceName = (string)r["InstanceName"],
                    WmiClass = r["WmiClass"] == DBNull.Value ? null : (string)r["WmiClass"],
                    WmiProperty = r["WmiProperty"] == DBNull.Value ? null : (string)r["WmiProperty"],
                    CounterType = r["CounterType"] == DBNull.Value ? 0 : (int)r["CounterType"],
                    BaseWmiProperty = r["BaseWmiProperty"] == DBNull.Value ? null : (string)r["BaseWmiProperty"]
                })
                .ToList();

        /// <summary>Per-instance: only "Custom" is editable; global mode is always editable.</summary>
        private void UpdateModeUI()
        {
            var editable = !pnlMode.Visible || rbCustom.Checked;
            foreach (var ctl in new Control[]
                     { txtHost, bttnDiscover, cboObjectFilter, txtSearch, dgvAvailable, cboInstance,
                       chkAllInstances, bttnAdd, dgvSelected, bttnDefaults, bttnClear, bttnGlobal })
            {
                if (ctl != null) ctl.Enabled = editable;
            }
        }

        private static DataTable BuildSelectedTable()
        {
            var dt = new DataTable("Selected");
            dt.Columns.Add("ObjectName", typeof(string));
            dt.Columns.Add("CounterName", typeof(string));
            dt.Columns.Add("InstanceName", typeof(string));
            dt.Columns.Add("WmiClass", typeof(string));
            dt.Columns.Add("WmiProperty", typeof(string));
            dt.Columns.Add("CounterType", typeof(int));      // persisted so collection needs no schema lookup
            dt.Columns.Add("BaseWmiProperty", typeof(string));
            return dt;
        }

        private void AddSelectedRow(PerfmonCounter c)
        {
            // Normalise null -> "*" once, so the existence check and the stored row agree.  A null
            // InstanceName (e.g. hand-edited config) checked against an already-stored "*" would otherwise
            // miss and add a duplicate row.
            var instance = c.InstanceName ?? "*";
            if (SelectedExists(c.EffectiveObjectName, c.EffectiveCounterName, instance)) return;
            dtSelected.Rows.Add(c.EffectiveObjectName, c.EffectiveCounterName, instance, c.WmiClass, c.WmiProperty, c.CounterType, c.BaseWmiProperty);
        }

        private bool SelectedExists(string objectName, string counterName, string instanceName)
        {
            return dtSelected.Rows.Cast<DataRow>().Any(r =>
                string.Equals((string)r["ObjectName"], objectName, StringComparison.OrdinalIgnoreCase) &&
                string.Equals((string)r["CounterName"], counterName, StringComparison.OrdinalIgnoreCase) &&
                string.Equals((string)r["InstanceName"], instanceName, StringComparison.OrdinalIgnoreCase));
        }

        private void AddRange(IEnumerable<PerfmonCounter> counters)
        {
            var added = 0;
            foreach (var c in counters)
            {
                if (SelectedExists(c.EffectiveObjectName, c.EffectiveCounterName, c.InstanceName ?? "*")) continue;
                AddSelectedRow(c);
                added++;
            }
            MessageBox.Show($"{added} counter(s) added", "Perfmon Counters", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private async void Discover_Click(object sender, EventArgs e)
        {
            var host = txtHost.Text.Trim();
            if (string.IsNullOrEmpty(host))
            {
                MessageBox.Show("Enter a host name to discover counters from.", "Discover", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            // Disable the button as the busy indicator - deliberately no wait cursor, so a slow/hung
            // WMI call (bounded to 30s by PerfmonCounterDiscovery) can never leave the form's cursor stuck.
            bttnDiscover.Enabled = false;
            var originalText = bttnDiscover.Text;
            bttnDiscover.Text = "Discovering…";
            try
            {
                var classes = await Task.Run(() => PerfmonCounterDiscovery.DiscoverClasses(host));
                dtAvailable = new DataTable("Available");
                dtAvailable.Columns.Add("ObjectName", typeof(string));
                dtAvailable.Columns.Add("CounterName", typeof(string));
                dtAvailable.Columns.Add("WmiClass", typeof(string));
                dtAvailable.Columns.Add("WmiProperty", typeof(string));
                dtAvailable.Columns.Add("CounterType", typeof(int));
                dtAvailable.Columns.Add("BaseWmiProperty", typeof(string));
                foreach (var cls in classes)
                {
                    foreach (var counter in cls.Counters)
                    {
                        dtAvailable.Rows.Add(cls.ObjectName, counter.WmiProperty, cls.WmiClass, counter.WmiProperty, counter.CounterType, counter.BaseWmiProperty);
                    }
                }
                dgvAvailable.DataSource = new DataView(dtAvailable);

                var objects = classes.Select(c => c.ObjectName).Distinct().OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
                objects.Insert(0, AllObjects);
                cboObjectFilter.DataSource = objects;
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex, $"Error discovering counters on '{host}'.  Check the host name and that WMI is accessible.");
            }
            finally
            {
                bttnDiscover.Text = originalText;
                bttnDiscover.Enabled = true;
            }
        }

        private void ApplyAvailableFilter()
        {
            if (dgvAvailable.DataSource is not DataView dv) return;
            var filters = new List<string>();
            var search = txtSearch.Text.Replace("'", "''");
            if (!string.IsNullOrEmpty(search))
            {
                filters.Add($"(ObjectName LIKE '*{search}*' OR CounterName LIKE '*{search}*')");
            }
            if (cboObjectFilter.SelectedItem is string obj && obj != AllObjects)
            {
                filters.Add($"ObjectName = '{obj.Replace("'", "''")}'");
            }
            try
            {
                dv.RowFilter = string.Join(" AND ", filters);
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex, "Error setting filter");
            }
        }

        private async void Available_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvAvailable.SelectedRows.Count == 0 || dgvAvailable.SelectedRows[0].DataBoundItem is not DataRowView drv) return;
            var wmiClass = (string)drv.Row["WmiClass"];
            if (wmiClass == lastInstanceClass) return; // avoid re-querying WMI on every row within the same class
            lastInstanceClass = wmiClass;
            var host = txtHost.Text.Trim();
            if (string.IsNullOrEmpty(host)) return;
            // Instance list is a convenience only (the instance can be typed), so this must never drive
            // the wait cursor or block the UI - if the WMI query is slow/hangs the combo just stays as-is.
            try
            {
                var instances = await Task.Run(() => PerfmonCounterDiscovery.DiscoverInstances(host, wmiClass));
                if (wmiClass != lastInstanceClass) return; // selection moved on while we were querying - don't clobber the newer list
                cboInstance.DataSource = instances;
            }
            catch
            {
                // Non-fatal: leave the combo empty and let the user type the instance (or use All instances).
                cboInstance.DataSource = null;
            }
        }

        private void Add_Click(object sender, EventArgs e)
        {
            if (dgvAvailable.SelectedRows.Count == 0 || dgvAvailable.SelectedRows[0].DataBoundItem is not DataRowView drv)
            {
                MessageBox.Show("Select a counter to add.", "Add", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var instance = chkAllInstances.Checked ? "*" : (cboInstance.Text ?? string.Empty);
            var objectName = (string)drv.Row["ObjectName"];
            var counterName = (string)drv.Row["CounterName"];
            if (SelectedExists(objectName, counterName, instance))
            {
                MessageBox.Show("The counter already exists in the selection.", "Counter exists", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            dtSelected.Rows.Add(objectName, counterName, instance, (string)drv.Row["WmiClass"], (string)drv.Row["WmiProperty"], drv.Row["CounterType"], drv.Row["BaseWmiProperty"]);
        }

        private void Selected_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (dgvSelected.Columns[e.ColumnIndex].Name == "Remove")
            {
                var drv = (DataRowView)dgvSelected.Rows[e.RowIndex].DataBoundItem;
                drv.Row.Delete();
            }
        }

        private void Save_Click(object sender, EventArgs e)
        {
            if (pnlMode.Visible) // per-instance tri-state
            {
                if (rbInherit.Checked) { Counters = null; DialogResult = DialogResult.OK; return; }        // inherit global
                if (rbDisabled.Checked) { Counters = new List<PerfmonCounter>(); DialogResult = DialogResult.OK; return; } // collect none
            }
            Counters = ExtractGrid();
            DialogResult = DialogResult.OK;
        }
    }
}
