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
            HookupNavigationButtons(this); // Handle mouse back button
        }


        public List<Int32> InstanceIDs;
    
        public Int32 DatabaseID = -1;

        public string DBName="";
        public string InstanceGroupName="";

        private void HandlePreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.XButton1:
                    NavigateBack();
                    break;
            }
        }
        private void HandleMouseDown(object sender, MouseEventArgs e)
        {
            switch (e.Button)
            {
                case MouseButtons.XButton1:
                    NavigateBack();
                    break;
            }
        }
        // https://stackoverflow.com/questions/41637248/how-do-i-capture-mouse-back-button-and-cause-it-to-do-something-else
        private void HookupNavigationButtons(Control ctrl)
        {
            for (int t = ctrl.Controls.Count - 1; t >= 0; t--)
            {
                Control c = ctrl.Controls[t];
                c.PreviewKeyDown -= HandlePreviewKeyDown;
                c.PreviewKeyDown += HandlePreviewKeyDown;
                c.MouseDown -= HandleMouseDown;
                c.MouseDown += HandleMouseDown;
                HookupNavigationButtons(c);
            }
        }


        public void RefreshData()
        {
            bool drillDownEnabled = DatabaseID > 0;
            tsBack.Enabled = false;
            DiableHyperLinks(drillDownEnabled);
            RefreshDataLocal();
        }

        private DataTable GetDBSpace()
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
                if (InstanceGroupName != null && InstanceGroupName.Length > 0)
                {
                    cmd.Parameters.AddWithValue("@InstanceGroupName", InstanceGroupName);
                }
                if (!string.IsNullOrEmpty(DBName))
                {
                    cmd.Parameters.AddWithValue("@DBName", DBName);
                }
                DataTable dt = new();
                da.Fill(dt);
                return dt;
            }
         }

        private void RefreshDataLocal()
        {
            tsContext.Text = InstanceGroupName + (String.IsNullOrEmpty(DBName) ? "" : " \\ " + DBName);
            var dt = GetDBSpace();
            dgv.AutoGenerateColumns = false;
            dgv.DataSource = dt;
            dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            pieChart1.Series.Clear();

            static string labelPoint(ChartPoint chartPoint) =>
            string.Format("{0} ({1:P})", chartPoint.SeriesView.Title, chartPoint.Participation);
            SeriesCollection sc = new();
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

        private void Dgv_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                var row = (DataRowView)dgv.Rows[e.RowIndex].DataBoundItem;
         
                var selectedGroupValue = row["Grp"] == DBNull.Value ? "" : (string)row["Grp"];
                if (dgv.Columns[e.ColumnIndex] == Grp)
                {        
                    if (InstanceIDs.Count>1 && string.IsNullOrEmpty(InstanceGroupName))
                    {
                        InstanceGroupName = selectedGroupValue;
                    }
                    else if (DBName.Length == 0)
                    {
                        DBName = selectedGroupValue;
                        DiableHyperLinks(true);
                    }
                    else
                    {
                        DiableHyperLinks(true);
                        return;
                    }
                    tsBack.Enabled = true;
                    RefreshDataLocal();
                }
                else if(dgv.Columns[e.ColumnIndex] == colHistory)
                {
                    var frm = new DBSpaceHistoryView
                    {
                        DatabaseID = DatabaseID,
                        InstanceGroupName = InstanceGroupName,
                        DBName = DBName
                    };
                    if (InstanceIDs.Count > 1 &&  string.IsNullOrEmpty(InstanceGroupName))
                    {
                        frm.InstanceGroupName = selectedGroupValue;
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
                        frm.DatabaseID  = CommonData.GetDatabaseID(frm.InstanceGroupName, frm.DBName);
                    }
                    frm.Show();
                }
            }
        }

        private void DiableHyperLinks(bool disable)
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

        private void TsCopy_Click(object sender, EventArgs e)
        {
            colHistory.Visible = false;
            Common.CopyDataGridViewToClipboard(dgv);
            colHistory.Visible = true;
        }

        private void TsRefresh_Click(object sender, EventArgs e)
        {
            RefreshDataLocal();
        }

        private void TsBack_Click(object sender, EventArgs e)
        {
            NavigateBack();
        }

        private void NavigateBack()
        {
            if (!tsBack.Enabled)
            {
                return;
            }
            if (DBName.Length > 0)
            {
                DBName = "";
            }
            else
            {
                InstanceGroupName = "";
                tsBack.Enabled = false;
            }
            DiableHyperLinks(false);
            RefreshDataLocal();
        }

        private void TsHistory_Click(object sender, EventArgs e)
        {
            var frm = new DBSpaceHistoryView
            {
                DatabaseID = DatabaseID,
                InstanceGroupName = InstanceGroupName,
                DBName = DBName
            };
            if (frm.DatabaseID < 1)
            {
                frm.DatabaseID = CommonData.GetDatabaseID(frm.InstanceGroupName, frm.DBName);
            }
            frm.Show();
        }

        private void TsExcel_Click(object sender, EventArgs e)
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
