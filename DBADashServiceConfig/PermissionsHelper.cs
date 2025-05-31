using DBADash;
using DBADashGUI;
using DBADashGUI.SchemaCompare;
using DBADashGUI.Theme;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.DirectoryServices;
using System.Drawing.Text;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
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

        public string ServiceName { get; set; }

        public List<DBADashSource> Connections { get; set; }

        private const string ExcludedResult = "Excluded";
        private const string SucceededResult = "Succeeded";
        private const string ReadyResult = "";
        private const string FailedResult = "Failed";
        private bool ApplyToSQLAuth;

        private string ServiceAccountName
        {
            get => txtServiceAccount.Text;
            set => txtServiceAccount.Text = value;
        }

        private bool ServiceIsDomainAccount => ServiceAccountName.Split("\\").Length == 2;

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
            new PermissionItem(){Name = "sysadmin", PermissionState = PermissionItem.PermissionStates.None, PermissionType = PermissionItem.PermissionTypes.ServerRole}
        };

        private void LoadGrid()
        {
            dgvPermissions.AutoGenerateColumns = false;
            dgvPermissions.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn() { DataPropertyName = "Name", HeaderText = "Name"},
                new DataGridViewCheckBoxColumn() { DataPropertyName = "Grant", HeaderText = "Grant"},
                new DataGridViewCheckBoxColumn() { DataPropertyName = "Revoke", HeaderText = "Revoke"}
            });
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

        private string GetScript()
        {
            var sb = new StringBuilder();
            var db = string.Empty;
            var lastDb = string.Empty;
            if (ServiceIsDomainAccount)
            {
                sb.AppendLine("USE [master]");
                sb.AppendLine($"IF SUSER_ID({ServiceAccountName.SqlSingleQuoteWithEncapsulation()}) IS NULL");
                sb.AppendLine("BEGIN");
                sb.AppendLine(
                    $"    CREATE LOGIN {ServiceAccountName.SqlQuoteName()} FROM WINDOWS WITH DEFAULT_DATABASE=[master]");
                sb.AppendLine("END");
            }

            foreach (var p in Permissions.Where(p => p.PermissionState != PermissionItem.PermissionStates.None).OrderBy(p => (p.PermissionType == PermissionItem.PermissionTypes.DatabaseRole ? "Z" : "A") + p.Name))
            {
                switch (p.PermissionType)
                {
                    case PermissionItem.PermissionTypes.ServerPermission:
                        sb.AppendLine((p.Grant ? "GRANT " : "REVOKE ") + p.Name + " TO " + ServiceAccountName.SqlQuoteName());
                        break;

                    case PermissionItem.PermissionTypes.DatabaseRole:
                        {
                            db = p.Name.Split(":")[0];
                            var role = p.Name.Split(":")[1];
                            if (db != lastDb)
                            {
                                sb.AppendLine("USE " + db.SqlQuoteName() + "");
                                sb.AppendLine($"IF NOT EXISTS(SELECT 1 FROM sys.database_principals WHERE name = {ServiceAccountName.SqlSingleQuoteWithEncapsulation()})");
                                sb.AppendLine("BEGIN");
                                sb.AppendLine($"    CREATE USER {ServiceAccountName.SqlQuoteName()} FOR LOGIN {ServiceAccountName.SqlQuoteName()}");
                                sb.AppendLine("END");
                            }

                            sb.AppendLine("ALTER ROLE " + role.SqlQuoteName() + (p.Grant ? " ADD " : " DROP ") + " MEMBER " + ServiceAccountName.SqlQuoteName());
                            break;
                        }
                    case PermissionItem.PermissionTypes.ServerRole:
                        sb.AppendLine("ALTER SERVER ROLE " + p.Name.SqlQuoteName() + (p.Grant ? " ADD " : " DROP ") + " MEMBER " + ServiceAccountName.SqlQuoteName());
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }

                lastDb = db;
            }
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

            if (Connections.Count == 1 && !string.IsNullOrEmpty(Connections[0].SourceConnection.UserName))
            {
                MessageBox.Show("Please enter the connecting details of the user that will provision access", Text,
                    MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                var connectionDialog = new DBConnection() { ConnectionString = Connections[0].ConnectionString };
                connectionDialog.ShowDialog();
                if (connectionDialog.DialogResult != DialogResult.OK) return;
                Connections[0] = new DBADashSource(connectionDialog.ConnectionString);
                LoadConnections();
                ApplyToSQLAuth = true;
            }
            else if (!ServiceIsDomainAccount)
            {
                MessageBox.Show("The service account doesn't appear to be a domain user account.", Text,
                    MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            var script = GetScript();
            var tasks = Connections.Select(src => ExecuteSQL(script, src)).ToList();

            try
            {
                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            if (row == null) return;
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
                    row["Message"] = "Versions of SQL Server prior to 2014 are not supported by the permissions helper dialog.";
                    return;
                }
                await using var cn = new SqlConnection(src.SourceConnection.ConnectionString);
                await using var cmd = new SqlCommand(sql, cn);
                await cn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();
                row["Result"] = SucceededResult;
            }
            catch (Exception ex)
            {
                row["Result"] = FailedResult;
                row["Message"] = ex.ToString();
            }
        }

        private void bttnViewScript_Click(object sender, EventArgs e)
        {
            DBADashGUI.Common.ShowCodeViewer(GetScript(), "Permissions Helper script", CodeEditor.CodeEditorModes.SQL);
        }

        private async void LocalAdmin_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtServiceAccount.Text))
            {
                MessageBox.Show("Please enter a name of the service account", "Permissions Helper",
                    MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            if (!ServiceIsDomainAccount)
            {
                MessageBox.Show("The service account doesn't appear to be a domain user account.", Text,
                    MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            var tasks = Connections.Select(AddToLocalAdmin).ToList();
            try
            {
                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            if (row == null) return;
            if (src.NoWMI && !chkRevokeLocalAdmin.Checked)
            {
                row["Result"] = ExcludedResult;
                row["Message"] = "WMI is not enabled.  Local admin is not required.";
                return;
            }

            string computerName;
            try
            {
                var connectionInfo = await ConnectionInfo.GetConnectionInfoAsync(src.SourceConnection.ConnectionString);
                computerName = connectionInfo.ComputerNetBIOSName;
                if (connectionInfo.IsAzureDB || connectionInfo.IsRDS || connectionInfo.IsLinux)
                {
                    row["Result"] = ExcludedResult;
                    row["Message"] = "Invalid instance type for WMI (AzureDB, RDS, Linux)";
                    return;
                }
            }
            catch (Exception ex)
            {
                row["Result"] = FailedResult;
                row["Message"] = "Error getting computer name: " + ex;
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
    }
}