using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SqlClient;
using DBADash;
namespace DBADashServiceConfig
{
    public partial class DBDeploy : Form
    {
        public DBDeploy()
        {
            InitializeComponent();
        }

      // public Version DACVersion;

        string _connectionString;

        public string DatabaseName
        {
            get
            {
                return cboDatabase.Text;
            }
            set
            {
                cboDatabase.Text = value;
            }
        }

        public string ConnectionString { 
            get {
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(_connectionString)
                {
                    InitialCatalog = cboDatabase.Text
                };
                return builder.ConnectionString;
            }
            set {
                string db = "DBADashDB";
                _connectionString = value;
                try
                {
                    SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(value);
                   
                    if (builder.InitialCatalog != null && builder.InitialCatalog.Length > 0 && builder.InitialCatalog != "master")
                    {
                        db= builder.InitialCatalog;
                    }
                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                DB = db;
            } 
        }

        public string ConnectionStringWithoutInitialCatalog
        {
            get
            {
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(ConnectionString)
                {
                    ConnectTimeout = 3,
                    InitialCatalog = ""
                };
                return builder.ConnectionString;
            }
        }

        private void getDatabases()
        {
            cboDatabase.Items.Clear();
            SqlConnection cn = new SqlConnection(ConnectionStringWithoutInitialCatalog);
            using (cn)
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand(@"SELECT name
FROM sys.databases
WHERE DATABASEPROPERTYEX(name, 'Updateability') = 'READ_WRITE'
AND database_id > 4 ", cn);
                var rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    cboDatabase.Items.Add((string)rdr[0]);
                }
            }
        }


        public string DB
        {
            get
            {
                return cboDatabase.Text;
            }
            set
            {
                cboDatabase.Text = value;
            }
        }

        public string DeployScript
        {
            get
            {
                return txtDeployScript.Text;
            }
            set
            {
                txtDeployScript.Text = value;
            }
        }

        private void DBDeploy_Load(object sender, EventArgs e)
        {

            dbChanged();
        }

        private void bttnGenerate_Click(object sender, EventArgs e)
        {
            try
            {
                this.Cursor = Cursors.WaitCursor;
                generateScript();
                lblNotice.Visible = true;
            }
            catch(Exception ex)
            {
                DeployScript = ex.Message;
            }
            finally
            {
                lblNotice.Visible = false;
                this.Cursor = Cursors.Default;
            }
        }

        private void generateScript()
        {
            isCancel = false;
            DacpacUtility.DacpacService dac = new DacpacUtility.DacpacService();
            string _db = DB;
            string _connectionString = ConnectionString;
            var t = Task.Run(()=> dac.GenerateDeployScript(_connectionString,_db, "DBADashDB.dacpac"));
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

        private void deploy()
        {
            isCancel = false;
            DacpacUtility.DacpacService dac = new DacpacUtility.DacpacService();
            string _db = DB;
            string _connectionString = ConnectionString;
            var t = Task.Run(() => dac.ProcessDacPac(_connectionString, _db, DBValidations.DACPackFile));
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
            bttnGenerate.Enabled= false;
            if (t.IsCompleted)
            {
                if(!t.IsFaulted)
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

        private void txtConnectionString_Validated(object sender, EventArgs e)
        {
            dbChanged();
        }



        private void cboDatabase_DropDown(object sender, EventArgs e)
        {
            if (cboDatabase.Items.Count == 0)
            {
                try
                {
                    this.Cursor = Cursors.WaitCursor;
                    getDatabases();
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
        bool isCancel;

        private void bttnCancel_Click(object sender, EventArgs e)
        {
            isCancel = true;
        }

        private void bttnCopy_Click(object sender, EventArgs e)
        {
            if (txtDeployScript.Text.Length > 0)
            {
                Clipboard.SetText(txtDeployScript.Text);
            }
        }

        private void txtDeployScript_Validated(object sender, EventArgs e)
        {
            bttnCopy.Enabled = txtDeployScript.Text.Length > 0;
        }

        private void txtDeployScript_TextChanged(object sender, EventArgs e)
        {
            bttnCopy.Enabled = txtDeployScript.Text.Length > 0;
        }

        private void cboDatabase_SelectedIndexChanged(object sender, EventArgs e)
        {
            dbChanged();
        }

        private void dbChanged()
        {
            try
            {
                var status = DBValidations.VersionStatus(ConnectionString);
                bttnGenerate.Enabled = true;
                bttnDeploy.Enabled = true;
                if (status.VersionStatus == DBValidations.DBVersionStatusEnum.CreateDB)
                {
                    lblVersionInfo.Text = "Create Database";
                    lblVersionInfo.ForeColor = System.Drawing.Color.Blue;
                }
                else if (status.VersionStatus == DBValidations.DBVersionStatusEnum.OK)
                {
                    lblVersionInfo.Text = status.DBVersion.ToString() + " (OK)";
                    lblVersionInfo.ForeColor = System.Drawing.Color.Green;
                    bttnGenerate.Enabled = true;
                }
                else if(status.VersionStatus == DBValidations.DBVersionStatusEnum.UpgradeRequired)
                {
                    lblVersionInfo.Text = status.DBVersion.ToString() + " Upgrade to " + status.DACVersion.ToString();
                    lblVersionInfo.ForeColor = System.Drawing.Color.Red;
                    bttnGenerate.Enabled = true;
                }
                else if(status.VersionStatus== DBValidations.DBVersionStatusEnum.AppUpgradeRequired)
                {
                    lblVersionInfo.Text = status.DBVersion.ToString() + "Newer than app:" + status.DACVersion.ToString() + ". Upgrade app";
                    lblVersionInfo.ForeColor = System.Drawing.Color.Red;
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

        private void cboDatabase_Validated(object sender, EventArgs e)
        {
            dbChanged();
        }

        private void bttnDeploy_Click(object sender, EventArgs e)
        {
            deploy();
        }
    }
}
