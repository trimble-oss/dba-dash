using DBADash;
using DBADashGUI.SchemaCompare;
using DBADashGUI.Theme;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.DirectoryServices;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Task = System.Threading.Tasks.Task;

namespace DBADashServiceConfig
{
    public partial class PermissionsHelper : Form
    {
        public PermissionsHelper()
        {
            InitializeComponent();
            this.ApplyTheme();
        }

        public CollectionConfig Config { get; set; }

        public List<DBADashSource> Connections
        {
            get => _connections;
            set => _connections = value.Where(src => src.SourceConnection.Type == DBADashConnection.ConnectionType.SQL).ToList();
        }

        private List<string> ProcedureNames =>
            Connections
                .SelectMany(src => src.CustomCollections.Values.Select(cc => cc.ProcedureName))
                .Concat(Config.CustomCollections.Values.Select(cc => cc.ProcedureName))
                .Distinct()
                .ToList();

        private string ServiceName => Config.ServiceName;
        private List<DBADashSource> _connections;
        private const string ExcludedResult = "Excluded";
        private const string SucceededResult = "Succeeded";
        private const string ReadyResult = "";
        private const string FailedResult = "Failed";
        private bool ApplyToSQLAuth;
        private int TotalTasks = 0;
        private int CompletedTasks = 0;
        private int ProgressDots = 0;
        private const int ProgressMaxDots = 5;

        private string ServiceAccountName
        {
            get => txtServiceAccount.Text;
            set => txtServiceAccount.Text = value;
        }

        private bool IsDomainAccount(string accountName) => accountName.Split("\\").Length == 2 || accountName.Contains("@");

        private static string GetServiceAccount(string serviceName)
        {
            var wmiQuery = $"SELECT StartName FROM Win32_Service WHERE Name='{serviceName}'";

            using var searcher = new ManagementObjectSearcher(wmiQuery);
            using var results = searcher.Get();
            return results.OfType<ManagementObject>().Select(result => result["StartName"]?.ToString()).FirstOrDefault();
        }

        private void PermissionsHelper_Load(object sender, EventArgs e)
        {
            try
            {
                ServiceAccountName = GetServiceAccount(ServiceName);
                if (Connections.Count == 1 && !string.IsNullOrEmpty(Connections[0].SourceConnection.UserName))
                {
                    ServiceAccountName = Connections[0].SourceConnection.UserName;
                }

                bttnGrantRepositoryDB.Enabled = Config.SQLDestinations.Any();
                LoadConnections();
            }
            catch (Exception ex)
            {
                errorProvider1.SetError(txtServiceAccount, "Error querying the service user account:" + ex.Message);
            }
            LoadGrid();
        }

        private DataTable InstancesDT;

        private void LoadConnections()
        {
            InstancesDT = new DataTable();
            InstancesDT.PrimaryKey = new[] { InstancesDT.Columns.Add("Connection", typeof(string)) };
            InstancesDT.Columns.Add("Windows Auth", typeof(bool));
            InstancesDT.Columns.Add("WMI", typeof(bool));
            InstancesDT.Columns.Add("Result", typeof(string));
            InstancesDT.Columns.Add("Message", typeof(string));

            foreach (var src in Connections)
            {
                InstancesDT.Rows.Add(new object[] { GetSourceID(src), src.SourceConnection.IsIntegratedSecurity, !src.NoWMI, ReadyResult });
            }
            dgvInstances.DataSource = InstancesDT;
            dgvInstances.AutoResizeColumnsWithMaxColumnWidth(DataGridViewAutoSizeColumnsMode.AllCells, 0.6f);
            bttnGrant.Enabled = Connections.Any();
            bttnLocalAdmin.Enabled = Connections.Any();
        }

        private readonly BindingList<PermissionItem> Permissions = new()
        {
            new PermissionItem(){Name = "VIEW SERVER STATE", PermissionState = PermissionItem.PermissionStates.Grant},
            new PermissionItem(){Name="VIEW ANY DATABASE", PermissionState = PermissionItem.PermissionStates.Grant },
            new PermissionItem(){Name = "CONNECT ANY DATABASE", PermissionState = PermissionItem.PermissionStates.Grant },
            new PermissionItem(){Name = "VIEW ANY DEFINITION", PermissionState = PermissionItem.PermissionStates.Grant},
            new PermissionItem(){Name = "ALTER ANY EVENT SESSION", PermissionState = PermissionItem.PermissionStates.Grant},
            new PermissionItem(){Name = "msdb:db_datareader", PermissionState = PermissionItem.PermissionStates.Grant, PermissionType = PermissionItem.PermissionTypes.DatabaseRole},
            new PermissionItem(){Name = "msdb:SQLAgentReaderRole", PermissionState = PermissionItem.PermissionStates.None, PermissionType = PermissionItem.PermissionTypes.DatabaseRole},
            new PermissionItem(){Name = "msdb:SQLAgentOperatorRole", PermissionState = PermissionItem.PermissionStates.Grant, PermissionType = PermissionItem.PermissionTypes.DatabaseRole},
            new PermissionItem(){Name = "sysadmin", PermissionState = PermissionItem.PermissionStates.None, PermissionType = PermissionItem.PermissionTypes.ServerRole},
            new PermissionItem(){Name = "DBADash_CustomCheck", PermissionState = PermissionItem.PermissionStates.Grant, PermissionType = PermissionItem.PermissionTypes.ExecuteProcedure },
            new PermissionItem(){Name = "DBADash_CustomPerformanceCounters", PermissionState = PermissionItem.PermissionStates.Grant, PermissionType = PermissionItem.PermissionTypes.ExecuteProcedure }
        };

        private void LoadGrid()
        {
            dgvPermissions.AutoGenerateColumns = false;
            dgvPermissions.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn() { DataPropertyName = "Name", HeaderText = "Name"},
                new DataGridViewTextBoxColumn() { DataPropertyName = "PermissionType", HeaderText = "Type"},
                new DataGridViewCheckBoxColumn() { DataPropertyName = "Grant", HeaderText = "Grant"},
                new DataGridViewCheckBoxColumn() { DataPropertyName = "Revoke", HeaderText = "Revoke"}
            });

            ProcedureNames.ForEach(p => Permissions.Add(new PermissionItem()
            {
                Name = p,
                PermissionState = PermissionItem.PermissionStates.Grant,
                PermissionType = PermissionItem.PermissionTypes.ExecuteProcedure
            }));
            dgvPermissions.DataSource = Permissions;
            dgvPermissions.CellValueChanged += (sender, e) =>
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
                dgvPermissions.InvalidateRow(e.RowIndex);
            };
            dgvPermissions.CurrentCellDirtyStateChanged += (sender, e) =>
            {
                if (dgvPermissions.IsCurrentCellDirty && dgvPermissions.CurrentCell is DataGridViewCheckBoxCell)
                {
                    dgvPermissions.CommitEdit(DataGridViewDataErrorContexts.Commit);
                }
            };
            dgvPermissions.AutoResizeColumns();
        }

        private string currentDB;
        private HashSet<string> UserCreatedDb = new();

        private static string ReApplySqlQuoteName(string proc)
        {
            // Note: There will be some edge cases where this doesn't work, but we will output safe SQL
            var unQuoted = proc.Replace("[", "").Replace("]", ""); // Object might be quoted already so remove quotes.
            // Split into components (schema + object), reapply quotename then combine.
            return string.Join(".", unQuoted.Split(".").Select(part => part.SqlQuoteName()));
        }

        private string GetScript(string serviceAccount)
        {
            var sb = new StringBuilder();
            UserCreatedDb = new();
            currentDB = string.Empty;

            if (IsDomainAccount(serviceAccount))
            {
                sb.AppendLine($"IF SUSER_ID({serviceAccount.SqlSingleQuoteWithEncapsulation()}) IS NULL");
                sb.AppendLine("BEGIN");
                sb.AppendLine(
                    $"    CREATE LOGIN {serviceAccount.SqlQuoteName()} FROM WINDOWS WITH DEFAULT_DATABASE=[master]");
                sb.AppendLine("END");
            }
            CreateUserInCurrentDB(sb);
            foreach (var p in Permissions.Where(p => p.PermissionState != PermissionItem.PermissionStates.None).OrderBy(p => (p.PermissionType is PermissionItem.PermissionTypes.ExecuteProcedure ? "A" : "Z") + p.Name))
            {
                switch (p.PermissionType)
                {
                    case PermissionItem.PermissionTypes.ExecuteProcedure: /* This is run first before we change database */
                        sb.AppendLine($"IF OBJECT_ID({p.Name.SqlSingleQuoteWithEncapsulation()}) IS NOT NULL");
                        sb.AppendLine("BEGIN");
                        sb.AppendLine($"\tGRANT EXECUTE ON {ReApplySqlQuoteName(p.Name)} TO {serviceAccount.SqlQuoteName()}");
                        sb.AppendLine("END");
                        break;

                    case PermissionItem.PermissionTypes.ServerPermission:
                        UseDatabase(sb, "master");
                        CreateUserInCurrentDB(sb);
                        sb.AppendLine((p.Grant ? "GRANT " : "REVOKE ") + p.Name + " TO " + serviceAccount.SqlQuoteName());
                        break;

                    case PermissionItem.PermissionTypes.DatabaseRole:
                        {
                            var db = p.Name.Split(":")[0];
                            var role = p.Name.Split(":")[1];
                            UseDatabase(sb, db);
                            CreateUserInCurrentDB(sb);
                            sb.AppendLine("ALTER ROLE " + role.SqlQuoteName() + (p.Grant ? " ADD " : " DROP ") + " MEMBER " + serviceAccount.SqlQuoteName());
                            break;
                        }
                    case PermissionItem.PermissionTypes.ServerRole:
                        UseDatabase(sb, "master");
                        CreateUserInCurrentDB(sb);
                        sb.AppendLine("ALTER SERVER ROLE " + p.Name.SqlQuoteName() + (p.Grant ? " ADD " : " DROP ") + " MEMBER " + serviceAccount.SqlQuoteName());
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            return sb.ToString();
        }

        private void UseDatabase(StringBuilder sb, string db)
        {
            if (currentDB == db) return;
            sb.AppendLine("USE " + db.SqlQuoteName() + "");
            currentDB = db;
        }

        private void CreateUserInCurrentDB(StringBuilder sb)
        {
            if (UserCreatedDb.Contains(currentDB)) return;
            sb.Append(GetCreateUserInCurrentDBScript(ServiceAccountName));
            UserCreatedDb.Add(currentDB);
        }

        public static string GetCreateUserInCurrentDBScript(string loginName, string linePrefix = "")
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{linePrefix}IF NOT EXISTS(SELECT 1 FROM sys.database_principals WHERE name = {loginName.SqlSingleQuoteWithEncapsulation()})");
            sb.AppendLine($"{linePrefix}BEGIN");
            sb.AppendLine($"{linePrefix}\tCREATE USER {loginName.SqlQuoteName()} FOR LOGIN {loginName.SqlQuoteName()}");
            sb.AppendLine($"{linePrefix}END");
            return sb.ToString();
        }

        private async void bttnGrant_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtServiceAccount.Text))
            {
                MessageBox.Show("Please enter a name of the service account", "Permissions Helper",
                    MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            var serviceAccount = ServiceAccountName;

            if (Connections.Count == 1 && !string.IsNullOrEmpty(Connections[0].SourceConnection.UserName))
            {
                // 1 connection using SQL auth
                MessageBox.Show("Please enter the connecting details of the user that will provision access", Text,
                    MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                var connectionDialog = new DBConnection() { ConnectionString = Connections[0].SourceConnection.ConnectionString };
                connectionDialog.ShowDialog();
                if (connectionDialog.DialogResult != DialogResult.OK) return;
                Connections[0] = new DBADashSource(connectionDialog.ConnectionString);
                LoadConnections();
                ApplyToSQLAuth = true;
            }
            else if (!IsDomainAccount(ServiceAccountName))
            {
                MessageBox.Show("The service account doesn't appear to be a domain user account.", Text,
                    MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            StartProgress(Connections.Count);
            var tasks = Connections.Select(src => ExecuteSQL(GetScript(GetServiceAccountName(src.SourceConnection)), src)).ToList();
            try
            {
                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                StopProgress();
            }
            dgvInstances.AutoResizeColumnsWithMaxColumnWidth(DataGridViewAutoSizeColumnsMode.AllCells, 0.6f);
            var succeededCount = InstancesDT.Rows.Cast<DataRow>().Count(r => (string)r["Result"] == SucceededResult);
            var failedCount = InstancesDT.Rows.Cast<DataRow>().Count(r => (string)r["Result"] == FailedResult);
            var excludedCount = InstancesDT.Rows.Cast<DataRow>().Count(r => (string)r["Result"] == ExcludedResult);
            var message = $"{succeededCount} Succeeded, {failedCount} Failed & {excludedCount} Excluded" + (excludedCount > 0 ? "\n\nPlease click the 'View Script' button to apply the permissions to the excluded instances manually" : string.Empty);

            MessageBox.Show(message, Text,
                    MessageBoxButtons.OK,
                    failedCount > 0 && succeededCount == 0 ? MessageBoxIcon.Error :
                    failedCount > 0 || excludedCount > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
        }

        private static string GetSourceID(DBADashSource src)
        {
            return string.IsNullOrEmpty(src.ConnectionID) ? src.SourceConnection.EncryptedConnectionString : src.ConnectionID;
        }

        private async Task ExecuteSQL(string sql, DBADashSource src)
        {
            var row = InstancesDT.Rows.Find(GetSourceID(src));
            if (row == null)
            {
                Interlocked.Increment(ref CompletedTasks);
                return;
            }

            try
            {
                var connectionInfo = await ConnectionInfo.GetConnectionInfoAsync(src.SourceConnection.ConnectionString);
                if (connectionInfo.IsAzureDB)
                {
                    row["Result"] = ExcludedResult;
                    row["Message"] = "AzureDB is not currently supported by the permissions helper dialog.";
                    return;
                }

                if (!src.SourceConnection.IsIntegratedSecurity && !ApplyToSQLAuth)
                {
                    row["Result"] = ExcludedResult;
                    row["Message"] = "Not using Windows authentication";
                    return;
                }

                if (connectionInfo.MajorVersion < 12)
                {
                    row["Result"] = ExcludedResult;
                    row["Message"] =
                        "Versions of SQL Server prior to 2014 are not supported by the permissions helper dialog.";
                    return;
                }
                await ExecuteSQL(sql, src.SourceConnection);
                row["Result"] = SucceededResult;
            }
            catch (Exception ex)
            {
                row["Result"] = FailedResult;
                row["Message"] = ex.ToString();
            }
            finally
            {
                Interlocked.Increment(ref CompletedTasks);
            }
        }

        private static async Task ExecuteSQL(string sql, DBADashConnection src)
        {
            await using var cn = new SqlConnection(src.ConnectionString);
            await using var cmd = new SqlCommand(sql, cn);
            await cn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        private async void LocalAdmin_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtServiceAccount.Text))
            {
                MessageBox.Show("Please enter a name of the service account", "Permissions Helper",
                    MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            if (!IsDomainAccount(ServiceAccountName))
            {
                MessageBox.Show("The service account doesn't appear to be a domain user account.", Text,
                    MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            StartProgress(Connections.Count);
            var tasks = Connections.Select(AddToLocalAdmin).ToList();
            try
            {
                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                StopProgress();
            }
            var succeededCount = InstancesDT.Rows.Cast<DataRow>().Count(r => (string)r["Result"] == SucceededResult);
            var failedCount = InstancesDT.Rows.Cast<DataRow>().Count(r => (string)r["Result"] == FailedResult);
            var excludedCount = InstancesDT.Rows.Cast<DataRow>().Count(r => (string)r["Result"] == ExcludedResult);
            var message = $"{succeededCount} Succeeded, {failedCount} Failed & {excludedCount} Excluded";
            dgvInstances.AutoResizeColumnsWithMaxColumnWidth(DataGridViewAutoSizeColumnsMode.AllCells, 0.6f);
            MessageBox.Show(message, Text,
                MessageBoxButtons.OK,
                failedCount > 0 && succeededCount == 0 ? MessageBoxIcon.Error :
                failedCount > 0 || excludedCount > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
        }

        private async Task AddToLocalAdmin(DBADashSource src)
        {
            var row = InstancesDT.Rows.Find(GetSourceID(src));
            if (row == null)
            {
                Interlocked.Increment(ref CompletedTasks);
                return;
            }
            try
            {
                if (src.NoWMI && !chkRevokeLocalAdmin.Checked)
                {
                    row["Result"] = ExcludedResult;
                    row["Message"] = "WMI is not enabled.  Local admin is not required.";
                    return;
                }
                var connectionInfo = await ConnectionInfo.GetConnectionInfoAsync(src.SourceConnection.ConnectionString);
                var computerName = connectionInfo.ComputerNetBIOSName;
                if (connectionInfo.IsAzureDB || connectionInfo.IsRDS || connectionInfo.IsLinux)
                {
                    row["Result"] = ExcludedResult;
                    row["Message"] = "Invalid instance type for WMI (AzureDB, RDS, Linux)";
                    return;
                }

                if (src.NoWMI)
                {
                    await RemoveUserFromLocalAdmin(src, computerName, row);
                }
                else
                {
                    await AddUserToLocalAdmin(src, computerName, row);
                }
            }
            catch (Exception ex)
            {
                row["Result"] = FailedResult;
                row["Message"] = "Error getting computer name: " + ex;
            }
            finally
            {
                Interlocked.Increment(ref CompletedTasks);
            }
        }

        private async Task AddUserToLocalAdmin(DBADashSource src, string computerName, DataRow row)
        {
            const int UserInGroupHResult = unchecked((int)0x80070562);
            try
            {
                await Task.Run(() => AddUserToLocalAdmin(computerName, ServiceAccountName));
                SetRowResult(row, SucceededResult, $"User added to Administrators group on {computerName}");
            }
            catch (TargetInvocationException ex) when (ex.InnerException is COMException { HResult: UserInGroupHResult })
            {
                SetRowResult(row, SucceededResult, $"User is already a member of Administrators group on {computerName}");
            }
            catch (COMException ex) when (ex.HResult == UserInGroupHResult)
            {
                SetRowResult(row, SucceededResult, $"User is already a member of Administrators group on {computerName}");
            }
            catch (Exception ex)
            {
                SetRowResult(row, FailedResult,
                    $"Failed to add user to Administrators group on {computerName}: " + ex.ToString());
            }
        }

        private async Task RemoveUserFromLocalAdmin(DBADashSource src, string computerName, DataRow row)
        {
            const int UserNotInGroupHResult = unchecked((int)0x80070561);
            try
            {
                await Task.Run(() => RemoveUserFromLocalAdmin(computerName, ServiceAccountName));
                SetRowResult(row, SucceededResult, $"User removed from Administrators group on {computerName}");
            }
            catch (TargetInvocationException ex) when (ex.InnerException is COMException
            {
                HResult: UserNotInGroupHResult
            })
            {
                SetRowResult(row, SucceededResult, $"User is not a member of Administrators group on {computerName}");
            }
            catch (COMException ex) when (ex.HResult == UserNotInGroupHResult)
            {
                SetRowResult(row, SucceededResult, $"User is not a member of Administrators group on {computerName}");
            }
            catch (Exception ex)
            {
                SetRowResult(row, FailedResult, $"Failed to remove user from Administrators group on {computerName}: " + ex.ToString());
            }
        }

        private static void SetRowResult(DataRow row, string result, string message)
        {
            row["Result"] = result;
            row["Message"] = message;
        }

        private static void AddUserToLocalAdmin(string remoteComputer, string username,
            string adminUsername = null, string adminPassword = null) =>
            AddRemoveUserFromLocalAdmin(remoteComputer, username, false, adminUsername, adminPassword);

        private static void RemoveUserFromLocalAdmin(string remoteComputer, string username,
            string adminUsername = null, string adminPassword = null) =>
            AddRemoveUserFromLocalAdmin(remoteComputer, username, true, adminUsername, adminPassword);

        private static void AddRemoveUserFromLocalAdmin(string remoteComputer, string username, bool remove,
            string adminUsername = null, string adminPassword = null)
        {
            var groupPath = $"WinNT://{remoteComputer}/Administrators,group";
            var userPath = $"WinNT://{username},user";

            // If dealing with domain user, format differently
            if (username.Contains("\\"))
            {
                var parts = username.Split('\\');
                userPath = $"WinNT://{parts[0]}/{parts[1]},user";
            }
            else if (username.Contains("@"))
            {
                // Handle UPN format
                userPath = $"WinNT://{username},user";
            }

            using var group = new DirectoryEntry(groupPath);
            // Set credentials if provided
            if (!string.IsNullOrEmpty(adminUsername) && !string.IsNullOrEmpty(adminPassword))
            {
                group.Username = adminUsername;
                group.Password = adminPassword;
            }

            group.Invoke(remove ? "Remove" : "Add", userPath);
            group.CommitChanges();
        }

        private void StartProgress(int count)
        {
            InstancesDT.Rows.Cast<DataRow>().ToList().ForEach(row => SetRowResult(row, "Pending", string.Empty));
            TotalTasks = count;
            CompletedTasks = 0;
            UpdateProgress();
            lblProgress.Visible = true;
            timer1.Enabled = true;
            bttnGrant.Enabled = false;
            bttnLocalAdmin.Enabled = false;
        }

        private void StopProgress()
        {
            UpdateProgress();
            lblProgress.Text = "Done";
            timer1.Enabled = false;
            bttnGrant.Enabled = true;
            bttnLocalAdmin.Enabled = true;
        }

        private void UpdateProgress()
        {
            if (InvokeRequired)
            {
                Invoke(UpdateProgress);
                return;
            }

            ProgressDots = (ProgressDots + 1) % (ProgressMaxDots + 1);
            var pct = (double)CompletedTasks / TotalTasks;
            var dots = new string('.', ProgressDots);

            lblProgress.Text = $"In progress.  Completed {CompletedTasks}/{TotalTasks} ({pct:P1}){dots}";
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            UpdateProgress();
        }

        private void Instances_UserDeletedRow(object sender, DataGridViewRowEventArgs e)
        {
            Connections.RemoveAll(src => InstancesDT.Rows.Find(GetSourceID(src)) == null);
        }

        private string GetServiceAccountName(DBADashConnection conn)
        {
            return string.IsNullOrEmpty(conn.UserName) ? ServiceAccountName : conn.UserName;
        }

        private async void GrantRepositoryDB_Click(object sender, EventArgs e)
        {
            var serviceAccount = GetServiceAccountName(Config.DestinationConnection);

            if (string.IsNullOrEmpty(serviceAccount))
            {
                MessageBox.Show("Please enter a name of the service account", "Permissions Helper",
                    MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            try
            {
                var tasks = new List<Task>();
                foreach (var dest in Config.SQLDestinations)
                {
                    var repoDB = dest.InitialCatalog();
                    DBADashConnection masterCn;
                    if (dest.IsIntegratedSecurity && !dest.IsAzureDB())
                    {
                        var builder = new SqlConnectionStringBuilder(dest.ConnectionString)
                        {
                            InitialCatalog = "master"
                        };
                        masterCn = new DBADashConnection(builder.ToString());
                    }
                    else
                    {
                        var prompt = new DBConnection() { ConnectionString = dest.ConnectionString };
                        prompt.ShowDialog();
                        if (prompt.DialogResult == DialogResult.OK)
                        {
                            masterCn = new DBADashConnection(prompt.ConnectionString);
                        }
                        else
                        {
                            continue;
                        }
                    }
                    tasks.Add(ExecuteSQL(GetRepositoryDBScript(repoDB, serviceAccount), masterCn));
                }

                await Task.WhenAll(tasks);
                if (!tasks.Any())
                {
                    MessageBox.Show("No destination connection to update", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    MessageBox.Show(
                        "Service account has been granted permissions to the repository database.\n(db_owner or CREATE ANY DATABASE if the repository database hasn't been created yet)",
                        Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static string GetRepositoryDBScript(string db, string serviceAccount)
        {
            if (string.IsNullOrEmpty(db))
            {
                db = "DBADashDB";
            }
            var sb = new StringBuilder();
            var sbDynamic = new StringBuilder();
            sb.AppendLine("IF DB_ID(" + db.SqlSingleQuoteWithEncapsulation() + ") IS NOT NULL");
            sb.AppendLine("BEGIN");
            sb.AppendLine("\tDECLARE @SQL NVARCHAR(MAX)");
            sb.Append("\tSET @SQL = N");
            sbDynamic.AppendLine("\tUSE " + db.SqlQuoteName() + "");
            sbDynamic.Append(PermissionsHelper.GetCreateUserInCurrentDBScript(serviceAccount, "\t"));
            sbDynamic.AppendLine("\tALTER ROLE db_owner ADD MEMBER " + serviceAccount.SqlQuoteName());
            sbDynamic.AppendLine("\tUSE [master]");
            sbDynamic.AppendLine("\tREVOKE CREATE ANY DATABASE TO " + serviceAccount.SqlQuoteName());
            sb.AppendLine(sbDynamic.ToString().SqlSingleQuoteWithEncapsulation());
            sb.AppendLine("\tEXEC sp_executesql @SQL");
            sb.AppendLine("END");
            sb.AppendLine("ELSE");
            sb.AppendLine("BEGIN");
            sb.AppendLine("\tUSE [master]");
            sb.AppendLine("\tGRANT CREATE ANY DATABASE TO " + serviceAccount.SqlQuoteName());
            sb.AppendLine("END");
            return sb.ToString();
        }

        private void ViewMonitoredInstanceScript_Click(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var serviceAccount = ServiceAccountName;

            if (Connections.Count == 1)
            {
                serviceAccount = GetServiceAccountName(Connections[0].SourceConnection);
            }
            CommonShared.ShowCodeViewer(GetScript(serviceAccount), "Permissions Helper script", CodeEditor.CodeEditorModes.SQL);
        }

        private void ViewRepositoryDBScript_Click(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var serviceAccount = GetServiceAccountName(Config.DestinationConnection);
            var script = GetRepositoryDBScript(Config.DestinationConnection.InitialCatalog(), serviceAccount);
            CommonShared.ShowCodeViewer(script, "Repository DB Permissions Script");
        }
    }
}