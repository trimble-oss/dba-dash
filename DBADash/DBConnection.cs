using System;
using System.Windows.Forms;
using System.Data.SqlClient;
using System.Collections.Generic;

namespace DBADash
{
    public partial class DBConnection : Form
    {
        public DBConnection()
        {
            InitializeComponent();
        }

        string connectionString;
        public bool ValidateInitialCatalog = false;

        public string ConnectionString
        {
            get {
                var builder = new SqlConnectionStringBuilder(connectionString);
               
                if (chkIntegratedSecurity.Checked)
                {
                    builder.Remove("UserID");
                    builder.Remove("Password");
                    builder.Remove("UID");
                    builder.Remove("PWD");
                }
                else
                {
                    builder.UserID = txtUserName.Text;
                    builder.Password= txtPassword.Text;
                }
                builder.IntegratedSecurity = chkIntegratedSecurity.Checked;
                builder.DataSource= txtServerName.Text;
                builder.InitialCatalog = cboDatabase.Text;
                return builder.ConnectionString;
            }
            set
            {
                connectionString = value;
                var builder = new SqlConnectionStringBuilder(connectionString);
                chkIntegratedSecurity.Checked = builder.IntegratedSecurity;
                cboDatabase.Text = builder.InitialCatalog;
                txtUserName.Text = builder.UserID;
                txtPassword.Text = builder.Password;
                txtServerName.Text = builder.DataSource;
            
            }
        }

        public string ConnectionStringWithoutInitialCatalog
        {
            get
            {
                var builder = new SqlConnectionStringBuilder(ConnectionString);
                builder.Remove("Initial Catalog");
                return builder.ConnectionString;
            }
        }

        private void chkIntegratedSecurity_CheckedChanged(object sender, EventArgs e)
        {
            pnlAuth.Enabled = !chkIntegratedSecurity.Checked;
        }

        private void bttnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
        }

        public void testConnection(string connectionString)
        {
            SqlConnection cn = new SqlConnection(connectionString);
            using (cn)
            {
                cn.Open();
            }
          
        }

        private void bttnConnect_Click(object sender, EventArgs e)
        {
            try
            {
                this.Cursor = Cursors.WaitCursor;
                if (ValidateInitialCatalog)
                {
                    testConnection(ConnectionString);
                }
                else
                {
                    testConnection(ConnectionStringWithoutInitialCatalog); // Try without initial catalog as DB might not have been created yet
                }
            }
            catch (Exception ex)
            {          
                this.Cursor = Cursors.Default;
                MessageBox.Show("Error connecting to data source:" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
            this.DialogResult = DialogResult.OK;
        }

        private void DBConnection_Load(object sender, EventArgs e)
        {
            if (txtServerName.Text == "localhost")
            {
                txtServerName.Text = Environment.MachineName;
            }
        }

        private void cboDatabase_Dropdown(object sender, EventArgs e)
        {
            try
            {
                cboDatabase.Items.Clear();
                var DBs = GetDatabases(ConnectionString);
                foreach(string db in DBs)
                {
                    cboDatabase.Items.Add(db);
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }


        }

        public List<string> GetDatabases(string ConnectionString)
        {
            using (var cn = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = new SqlCommand("SELECT name FROM sys.databases WHERE state=0", cn))
            {
                cn.Open();
                var DBs = new List<string>();
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        DBs.Add((string)rdr[0]);
                    }
                    return DBs;
                }
            }           
        }
    }
}
