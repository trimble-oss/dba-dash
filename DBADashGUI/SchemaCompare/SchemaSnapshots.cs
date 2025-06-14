using DBADash;
using DBADashGUI.Interface;
using DBADashGUI.Messaging;
using DBADashGUI.Theme;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using static DBADashGUI.DiffControl;

namespace DBADashGUI.Changes
{
    public partial class SchemaSnapshots : UserControl, INavigation, ISetContext, ISetStatus
    {
        public SchemaSnapshots()
        {
            InitializeComponent();
        }

        private int currentSummaryPage = 1;
        private int InstanceID;
        private string InstanceName;
        private int DatabaseID;
        private List<int> InstanceIDs;
        private DBADashContext CurrentContext;

        private static bool IsExternalDiffConfigured => !string.IsNullOrWhiteSpace(Properties.Settings.Default.DiffToolBinaryPath) &&
                                                        !string.IsNullOrWhiteSpace(Properties.Settings.Default.DiffToolArguments);

        public bool CanNavigateBack => tsBack.Enabled;

        private static DataSet DdlSnapshotDiff(DateTime snapshotDateUTC, int databaseID)
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.DDLSnapshotDiff_Get", cn) { CommandType = CommandType.StoredProcedure };
            using var da = new SqlDataAdapter(cmd);
            cn.Open();
            cmd.Parameters.AddWithValue("DatabaseID", databaseID);
            var p = cmd.Parameters.AddWithValue("SnapshotDate", snapshotDateUTC);
            p.DbType = DbType.DateTime2;
            DataSet ds = new();
            da.Fill(ds);

            return ds;
        }

        private void GvSnapshots_SelectionChanged(object sender, EventArgs e)
        {
            colExternalDiff.Visible = IsExternalDiffConfigured;
            if (gvSnapshots.SelectedRows.Count != 1) return;
            var row = (DataRowView)gvSnapshots.SelectedRows[0].DataBoundItem;
            var snapshotDateUTC = ((DateTime)row["SnapshotDate"]).AppTimeZoneToUtc();
            var databaseID = (int)row["DatabaseID"];

            var ds = DdlSnapshotDiff(snapshotDateUTC, databaseID);
            gvSnapshotsDetail.AutoGenerateColumns = false;
            gvSnapshotsDetail.DataSource = new DataView(ds.Tables[0]);
            gvSnapshotsDetail.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
        }

        private void GvSnapshotsDetail_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || (e.ColumnIndex != colView.Index && e.ColumnIndex != colDiff.Index && e.ColumnIndex != colExternalDiff.Index)) return;
            var row = (DataRowView)gvSnapshotsDetail.Rows[e.RowIndex].DataBoundItem;
            var ddl = "";
            if (row["NewDDLID"] != DBNull.Value)
            {
                ddl = Common.DDL((long)row["NewDDLID"]);
            }
            var ddlOld = "";
            if (row["OldDDLID"] != DBNull.Value)
            {
                ddlOld = Common.DDL((long)row["OldDDLID"]);
            }
            var diffToolBinaryPath = Properties.Settings.Default.DiffToolBinaryPath;
            var diffToolArguments = Properties.Settings.Default.DiffToolArguments;
            if (e.ColumnIndex == colExternalDiff.Index && !string.IsNullOrWhiteSpace(diffToolBinaryPath) && !string.IsNullOrWhiteSpace(diffToolArguments))
            {
                var oldDDLTempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".sql");
                var newDDLTempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".sql");

                File.WriteAllText(oldDDLTempPath, ddlOld);
                File.WriteAllText(newDDLTempPath, ddl);

                using var process = new Process();
                process.StartInfo.FileName = diffToolBinaryPath;
                process.StartInfo.Arguments = diffToolArguments.Replace("$OLD$", $"\"{oldDDLTempPath}\"", true, null).Replace("$NEW$", $"\"{newDDLTempPath}\"", true, null);
                process.StartInfo.UseShellExecute = true;

                try
                {
                    process.Start();
                }
                catch (Exception ex)
                {
                    CommonShared.ShowExceptionDialog(ex);
                }
            }
            else
            {
                var mode = e.ColumnIndex == colDiff.Index ? ViewMode.Diff : ViewMode.Code;
                var frm = new Diff();
                frm.SetText(ddlOld, ddl, mode);
                frm.Show();
            }
        }

        public void SetContext(DBADashContext _context)
        {
            InstanceID = _context.InstanceID;
            InstanceName = _context.InstanceName;
            DatabaseID = _context.DatabaseID;
            InstanceIDs = _context.InstanceIDs.ToList();
            lblStatus.Text = "";
            tsTrigger.Visible = _context.CanMessage;
            CurrentContext = _context;
            colTriggerSnapshot.Visible = DBADashUser.AllowMessaging;
            RefreshData();
        }

        public void RefreshData()
        {
            if (InvokeRequired)
            {
                Invoke(RefreshData);
                return;
            }

            if (InstanceID > 0 || InstanceName is { Length: > 0 })
            {
                LoadSnapshots();
                tsBack.Enabled = true;
            }
            else
            {
                tsBack.Enabled = false;
                LoadInstanceSummary();
            }
        }

        public void SetStatus(string message, string tooltip, Color color)
        {
            lblStatus.InvokeSetStatus(message, tooltip, color);
        }

        private DataTable DdlSnapshotInstanceSummary()
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.DDLSnapshotInstanceSummary_Get", cn) { CommandType = CommandType.StoredProcedure };
            using var da = new SqlDataAdapter(cmd);
            cn.Open();
            cmd.Parameters.AddWithValue("InstanceIDs", string.Join(",", InstanceIDs));
            cmd.Parameters.AddWithValue("ShowHidden", InstanceIDs.Count == 1 || Common.ShowHidden);
            var dt = new DataTable();
            da.Fill(dt);
            DateHelper.ConvertUTCToAppTimeZone(ref dt);
            return dt;
        }

        private void LoadInstanceSummary()
        {
            splitSnapshotSummary.Visible = false;
            dgvInstanceSummary.Visible = true;
            var dt = DdlSnapshotInstanceSummary();
            dgvInstanceSummary.DataSource = new DataView(dt);
            dgvInstanceSummary.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
        }

        private DataTable GetDDLSnapshots(int pageNum)
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.DDLSnapshots_Get", cn) { CommandType = CommandType.StoredProcedure };
            using var da = new SqlDataAdapter(cmd);
            cn.Open();
            cmd.Parameters.AddWithValue("DatabaseID", DatabaseID);
            cmd.Parameters.AddWithValue("InstanceGroupName", InstanceName);
            cmd.Parameters.AddWithValue("PageSize", currentSummaryPage);
            cmd.Parameters.AddWithValue("PageNumber", pageNum);

            var dt = new DataTable();
            da.Fill(dt);
            DateHelper.ConvertUTCToAppTimeZone(ref dt);
            return dt;
        }

        private void LoadSnapshots(int pageNum = 1)
        {
            splitSnapshotSummary.Visible = true;
            dgvInstanceSummary.Visible = false;
            gvSnapshotsDetail.DataSource = null;
            currentSummaryPage = int.Parse(tsSummaryPageSize.Text);
            var dt = GetDDLSnapshots(pageNum);
            gvSnapshots.AutoGenerateColumns = false;
            gvSnapshots.DataSource = new DataView(dt);
            gvSnapshots.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            tsSummaryPageNum.Text = "Page " + pageNum;
            tsSummaryBack.Enabled = (pageNum > 1);
            tsSummaryNext.Enabled = dt.Rows.Count == currentSummaryPage;
            currentSummaryPage = pageNum;
        }

        private void TsSummaryBack_Click(object sender, EventArgs e)
        {
            LoadSnapshots(currentSummaryPage - 1);
        }

        private void TsSummaryNext_Click(object sender, EventArgs e)
        {
            LoadSnapshots(currentSummaryPage + 1);
        }

        private void TsSummaryPageSize_Validated(object sender, EventArgs e)
        {
            if (int.Parse(tsSummaryPageSize.Text) != currentSummaryPage)
            {
                LoadSnapshots();
            }
        }

        private void DgvInstanceSummary_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != colInstance.Index) return;
            InstanceName = (string)dgvInstanceSummary.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;
            RefreshData();
        }

        private void SchemaSnapshots_Load(object sender, EventArgs e)
        {
            gvSnapshots.ApplyTheme();
            gvSnapshotsDetail.ApplyTheme();
            dgvInstanceSummary.ApplyTheme();
            splitSnapshotSummary.Dock = DockStyle.Fill;
        }

        private async void GvSnapshots_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            if (e.ColumnIndex == colDB.Index)
            {
                var row = (DataRowView)gvSnapshots.Rows[e.RowIndex].DataBoundItem;
                DatabaseID = (int)row["DatabaseID"];
                RefreshData();
            }
            else if (e.ColumnIndex == colExport.Index)
            {
                var row = (DataRowView)gvSnapshots.Rows[e.RowIndex].DataBoundItem;
                Export(row);
            }
            else if (e.ColumnIndex == colTriggerSnapshot.Index)
            {
                var row = (DataRowView)gvSnapshots.Rows[e.RowIndex].DataBoundItem;
                await TriggerSchemaSnapshot(row);
            }
        }

        private async Task TriggerSchemaSnapshot(DataRowView row)
        {
            var _instanceID = (int)row["InstanceID"];
            var db = (string)row["DB"];
            var tempContext = (DBADashContext)CurrentContext.Clone();
            tempContext.InstanceID = _instanceID;
            if (tempContext.CanMessage)
            {
                await CollectionMessaging.TriggerCollection(_instanceID, new List<CollectionType> { CollectionType.SchemaSnapshot }, this, db);
            }
            else
            {
                SetStatus("Collections can't be triggered for this instance.", "Enable messaging in the service configuration tool to allow communication", DashColors.Warning);
            }
        }

        private void Export(DataRowView row)
        {
            var dbid = (int)row["DatabaseID"];
            var db = (string)row["DB"];
            var snapshotDate = (DateTime)row["SnapshotDate"];
            using var ofd = new FolderBrowserDialog() { Description = "Select a folder" };
            if (ofd.ShowDialog() != DialogResult.OK) return;
            var folder = Path.Combine(ofd.SelectedPath, InstanceName + "_" + db + "_" + snapshotDate.ToString("yyyyMMdd_HHmmss"));
            Directory.CreateDirectory(folder);
            try
            {
                ExportSchema(folder, dbid, snapshotDate);
                MessageBox.Show("Export Completed", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Process.Start(new ProcessStartInfo()
                {
                    FileName = folder,
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex);
            }
        }

        private static void ExportSchema(string folder, int DBID, DateTime SnapshotDate)
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.DBSchemaAtDate_Get", cn) { CommandType = CommandType.StoredProcedure, CommandTimeout = 300 };
            cn.Open();
            cmd.Parameters.AddWithValue("DBID", DBID);
            var pSnapshotDate = cmd.Parameters.AddWithValue("SnapshotDate", SnapshotDate);
            pSnapshotDate.SqlDbType = SqlDbType.DateTime2;
            using var rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                var schema = (string)rdr["SchemaName"];
                var name = (string)rdr["ObjectName"];
                var objType = (string)rdr["TypeDescription"];
                var subFolder = Path.Combine(Path.Combine(folder, Common.StripInvalidFileNameChars(schema)), objType);
                var filePath = Path.Combine(subFolder, Common.StripInvalidFileNameChars(name) + ".sql");
                if (!Directory.Exists(subFolder))
                {
                    Directory.CreateDirectory(subFolder);
                }
                var bDDL = (byte[])rdr["DDL"];
                var sql = SMOBaseClass.Unzip(bDDL);
                File.WriteAllText(filePath, sql);
            }
        }

        private void TsRefresh_Click(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void TsBack_Click(object sender, EventArgs e)
        {
            NavigateBack();
        }

        public bool NavigateBack()
        {
            if (CanNavigateBack)
            {
                if (DatabaseID > 0)
                {
                    DatabaseID = -1;
                }
                else if (InstanceName.Length > 0 || InstanceID > 0)
                {
                    InstanceName = "";
                    InstanceID = -1;
                }
                RefreshData();
                return true;
            }
            else
            {
                return false;
            }
        }

        private async void tsTrigger_Click(object sender, EventArgs e)
        {
            if (InstanceID <= 0)
            {
                lblStatus.Text = "Please select a single instance to trigger a collection";
            }

            if (MessageBox.Show("This collection might take some time to process depending on the number of databases and objects within those databases.  Are you sure you want to trigger a collection?", "Trigger Collection",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                await CollectionMessaging.TriggerCollection(InstanceID, CollectionType.SchemaSnapshot, this);
            }
        }
    }
}