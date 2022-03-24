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

namespace DBADashGUI.Changes
{
    public partial class TraceFlagHistory : UserControl
    {
        public TraceFlagHistory()
        {
            InitializeComponent();
        }

        public string ConnectionString;
        public List<Int32> InstanceIDs;


        // Pivot data returned by TraceFlags_Get by trace flag
        private DataTable getTraceFlags()
        {
            var dt = new DataTable();
            dt.Columns.Add("Instance");
            using (var cn = new SqlConnection(ConnectionString))
            using (var cmd = new SqlCommand("dbo.TraceFlags_Get", cn) { CommandType = CommandType.StoredProcedure })
            {
                cn.Open();
                cmd.Parameters.AddWithValue("@InstanceIDs", string.Join(",", InstanceIDs));
                using (var rdr = cmd.ExecuteReader())
                {
                    string instance = "";
                    string previousInstance = "";
                    DataRow r = null;
                    while (rdr.Read())
                    {
                        instance = (string)rdr["InstanceDisplayName"];
                        if (instance != previousInstance)
                        {
                            r = dt.NewRow();
                            dt.Rows.Add(r);
                            r["Instance"] = instance;
                        }
                        if (rdr["TraceFlag"] != DBNull.Value)
                        {
                            var flag = (Int16)rdr["TraceFlag"];
                            var colName = "T" + flag.ToString();
                            var validFrom = (DateTime)rdr["ValidFrom"];
                            if (!dt.Columns.Contains(colName))
                            {
                                dt.Columns.Add(colName);
                            }
                            if (validFrom > DateTime.Parse("1900-01-01"))
                            {
                                r[colName] = "Y (" + validFrom.ToLocalTime().ToString("yyyy-MM-dd") + ")";
                            }
                            else
                            {
                                r[colName] = "Y";
                            }
                        }
                        previousInstance = instance;
                    }
                }
            }
            return dt;
        }

        private void refreshFlags()
        {
            var dt = getTraceFlags();
            dgvFlags.DataSource = dt;
            dgvFlags.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
        }

        private void refreshHistory()
        {
            var dt = getTraceFlagHistory();
            dgv.AutoGenerateColumns = false;
            dgv.DataSource = dt;
            dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
        }

        private DataTable getTraceFlagHistory()
        {
            using (var cn = new SqlConnection(ConnectionString))
            using (var cmd = new SqlCommand("dbo.TraceFlagHistory_Get", cn) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                cn.Open();
                cmd.Parameters.AddWithValue("@InstanceIDs", string.Join(",", InstanceIDs));
                DataTable dt = new DataTable();
                da.Fill(dt);
                Common.ConvertUTCToLocal(ref dt);
                return dt;
            }
        }

        public void RefreshData()
        {
            refreshFlags();
            refreshHistory();

        }

        private void tsRefreshHistory_Click(object sender, EventArgs e)
        {
            refreshHistory();
        }

        private void tsRefresh_Click(object sender, EventArgs e)
        {
            refreshFlags();
        }

        private void tsCopy_Click(object sender, EventArgs e)
        {
            Common.CopyDataGridViewToClipboard(dgvFlags);
        }

        private void tsCopyHistory_Click(object sender, EventArgs e)
        {
            Common.CopyDataGridViewToClipboard(dgv);
        }

        private void tsExcel_Click(object sender, EventArgs e)
        {
            Common.PromptSaveDataGridView(ref dgvFlags);
        }

        private void tsExcelHistory_Click(object sender, EventArgs e)
        {
            Common.PromptSaveDataGridView(ref dgv);
        }
    }


}
