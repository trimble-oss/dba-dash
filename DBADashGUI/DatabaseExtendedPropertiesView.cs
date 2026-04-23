using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI
{
    public partial class DatabaseExtendedPropertiesView : UserControl, ISetContext
    {
        public DatabaseExtendedPropertiesView()
        {
            InitializeComponent();
        }

        private List<int> InstanceIDs { get; set; }
        private int DatabaseID { get; set; }

        public void SetContext(DBADashContext context)
        {
            InstanceIDs = context.InstanceIDs.ToList();
            DatabaseID = context.DatabaseID;
            RefreshData();
        }

        public void RefreshData()
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.DatabaseExtendedProperties_Get", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("InstanceIDs", string.Join(",", InstanceIDs));
            cmd.Parameters.AddWithValue("DatabaseID", DatabaseID);
            try
            {
                cn.Open();
                using var da = new SqlDataAdapter(cmd);
                var dt = new DataTable();
                da.Fill(dt);
                dgv.DataSource = dt;
                dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            }
            catch (SqlException ex)
            {
                throw new System.Exception($"Error connecting to database '{cn.Database}' on server '{cn.DataSource}': {ex.Message}", ex);
            }
        }

        private void TsRefresh_Click(object sender, System.EventArgs e)
        {
            RefreshData();
        }
    }
}
