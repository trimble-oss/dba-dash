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


        private void refreshFlags()
        {
            using (var cn = new SqlConnection(ConnectionString))
            {
                using (var cmd = new SqlCommand("dbo.TraceFlags_Get", cn) { CommandType = CommandType.StoredProcedure }) {
                    cn.Open();
                    cmd.Parameters.AddWithValue("@InstanceIDs", string.Join(",", InstanceIDs));
                    SqlDataReader rdr = cmd.ExecuteReader();
                    var dt = new DataTable();
                    dt.Columns.Add("Instance");
                    string instance = "";
                    string previousInstance = "";
                    DataRow r = null;
                    while (rdr.Read())
                    {
                        instance = (string)rdr["ConnectionID"];
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

                    dgvFlags.DataSource = dt;
                }
            }
        }

        private void refreshHistory()
        {
            using (var cn = new SqlConnection(ConnectionString))
            {
                using (var cmd = new SqlCommand("dbo.TraceFlagHistory_Get", cn) { CommandType = CommandType.StoredProcedure })
                {
                    cn.Open();
                    cmd.Parameters.AddWithValue("@InstanceIDs", string.Join(",", InstanceIDs));
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    Common.ConvertUTCToLocal(ref dt);
                    dgv.AutoGenerateColumns = false;
                    dgv.DataSource = dt;
                }
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
