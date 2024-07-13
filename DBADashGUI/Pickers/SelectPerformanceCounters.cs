using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using DBADashGUI.Theme;

namespace DBADashGUI.Performance
{
    public partial class SelectPerformanceCounters : Form, IThemedControl
    {
        public SelectPerformanceCounters()
        {
            InitializeComponent();
        }

        public DataTable Counters;

        private Dictionary<int, Counter> selectedCounters;

        public Dictionary<int, Counter> SelectedCounters
        {
            get
            {
                return (from DataRow row in Counters.Rows
                    select new Counter()
                    {
                        CounterID = (int)row["CounterID"],
                        Avg = (bool)row["Avg"],
                        Max = (bool)row["Max"],
                        Min = (bool)row["Min"],
                        SampleCount = (bool)row["SampleCount"],
                        Current = (bool)row["Current"],
                        Total = (bool)row["Total"],
                        CounterName = (string)row["counter_name"],
                        ObjectName = (string)row["object_name"],
                        InstanceName = (string)row["instance_name"]
                    }
                    into ctr
                    where ctr.GetAggColumns().Count > 0
                    select ctr).ToDictionary(ctr => ctr.CounterID);
            }
            set => selectedCounters = value;
        }

        private void AddAggSelectionColumns()
        {
            Counters.Columns.Add(new DataColumn("Total", typeof(bool)) { DefaultValue = false });
            Counters.Columns.Add(new DataColumn("Avg", typeof(bool)) { DefaultValue = false });
            Counters.Columns.Add(new DataColumn("Max", typeof(bool)) { DefaultValue = false });
            Counters.Columns.Add(new DataColumn("Min", typeof(bool)) { DefaultValue = false });
            Counters.Columns.Add(new DataColumn("Current", typeof(bool)) { DefaultValue = false });
            Counters.Columns.Add(new DataColumn("SampleCount", typeof(bool)) { DefaultValue = false });
        }

        private void SelectPerformanceCounters_Load(object sender, EventArgs e)
        {
            dgvCounters.CurrentCellDirtyStateChanged += DgvCounters_CurrentCellDirtyStateChanged;
            if (Counters == null || Counters.Rows.Count == 0)
            {
                Counters = CommonData.GetCounters();
                AddAggSelectionColumns();
                if (selectedCounters is { Count: > 0 })
                {
                    foreach (DataRow row in Counters.Rows)
                    {
                        var counterID = (int)row["CounterID"];
                        if (!selectedCounters.TryGetValue(counterID, out var counter)) continue;
                        row["Total"] = counter.Total;
                        row["Avg"] = counter.Avg;
                        row["Max"] = counter.Max;
                        row["Min"] = counter.Min;
                        row["Current"] = counter.Current;
                        row["SampleCount"] = counter.SampleCount;
                    }
                }
            }
            dgvCounters.AutoGenerateColumns = false;
            dgvCounters.DataSource = new DataView(Counters);
        }

        private void DgvCounters_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (dgvCounters.CurrentCell is DataGridViewCheckBoxCell)
            {
                dgvCounters.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        private void BttnOK_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private void TxtSearch_TextChanged(object sender, EventArgs e)
        {
            var dv = (DataView)dgvCounters.DataSource;
            dv.RowFilter = string.Format("counter_name LIKE '*{0}*' OR object_name LIKE '*{0}*' OR instance_name LIKE '*{0}*'", txtSearch.Text.Replace("'", ""));
        }

        private void BttnClear_Click(object sender, EventArgs e)
        {
            foreach (DataRow row in Counters.Rows)
            {
                row["Total"] = false;
                row["Avg"] = false;
                row["Max"] = false;
                row["Min"] = false;
                row["Current"] = false;
                row["SampleCount"] = false;
            }
        }

        private void BttnCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        public void ApplyTheme(BaseTheme theme)
        {
            foreach (Control child in Controls)
            {
                child.ApplyTheme(theme);
            }
            panel1.BackColor = theme.PanelBackColor;
            panel1.ForeColor = theme.ForegroundColor;
            lblSearch.ForeColor = theme.ForegroundColor;
        }
    }
}