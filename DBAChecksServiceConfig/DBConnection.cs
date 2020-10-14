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
using System.Data;
namespace DBAChecksServiceConfig
{
    public partial class DBConnection : Form
    {
        public DBConnection()
        {
            InitializeComponent();
        }

        string connectionString;

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
                return builder.ConnectionString;
            }
            set
            {
                connectionString = value;
                var builder = new SqlConnectionStringBuilder(connectionString);
                chkIntegratedSecurity.Checked = builder.IntegratedSecurity;
                txtUserName.Text = builder.UserID;
                txtPassword.Text = builder.Password;
                txtServerName.Text = builder.DataSource;
            
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
                testConnection(ConnectionString);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error connecting to data source:" +ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            this.DialogResult = DialogResult.OK;
        }
    }
}
