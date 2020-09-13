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
namespace DBAChecksServiceConfig
{
    public partial class DBDeploy : Form
    {
        public DBDeploy()
        {
            InitializeComponent();
        }



        public string ConnectionString { 
            get {
                return txtConnectionString.Text;
            }
            set {
                txtConnectionString.Text = value;
                string db = "DBAChecksDB";
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
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(ConnectionString);
                builder.ConnectTimeout = 3;
                builder.InitialCatalog = "";
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
            var t = Task.Run(()=> dac.GenerateDeployScript(_connectionString,_db, "DBAChecksDB.dacpac"));
            lblNotice.Visible = true;
            bttnCancel.Visible = true;
            bttnGenerate.Visible = false;
            while (!t.IsCompleted)
            {
                if (lblNotice.Text.Length > 50)
                {
                    lblNotice.Text = "Please Wait";
                }
                else
                {
                    lblNotice.Text = lblNotice.Text + ".";
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

        private void txtConnectionString_Validated(object sender, EventArgs e)
        {
            
        }

        string dbListConnectionString;

        private void cboDatabase_DropDown(object sender, EventArgs e)
        {
            if (dbListConnectionString != ConnectionString)
            {
                try
                {
                    this.Cursor = Cursors.WaitCursor;
                    getDatabases();
                    dbListConnectionString = ConnectionString;
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
    }
}
