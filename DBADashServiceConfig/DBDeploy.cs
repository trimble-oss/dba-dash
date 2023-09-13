using DBADash;
using Microsoft.Data.SqlClient;
using System;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DBADashGUI.Theme;

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

        public string DatabaseName
        {
            get => cboDatabase.Text;
            set => cboDatabase.Text = value;
        }

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

                    if (builder.InitialCatalog != null && builder.InitialCatalog.Length > 0 && builder.InitialCatalog != "master")
                    {
                        db = builder.InitialCatalog;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

        public string DB
        {
            get => cboDatabase.Text;
            set => cboDatabase.Text = value;
        }

        public string DeployScript
        {
            get => txtDeployScript.Text;
            set => txtDeployScript.Text = value;
        }

        private void DBDeploy_Load(object sender, EventArgs e)
        {
            try
            {
                CollectionConfig.ValidateDestination(new DBADashConnection(ConnectionString));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.DialogResult = DialogResult.Abort;
            }
            DbChanged();
        }

        private void BttnGenerate_Click(object sender, EventArgs e)
        {
            try
            {
                this.Cursor = Cursors.WaitCursor;
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
                this.Cursor = Cursors.Default;
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

        private void Deploy()
        {
            isCancel = false;
            DacpacUtility.DacpacService dac = new();
            string _db = DB;
            string _connectionString = ConnectionString;
            var t = Task.Run(() => dac.ProcessDacPac(_connectionString, _db, DBValidations.DACPackFile, dbVersionStatus.VersionStatus));
            lblNotice.Visible = true;
            bttnGenerate.Enabled = false;
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
            bttnGenerate.Enabled = false;
            if (t.IsCompleted)
            {
                if (!t.IsFaulted)
                {
                    MessageBox.Show("Deploy succeeded", "Deploy", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Deploy failed", "Deploy", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    var sb = new StringBuilder();
                    foreach (var item in dac.MessageList)
                    {
                        sb.AppendLine(item);
                    }
                    DeployScript = sb.ToString();
                }
            }
        }

        private void TxtConnectionString_Validated(object sender, EventArgs e)
        {
            DbChanged();
        }

        private void CboDatabase_DropDown(object sender, EventArgs e)
        {
            if (cboDatabase.Items.Count == 0)
            {
                try
                {
                    this.Cursor = Cursors.WaitCursor;
                    GetDatabases();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    this.Cursor = Cursors.Default;
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

        private void CboDatabase_SelectedIndexChanged(object sender, EventArgs e)
        {
            DbChanged();
        }

        private void DbChanged()
        {
            try
            {
                dbVersionStatus = DBValidations.VersionStatus(ConnectionString);
                bttnGenerate.Enabled = true;
                bttnDeploy.Enabled = true;
                if (dbVersionStatus.VersionStatus == DBValidations.DBVersionStatusEnum.CreateDB)
                {
                    lblVersionInfo.Text = "Create Database";
                    lblVersionInfo.ForeColor = System.Drawing.Color.Blue;
                }
                else if (dbVersionStatus.VersionStatus == DBValidations.DBVersionStatusEnum.OK)
                {
                    lblVersionInfo.Text = dbVersionStatus.DBVersion.ToString() + " (OK)";
                    lblVersionInfo.ForeColor = DashColors.Success;
                    bttnGenerate.Enabled = true;
                }
                else if (dbVersionStatus.VersionStatus == DBValidations.DBVersionStatusEnum.UpgradeRequired)
                {
                    lblVersionInfo.Text = dbVersionStatus.DBVersion.ToString() + " Upgrade to " + dbVersionStatus.DACVersion.ToString();
                    lblVersionInfo.ForeColor = DashColors.Fail;
                    bttnGenerate.Enabled = true;
                }
                else if (dbVersionStatus.VersionStatus == DBValidations.DBVersionStatusEnum.AppUpgradeRequired)
                {
                    lblVersionInfo.Text = dbVersionStatus.DBVersion.ToString() + "Newer than app:" + dbVersionStatus.DACVersion.ToString() + ". Upgrade app";
                    lblVersionInfo.ForeColor = DashColors.Fail;
                    bttnGenerate.Enabled = false;
                    bttnDeploy.Enabled = false;
                }
            }
            catch (Exception ex)
            {
                bttnGenerate.Enabled = false;
                bttnDeploy.Enabled = false;
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CboDatabase_Validated(object sender, EventArgs e)
        {
            DbChanged();
        }

        private void BttnDeploy_Click(object sender, EventArgs e)
        {
            Deploy();
        }
    }
}