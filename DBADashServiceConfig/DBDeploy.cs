using DBADash;
using DBADashGUI.Theme;
using Microsoft.Data.SqlClient;
using System;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBADashServiceConfig
{
    public partial class DBDeploy : Form
    {
        public DBDeploy()
        {
            InitializeComponent();
            this.ApplyTheme();
        }

        // public Version DACVersion;

        private string _connectionString;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string DatabaseName
        {
            get => cboDatabase.Text;
            set => cboDatabase.Text = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string ConnectionString
        {
            get
            {
                SqlConnectionStringBuilder builder = new(_connectionString)
                {
                    InitialCatalog = cboDatabase.Text
                };
                return builder.ConnectionString;
            }
            set
            {
                string db = "DBADashDB";
                _connectionString = value;
                try
                {
                    SqlConnectionStringBuilder builder = new(value);

                    if (!string.IsNullOrEmpty(builder.InitialCatalog) && builder.InitialCatalog != "master")
                    {
                        db = builder.InitialCatalog;
                    }
                }
                catch (Exception ex)
                {
                    CommonShared.ShowExceptionDialog(ex);
                }
                DB = db;
            }
        }

        private DBValidations.DBVersionStatus dbVersionStatus;

        public string ConnectionStringWithoutInitialCatalog
        {
            get
            {
                SqlConnectionStringBuilder builder = new(ConnectionString)
                {
                    ConnectTimeout = 3,
                    InitialCatalog = ""
                };
                return builder.ConnectionString;
            }
        }

        private void GetDatabases()
        {
            var sql = @"SELECT name
FROM sys.databases
WHERE DATABASEPROPERTYEX(name, 'Updateability') = 'READ_WRITE'
AND database_id > 4 ";
            cboDatabase.Items.Clear();
            using (SqlConnection cn = new(ConnectionStringWithoutInitialCatalog))
            using (SqlCommand cmd = new(sql, cn))
            {
                cn.Open();
                using var rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    cboDatabase.Items.Add((string)rdr[0]);
                }
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string DB
        {
            get => cboDatabase.Text;
            set => cboDatabase.Text = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string DeployScript
        {
            get => txtDeployScript.Text;
            set => txtDeployScript.Text = value;
        }

        private async void DBDeploy_Load(object sender, EventArgs e)
        {
            try
            {
                await CollectionConfig.ValidateDestinationAsync(new DBADashConnection(ConnectionString));
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex);
                DialogResult = DialogResult.Abort;
            }
            await DbChangedAsync();
        }

        private void BttnGenerate_Click(object sender, EventArgs e)
        {
            try
            {
                Cursor = Cursors.WaitCursor;
                GenerateScript();
                lblNotice.Visible = true;
            }
            catch (Exception ex)
            {
                DeployScript = ex.Message;
            }
            finally
            {
                lblNotice.Visible = false;
                Cursor = Cursors.Default;
            }
        }

        private void GenerateScript()
        {
            isCancel = false;
            DacpacUtility.DacpacService dac = new();
            string _db = DB;
            string _connectionString = ConnectionString;
            var t = Task.Run(() => dac.GenerateDeployScript(_connectionString, _db, "DBADashDB.dacpac"));
            lblNotice.Visible = true;
            bttnCancel.Visible = true;
            bttnGenerate.Visible = false;
            bttnDeploy.Enabled = false;
            while (!t.IsCompleted)
            {
                if (lblNotice.Text.Length > 50)
                {
                    lblNotice.Text = "Please Wait";
                }
                else
                {
                    lblNotice.Text += ".";
                }
                Application.DoEvents();
                if (isCancel)
                {
                    break;
                }
                System.Threading.Thread.Sleep(500);
            }
            lblNotice.Visible = false;
            bttnCancel.Visible = false;
            bttnGenerate.Visible = true;
            bttnDeploy.Enabled = true;
            if (t.IsCompleted)
            {
                string deployScript = t.Result;
                if (deployScript == null)
                {
                    var sb = new StringBuilder();
                    foreach (var item in dac.MessageList)
                    {
                        sb.AppendLine(item);
                    }
                    DeployScript = sb.ToString();
                }
                else
                {
                    DeployScript = deployScript;
                    MessageBox.Show("Please copy/paste the script into SSMS and run in SQLCMD mode", "Note", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private bool isDeploying;

        // DacFx raises its progress/message events on a worker thread - marshal each one onto the UI thread and append
        // it so progress is visible while the deployment runs, rather than only once it has finished.
        private void AppendMessage(string message)
        {
            if (IsDisposed || !IsHandleCreated) return;
            try
            {
                BeginInvoke(() =>
                {
                    if (txtDeployScript.IsDisposed) return;
                    txtDeployScript.AppendText(message + Environment.NewLine);
                });
            }
            // The dialog can be closed between the check above and BeginInvoke - just drop the UI output, the
            // deployment itself must carry on.
            catch (Exception ex) when (ex is InvalidOperationException or ObjectDisposedException)
            {
            }
        }

        private async Task DeployAsync()
        {
            isCancel = false;
            DacpacUtility.DacpacService dac = new();
            dac.MessageAdded += AppendMessage;
            string _db = DB;
            string _connectionString = ConnectionString;
            var status = dbVersionStatus.VersionStatus; // Can throw if validation hasn't completed - set isDeploying after this so it can't get stuck
            isDeploying = true;
            DeployScript = string.Empty;
            lblNotice.Text = "Please Wait";
            lblNotice.Visible = true;
            bttnGenerate.Enabled = false;
            bttnDeploy.Enabled = false;
            var t = Task.Run(() => dac.ProcessDacPac(_connectionString, _db, DBValidations.DACPackPath, status));
            try
            {
                while (!t.IsCompleted && !isCancel)
                {
                    lblNotice.Text = lblNotice.Text.Length > 50 ? "Please Wait" : lblNotice.Text + ".";
                    await Task.WhenAny(t, Task.Delay(500));
                }
            }
            finally
            {
                isDeploying = false;
                lblNotice.Visible = false;
            }
            if (!t.IsCompleted) return;
            if (!t.IsFaulted)
            {
                MessageBox.Show("Deploy succeeded", "Deploy", MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                // The messages are already in the textbox - streamed as the deployment ran
                CommonShared.ShowExceptionDialog("Deploy failed", string.Empty, string.Join(Environment.NewLine, dac.MessageList));
            }
        }

        private void CboDatabase_DropDown(object sender, EventArgs e)
        {
            if (cboDatabase.Items.Count == 0)
            {
                try
                {
                    Cursor = Cursors.WaitCursor;
                    GetDatabases();
                }
                catch (Exception ex)
                {
                    CommonShared.ShowExceptionDialog(ex);
                }
                finally
                {
                    Cursor = Cursors.Default;
                }
            }
        }

        private bool isCancel;

        private void BttnCancel_Click(object sender, EventArgs e)
        {
            isCancel = true;
        }

        private void BttnCopy_Click(object sender, EventArgs e)
        {
            if (txtDeployScript.Text.Length > 0)
            {
                Clipboard.SetText(txtDeployScript.Text);
            }
        }

        private void TxtDeployScript_Validated(object sender, EventArgs e)
        {
            bttnCopy.Enabled = txtDeployScript.Text.Length > 0;
        }

        private void TxtDeployScript_TextChanged(object sender, EventArgs e)
        {
            bttnCopy.Enabled = txtDeployScript.Text.Length > 0;
        }

        private async void CboDatabase_SelectedIndexChanged(object sender, EventArgs e)
        {
            await DbChangedAsync();
        }

        private async Task DbChangedAsync()
        {
            try
            {
                dbVersionStatus = await DBValidations.VersionStatusAsync(ConnectionString);
                // Clicking Deploy validates the database combo, so this can complete after the deployment has started -
                // don't re-enable the buttons (or show a mid-upgrade version) while it's running.
                if (isDeploying) return;
                bttnGenerate.Enabled = true;
                bttnDeploy.Enabled = true;
                if (dbVersionStatus.VersionStatus == DBValidations.DBVersionStatusEnum.CreateDB)
                {
                    lblVersionInfo.Text = "Create Database";
                    lblVersionInfo.ForeColor = System.Drawing.Color.Blue;
                }
                else if (dbVersionStatus.VersionStatus == DBValidations.DBVersionStatusEnum.OK)
                {
                    lblVersionInfo.Text = dbVersionStatus.DBVersion + " (OK)";
                    lblVersionInfo.ForeColor = DashColors.Success;
                    bttnGenerate.Enabled = true;
                }
                else if (dbVersionStatus.VersionStatus == DBValidations.DBVersionStatusEnum.UpgradeRequired)
                {
                    lblVersionInfo.Text = dbVersionStatus.DBVersion + " Upgrade to " + dbVersionStatus.DACVersion;
                    lblVersionInfo.ForeColor = DashColors.Fail;
                    bttnGenerate.Enabled = true;
                }
                else if (dbVersionStatus.VersionStatus == DBValidations.DBVersionStatusEnum.AppUpgradeRequired)
                {
                    lblVersionInfo.Text = dbVersionStatus.DBVersion + "Newer than app:" + dbVersionStatus.DACVersion + ". Upgrade app";
                    lblVersionInfo.ForeColor = DashColors.Fail;
                    bttnGenerate.Enabled = false;
                    bttnDeploy.Enabled = false;
                }
            }
            catch (Exception ex)
            {
                bttnGenerate.Enabled = false;
                bttnDeploy.Enabled = false;
                CommonShared.ShowExceptionDialog(ex);
            }
        }

        private async void CboDatabase_Validated(object sender, EventArgs e)
        {
            await DbChangedAsync();
        }

        private async void BttnDeploy_Click(object sender, EventArgs e)
        {
            try
            {
                await DeployAsync();
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex);
            }
        }
    }
}