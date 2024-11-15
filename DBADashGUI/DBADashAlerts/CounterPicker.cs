using DBADashGUI.Performance;
using DBADashGUI.Theme;
using System;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI.DBADashAlerts
{
    public partial class CounterPicker : Form
    {
        public CounterPicker()
        {
            InitializeComponent();
            this.ApplyTheme();
        }

        public AlertCounter SelectedCounter { get; set; }

        private bool SuppressSelectionChanged = true;

        private void CounterPicker_Load(object sender, EventArgs e)
        {
            chkAllInstances.Checked = SelectedCounter?.ApplyToAllInstances == true;
            var dt = CommonData.GetCounters();
            dgv.DataSource = new DataView(dt);
            dgv.AutoResizeColumnsWithMaxColumnWidth(DataGridViewAutoSizeColumnsMode.AllCells, 0.4F);
            var row = dgv.Rows.Cast<DataGridViewRow>().FirstOrDefault(r => (string)r.Cells["colObjectName"].Value == SelectedCounter?.ObjectName
                && (string)r.Cells["colCounterName"].Value == SelectedCounter?.CounterName
                && ((string)r.Cells["colInstanceName"].Value == SelectedCounter?.InstanceName || SelectedCounter?.ApplyToAllInstances == true)
                );
            SuppressSelectionChanged = false;
            if (row != null)
            {
                row.Selected = true;
            }

            UpdateSelected();
        }

        private void UpdateSelected() => lblSelectedCounter.Text = SelectedCounter == null ? "Selected Counter: None" : @$"Selected Counter: {SelectedCounter.ObjectName}\{SelectedCounter.CounterName}\{SelectedCounter.InstanceName}";

        private void Dgv_SelectionChanged(object sender, EventArgs e)
        {
            if (SuppressSelectionChanged) return;
            if (dgv.SelectedRows.Count != 1) return;
            SelectedCounter = new AlertCounter()
            {
                ObjectName = dgv.SelectedRows[0].Cells["colObjectName"].Value.ToString(),
                CounterName = dgv.SelectedRows[0].Cells["colCounterName"].Value.ToString(),
                InstanceName = dgv.SelectedRows[0].Cells["colInstanceName"].Value.ToString(),
                ApplyToAllInstances = chkAllInstances.Checked
            };
            UpdateSelected();
        }

        private void bttnOK_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
        }

        private void bttnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
        }

        private void Search_TextChanged(object sender, EventArgs e)
        {
            timer1.Stop();
            timer1.Start();
        }

        private void ApplySearch(object sender, EventArgs e)
        {
            var searchText = txtSearch.Text;
            timer1.Stop();
            var filter =
                $"(object_name LIKE '*{searchText}*' OR counter_name LIKE '*{searchText}*' OR instance_name LIKE '*{searchText}*')";
            try
            {
                ((DataView)dgv.DataSource).RowFilter = filter;
            }
            catch
            {
                //Ignore
            }
        }

        private void ChkAllInstances_CheckedChanged(object sender, EventArgs e)
        {
            if (SelectedCounter == null) return;
            SelectedCounter.ApplyToAllInstances = chkAllInstances.Checked;
            UpdateSelected();
        }
    }
}