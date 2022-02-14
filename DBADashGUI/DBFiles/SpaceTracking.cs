using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using LiveCharts;
using LiveCharts.Wpf;
using DBADashGUI.DBFiles;

namespace DBADashGUI
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

        private DataTable getDBSpace()
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.DBSpace_Get", cn) { CommandType = CommandType.StoredProcedure, CommandTimeout = Properties.Settings.Default.CommandTimeout })
            using (var da = new SqlDataAdapter(cmd))
            {
                cn.Open();
                cmd.Parameters.AddWithValue("@InstanceIDs", string.Join(",", InstanceIDs));
                if (DatabaseID > 0)
                {
                    cmd.Parameters.AddWithValue("@DatabaseID", DatabaseID);
                }
                if (Instance != null && Instance.Length > 0)
                {
                    cmd.Parameters.AddWithValue("@Instance", Instance);
                }
                if (!string.IsNullOrEmpty(DBName))
                {
                    cmd.Parameters.AddWithValue("@DBName", DBName);
                }
                DataTable dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
         }

        private void refreshData()
        {
            var dt = getDBSpace();
            dgv.AutoGenerateColumns = false;
            dgv.DataSource = dt;
            dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            pieChart1.Series.Clear();
            string labelPoint(ChartPoint chartPoint) =>
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

        private void dgv_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                var row = (DataRowView)dgv.Rows[e.RowIndex].DataBoundItem;
         
                var selectedGroupValue = row["Grp"] == DBNull.Value ? "" : (string)row["Grp"];
                if (dgv.Columns[e.ColumnIndex] == Grp)
                {        
                    if (InstanceIDs.Count>1 && string.IsNullOrEmpty(Instance))
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
                else if(dgv.Columns[e.ColumnIndex] == colHistory)
                {
                    var frm = new DBSpaceHistoryView
                    {
                        DatabaseID = DatabaseID,
                        Instance = Instance,
                        DBName = DBName
                    };
                    if (InstanceIDs.Count > 1 &&  string.IsNullOrEmpty(Instance))
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
                        frm.DatabaseID  = CommonData.GetDatabaseID(frm.Instance, frm.DBName);
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
                Grp.LinkColor = DashColors.LinkColor;
                Grp.ActiveLinkColor = DashColors.LinkColor;
                Grp.LinkBehavior = LinkBehavior.AlwaysUnderline;
            }
        }

        private void tsCopy_Click(object sender, EventArgs e)
        {
            colHistory.Visible = false;
            Common.CopyDataGridViewToClipboard(dgv);
            colHistory.Visible = true;
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
            var frm = new DBSpaceHistoryView
            {
                DatabaseID = DatabaseID,
                Instance = Instance,
                DBName = DBName
            };
            if (frm.DatabaseID < 1)
            {
                frm.DatabaseID = CommonData.GetDatabaseID(frm.Instance, frm.DBName);
            }
            frm.Show();
        }

        private void tsExcel_Click(object sender, EventArgs e)
        {
            colHistory.Visible = false;
            Common.PromptSaveDataGridView(ref dgv);
            colHistory.Visible = true;
        }

        private void SpaceTracking_Load(object sender, EventArgs e)
        {
            Common.StyleGrid(ref dgv);
        }
    }
}
