using DBADashGUI.Theme;
using DBADashSharedGUI;
using Microsoft.Data.SqlClient;
using System.ComponentModel;
using System.Runtime.Versioning;

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

        public bool InitialCatalogRequired = false;

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
                { SqlAuthenticationMethod.NotSpecified, "Windows Authentication"},
                { SqlAuthenticationMethod.SqlPassword, "SQL Server Authentication" },
                { SqlAuthenticationMethod.ActiveDirectoryIntegrated, "Microsoft Entra Integrated" },
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

        private string GetConnectionString()
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
            if (SelectedAuthenticationMethod == SqlAuthenticationMethod.NotSpecified)
            {
                builder.IntegratedSecurity = true;
            }
            else
            {
                builder.Remove("Integrated Security");
            }
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

            return builder.ConnectionString;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string ConnectionString
        {
            get => tab.SelectedTab == tabBasic ? GetConnectionString() : ((SqlConnectionStringBuilder)propertyGrid1.SelectedObject).ConnectionString;
            set
            {
                connectionString = value;
                var builder = new SqlConnectionStringBuilder(connectionString);
                if (builder.Authentication == SqlAuthenticationMethod.NotSpecified && !string.IsNullOrEmpty(builder.UserID))
                {
                    builder.Authentication = SqlAuthenticationMethod.SqlPassword;
                }
                if (AuthenticationMethods.ContainsKey(builder.Authentication))
                {
                    cboAuthType.SelectedValue = builder.Authentication;
                }

                cboDatabase.Text = builder.InitialCatalog;
                txtUserName.Text = builder.UserID ?? string.Empty;
                txtPassword.Text = builder.Password;
                txtServerName.Text = builder.DataSource;
                chkTrustServerCert.Checked = builder.TrustServerCertificate;
                cboEncryption.SelectedValue = builder.Encrypt;
                txtHostNameInCertificate.Text = builder.HostNameInCertificate;
                lnkOptions.Text = string.IsNullOrEmpty(OtherConnectionOptions) ? "{Other Options}" : OtherConnectionOptions.Truncate(40, true);
                toolTip1.SetToolTip(lnkOptions, OtherConnectionOptions);
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

        public string ConnectionStringToMaster
        {
            get
            {
                var builder = new SqlConnectionStringBuilder(ConnectionString);
                builder.InitialCatalog = "master";
                return builder.ConnectionString;
            }
        }

        private void BttnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }

        public static async Task TestConnectionAsync(string connectionString, CancellationToken ct = default)
        {
            using var cn = new SqlConnection(connectionString);
            await cn.OpenAsync(ct);
        }

        private async void BttnConnect_Click(object sender, EventArgs e)
        {
            if (InitialCatalogRequired && string.IsNullOrWhiteSpace(cboDatabase.Text))
            {
                MessageBox.Show("Please select a database", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            Cursor = Cursors.WaitCursor;

            try
            {
                var dbTask = TestConnectionAsync(ConnectionString, cts.Token);
                var masterTask = ValidateInitialCatalog
                    ? Task.CompletedTask
                    : TestConnectionAsync(ConnectionStringToMaster, cts.Token);

                if (ValidateInitialCatalog)
                {
                    await dbTask; // throws if invalid
                    DialogResult = DialogResult.OK;
                    return;
                }

                var first = await Task.WhenAny(dbTask, masterTask);

                var other = first == dbTask ? masterTask : dbTask;

                if (first.IsCompletedSuccessfully)
                {
                    // Success on first completed (either DB or master)
                    DialogResult = DialogResult.OK;
                    cts.Cancel(); // cancel other pending attempt if still running
                    other.ObserveFault(); // observe any exception to prevent unobserved task exceptions
                    return;
                }

                // First faulted: check the other
                try
                {
                    await other; // succeeds -> OK
                    DialogResult = DialogResult.OK;
                    return;
                }
                catch
                {
                    // Both failed: await DB task (if not first) to surface its exception (primary target)
                    if (first != dbTask)
                    {
                        await dbTask; // throws original DB exception
                    }
                    throw; // rethrow (master was first and both failed)
                }
            }
            catch (SqlException ex) when (ex.Number == -2146893019)
            {
                CommonShared.ShowExceptionDialog(ex,
                    "Deploy a trusted certificate or use the 'Trust Server Certificate' connection option.");
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex, "Error connecting to data source.");
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void DBConnection_Load(object sender, EventArgs e)
        {
            if (txtServerName.Text == @"localhost")
            {
                txtServerName.Text = Environment.MachineName;
            }
        }

        private async void CboDatabase_Dropdown(object sender, EventArgs e)
        {
            try
            {
                cboDatabase.BeginUpdate();
                cboDatabase.Items.Clear();
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                // Fire task to get databases from master & user specified DBs concurrently
                var masterTask = GetDatabasesAsync(ConnectionStringToMaster, cts.Token);
                var dbTask = GetDatabasesAsync(ConnectionString, cts.Token);

                // Prefer whichever completes successfully first
                var firstCompleted = await Task.WhenAny(masterTask, dbTask);
                var other = firstCompleted == masterTask ? dbTask : masterTask;
                List<string> dbs;
                if (!firstCompleted.IsFaulted)
                {
                    cts.Cancel(); // Cancel the other task if still running
                    other.ObserveFault(); // Observe any exception to prevent unobserved task exceptions
                    dbs = firstCompleted.Result; // Already completed
                }
                else
                {
                    // First failed; try the other. If that also fails, its await will throw and be caught.
                    dbs = await other;
                }

                if (dbs.Count > 0)
                {
                    cboDatabase.Items.AddRange([.. dbs]);
                }
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex, "Unable to list databases");
            }
            finally
            {
                cboDatabase.EndUpdate();
            }
        }

        public static async Task<List<string>> GetDatabasesAsync(string ConnectionString, CancellationToken ct)
        {
            using var cn = new SqlConnection(ConnectionString);
            using SqlCommand cmd = new("SELECT name FROM sys.databases WHERE state=0", cn);
            await cn.OpenAsync(ct);
            var DBs = new List<string>();
            using var rdr = await cmd.ExecuteReaderAsync(ct);
            while (await rdr.ReadAsync(ct))
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
                CommonShared.ShowExceptionDialog(ex);
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

        private void bttnAdvanced_Click(object sender, EventArgs e)
        {
            tab.SelectedTab = tab.SelectedTab == tabAdvanced ? tabBasic : tabAdvanced;
            bttnAdvanced.Text = tab.SelectedTab == tabAdvanced ? "Basic" : "Advanced";
            if (tab.SelectedTab == tabAdvanced)
            {
                var builder = new SqlConnectionStringBuilder(GetConnectionString());
                propertyGrid1.SelectedObject = builder;
            }
            else
            {
                ConnectionString = ((SqlConnectionStringBuilder)propertyGrid1.SelectedObject).ConnectionString;
            }
        }
    }
}