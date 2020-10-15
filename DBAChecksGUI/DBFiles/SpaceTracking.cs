using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SqlClient;
using LiveCharts;
using LiveCharts.Wpf;
using DBAChecksGUI.DBFiles;

namespace DBAChecksGUI
{
    public partial class SpaceTracking : UserControl
    {
        public SpaceTracking()
        {
            InitializeComponent();
        }


        public List<Int32> InstanceIDs;
    
        public Int32 DatabaseID = -1;

        public string DBName="";
        public string Instance="";

        public void RefreshData()
        {
            bool drillDownEnabled = DatabaseID > 0;
            tsBack.Enabled = false;
            diableHyperLinks(drillDownEnabled);
            refreshData();
        }

        private void refreshData()
        {
            SqlConnection cn = new SqlConnection(Common.ConnectionString);
            using (cn)
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("dbo.DBSpace_Get", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandTimeout = Properties.Settings.Default.CommandTimeout;
                cmd.Parameters.AddWithValue("@InstanceIDs", string.Join(",", InstanceIDs));
                if (DatabaseID > 0)
                {
                    cmd.Parameters.AddWithValue("@DatabaseID", DatabaseID);
                }
                if (Instance.Length > 0)
                {
                    cmd.Parameters.AddWithValue("@Instance", Instance);
                }
                if (DBName.Length > 0)
                {
                    cmd.Parameters.AddWithValue("@DBName", DBName);
                }
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                dgv.AutoGenerateColumns = false;
                dgv.DataSource = dt;

                pieChart1.Series.Clear();
                Func<ChartPoint, string> labelPoint = chartPoint =>
               string.Format("{0} ({1:P})", chartPoint.SeriesView.Title, chartPoint.Participation);
                SeriesCollection sc = new SeriesCollection();
                var other = (double)0;
                foreach (DataRow r in dt.Rows)
                {
                    var pct = (double)(decimal)r["Pct"];
                    var allocated = (double)(decimal)r["AllocatedGB"];
                    if (pct > 0.02)
                    {
                        var s = new PieSeries() { Title = (string)r["Grp"], Values = new ChartValues<double> { allocated }, LabelPoint = labelPoint, DataLabels = true, ToolTip = false };
                        sc.Add(s);
                    }
                    else
                    {
                        other += allocated;
                    }
                }
                if (other > 0)
                {
                    var s = new PieSeries() { Title = "{Other}", Values = new ChartValues<double> { other }, LabelPoint = labelPoint, DataLabels = true, ToolTip = false };
                    sc.Add(s);
                }

                pieChart1.Series = sc;
                pieChart1.LegendLocation = LegendLocation.Bottom;
            }
        }



        private void dgv_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                var row = (DataRowView)dgv.Rows[e.RowIndex].DataBoundItem;
         
                var selectedGroupValue = row["Grp"] == DBNull.Value ? "" : (string)row["Grp"];
                if (dgv.Columns[e.ColumnIndex] == Grp)
                {        
                    if (InstanceIDs.Count>1 && Instance.Length == 0)
                    {
                        Instance = selectedGroupValue;
                    }
                    else if (DBName.Length == 0)
                    {
                        DBName = selectedGroupValue;
                        diableHyperLinks(true);
                    }
                    else
                    {
                        diableHyperLinks(true);
                        return;
                    }
                    tsBack.Enabled = true;
                    refreshData();
                }
                else if(dgv.Columns[e.ColumnIndex] == History)
                {
                    var frm = new DBSpaceHistoryView();
                    frm.DatabaseID = DatabaseID;
                    frm.Instance =  Instance;
                    frm.DBName = DBName;
                    if (InstanceIDs.Count > 1 && Instance.Length == 0)
                    {
                        frm.Instance = selectedGroupValue;
                    }
                    else if (DBName.Length == 0)
                    {
                        frm.DBName = selectedGroupValue;
                    }
                    else
                    {
                        frm.FileName = selectedGroupValue;
                    }
                    if (frm.DatabaseID < 1)
                    {
                        frm.DatabaseID  = Common.GetDatabaseID(frm.Instance, frm.DBName);
                    }
                    frm.Show();
                }
            }
        }

        private void diableHyperLinks(bool disable)
        {
            if (disable)
            {
                Grp.LinkBehavior = LinkBehavior.NeverUnderline;
                Grp.LinkColor = Color.Black;
                Grp.ActiveLinkColor = Color.Black;
            }
            else
            {
                Grp.LinkColor = Color.Blue;
                Grp.ActiveLinkColor = Color.Blue;
                Grp.LinkBehavior = LinkBehavior.AlwaysUnderline;
            }
        }

        private void tsCopy_Click(object sender, EventArgs e)
        {
            Common.CopyDataGridViewToClipboard(dgv);
        }

        private void tsRefresh_Click(object sender, EventArgs e)
        {
            refreshData();
        }

        private void tsBack_Click(object sender, EventArgs e)
        {
            if (DBName.Length > 0)
            {
                DBName = "";
            }
            else
            {               
                Instance = "";
                tsBack.Enabled = false;
            }
            diableHyperLinks(false);
            refreshData();
        }

        private void tsHistory_Click(object sender, EventArgs e)
        {
            var frm = new DBSpaceHistoryView();
            frm.DatabaseID = DatabaseID;
            frm.Instance = Instance;
            frm.DBName = DBName;
            if (frm.DatabaseID < 1)
            {
                frm.DatabaseID = Common.GetDatabaseID(frm.Instance, frm.DBName);
            }
            frm.Show();
        }
    }
}
