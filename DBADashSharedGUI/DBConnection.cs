using DBADashGUI.Theme;
using Microsoft.Data.SqlClient;
using System.Runtime.Versioning;
using DBADashSharedGUI;

namespace DBADash
{
    [SupportedOSPlatform("windows")]
    public partial class DBConnection : Form
    {
        public DBConnection()
        {
            InitializeComponent();
            this.ApplyTheme();
            PopulateCombos();
        }

        private string connectionString = string.Empty;

        public bool ValidateInitialCatalog = false;

        private SqlAuthenticationMethod SelectedAuthenticationMethod => cboAuthType.SelectedItem is KeyValuePair<SqlAuthenticationMethod, string> selectedPair ? selectedPair.Key : SqlAuthenticationMethod.ActiveDirectoryIntegrated;
        private SqlConnectionEncryptOption SelectedEncryptionOption => cboEncryption.SelectedItem is KeyValuePair<SqlConnectionEncryptOption, string> selectedPair ? selectedPair.Key : SqlConnectionEncryptOption.Mandatory;

        private bool IsPasswordSupported => SelectedAuthenticationMethod is SqlAuthenticationMethod.SqlPassword
            or SqlAuthenticationMethod.ActiveDirectoryPassword
            or SqlAuthenticationMethod.ActiveDirectoryServicePrincipal;

        private bool IsUserNameSupported => SelectedAuthenticationMethod is SqlAuthenticationMethod.SqlPassword
            or SqlAuthenticationMethod.ActiveDirectoryInteractive or SqlAuthenticationMethod.ActiveDirectoryPassword
            or SqlAuthenticationMethod.ActiveDirectoryServicePrincipal or SqlAuthenticationMethod.ActiveDirectoryManagedIdentity;

        private static Dictionary<SqlAuthenticationMethod, string> AuthenticationMethods =>
            new()
            {
                { SqlAuthenticationMethod.ActiveDirectoryIntegrated, "Windows Authentication" },
                { SqlAuthenticationMethod.SqlPassword, "SQL Server Authentication" },
                { SqlAuthenticationMethod.ActiveDirectoryInteractive, "Microsoft Entra MFA" },
                { SqlAuthenticationMethod.ActiveDirectoryPassword ,"Microsoft Entra Password"},
                { SqlAuthenticationMethod.ActiveDirectoryServicePrincipal, "Microsoft Entra Service Principal" },
                { SqlAuthenticationMethod.ActiveDirectoryManagedIdentity, "Microsoft Entra Managed Identity"},
                { SqlAuthenticationMethod.ActiveDirectoryDefault, "Microsoft Entra Default" }
            };

        private static Dictionary<SqlConnectionEncryptOption, string> EncryptionOptions => new()
        {
            { SqlConnectionEncryptOption.Mandatory, "Mandatory" },
            { SqlConnectionEncryptOption.Optional, "Optional" },
            { SqlConnectionEncryptOption.Strict, "Strict (SQL Server 2022 & Azure SQL)" }
        };

        private void PopulateCombos()
        {
            cboAuthType.DataSource = new BindingSource(AuthenticationMethods, null);
            cboAuthType.DisplayMember = "Value";
            cboAuthType.ValueMember = "Key";

            cboEncryption.DataSource = new BindingSource(EncryptionOptions, null);
            cboEncryption.DisplayMember = "Value";
            cboEncryption.ValueMember = "Key";
        }

        public string ConnectionString
        {
            get
            {
                var builder = new SqlConnectionStringBuilder(connectionString)
                {
                    UserID = txtUserName.Text,
                    Password = txtPassword.Text,
                    Authentication = SelectedAuthenticationMethod,
                    DataSource = txtServerName.Text,
                    InitialCatalog = cboDatabase.Text,
                    Encrypt = SelectedEncryptionOption,
                    TrustServerCertificate = chkTrustServerCert.Checked,
                    HostNameInCertificate = txtHostNameInCertificate.Text
                };

                if (!IsPasswordSupported || string.IsNullOrEmpty(txtPassword.Text))
                {
                    builder.Remove("Password");
                    builder.Remove("PWD");
                }
                if (!IsUserNameSupported || string.IsNullOrEmpty(txtUserName.Text))
                {
                    builder.Remove("UID");
                    builder.Remove("UserID");
                }

                builder.Remove("Integrated Security"); // Replaced with Authentication

                return builder.ConnectionString;
            }
            set
            {
                connectionString = value;
                var builder = new SqlConnectionStringBuilder(connectionString);

                if (AuthenticationMethods.ContainsKey(builder.Authentication))
                {
                    cboAuthType.SelectedValue = builder.Authentication;
                }
                else if (builder.Authentication == SqlAuthenticationMethod.NotSpecified)
                {
                    cboAuthType.SelectedValue = builder.IntegratedSecurity ? SqlAuthenticationMethod.ActiveDirectoryIntegrated : SqlAuthenticationMethod.SqlPassword;
                }

                cboDatabase.Text = builder.InitialCatalog;
                txtUserName.Text = builder.UserID;
                txtPassword.Text = builder.Password;
                txtServerName.Text = builder.DataSource;
                chkTrustServerCert.Checked = builder.TrustServerCertificate;
                cboEncryption.SelectedValue = builder.Encrypt;
                txtHostNameInCertificate.Text = builder.HostNameInCertificate;
                lnkOptions.Text = string.IsNullOrEmpty(OtherConnectionOptions) ? "{Other Options}" : OtherConnectionOptions.Truncate(40, true);
            }
        }

        private string OtherConnectionOptions
        {
            get
            {
                var builder = new SqlConnectionStringBuilder(connectionString);
                builder.Remove("User ID");
                builder.Remove("UID");
                builder.Remove("PWD");
                builder.Remove("Password");
                builder.Remove("Integrated Security");
                builder.Remove("Initial Catalog");
                builder.Remove("Data Source");
                builder.Remove("Encrypt");
                builder.Remove("Authentication");
                builder.Remove("TrustServerCertificate");
                builder.Remove("HostNameInCertificate");
                builder.Remove("Application Name");
                return builder.ConnectionString;
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

        private void BttnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }

        public static void TestConnection(string connectionString)
        {
            using var cn = new SqlConnection(connectionString);
            cn.Open();
        }

        private void BttnConnect_Click(object sender, EventArgs e)
        {
            try
            {
                Cursor = Cursors.WaitCursor;
                TestConnection(ValidateInitialCatalog ? ConnectionString : ConnectionStringWithoutInitialCatalog); // Try without initial catalog as DB might not have been created yet
            }
            catch (SqlException ex) when (ex.Number == -2146893019)
            {
                Cursor = Cursors.Default;
                MessageBox.Show($"Error: Deploy a trusted certificate or use the 'Trust Server Certificate' connection option.\n\n{ex.Message}", @"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            catch (Exception ex)
            {
                Cursor = Cursors.Default;
                MessageBox.Show($"Error connecting to data source. \n{ex.Message}", @"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            finally
            {
                Cursor = Cursors.Default;
            }
            DialogResult = DialogResult.OK;
        }

        private void DBConnection_Load(object sender, EventArgs e)
        {
            if (txtServerName.Text == @"localhost")
            {
                txtServerName.Text = Environment.MachineName;
            }
        }

        private void CboDatabase_Dropdown(object sender, EventArgs e)
        {
            try
            {
                cboDatabase.Items.Clear();
                var DBs = GetDatabases(ConnectionStringWithoutInitialCatalog);
                foreach (var db in DBs)
                {
                    cboDatabase.Items.Add(db);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static List<string> GetDatabases(string ConnectionString)
        {
            using var cn = new SqlConnection(ConnectionString);
            using SqlCommand cmd = new("SELECT name FROM sys.databases WHERE state=0", cn);
            cn.Open();
            var DBs = new List<string>();
            using var rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                DBs.Add((string)rdr[0]);
            }
            DBs.Sort();
            return DBs;
        }

        private void cboAuthType_SelectedIndexChanged(object sender, EventArgs e)
        {
            txtPassword.Visible = IsPasswordSupported;
            txtUserName.Visible = IsUserNameSupported;
            lblPassword.Visible = IsPasswordSupported;
            lblUserName.Visible = IsUserNameSupported;
            lblUserName.Text = SelectedAuthenticationMethod == SqlAuthenticationMethod.ActiveDirectoryManagedIdentity ? "User assigned identity:" : "User name:";
        }

        private void cboEncryption_SelectedIndexChanged(object sender, EventArgs e)
        {
            chkTrustServerCert.Enabled = !Equals(cboEncryption.SelectedValue, SqlConnectionEncryptOption.Strict);
            chkTrustServerCert.Checked = !Equals(cboEncryption.SelectedValue, SqlConnectionEncryptOption.Strict) && chkTrustServerCert.Checked;
        }

        private void lnkOptions_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var input = OtherConnectionOptions;
            if (CommonShared.ShowInputDialog(ref input,
                    "Enter additional connection string options: \n", default, "Enter any additional connection string options as required. e.g.\nApplicationIntent=ReadOnly;Connect Timeout=10") !=
                DialogResult.OK) return;

            try
            {
                var newConnectionString = StripOtherConnectionOptions();
                ConnectionString = CombineConnectionStrings(newConnectionString, input);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string StripOtherConnectionOptions()
        {
            var builder = new SqlConnectionStringBuilder(ConnectionString);
            foreach (var pair in OtherConnectionOptions.Split(';'))
            {
                builder.Remove(pair.Split('=')[0]);
            }
            return builder.ConnectionString;
        }

        private static string CombineConnectionStrings(string originalConnectionString, string additionalOptions)
        {
            var builder = new SqlConnectionStringBuilder(originalConnectionString);

            var additionalOptionsPairs = additionalOptions.Split(';', StringSplitOptions.RemoveEmptyEntries);

            foreach (var pair in additionalOptionsPairs)
            {
                var keyValue = pair.Split('=', StringSplitOptions.RemoveEmptyEntries);
                if (keyValue.Length == 2)
                {
                    // Update or add the key-value pair to the original connection string
                    builder[keyValue[0]] = keyValue[1];
                }
            }

            return builder.ConnectionString;
        }
    }
}