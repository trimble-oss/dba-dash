using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBADashGUI.Performance
{
    public partial class SelectPerformanceCounters : Form
    {
        public SelectPerformanceCounters()
        {
            InitializeComponent();
        }

        public DataTable Counters;

        private Dictionary<int, Counter>selectedCounters;

        public Dictionary<int,Counter> SelectedCounters
        {
            get
            {
                var selected = new Dictionary<int, Counter>();
                foreach (DataRow row in Counters.Rows)
                {
                    var ctr = new Counter() { CounterID = (int)row["CounterID"], Avg = (bool)row["Avg"], Max = (bool)row["Max"], Min = (bool)row["Min"], SampleCount = (bool)row["SampleCount"],Current= (bool)row["Current"], Total = (bool)row["Total"], CounterName = (string)row["counter_name"], ObjectName=(string)row["object_name"], InstanceName = (string)row["instance_name"] };
                    if (ctr.GetAggColumns().Count > 0)
                    {
                        selected.Add(ctr.CounterID,ctr);
                    }
                }
                return selected;
            }
            set
            {
                selectedCounters = value;
            }
        }

        public DataTable GetCounters()
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand("dbo.Counters_Get", cn) { CommandType = CommandType.StoredProcedure })
                {
                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        da.Fill(dt);
                        dt.Columns.Add(new DataColumn("Total", typeof(bool)) { DefaultValue = false }) ;
                        dt.Columns.Add(new DataColumn("Avg", typeof(bool)) { DefaultValue = false });
                        dt.Columns.Add(new DataColumn("Max", typeof(bool)) { DefaultValue = false });
                        dt.Columns.Add(new DataColumn("Min", typeof(bool)) { DefaultValue = false });
                        dt.Columns.Add(new DataColumn("Current", typeof(bool)) { DefaultValue = false });
                        dt.Columns.Add(new DataColumn("SampleCount", typeof(bool)) { DefaultValue = false });
                        return dt;
                    }
                }
            }
        }

        private void SelectPerformanceCounters_Load(object sender, EventArgs e)
        {
            dgvCounters.CurrentCellDirtyStateChanged += dgvCounters_CurrentCellDirtyStateChanged;
            if (Counters == null || Counters.Rows.Count == 0)
            {
                Counters = GetCounters();
                if(selectedCounters!=null && selectedCounters.Count > 0)
                {
                    foreach(DataRow row in Counters.Rows)
                    {
                        int counterID = (int)row["CounterID"];                       
                        if (selectedCounters.ContainsKey(counterID))
                        {
                            var counter = selectedCounters[counterID];
                            row["Total"] = counter.Total;
                            row["Avg"] = counter.Avg;
                            row["Max"] = counter.Max;
                            row["Min"] = counter.Min;
                            row["Current"] = counter.Current;
                            row["SampleCount"] = counter.SampleCount;
                        }
                    }
                }
            }
            dgvCounters.AutoGenerateColumns = false;
            dgvCounters.DataSource =new DataView(Counters);
        }
     
        private void dgvCounters_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (dgvCounters.CurrentCell is DataGridViewCheckBoxCell)
            {
                dgvCounters.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        private void bttnOK_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            var dv = (DataView)dgvCounters.DataSource;
            dv.RowFilter = String.Format("counter_name LIKE '*{0}*' OR object_name LIKE '*{0}*' OR instance_name LIKE '*{0}*'", txtSearch.Text.Replace("'", ""));
        }


        private void bttnClear_Click(object sender, EventArgs e)
        {
            foreach(DataRow row in Counters.Rows)
            {
                row["Total"] = false;
                row["Avg"] = false;
                row["Max"] = false;
                row["Min"] = false;
                row["Current"] = false;
                row["SampleCount"] = false;
            }
        }

        private void bttnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
