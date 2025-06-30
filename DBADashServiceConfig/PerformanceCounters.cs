using DBADash;
using DBADashGUI.Theme;
using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBADashServiceConfig
{
    public partial class PerformanceCounters : Form
    {
        public PerformanceCounters()
        {
            InitializeComponent();
            this.ApplyTheme();
        }

        public string ConnectionString { get; set; }

        private DataTable dtAvailable;
        private DataTable dtSelected;
        private readonly DataTable defaultCounters = DBADash.PerformanceCounters.GetDefaultCountersDataTable();

        private async void PerformanceCounters_Load(object sender, EventArgs e)
        {
            try
            {
                dtSelected = DBADash.PerformanceCounters.PerformanceCountersDataTable();
                SetIsDefault();
                dgv.DataSource = new DataView(dtSelected);
                await LoadAvailableCounters();
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex);
            }
        }

        private async Task LoadAvailableCounters()
        {
            try
            {
                if(string.IsNullOrEmpty(ConnectionString))
                {
                    await PromptConnection();
                    return;
                }
                var builder = new SqlConnectionStringBuilder(ConnectionString);
                tsChangeConnection.ToolTipText = "Connected to " + builder.DataSource + "\nChange the connection to view the available counters on a different instance.";
                dtAvailable = await GetAvailableCountersAsync();
                dgv.AutoResizeColumns();
                var dtObjectCounter = GetObjectCounterDataTable(dtAvailable);
                dgvAvailable.DataSource = new DataView(dtObjectCounter);
                dgvAvailable.AutoResizeColumns();
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex, "Error loading available counters.  Use the Change Connection button to select a different instance.");
            }
        }

        private void SetIsDefault()
        {
            if (!dtSelected.Columns.Contains("IsDefault"))
            {
                dtSelected.Columns.Add("IsDefault");
            }
            foreach (DataRow row in dtSelected.Rows)
            {
                var objectName = (string)row["ObjectName"];
                var counterName = (string)row["CounterName"];
                var instanceName = (string)row["InstanceName"];
                row["IsDefault"] = IsDefaultCounter(objectName, counterName, instanceName);
            }
        }

        private bool IsDefaultCounter(string objectName, string counterName, string instanceName)
        {
            return CounterExists(defaultCounters, objectName, counterName, instanceName);
        }

        /// <summary>
        /// Gets a distinct list of object_name & counter_name from DataTable
        /// </summary>
        /// <param name="dtAvailable">All available performance counters DataTable.  object_name, counter_name & instance_name columns</param>
        /// <returns>DataTable containing unique Object Name and Counter Name columns</returns>
        private static DataTable GetObjectCounterDataTable(DataTable dtAvailable)
        {
            var objectCounterTable = new DataTable("ObjectCounter");
            objectCounterTable.Columns.Add("Object Name", typeof(string));
            objectCounterTable.Columns.Add("Counter Name", typeof(string));

            try
            {
                var uniqueObjectCounter = dtAvailable.AsEnumerable()
                    .Select(row => new
                    {
                        ObjectName = row.Field<string>("object_name"),
                        CounterName = row.Field<string>("counter_name")
                    })
                    .Distinct()
                    .OrderBy(x => x.ObjectName)
                    .ThenBy(x => x.CounterName);

                foreach (var combination in uniqueObjectCounter)
                {
                    var row = objectCounterTable.NewRow();
                    row["Object Name"] = combination.ObjectName;
                    row["Counter Name"] = combination.CounterName;
                    objectCounterTable.Rows.Add(row);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error creating unique combinations: {ex.Message}", ex);
            }

            return objectCounterTable;
        }

        private async Task<DataTable> GetAvailableCountersAsync()
        {
            var cn = new SqlConnection(ConnectionString);
            await using var cmd = new SqlCommand(DBADash.SqlStrings.AvailableCounters, cn);
            await cn.OpenAsync();
            await using var rdr = await cmd.ExecuteReaderAsync();
            var dt = new DataTable();
            dt.Load(rdr);
            return dt;
        }

        private void AvailableCounters_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvAvailable.SelectedRows.Count == 0) return;
            txtObjectName.Text = dgvAvailable.SelectedRows[0].Cells["Object Name"].Value?.ToString() ?? string.Empty;
            txtCounterName.Text = dgvAvailable.SelectedRows[0].Cells["Counter Name"].Value?.ToString() ?? string.Empty;
            cboInstance.DataSource = dtAvailable.Rows.Cast<DataRow>().Where(r => (string)r["object_name"] == txtObjectName.Text && (string)r["counter_name"] == txtCounterName.Text).Select(r => (string)r["instance_name"]).ToList();
        }

        private void AllInstances_CheckedChanged(object sender, EventArgs e)
        {
            cboInstance.Enabled = !chkAllInstances.Checked;
        }

        private void Add_Click(object sender, EventArgs e)
        {
            var instance = chkAllInstances.Checked ? "*" : cboInstance.Text;
            if (CounterExists(txtObjectName.Text, txtCounterName.Text, instance))
            {
                MessageBox.Show("The counter already exists in the selection", "Counter exists", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            AddCounter(txtObjectName.Text, txtCounterName.Text, instance);
        }

        private void AddCounter(string objectName, string counterName, string instanceName)
        {
            var isDefault = IsDefaultCounter(objectName, counterName, instanceName);
            dtSelected.Rows.Add(new object[]
                { objectName, counterName,instanceName, isDefault });
        }

        private bool CounterExists(string objectName, string counterName, string instanceName)
        {
            return CounterExists(dtSelected, objectName, counterName, instanceName);
        }

        private static bool CounterExists(DataTable dt, string objectName, string counterName, string instanceName)
        {
            return dt.Rows.Cast<DataRow>().Count(r =>
                string.Equals((string)r["ObjectName"], objectName, StringComparison.OrdinalIgnoreCase) &&
                string.Equals((string)r["CounterName"], counterName) && string.Equals((string)r["InstanceName"],
                    instanceName, StringComparison.OrdinalIgnoreCase)) > 0;
        }

        private void Search_TextChanged(object sender, EventArgs e)
        {
            var dv = (DataView)dgvAvailable.DataSource;
            var searchString = txtSearch.Text.Replace("'", "''");
            try
            {
                dv.RowFilter = $"[Object Name] LIKE '*{searchString}*' OR [Counter Name] LIKE '*{searchString}*'";
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex, "Error setting RowFilter");
            }
        }

        private void Save_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Save changes?", "Performance Counters", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            try
            {
                DBADash.PerformanceCounters.Save(dtSelected);
                this.DialogResult = DialogResult.OK;
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex, "Error saving changes");
            }
        }

        private void bttnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
        }

        private void Reset_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Reset performance counters back to default values, removing all customizations?", "Reset", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) != DialogResult.Yes) return;
            dtSelected = defaultCounters.Copy();
            SetIsDefault();
            dgv.DataSource = new DataView(dtSelected);
        }

        private void LoadDefaults_Click(object sender, EventArgs e)
        {
            var added = 0;
            foreach (DataRow row in defaultCounters.Rows)
            {
                var objectName = (string)row["ObjectName"];
                var counterName = (string)row["CounterName"];
                var instanceName = (string)row["InstanceName"];
                if (CounterExists(objectName, counterName, instanceName)) continue;
                AddCounter(objectName, counterName, instanceName);
                added++;
            }

            MessageBox.Show($"{added} counters added", "Load Defaults", MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void SelectedGrid_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (e.ColumnIndex == colRemove.Index)
            {
                dgv.Rows.RemoveAt(e.RowIndex);
            }
        }

        private async void Preview_Click(object sender, EventArgs e)
        {
            try
            {
                const string compatVersion = "9.0.0.0";
                var dt = await DBCollector.GetPerformanceCountersDataTableAsync(ConnectionString, compatVersion, DBADash.PerformanceCounters.DataTableToXML(dtSelected));
                var frm = new Form()
                {
                    Text = $"Preview ({dt.Rows.Count} metrics returned)",
                    Width = this.Width,
                    Height = this.Height,
                };
                var dgvPreview = new DataGridView()
                {
                    Dock = DockStyle.Fill,
                    AllowUserToAddRows = false,
                    AllowUserToDeleteRows = false,
                    ReadOnly = true
                };
                dgvPreview.DataSource = dt;
                frm.Load += (_, _) =>
                {
                    dgvPreview.AutoResizeColumnsWithMaxColumnWidth();
                };
                frm.Controls.Add(dgvPreview);
                frm.ApplyTheme();
                frm.Show();
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex, "Error loading preview");
            }
        }

        private async void ChangeConnection_Click(object sender, EventArgs e)
        {
            await PromptConnection();
        }

        private async Task PromptConnection()
        {
            var connectDialog = new DBConnection() { ConnectionString = ConnectionString };
            if (connectDialog.ShowDialog() != DialogResult.OK) return;
            ConnectionString = connectDialog.ConnectionString;
            await LoadAvailableCounters();
        }
    }
}