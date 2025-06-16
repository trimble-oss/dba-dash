using DBADash;
using DBADash.Messaging;
using DBADashGUI.Theme;
using Humanizer;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static DBADash.DBADashConnection;
using Control = System.Windows.Forms.Control;
using Font = System.Drawing.Font;
using SortOrder = System.Windows.Forms.SortOrder;

namespace DBADashServiceConfig
{
    public partial class ServiceConfig : Form, IThemedControl
    {
        private static string FatalErrorFilePath => Path.Combine(AppContext.BaseDirectory, "Logs", "FatalError.txt");

        public ServiceConfig()
        {
            InitializeComponent();
            this.ApplyTheme();
            CustomCollectionsNew = new();
        }

        private string originalJson = "";
        private CollectionConfig collectionConfig = new();
        private bool isInstalled;
        private Dictionary<string, CustomCollection> _customCollectionsNew;
        private DateTime lastUserActivity = DateTime.Now;
        private DateTime lastServiceActivity = DateTime.Now;
        private DateTime lastFatalErrorPrompt = DateTime.MinValue;

        private string[] NewSourceConnections =>
            txtSource.Text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

        private Dictionary<string, CustomCollection> CustomCollectionsNew
        {
            get => _customCollectionsNew;
            set
            {
                _customCollectionsNew = value;
                bttnCustomCollectionsNew.Text = $"Custom Collections ({_customCollectionsNew?.Count ?? 0})";
                bttnCustomCollectionsNew.Font = _customCollectionsNew?.Count > 0
                    ? new Font(bttnCustomCollectionsNew.Font, FontStyle.Bold)
                    : new Font(bttnCustomCollectionsNew.Font, FontStyle.Regular);
            }
        }

        private async void BttnAdd_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtSource.Text))
            {
                MessageBox.Show(
                    "Please click the connect button or enter a list of connection strings for the SQL instances you want to monitor",
                    "Enter Source", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                await AddInstance();
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex, "Error adding instance");
            }
        }

        private DBADashSource CreateSourceConnection(string sourceString, string schemaSnapshotDBs)
        {
            if (!sourceString.Contains(';') && !sourceString.Contains(':') && !sourceString.StartsWith("\\\\") &&
                !sourceString.StartsWith("//"))
            {
                // Providing the name of the SQL instances - build the connection string automatically
                var builder = new SqlConnectionStringBuilder
                {
                    DataSource = sourceString,
                    IntegratedSecurity = true,
                    TrustServerCertificate = true,
                    Encrypt = true,
                    ApplicationName = "DBADash"
                };
                sourceString = builder.ConnectionString;
            }

            var src = new DBADashSource(sourceString)
            {
                NoWMI = chkNoWMI.Checked,
                UseDualEventSession = chkDualSession.Checked,
                PersistXESessions = chkPersistXESession.Checked,
                SlowQueryThresholdMs = chkSlowQueryThreshold.Checked ? (int)numSlowQueryThreshold.Value : -1,
                RunningQueryPlanThreshold = chkCollectPlans.Checked
                    ? new PlanCollectionThreshold()
                    {
                        CountThreshold = int.Parse(txtCountThreshold.Text),
                        CPUThreshold = int.Parse(txtCPUThreshold.Text),
                        DurationThreshold = int.Parse(txtDurationThreshold.Text),
                        MemoryGrantThreshold = int.Parse(txtGrantThreshold.Text)
                    }
                    : null,
                SchemaSnapshotDBs = schemaSnapshotDBs,
                CollectSessionWaits = chkCollectSessionWaits.Checked,
                ScriptAgentJobs = chkScriptJobs.Checked,
                IOCollectionLevel = (DBADashSource.IOCollectionLevels)cboIOLevel.SelectedItem!,
                WriteToSecondaryDestinations = chkWriteToSecondaryDestinations.Checked
            };
            src.CustomCollections = src.SourceConnection.Type == ConnectionType.SQL ? CustomCollectionsNew : null;
            return src;
        }

        private List<DBADashSource> GetNewSourceConnections()
        {
            var schemaSnapshotDBs = txtSnapshotDBs.Text.Trim();
            return NewSourceConnections.Select(src => src.Trim()).Where(src => !string.IsNullOrEmpty(src))
                .Select(src => CreateSourceConnection(src, schemaSnapshotDBs)).ToList();
        }

        private async Task AddInstance()
        {
            var schemaSnapshotDBs = txtSnapshotDBs.Text.Trim();
            if (!string.IsNullOrEmpty(schemaSnapshotDBs) && collectionConfig.SchemaSnapshotOptions == null)
            {
                collectionConfig.SchemaSnapshotOptions = new SchemaSnapshotDBOptions();
            }

            var hasUpdateApproval = false;
            var warnXENotSupported = false;
            var addUnvalidated = false;
            var doNotAddUnvalidated = false;
            var doesNotHaveUpdateApproval = false;
            var isDeleted = false;
            foreach (var src in GetNewSourceConnections())
            {
                var validated = ValidateSource(src.SourceConnection.ConnectionString);

                var validationError = "";
                if (validated)
                {
                    if (!(src.SourceConnection.Type == ConnectionType.SQL ||
                          collectionConfig.DestinationConnection.Type == ConnectionType.SQL))
                    {
                        MessageBox.Show(
                            "Error: Invalid source and destination connection combination.  One of these should be a SQL connection string",
                            "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    collectionConfig ??= new CollectionConfig();
                    if (!addUnvalidated)
                    {
                        Cursor.Current = Cursors.WaitCursor;
                        try
                        {
                            src.SourceConnection.Validate();
                            validated = true;
                            src.ConnectionID = await src.GetGeneratedConnectionIDAsync();
                            if (src.SourceConnection.ApplicationIntent() == ApplicationIntent.ReadOnly)
                            {
                                src.ConnectionID += "|ReadOnly";
                            }

                            if (src.SourceConnection.Type == ConnectionType.SQL &&
                                string.IsNullOrEmpty(src.SourceConnection.ConnectionInfo.ServerName))
                            {
                                MessageBox.Show(
                                    "Warning @@SERVERNAME returned NULL.  Consider fixing this using sp_addserver",
                                    "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            }
                        }
                        catch (Exception ex)
                        {
                            validated = false;
                            validationError = ex.Message;
                        }

                        Cursor.Current = Cursors.Default;
                    }

                    if (!addUnvalidated && !validated)
                    {
                        if (doNotAddUnvalidated)
                        {
                            continue;
                        }
                        else if (MessageBox.Show(
                                     "Error connecting to data source.  Do you wish to add anyway?" +
                                     Environment.NewLine + Environment.NewLine + validationError, "Error",
                                     MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) == DialogResult.No)
                        {
                            doNotAddUnvalidated = true;
                            continue;
                        }
                        else
                        {
                            addUnvalidated = true;
                        }
                    }
                    else if (!addUnvalidated && !src.SourceConnection.IsXESupported() && src.SlowQueryThresholdMs >= 0)
                    {
                        warnXENotSupported = true;
                        src.SlowQueryThresholdMs = -1;
                        src.PersistXESessions = false;
                    }
                    // Ensure we have a ConnectionID set for SQL connections
                    src.SetConnectionIDFromBuilderIfNotSet();

                    var existingConnection = await collectionConfig.FindSourceConnectionAsync(src.SourceConnection.ConnectionString, src.ConnectionID); // Check if the connection string exists in the config

                    if (existingConnection != null)
                    {
                        src.CollectionSchedules = existingConnection.CollectionSchedules;
                        if (doesNotHaveUpdateApproval)
                        {
                            continue;
                        }
                        else if (hasUpdateApproval || MessageBox.Show("Update existing connection(s)?", "Update",
                                     MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                        {
                            collectionConfig.SourceConnections.Remove(existingConnection);
                            src.ConnectionID = existingConnection.ConnectionID;
                            hasUpdateApproval = true;
                        }
                        else
                        {
                            doesNotHaveUpdateApproval = true;
                            continue;
                        }
                    }

                    if (RepoDbVersionStatus?.VersionStatus == DBValidations.DBVersionStatusEnum.OK)
                    {
                        try // Check if instance is marked deleted so we can warn the user that the instance needs to be restored
                        {
                            foreach (var dest in collectionConfig.SQLDestinations.Where(dest => !isDeleted))
                            {
                                isDeleted = await IsInstanceDeleted(src.ConnectionID, dest.ConnectionString);
                            }
                        }
                        catch (Exception ex)
                        {
                            CommonShared.ShowExceptionDialog(ex, "Error checking connection status in repository database:");
                        }
                    }

                    collectionConfig.SourceConnections.Add(src);
                }
            }

            SetJson();
            SetConnectionCount();
            SetDgv();
            RefreshEncryption();
            if (isDeleted)
            {
                MessageBox.Show(
                    "Warning. The instance you have added is marked deleted in the repository database.  Use the recycle bin folder in the GUI to restore the instance.",
                    "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            if (warnXENotSupported)
            {
                MessageBox.Show(
                    "Warning: Slow query capture requires an extended event session which is not supported for one or more connections.\nRequirements:\nSQL 2012 or later.\nStandard or Enterprise Edition on Amazon RDS",
                    "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private async Task<bool> IsInstanceDeleted(string connectionID, string connectionString)
        {
            await using var cn = new SqlConnection(connectionString);
            await using var cmd = new SqlCommand("dbo.Instances_Get", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("IsActive", 0);
            cmd.Parameters.AddWithValue("ConnectionID", connectionID);
            await cn.OpenAsync();
            await using var rdr = await cmd.ExecuteReaderAsync();
            return rdr.Read(); // Instance is soft deleted (IsActive=0) if a row is returned
        }

        private bool ValidateSource(string sourceString)
        {
            errorProvider1.SetError(txtSource, null);
            DBADashConnection source = new(sourceString);
            if (string.IsNullOrEmpty(sourceString))
            {
                return false;
            }

            if (source.Type == ConnectionType.Invalid)
            {
                errorProvider1.SetError(txtSource, "Invalid connection string, directory or S3 path");
                return false;
            }
            else
            {
                return true;
            }
        }

        private DBValidations.DBVersionStatus RepoDbVersionStatus;

        private void ValidateDestination()
        {
            errorProvider1.SetError(txtDestination, null);
            lblServerNameWarning.Visible = false;
            DBADashConnection dest = new(txtDestination.Text);
            if(!string.IsNullOrEmpty(txtDestination.Text))
            {
                try
                {
                    if (dest.Type == ConnectionType.Invalid)
                    {
                        throw new ArgumentException("Invalid connection string, directory or S3 path");
                    }
                    CollectionConfig.ValidateDestination(dest);
                }
                catch (Exception ex)
                {
                    errorProvider1.SetError(txtDestination, ex.Message);
                }
            }
            UpdateDBVersionStatus();
        }

        private static void InvokeSetStatus(Control statusControl, string status, Color color, FontStyle style)
        {
            if (statusControl.InvokeRequired)
            {
                statusControl.Invoke(() => InvokeSetStatus(statusControl, status, color, style));
                return;
            }
            statusControl.Text = status;
            statusControl.ForeColor = color;
            statusControl.Font = new Font(statusControl.Font, style);
            statusControl.Visible = status != string.Empty;
        }

        private static void InvokeSetVisible(Control control, bool isVisible)
        {
            if (control.InvokeRequired)
            {
                control.Invoke(() => InvokeSetVisible(control, isVisible));
                return;
            }
            control.Visible = isVisible;
        }

        private static void InvokeEnable(Control control, bool isEnabled)
        {
            if (control.InvokeRequired)
            {
                control.Invoke(() => InvokeEnable(control, isEnabled));
            }
            control.Enabled = isEnabled;
        }

        private void UpdateDBVersionStatus()
        {
            
            var dest = collectionConfig.DestinationConnection;

            if (collectionConfig.Destination == "")
            {
                RepoDbVersionStatus = null;
                InvokeSetStatus(lblVersionInfo, "Please start by setting the destination connection for your DBA Dash repository database.", DashColors.Information, FontStyle.Bold);
                return;
            }
            if (dest.Type == ConnectionType.Invalid)
            {
                RepoDbVersionStatus = null;
                InvokeSetStatus(lblVersionInfo, "Invalid connection string, directory or S3 path", DashColors.Fail, FontStyle.Regular);
                return;
            }
            if (dest.Type != ConnectionType.SQL)
            {
                RepoDbVersionStatus = null;
                InvokeSetStatus(lblVersionInfo, string.Empty, DashColors.TrimbleBlue, FontStyle.Regular);
                return;
            }

            try
            {
                if (string.IsNullOrEmpty(dest.MasterConnection().ConnectionInfo.ServerName))
                {
                    InvokeSetStatus(lblServerNameWarning, lblServerNameWarning.Text, lblServerNameWarning.ForeColor, lblServerNameWarning.Font.Style);
                    InvokeSetVisible(lblServerNameWarning, true);
                }

                RepoDbVersionStatus = DBValidations.VersionStatus(dest.ConnectionString);

                switch (RepoDbVersionStatus?.VersionStatus)
                {
                    case null:
                        InvokeSetStatus(lblVersionInfo, "???", DashColors.Fail, FontStyle.Bold);
                        break;

                    case DBValidations.DBVersionStatusEnum.CreateDB:
                        InvokeSetStatus(lblVersionInfo,
                            "Start service to create repository database or click Deploy to create manually.",
                            DashColors.Warning, FontStyle.Bold);
                        InvokeEnable(bttnDeployDatabase, true);
                        return;

                    case DBValidations.DBVersionStatusEnum.OK:
                        InvokeSetStatus(lblVersionInfo,
                            "Repository database version check successful. DacVersion/DB Version: " +
                            RepoDbVersionStatus.DACVersion, DashColors.Success, FontStyle.Regular);
                        InvokeEnable(bttnDeployDatabase, true);
                        break;

                    case DBValidations.DBVersionStatusEnum.AppUpgradeRequired:
                        InvokeSetStatus(lblVersionInfo,
                            $"Repository database version {RepoDbVersionStatus.DBVersion} is newer than dac version {RepoDbVersionStatus.DACVersion}.  Please update this app.",
                            DashColors.Warning, FontStyle.Bold);
                        InvokeEnable(bttnDeployDatabase, false);
                        break;

                    case DBValidations.DBVersionStatusEnum.UpgradeRequired:
                        InvokeSetStatus(lblVersionInfo,
                            $"Repository database version {RepoDbVersionStatus.DBVersion}  requires upgrade to {RepoDbVersionStatus.DACVersion}.  Database will be upgraded on service start.",
                            DashColors.Warning, FontStyle.Bold);
                        InvokeEnable(bttnDeployDatabase, true);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            catch (Exception ex)
            {
                InvokeSetStatus(lblVersionInfo, ex.Message, DashColors.Fail, FontStyle.Regular);
            }
        }

        private void BttnSave_Click(object sender, EventArgs e)
        {
            try
            {
                collectionConfig.ValidateDestination();
                SaveChanges();
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex);
            }
        }

        private void SaveChanges()
        {
            SetJson();
            collectionConfig.Save();
            originalJson = txtJson.Text;
            UpdateSaveButton();
            MessageBox.Show("Config saved.  Restart service to apply changes.", "Save", MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private async void ServiceConfig_Load(object sender, EventArgs e)
        {
            cboDeleteAction.SelectedIndex = 0;
            dgvConnections.ColumnHeaderMouseClick += dgvConnections_ColumnHeaderMouseClick;

            await CommonShared.CheckForIncompleteUpgrade();
            if (Upgrade.IsUpgradeIncomplete) return;

            cboIOLevel.DataSource = Enum.GetValues(typeof(DBADashSource.IOCollectionLevels));

            dgvConnections.AutoGenerateColumns = false;
            dgvConnections.Columns.Add(new DataGridViewTextBoxColumn()
            {
                Name = "ConnectionString",
                DataPropertyName = "ConnectionString",
                HeaderText = "Connection String",
                Width = 300
            });
            dgvConnections.Columns.Add(new DataGridViewTextBoxColumn()
            {
                Name = "ConnectionID",
                DataPropertyName = "ConnectionID",
                HeaderText = "Connection ID",
                ToolTipText =
                    "The ConnectionID is used to uniquely identify the SQL Instance in the repository database.  The ConnectionID is automatically assigned to @@SERVERNAME but you can override this with a custom value.  If you change the ConnectionID for an existing server it will appear as a new instance in the repository database."
            });
            dgvConnections.Columns.Add(new DataGridViewCheckBoxColumn()
            { DataPropertyName = "NoWMI", HeaderText = "No WMI" });
            dgvConnections.Columns.Add(new DataGridViewTextBoxColumn()
            { DataPropertyName = "SlowQueryThresholdMs", HeaderText = "Slow Query Threshold (ms)" });
            dgvConnections.Columns.Add(new DataGridViewCheckBoxColumn()
            { DataPropertyName = "UseDualEventSession", HeaderText = "Use Dual Event Session" });
            dgvConnections.Columns.Add(new DataGridViewCheckBoxColumn()
            { DataPropertyName = "PersistXESessions", HeaderText = "Persist XE Sessions" });
            dgvConnections.Columns.Add(new DataGridViewTextBoxColumn()
            { DataPropertyName = "SchemaSnapshotDBs", HeaderText = "Schema Snapshot DBs" });
            dgvConnections.Columns.Add(new DataGridViewTextBoxColumn()
            {
                DataPropertyName = "SlowQuerySessionMaxMemoryKB",
                HeaderText = "Slow Query Session Max Memory (KB)",
                ToolTipText = "Max amount of memory to allocate for event buffering."
            });
            dgvConnections.Columns.Add(new DataGridViewTextBoxColumn()
            {
                DataPropertyName = "SlowQueryTargetMaxMemoryKB",
                HeaderText = "Slow Query Target Max Memory (KB)",
                ToolTipText = "Max memory target parameter for ring_buffer"
            });
            dgvConnections.Columns.Add(new DataGridViewCheckBoxColumn()
            {
                DataPropertyName = "CollectSessionWaits",
                HeaderText = "Collect Session Waits",
                ToolTipText = "Collect Session Waits for Running Queries"
            });
            dgvConnections.Columns.Add(new DataGridViewCheckBoxColumn()
            { DataPropertyName = "PlanCollectionEnabled", HeaderText = "Running Query Plan Collection" });
            dgvConnections.Columns.Add(new DataGridViewTextBoxColumn()
            { DataPropertyName = "PlanCollectionCPUThreshold", HeaderText = "Plan Collection CPU Threshold" });
            dgvConnections.Columns.Add(new DataGridViewTextBoxColumn()
            {
                DataPropertyName = "PlanCollectionDurationThreshold",
                HeaderText = "Plan Collection Duration Threshold"
            });
            dgvConnections.Columns.Add(new DataGridViewTextBoxColumn()
            { DataPropertyName = "PlanCollectionCountThreshold", HeaderText = "Plan Collection Count Threshold" });
            dgvConnections.Columns.Add(new DataGridViewTextBoxColumn()
            {
                DataPropertyName = "PlanCollectionMemoryGrantThreshold",
                HeaderText = "Plan Collection Memory Grant Threshold"
            });
            dgvConnections.Columns.Add(new DataGridViewCheckBoxColumn()
            { DataPropertyName = "ScriptAgentJobs", HeaderText = "Script Agent Jobs" });
            dgvConnections.Columns.Add(new DataGridViewCheckBoxColumn()
            { DataPropertyName = "HasCustomSchedule", HeaderText = "Custom Schedule" });
            dgvConnections.Columns.Add(new DataGridViewComboBoxColumn()
            {
                DataPropertyName = "IOCollectionLevel",
                HeaderText = "IO Collection Level",
                DataSource = Enum.GetValues(typeof(DBADashSource.IOCollectionLevels))
            });
            dgvConnections.Columns.Add(new DataGridViewCheckBoxColumn()
            { DataPropertyName = "WriteToSecondaryDestinations", HeaderText = "Write to Secondary Destinations" });
            dgvConnections.Columns.Add(new DataGridViewTextBoxColumn()
            {
                DataPropertyName = "TableSizeCollectionThresholdMB",
                HeaderText = "Table Size Threshold MB",
                ToolTipText = "Only collect tables that are larger then the specified threshold"
            });
            dgvConnections.Columns.Add(new DataGridViewTextBoxColumn()
            {
                DataPropertyName = "TableSizeDatabases",
                HeaderText = "Table Size Databases",
                ToolTipText =
                    "Comma separated list of databases to collect table size. \ndb1,db2\t\t=Databases db1 & db2\n*,-db1\t\t=All databases except db1"
            });
            dgvConnections.Columns.Add(new DataGridViewTextBoxColumn()
            {
                DataPropertyName = "TableSizeMaxTableThreshold",
                HeaderText = "Table Size Max Tables",
                ToolTipText = "Skip table size collection if database has a large number of tables"
            });
            dgvConnections.Columns.Add(new DataGridViewTextBoxColumn()
            {
                DataPropertyName = "TableSizeMaxDatabaseThreshold",
                HeaderText = "Table Size Max Databases",
                ToolTipText = "Skip table size collection if instance has a large number of databases"
            });
            dgvConnections.Columns.Add(new DataGridViewLinkColumn()
            {
                Name = "Schedule",
                HeaderText = "Schedule",
                Text = "Schedule",
                UseColumnTextForLinkValue = true,
                LinkColor = DashColors.LinkColor
            });
            dgvConnections.Columns.Add(new DataGridViewLinkColumn()
            {
                Name = "CustomCollections",
                HeaderText = "Custom Collections",
                Text = "View/Edit",
                LinkColor = DashColors.LinkColor
            });
            dgvConnections.Columns.Add(new DataGridViewLinkColumn()
            {
                Name = "Edit",
                HeaderText = "Copy Connection",
                Text = "Copy",
                UseColumnTextForLinkValue = true,
                LinkColor = DashColors.LinkColor
            });
            dgvConnections.Columns.Add(new DataGridViewLinkColumn()
            {
                Name = "Delete",
                HeaderText = "Delete",
                Text = "Delete",
                UseColumnTextForLinkValue = true,
                LinkColor = DashColors.LinkColor
            });

            txtJson.MaxLength = 0;

            if (BasicConfig.ConfigExists)
            {
                try
                {
                    SetOriginalJson();

                    if (!originalJson.Like("%Trust%Server%Certificate%") &&
                        collectionConfig.SourceConnections.Count > 0)
                    {
                        if (MessageBox.Show(
                                "Encryption is applied by default to your SQL Server connections with Microsoft.Data.SqlClient 4.0.  Connections might fail if the server certificate is not trusted.  Apply Trust Server Certificate?",
                                "Trust Server Certificate", MessageBoxButtons.YesNo, MessageBoxIcon.Question) ==
                            DialogResult.Yes)
                        {
                            ApplyTrustServerCertificate();
                            SaveChanges();
                        }
                    }
                }
                catch (Exception ex)
                {
                    CommonShared.ShowExceptionDialog(ex, "Error reading ServiceConfig.json: ");
                    tab1.SelectedTab = tabJson; // Set tab to Json to allow user to correct error in Json
                }
            }
            else
            {
                numIdentityCollectionThreshold.Value = DBCollector.DefaultIdentityCollectionThreshold;
                numIdentityCollectionThreshold.Enabled = false;
                chkDefaultIdentityCollection.Checked = true;
            }

            SetConnectionCount();
            SubscribeActivityEvents(this);
            ValidateDestination();
            RefreshEncryption();
            dgvConnections.ApplyTheme();
            _ = Task.Run(AutoRefreshServiceStatus);
        }

        private void SubscribeActivityEvents(Control parent)
        {
            parent.MouseMove += UpdateActivityTime;

            //// Recursively subscribe for child controls
            foreach (Control c in parent.Controls)
            {
                SubscribeActivityEvents(c);
            }
        }

        private void UpdateActivityTime(object sender, EventArgs e)
        {
            lastUserActivity = DateTime.Now;
        }

        private void dgvConnections_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            DataGridViewColumn column = dgvConnections.Columns[e.ColumnIndex];
            if (dgvConnections.DataSource != null)
            {
                ListSortDirection direction;
                if (column.HeaderCell.SortGlyphDirection == SortOrder.Ascending ||
                    column.HeaderCell.SortGlyphDirection == SortOrder.None)
                {
                    direction = ListSortDirection.Descending;
                }
                else
                {
                    direction = ListSortDirection.Ascending;
                }
                foreach (DataGridViewColumn col in dgvConnections.Columns)
                {
                    col.HeaderCell.SortGlyphDirection = SortOrder.None;
                }

                if (column.DataPropertyName == "ConnectionString")
                {
                    collectionConfig.SourceConnections = direction == ListSortDirection.Ascending
                        ? collectionConfig.SourceConnections.OrderBy(c => c.ConnectionString).ToList()
                        : collectionConfig.SourceConnections.OrderByDescending(c => c.ConnectionString).ToList();
                }
                else if (column.DataPropertyName == "ConnectionID")
                {
                    collectionConfig.SourceConnections = direction == ListSortDirection.Ascending
                        ? collectionConfig.SourceConnections.OrderBy(c => c.ConnectionID).ToList()
                        : collectionConfig.SourceConnections.OrderByDescending(c => c.ConnectionID).ToList();
                }
                else if (column.Name == "SnapshotAge")
                {
                    collectionConfig.SourceConnections = direction == ListSortDirection.Ascending
                        ? collectionConfig.SourceConnections.OrderBy(c =>
                           !string.IsNullOrEmpty(c.ConnectionID) && SnapshotDates.ContainsKey(c.ConnectionID)
                                ? SnapshotDates[c.ConnectionID]
                                : DateTime.MinValue).ToList()
                        : collectionConfig.SourceConnections.OrderByDescending(c =>
                            !string.IsNullOrEmpty(c.ConnectionID) && SnapshotDates.ContainsKey(c.ConnectionID)
                                ? SnapshotDates[c.ConnectionID]
                                : DateTime.MinValue).ToList();
                }
                else if (column.Name == "ConnectionStatus")

                {
                    collectionConfig.SourceConnections = direction == ListSortDirection.Ascending
                        ? collectionConfig.SourceConnections.OrderBy(c =>
                            ConnectionStatuses.ContainsKey(c.ConnectionString)
                                ? ConnectionStatuses[c.ConnectionString].statusValue
                                : 2).ToList()
                        : collectionConfig.SourceConnections.OrderByDescending(c =>
                            ConnectionStatuses.ContainsKey(c.ConnectionString)
                                ? ConnectionStatuses[c.ConnectionString].statusValue
                                : 2).ToList();
                }
                else
                {
                    return;
                }

                SetDgv();
                column.HeaderCell.SortGlyphDirection = direction == ListSortDirection.Descending
                    ? SortOrder.Descending
                    : SortOrder.Ascending;
            }
        }

        private void SetOriginalJson()
        {
            try
            {
                var cfg = BasicConfig.Load<CollectionConfig>();
                originalJson = cfg.Serialize();
                txtJson.Text = originalJson;
            }
            catch
            {
                if (BasicConfig.IsConfigFileEncrypted())
                {
                    if (ShowPassword())
                    {
                        SetOriginalJson();
                    }
                    else
                    {
                        Application.Exit();
                        return;
                    }
                }
            }

            SetFromJson(originalJson);
        }

        private bool ShowPassword()
        {
            string password = "";
            if (CommonShared.ShowInputDialog(ref password, "Enter password to decrypt config", '*') == DialogResult.OK)
            {
                try
                {
                    BasicConfig.Load<CollectionConfig>(password);
                    EncryptedConfig.SetPassword(password, false);
                    return true;
                }
                catch
                {
                    if (MessageBox.Show("Invalid Password", "Error", MessageBoxButtons.RetryCancel) ==
                        DialogResult.Retry)
                    {
                        return ShowPassword();
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            return false;
        }

        private void ApplyTrustServerCertificate()
        {
            if (collectionConfig.DestinationConnection.Type == ConnectionType.SQL)
            {
                collectionConfig.DestinationConnection.EncryptedConnectionString =
                    AddTrustServerCertificate(collectionConfig.DestinationConnection.EncryptedConnectionString);
                txtDestination.Text = collectionConfig.DestinationConnection.EncryptedConnectionString;
            }

            foreach (var dest in collectionConfig.SecondaryDestinationConnections.Where(dest =>
                         dest.Type == ConnectionType.SQL))
            {
                dest.EncryptedConnectionString = AddTrustServerCertificate(dest.EncryptedConnectionString);
            }

            foreach (var src in collectionConfig.SourceConnections.Where(src =>
                         src.SourceConnection.Type == ConnectionType.SQL))
            {
                src.SourceConnection.EncryptedConnectionString =
                    AddTrustServerCertificate(src.SourceConnection.EncryptedConnectionString);
            }
        }

        private static string AddTrustServerCertificate(string connectionString)
        {
            var builder = new SqlConnectionStringBuilder(connectionString)
            {
                TrustServerCertificate = true,
                Encrypt = true
            };
            return builder.ConnectionString;
        }

        private void SetConnectionCount()
        {
            int cnt = collectionConfig.SourceConnections.Count;
            lnkSourceConnections.Text = "Source Connections: " + cnt;
            if (cnt == 0)
            {
                lnkSourceConnections.Text += ".  (Add source connections to monitor)";
                lnkSourceConnections.LinkColor = DashColors.Warning;
            }
            else
            {
                lnkSourceConnections.LinkColor = DashColors.Success;
            }
        }

        private bool IsSetFromJson;

        private void SetFromJson(string json)
        {
            IsSetFromJson = true;
            try
            {
                collectionConfig = CollectionConfig.Deserialize(json);
                txtDestination.Text = collectionConfig.DestinationConnection.EncryptedConnectionString;
                chkScanAzureDB.Checked = collectionConfig.ScanForAzureDBs;
                chkScanEvery.Checked = collectionConfig.ScanForAzureDBsInterval > 0;
                numAzureScanInterval.Value = collectionConfig.ScanForAzureDBsInterval;
                chkAutoUpgradeRepoDB.Checked = collectionConfig.AutoUpdateDatabase;
                chkLogInternalPerfCounters.Checked = collectionConfig.LogInternalPerformanceCounters;
                chkDefaultIdentityCollection.Checked = !collectionConfig.IdentityCollectionThreshold.HasValue;
                numIdentityCollectionThreshold.Value = collectionConfig.IdentityCollectionThreshold ??
                                                       DBCollector.DefaultIdentityCollectionThreshold;
                numBackupRetention.Value = collectionConfig.ConfigBackupRetentionDays;
                txtSummaryRefreshCron.Text = collectionConfig.SummaryRefreshCron;
                chkSummaryRefresh.Checked = !string.IsNullOrEmpty(collectionConfig.SummaryRefreshCron);
                chkEnableMessaging.Checked = collectionConfig.EnableMessaging;
                txtSQS.Text = collectionConfig.ServiceSQSQueueUrl;
                chkAllowPlanForcing.Checked = collectionConfig.AllowPlanForcing;
                txtAllowScripts.Text = collectionConfig.AllowedScripts;
                txtAllowedJobs.Text = collectionConfig.AllowedJobs;
                chkProcessAlerts.Checked = collectionConfig.ProcessAlerts;
                chkAlertPollingFrequency.Checked = collectionConfig.AlertProcessingFrequencySeconds != null;
                numAlertPollingFrequency.Value = collectionConfig.AlertProcessingFrequencySeconds ?? CollectionConfig.DefaultAlertProcessingFrequencySeconds;
                numAlertPollingFrequency.Enabled = collectionConfig.AlertProcessingFrequencySeconds != null &&
                                                   collectionConfig.ProcessAlerts;
                chkAlertPollingFrequency.Enabled = collectionConfig.ProcessAlerts;
                chkAlertStartupDelay.Checked = collectionConfig.AlertProcessingStartupDelaySeconds != null;
                numAlertStartupDelay.Value = collectionConfig.AlertProcessingStartupDelaySeconds ??
                                             CollectionConfig.DefaultAlertProcessingStartupDelaySeconds;
                numAlertStartupDelay.Enabled = collectionConfig.AlertProcessingStartupDelaySeconds != null && collectionConfig.ProcessAlerts;
                chkAlertStartupDelay.Enabled = collectionConfig.ProcessAlerts;

                UpdateSummaryCron();
                UpdateScanInterval();
                SetDgv();
                RefreshEncryption();
                UpdateCustomCollectionCount();
            }
            finally
            {
                IsSetFromJson = false;
            }
        }

        private void UpdateCustomCollectionCount()
        {
            bttnCustomCollections.Text = $"Custom Collections ({collectionConfig.CustomCollections.Count})";
            bttnCustomCollections.Font = collectionConfig.CustomCollections.Count > 0
                ? new Font(bttnCustomCollections.Font, FontStyle.Bold)
                : new Font(bttnCustomCollections.Font, FontStyle.Regular);
            toolTip1.SetToolTip(bttnCustomCollections,
                collectionConfig.CustomCollections.Count > 0
                    ? string.Join(", ", collectionConfig.CustomCollections.Keys.OrderBy(k => k))
                    : "No Custom Collections Defined");
        }

        private void SetJson()
        {
            if (IsSetFromJson) return;
            txtJson.Text = collectionConfig.Serialize();
        }

        private void SetDgv()
        {
            dgvConnections.DataSource = new BindingSource() { DataSource = collectionConfig.SourceConnections };
            ApplySearch();
        }

        private string lastStatus = string.Empty;
        private const string NotInstalled = "Not Installed";

        private async Task AutoRefreshServiceStatus()
        {
            while (true)
            {
                // Only refresh service status if there has been user activity within the last 5min & selected tab is tab with service control/status
                var isDestTabSelected = tab1.Invoke(new Func<bool>(() => tab1.SelectedTab == tabDest));
                if (DateTime.Now.Subtract(lastUserActivity).TotalMinutes < 5 && isDestTabSelected)
                {
                    RefreshServiceStatus();
                    if (RepoDbVersionStatus?.VersionStatus is DBValidations.DBVersionStatusEnum.CreateDB
                        or DBValidations.DBVersionStatusEnum.UpgradeRequired)
                    {
                        try
                        {
                            UpdateDBVersionStatus();
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex);
                        }
                    }
                }
                // Use a short 5 second delay between refreshes if the app is loaded within the last 2min or the user has started/stopped or installed the service.
                // Otherwise use a longer 30 second delay between iterations
                await Task.Delay(DateTime.Now.Subtract(lastServiceActivity).TotalMinutes < 2 ? 5000 : 30000);
            }
            // ReSharper disable once FunctionNeverReturns
        }

        private void RefreshServiceStatus()
        {
            try
            {
                using var service = new ServiceController(collectionConfig.ServiceName);
                var nameOfServiceFromPath = ServiceTools.GetServiceNameFromPath();
                var pathOfService = ServiceTools.GetPathOfService(collectionConfig.ServiceName);
                var warningText = string.Empty;
                var status = service.Status;
                if (!pathOfService.Contains(ServiceTools.ServicePath,
                        StringComparison.CurrentCultureIgnoreCase) &&
                    pathOfService != string.Empty)
                {
                    warningText = $"Warning service with name {collectionConfig.ServiceName} is installed at a different location: {pathOfService}";
                }
                else if (nameOfServiceFromPath != collectionConfig.ServiceName &&
                         nameOfServiceFromPath != string.Empty)
                {
                    warningText =
                        $"Warning: Service name from path {nameOfServiceFromPath} does not match the service name {collectionConfig.ServiceName} in ServiceConfig.json.";
                }

                if (service.Status.ToString() != lastStatus || lblServiceWarning.Text != warningText)
                {
                    this.Invoke(() =>
                    {
                        lblServiceWarning.Visible = !string.IsNullOrEmpty(warningText);
                        lblServiceWarning.Text = warningText;
                        isInstalled = true;
                        lblServiceStatus.Text =
                            "Service Status: " + Enum.GetName(typeof(ServiceControllerStatus), status);
                        lblServiceStatus.ForeColor = status switch
                        {
                            ServiceControllerStatus.Running => Color.Green,
                            ServiceControllerStatus.Stopped or ServiceControllerStatus.StopPending
                                or ServiceControllerStatus.Paused => Color.Red,
                            _ => Color.Orange
                        };
                        lnkStart.Enabled = (status == ServiceControllerStatus.Stopped);
                        lnkStop.Enabled = (status == ServiceControllerStatus.Running);
                        lnkInstall.Font = new Font(lnkInstall.Font, FontStyle.Regular);
                        toolTip1.SetToolTip(lnkInstall, "Remove Windows service for DBA Dash agent");
                        lnkInstall.Text = "Uninstall service";
                        lastStatus = status.ToString();
                    });
                }
                ProcessFatalError();
            }
            catch (Exception ex)
            {
                string status, warningText = string.Empty, warningToolTip = string.Empty;
                if (ex is InvalidOperationException && ex.Message.Contains("not found"))
                {
                    status = NotInstalled;
                }
                else
                {
                    status = "Error";
                    warningText = $"Error checking service {ex.Message}";
                    warningToolTip = ex.ToString();
                }

                if (lastStatus != status || lblServiceWarning.Text != warningText)
                {
                    this.Invoke(() =>
                    {
                        lblServiceWarning.Text = warningText;
                        toolTip1.SetToolTip(lblServiceWarning, warningToolTip);
                        lblServiceWarning.Visible = !(string.IsNullOrEmpty(warningText));
                        isInstalled = false;
                        lnkStart.Enabled = status != NotInstalled;
                        lnkStop.Enabled = status != NotInstalled;
                        lnkInstall.Enabled = true;
                        lnkInstall.Font = new Font(lnkInstall.Font, FontStyle.Bold);
                        lnkInstall.Text = "Install as service";
                        toolTip1.SetToolTip(lnkInstall, "Install DBA Dash agent as a Windows service");
                        lblServiceStatus.Text = "Service Status: " + status;
                        lblServiceStatus.ForeColor = Color.Brown;
                        lastStatus = NotInstalled;
                    });
                }
            }
        }

        private void TxtJson_Validating(object sender, CancelEventArgs e)
        {
            errorProvider1.SetError(txtJson, null);
            if (txtJson.Text.Trim() == "")
            {
                collectionConfig = new CollectionConfig();
                return;
            }

            try
            {
                SetFromJson(txtJson.Text);
            }
            catch (Exception ex)
            {
                errorProvider1.SetError(txtJson, ex.Message);
                CommonShared.ShowExceptionDialog(ex, "SetFromJson error");
            }
        }

        private void ServiceConfig_FromClosing(object sender, FormClosingEventArgs e)
        {
            PromptSaveChanges();
        }

        private void PromptSaveChanges()
        {
            if (originalJson != txtJson.Text)
            {
                if (MessageBox.Show("Save Changes?", "Save", MessageBoxButtons.YesNo, MessageBoxIcon.Question) ==
                    DialogResult.Yes)
                {
                    SaveChanges();
                }
            }
        }

        private void StartService()
        {
            PromptSaveChanges();
            try
            {
                lastServiceActivity = DateTime.Now;
                using var service = new ServiceController(collectionConfig.ServiceName);
                service.Refresh();
                if (service.Status == ServiceControllerStatus.Stopped)
                {
                    service.Start();
                    System.Threading.Thread.Sleep(500);
                }
            }
            catch (Exception ex)
            {
                if (File.Exists(FatalErrorFilePath))
                {
                    ProcessFatalError();
                }
                else
                {
                    PromptError(ex);
                }
            }
            RefreshServiceStatus();
            ValidateDestination();
        }

        private void ProcessFatalError()
        {
            if (!File.Exists(FatalErrorFilePath)) return;

            var lastWrite = File.GetLastWriteTime(FatalErrorFilePath);
            if (lastFatalErrorPrompt >= lastWrite) return;
            if (this.InvokeRequired)
            {
                Invoke(ProcessFatalError);
                return;
            }
            lastFatalErrorPrompt = lastWrite;
            var fatalError = File.ReadAllText(FatalErrorFilePath);
            if (fatalError.Contains(nameof(ConfigDecryptionError)))
            {
                if (MessageBox.Show(
                        "The service was unable to decrypt the config file, preventing it from starting.  Do you want to create a temporary key and retry?",
                        "Decryption Error", MessageBoxButtons.YesNo, MessageBoxIcon.Error) != DialogResult.Yes) return;
                try
                {
                    EncryptedConfig.CreateTemporaryKey();
                }
                catch (Exception ex)
                {
                    CommonShared.ShowExceptionDialog(ex, "Error creating temporary key");
                    return;
                }
                StartService();
            }
            else
            {
                CommonShared.ShowExceptionDialog($"Warning, there was fatal error last time the service started at {lastWrite}: ", fatalError.Split("\n")[0], fatalError, "Fatal error starting service", TaskDialogIcon.Warning);
            }
        }

        private static string GetMessages(Exception ex)
        {
            var sbError = new StringBuilder();
            sbError.Append(ex.Message);
            if (ex.InnerException != null)
            {
                sbError.AppendLine();
                sbError.AppendLine();
                sbError.Append(ex.InnerException.Message);
            }
            return sbError.ToString();
        }

        private static void PromptError(Exception ex)
        {
            var message = GetMessages(ex);
            CommonShared.ShowExceptionDialog(ex, message);
        }

        private void BttnStop_Click(object sender, EventArgs e)
        {
            StopService();
        }

        private void StopService()
        {
            try
            {
                lastServiceActivity = DateTime.Now;
                using var service = new ServiceController(collectionConfig.ServiceName);
                if (service.Status == ServiceControllerStatus.Running)
                {
                    service.Stop();
                    System.Threading.Thread.Sleep(500);
                }
            }
            catch (Exception ex)
            {
                PromptError(ex);
            }
            RefreshServiceStatus();
        }

        private void BttnRefresh_Click(object sender, EventArgs e)
        {
            RefreshServiceStatus();
            ValidateDestination();
        }

        private void BttnUninstall_Click(object sender, EventArgs e)
        {
            UninstallService();
        }

        private void UninstallService()
        {
            if (MessageBox.Show("Are you sure you want to remove the DBA Dash Windows service?", "Uninstall",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) == DialogResult.Yes)
            {
                try
                {
                    lastServiceActivity = DateTime.Now;
                    var result = ServiceTools.UninstallService(collectionConfig.ServiceName);
                    MessageBox.Show(result.Output, "Uninstall", MessageBoxButtons.OK,
                        result.Success ? MessageBoxIcon.Information : MessageBoxIcon.Error);
                }
                catch (Exception ex)
                {
                    PromptError(ex);
                }

                System.Threading.Thread.Sleep(500);
                RefreshServiceStatus();
            }
        }

        private void BttnCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void TxtDestination_Validated(object sender, EventArgs e)
        {
            DestinationChanged();
        }

        private void DestinationChanged()
        {
            if (collectionConfig.Destination != txtDestination.Text)
            {
                try
                {
                    collectionConfig.Destination = txtDestination.Text;
                }
                catch (Exception ex)
                {
                    errorProvider1.SetError(txtDestination, ex.Message);
                    return;
                }

                SetJson();
                ValidateDestination();
                RefreshEncryption();
            }
        }

        private void ChkSlowQueryThreshold_CheckedChanged(object sender, EventArgs e)
        {
            numSlowQueryThreshold.Enabled = chkSlowQueryThreshold.Checked;
            if (chkSlowQueryThreshold.Checked)
            {
                numSlowQueryThreshold.Value = 1000;
                lblSlow.Text = "Extended events trace to capture slow rpc and batch completed events IS enabled";
            }
            else
            {
                numSlowQueryThreshold.Value = -1;
                lblSlow.Text = "Extended events trace to capture slow rpc and batch completed events is NOT enabled";
            }

            chkDualSession.Enabled = chkSlowQueryThreshold.Checked;
            chkPersistXESession.Enabled = chkSlowQueryThreshold.Checked;
        }

        private void LoadConnectionForEdit(DBADashSource src)
        {
            if (src != null)
            {
                txtSource.Text = src.ConnectionString;
                chkNoWMI.Checked = src.NoWMI;
                chkPersistXESession.Checked = src.PersistXESessions;
                chkSlowQueryThreshold.Checked = (src.SlowQueryThresholdMs != -1);
                chkScriptJobs.Checked = src.ScriptAgentJobs;
                numSlowQueryThreshold.Value = chkSlowQueryThreshold.Checked ? src.SlowQueryThresholdMs : 0;

                txtSnapshotDBs.Text = src.SchemaSnapshotDBs;
                chkDualSession.Checked = src.UseDualEventSession;
                if (src.RunningQueryPlanThreshold != null)
                {
                    txtCountThreshold.Text = src.RunningQueryPlanThreshold.CountThreshold.ToString();
                    txtDurationThreshold.Text = src.RunningQueryPlanThreshold.DurationThreshold.ToString();
                    txtGrantThreshold.Text = src.RunningQueryPlanThreshold.MemoryGrantThreshold.ToString();
                    txtCPUThreshold.Text = src.RunningQueryPlanThreshold.CPUThreshold.ToString();
                    chkCollectPlans.Checked = src.RunningQueryPlanThreshold.PlanCollectionEnabled;
                }
                else
                {
                    chkCollectPlans.Checked = false;
                }

                cboIOLevel.SelectedItem = src.IOCollectionLevel;
            }
            else
            {
                src.RunningQueryPlanThreshold = null;
            }

            CustomCollectionsNew = src.CustomCollections;
            SetAvailableOptionsForSource();
        }

        private void BttnDeployDatabase_Click(object sender, EventArgs e)
        {
            var frm = new DBDeploy();
            var cn = new DBADashConnection(txtDestination.Text);
            if (cn.Type == ConnectionType.SQL)
            {
                frm.ConnectionString = cn.ConnectionString;
            }
            else
            {
                if (SetDestination())
                {
                    cn = new DBADashConnection(txtDestination.Text);
                    frm.ConnectionString = cn.ConnectionString;
                }
                else
                {
                    return;
                }
            }

            frm.ShowDialog();
            if (frm.DatabaseName != cn.InitialCatalog())
            {
                if (MessageBox.Show("Update connection string?", "Update", MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    var builder = new SqlConnectionStringBuilder(txtDestination.Text)
                    {
                        InitialCatalog = frm.DatabaseName
                    };
                    txtDestination.Text = builder.ConnectionString;
                    DestinationChanged();
                }
            }

            ValidateDestination();
        }

        private bool SetDestination()
        {
            var frm = new DBConnection();
            var cn = new DBADashConnection(txtDestination.Text);
            if (cn.Type == ConnectionType.SQL)
            {
                frm.ConnectionString = cn.ConnectionString;
            }
            else
            {
                var builder = new SqlConnectionStringBuilder()
                {
                    DataSource = Environment.MachineName,
                    InitialCatalog = "DBADashDB",
                    IntegratedSecurity = true,
                    Encrypt = true,
                };

                frm.ConnectionString = builder.ConnectionString;
            }

            frm.ShowDialog();
            if (frm.DialogResult == DialogResult.OK)
            {
                var builder = new SqlConnectionStringBuilder(frm.ConnectionString);
                if (string.IsNullOrEmpty(builder.InitialCatalog))
                {
                    builder.InitialCatalog = "DBADashDB";
                }

                cn = new DBADashConnection(builder.ConnectionString);

                txtDestination.Text = cn.EncryptedConnectionString;
                DestinationChanged();
                return true;
            }

            return false;
        }

        private void BttnConnect_Click(object sender, EventArgs e)
        {
            SetDestination();
        }

        private void BttnConnectSource_Click(object sender, EventArgs e)
        {
            var frm = new DBConnection();
            var cn = new DBADashConnection(txtSource.Text);
            if (cn.Type == ConnectionType.SQL)
            {
                frm.ConnectionString = cn.ConnectionString;
            }

            frm.ValidateInitialCatalog = true;
            frm.ShowDialog();
            if (frm.DialogResult == DialogResult.OK)
            {
                cn = new DBADashConnection(frm.ConnectionString);
                txtSource.Text = cn.EncryptedConnectionString;
            }

            SetAvailableOptionsForSource();
        }

        private void BttnScanNow_Click(object sender, EventArgs e)
        {
            var newConnections = collectionConfig.GetNewAzureDBConnections();
            if (newConnections.Count == 0)
            {
                MessageBox.Show("No new Azure DB connections found", "Scan", MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            else
            {
                if (MessageBox.Show($"Found {newConnections.Count} new connections.  Add connections to config file?",
                        "Scan", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    collectionConfig.AddConnections(newConnections);
                    SetJson();
                    SetConnectionCount();
                    SetDgv();
                }
            }
        }

        private void ChkScanAzureDB_CheckedChanged(object sender, EventArgs e)
        {
            collectionConfig.ScanForAzureDBs = chkScanAzureDB.Checked;
            SetJson();
        }

        private void ChkAutoUpgradeRepoDB_CheckedChanged(object sender, EventArgs e)
        {
            collectionConfig.AutoUpdateDatabase = chkAutoUpgradeRepoDB.Checked;
            SetJson();
        }

        private void ChkScanEvery_CheckedChanged(object sender, EventArgs e)
        {
            if (numAzureScanInterval.Value == 0 && chkScanEvery.Checked)
            {
                numAzureScanInterval.Value = 3600;
            }

            if (!chkScanEvery.Checked)
            {
                numAzureScanInterval.Value = 0;
            }

            collectionConfig.ScanForAzureDBsInterval = Convert.ToInt32(numAzureScanInterval.Value);
            UpdateScanInterval();
            SetJson();
        }

        private void UpdateScanInterval()
        {
            lblHHmm.Visible = chkScanEvery.Checked;
            lblHHmm.Text = TimeSpan.FromSeconds(Convert.ToInt32(numAzureScanInterval.Value)).ToString();
        }

        private void NumAzureScanInterval_ValueChanged(object sender, EventArgs e)
        {
            chkScanEvery.Checked = numAzureScanInterval.Value > 0;
            collectionConfig.ScanForAzureDBsInterval = Convert.ToInt32(numAzureScanInterval.Value);
            UpdateScanInterval();
            SetJson();
        }

        private void LnkCronBuilder_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var psi = new ProcessStartInfo("http://www.cronmaker.com/") { UseShellExecute = true };
            Process.Start(psi);
        }

        private void BttnDestFolder_Click(object sender, EventArgs e)
        {
            using var fbd = new FolderBrowserDialog();
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                txtDestination.Text = fbd.SelectedPath;
                DestinationChanged();
            }
        }

        private void BttnSrcFolder_Click_1(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    txtSource.Text = fbd.SelectedPath;
                }
            }

            SetAvailableOptionsForSource();
        }

        private void BttnS3_Click(object sender, EventArgs e)
        {
            var cfg = new CollectionConfig
            {
                AccessKey = collectionConfig.AccessKey,
                SecretKey = collectionConfig.SecretKey,
                AWSProfile = collectionConfig.AWSProfile
            };

            using var frm = new S3Browser()
            {
                AccessKey = cfg.AccessKey,
                SecretKey = cfg.GetSecretKey(),
                Folder = "DBADash_" + Environment.MachineName
            };
            frm.ShowDialog();
            if (frm.DialogResult == DialogResult.OK)
            {
                txtDestination.Text = frm.AWSURL;
                DestinationChanged();
            }
        }

        private void BttnS3Src_Click(object sender, EventArgs e)
        {
            var cfg = new CollectionConfig
            {
                AccessKey = collectionConfig.AccessKey,
                SecretKey = collectionConfig.SecretKey,
                AWSProfile = collectionConfig.AWSProfile
            };
            using (var frm = new S3Browser()
            { AccessKey = cfg.AccessKey, SecretKey = cfg.GetSecretKey(), Folder = "DBADash_{HostName}" })
            {
                frm.ShowDialog();
                if (frm.DialogResult == DialogResult.OK)
                {
                    txtSource.Text = frm.AWSURL;
                }
            }

            SetAvailableOptionsForSource();
        }

        private void LnkSourceConnections_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            tab1.SelectedTab = tabSource;
        }

        private void LnkPermissions_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var psi = new ProcessStartInfo("https://github.com/DavidWiseman/DBADash/blob/develop/Docs/Security.md")
            { UseShellExecute = true };
            Process.Start(psi);
        }

        private void CboSource_Validated(object sender, EventArgs e)
        {
            SetAvailableOptionsForSource();
        }

        private void SetAvailableOptionsForSource()
        {
            var src = new DBADashSource(txtSource.Text);
            bool isSql = src.SourceConnection.Type == ConnectionType.SQL;

            pnlExtendedEvents.Enabled = isSql;
            chkCollectPlans.Enabled = isSql;
            grpRunningQueryThreshold.Enabled = isSql && chkCollectPlans.Checked;
            chkNoWMI.Enabled = isSql;
            chkScriptJobs.Enabled = isSql;
            cboIOLevel.Enabled = isSql;
            txtSnapshotDBs.Enabled = isSql;
            lblIOCollectionLevel.Enabled = isSql;
            lblSchemaSnapshotDBs.Enabled = isSql;
            chkCollectSessionWaits.Enabled = isSql;
            lnkALL.Enabled = isSql;
            lnkExample.Enabled = isSql;
            lnkNone.Enabled = isSql;
            chkScriptJobs.Enabled = isSql;
        }

        private void ChkLogInternalPerfCounters_CheckedChanged(object sender, EventArgs e)
        {
            collectionConfig.LogInternalPerformanceCounters = chkLogInternalPerfCounters.Checked;
            SetJson();
        }

        private void BttnSchedule_Click(object sender, EventArgs e)
        {
            var frm = new ScheduleConfig()
            {
                ConfiguredSchedule = collectionConfig.CollectionSchedules
            };

            frm.ShowDialog();
            if (frm.DialogResult == DialogResult.OK)
            {
                collectionConfig.CollectionSchedules = frm.ConfiguredSchedule;
                SetJson();
            }
        }

        private void Dgv_RowValidated(object sender, DataGridViewCellEventArgs e)
        {
            UpdateFromGrid();
        }

        private void UpdateFromGrid()
        {
            SetJson();
            SetConnectionCount();
            RefreshEncryption();
        }

        private void DgvConnections_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == dgvConnections.Columns["Delete"].Index)
            {
                var src = (DBADashSource)dgvConnections.Rows[e.RowIndex].DataBoundItem;
                switch (PromptMarkDeleted(src))
                {
                    case DialogResult.OK:
                    case DialogResult.No:// Remove only
                        dgvConnections.Rows.RemoveAt(e.RowIndex);
                        break;

                    case DialogResult.Yes: // Remove and delete
                        DeleteInstance(src);
                        dgvConnections.Rows.RemoveAt(e.RowIndex);
                        break;

                    case DialogResult.Cancel:
                        return;
                }
                ApplySearch();
            }
            else if (e.RowIndex >= 0 && e.ColumnIndex == dgvConnections.Columns["Edit"].Index)
            {
                var src = (DBADashSource)dgvConnections.Rows[e.RowIndex].DataBoundItem;
                LoadConnectionForEdit(src);
            }
            else if (e.RowIndex >= 0 &&
                     dgvConnections.Columns[e.ColumnIndex].CellType == typeof(DataGridViewCheckBoxCell))
            {
                dgvConnections.EndEdit();
            }
            else if (e.RowIndex >= 0 && e.ColumnIndex == dgvConnections.Columns["Schedule"].Index)
            {
                var src = (DBADashSource)dgvConnections.Rows[e.RowIndex].DataBoundItem;
                if (src.SourceConnection.Type == ConnectionType.SQL)
                {
                    var frm = new ScheduleConfig()
                    {
                        ConfiguredSchedule = src.CollectionSchedules,
                        BaseSchedule = collectionConfig.GetSchedules()
                    };
                    frm.ShowDialog();
                    if (frm.DialogResult == DialogResult.OK)
                    {
                        src.CollectionSchedules = frm.ConfiguredSchedule;
                        dgvConnections.Refresh();
                    }
                }
                else
                {
                    MessageBox.Show("Custom schedule configuration is only available for SQL connections", "Schedule",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else if (e.RowIndex >= 0 && e.ColumnIndex == dgvConnections.Columns["CustomCollections"].Index)
            {
                var src = (DBADashSource)dgvConnections.Rows[e.RowIndex].DataBoundItem;
                if (src.SourceConnection.Type == ConnectionType.SQL)
                {
                    var frm = new ManageCustomCollections()
                    {
                        CustomCollections = src.CustomCollections,
                        ConnectionString = src.SourceConnection.ConnectionString
                    };
                    frm.ShowDialog();
                    if (frm.DialogResult == DialogResult.OK)
                    {
                        src.CustomCollections = frm.CustomCollections;
                        SetDgv();
                    }
                }
                else
                {
                    MessageBox.Show("Custom collections are only available for SQL connections", "Custom Collections",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }

            UpdateFromGrid();
        }

        private void Dgv_UserDeletedRow(object sender, DataGridViewRowEventArgs e)
        {
            UpdateFromGrid();
        }

        private void Dgv_UserDeletingRow(object sender, DataGridViewRowCancelEventArgs e)
        {
            var src = (DBADashSource)e.Row.DataBoundItem;
            switch (PromptMarkDeleted(src))
            {
                case DialogResult.OK:
                case DialogResult.No:// Remove only
                    break;

                case DialogResult.Yes: // Remove and delete
                    DeleteInstance(src);
                    break;

                case DialogResult.Cancel:
                    e.Cancel = true;
                    break;
            }
        }

        private DialogResult PromptMarkDeleted(DBADashSource src)
        {
            if (!collectionConfig.SQLDestinations.Any() || src.ConnectionID == null)
            {
                if (cboDeleteAction.SelectedIndex > 0)
                {
                    return DialogResult.OK;
                }
                return MessageBox.Show(
                    @$"Remove connection '{src.ConnectionID ?? src.SourceConnection.DataSource()}' from configuration?",
                    @"Remove?", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
            }

            switch (cboDeleteAction.SelectedIndex)
            {
                case 1:
                    return DialogResult.No;

                case 2:
                    return DialogResult.Yes;

                default:
                    {
                        var removeAndMarkDeleted =
                            new TaskDialogRadioButton($"Remove from config && mark deleted");
                        var removeOnly = new TaskDialogRadioButton("Remove from config only") { Checked = true };
                        var page = new TaskDialogPage
                        {
                            Heading =
                                $"Would you like to remove remove '{src.ConnectionID ?? src.SourceConnection.DataSource()}' from config only or also mark it deleted in the repository database?",
                            Caption = "Delete Connection",
                            Text =
                                "💡A connection marked deleted can be restored using the recycle bin folder in the DBA Dash GUI.",
                            Icon = TaskDialogIcon.Warning,
                            Buttons = new TaskDialogButtonCollection() { TaskDialogButton.OK, TaskDialogButton.Cancel },
                            SizeToContent = true,
                            RadioButtons = new TaskDialogRadioButtonCollection() { removeOnly, removeAndMarkDeleted }
                        };

                        if (TaskDialog.ShowDialog(page) != TaskDialogButton.OK) return DialogResult.Cancel;

                        return removeAndMarkDeleted.Checked ? DialogResult.Yes :
                            removeOnly.Checked ? DialogResult.No : DialogResult.Cancel;
                    }
            }
        }

        private void DeleteInstance(DBADashSource src)
        {
            var exceptions = new List<Exception>();
            foreach (var dest in collectionConfig.SQLDestinations)
            {
                try
                {
                    SharedData.MarkInstanceDeleted(src.ConnectionID, dest.ConnectionString);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }
            if (exceptions.Count > 0)
            {
                CommonShared.ShowExceptionDialog(new AggregateException(exceptions),
                    string.Join(Environment.NewLine, exceptions.Select(ex => ex.Message)).Truncate(500));
            }
        }

        private void TxtSearch_TextChanged(object sender, EventArgs e)
        {
            ApplySearch();
        }

        private void ApplySearch()
        {
            CurrencyManager currencyManager1 = (CurrencyManager)BindingContext[dgvConnections.DataSource];
            currencyManager1.SuspendBinding();
            foreach (DataGridViewRow row in dgvConnections.Rows)
            {
                if (string.IsNullOrEmpty(txtSearch.Text) ||
                    (row.Cells["ConnectionString"].Value as string ?? "").Contains(txtSearch.Text,
                        StringComparison.CurrentCultureIgnoreCase) ||
                    (row.Cells["ConnectionID"].Value as string ?? "").Contains(txtSearch.Text,
                        StringComparison.CurrentCultureIgnoreCase))
                {
                    row.Visible = true;
                }
                else
                {
                    row.Visible = false;
                }
            }

            currencyManager1.ResumeBinding();
        }

        private void ChkCollectPlans_CheckedChanged(object sender, EventArgs e)
        {
            grpRunningQueryThreshold.Enabled = chkCollectPlans.Checked;
            if (chkCollectPlans.Checked)
            {
                var defaultThreshold = PlanCollectionThreshold.DefaultThreshold;
                txtCPUThreshold.Text = defaultThreshold.CPUThreshold.ToString();
                txtCountThreshold.Text = defaultThreshold.CountThreshold.ToString();
                txtDurationThreshold.Text = defaultThreshold.DurationThreshold.ToString();
                txtGrantThreshold.Text = defaultThreshold.MemoryGrantThreshold.ToString();
            }
        }

        private void LnkALL_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            txtSnapshotDBs.Text = "*";
        }

        private void LnkNone_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            txtSnapshotDBs.Text = string.Empty;
        }

        private void LnkExample_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            txtSnapshotDBs.Text = "Database1,Database2,Database3";
        }

        private void Dgv_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            var hasStatus = dgvConnections.Columns.Contains("ConnectionStatus") && ConnectionStatuses != null;
            var hasSnapshotStatus = dgvConnections.Columns.Contains("SnapshotAge") && SnapshotDates != null;
            for (var i = e.RowIndex; i < e.RowIndex + e.RowCount; i++)
            {
                var src = (DBADashSource)dgvConnections.Rows[i].DataBoundItem;
                SetCellsReadOnly(i, src.SourceConnection.Type);
                if (src.SourceConnection.Type == ConnectionType.SQL)
                {
                    dgvConnections.Rows[i].Cells["CustomCollections"].Value =
                        src.CustomCollections == null || src.CustomCollections.Keys.Count == 0
                            ? "Add Collection"
                            : string.Join(", ", src.CustomCollections.Keys.OrderBy(key => key));
                }
                var row = dgvConnections.Rows[i];
                if (hasStatus)
                {
                    var status = ConnectionStatuses.ContainsKey(src.ConnectionString) ? ConnectionStatuses[src.ConnectionString] : new("Unknown", DashColors.NotApplicable, 2);

                    row.Cells["ConnectionStatus"].Value = status.status;
                    row.Cells["ConnectionStatus"].Style.BackColor = status.statusColor;
                    row.Cells["ConnectionStatus"].Style.ForeColor = status.statusColor.ContrastColor();
                    row.Cells["ConnectionStatus"].Style.SelectionBackColor = status.statusColor.AdjustBasedOnLuminance();
                    row.Cells["ConnectionStatus"].Style.SelectionForeColor = status.statusColor.AdjustBasedOnLuminance().ContrastColor();
                }
                if (!hasSnapshotStatus || src.SourceConnection.Type != ConnectionType.SQL) continue;

                string value;
                Color primaryBackColor;
                var toolTipText = string.Empty;
                if (src.ConnectionID == null)
                {
                    value = "Skipped";
                    primaryBackColor = DashColors.NotApplicable;
                }
                else if (SnapshotDates.TryGetValue(src.ConnectionID, out var snapshotDate))
                {
                    var age = SnapshotDatesLastChecked - snapshotDate;
                    value = age.Humanize(3);
                    toolTipText =
                        snapshotDate.ToLocalTime().ToString(CultureInfo.CurrentCulture);
                    primaryBackColor = age.TotalDays > 7 ? DashColors.Fail :
                         age.TotalHours > 24 ? DashColors.Warning : DashColors.Success;
                }
                else
                {
                    value = "Not found";
                    primaryBackColor = DashColors.Warning;
                }
                row.Cells["SnapshotAge"].Value = value;
                row.Cells["SnapshotAge"].ToolTipText = toolTipText;
                row.Cells["SnapshotAge"].Style.BackColor = primaryBackColor;
                row.Cells["SnapshotAge"].Style.ForeColor = primaryBackColor.ContrastColor();
                row.Cells["SnapshotAge"].Style.SelectionBackColor = primaryBackColor.AdjustBasedOnLuminance();
                row.Cells["SnapshotAge"].Style.SelectionForeColor = primaryBackColor.AdjustBasedOnLuminance().ContrastColor();
            }
        }

        private void SetCellsReadOnly(int row, ConnectionType connectionType)
        {
            foreach (DataGridViewColumn col in dgvConnections.Columns)
            {
                var isReadOnly = connectionType != ConnectionType.SQL &&
                                 !(col.DataPropertyName == "WriteToSecondaryDestinations" ||
                                   col.Name is "Delete" or "Edit");

                dgvConnections.Rows[row].Cells[col.Index].ReadOnly = isReadOnly;
                dgvConnections.Rows[row].Cells[col.Index].Style.BackColor = isReadOnly ? Color.Silver : Color.White;
            }
        }

        private void TxtJson_TextChanged(object sender, EventArgs e)
        {
            UpdateSaveButton();
        }

        private void UpdateSaveButton()
        {
            bttnSave.Font = txtJson.Text != originalJson
                ? new Font(bttnSave.Font, FontStyle.Bold)
                : new Font(bttnSave.Font, FontStyle.Regular);
        }

        private void LnkStart_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            StartService();
        }

        private void LnkStop_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            StopService();
        }

        private void LnkRefresh_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            RefreshServiceStatus();
            ValidateDestination(); // DB could be upgraded on service start so refresh destination
        }

        private void LnkInstall_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (isInstalled)
            {
                UninstallService();
            }
            else
            {
                if (txtJson.Text != originalJson)
                {
                    if (MessageBox.Show("Save changes to config", "Save", MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        SaveChanges();
                    }
                }

                if (!BasicConfig.ConfigExists)
                {
                    MessageBox.Show("Please configure the service and save changes before installing as a service.",
                        "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return;
                }

                var frm = new InstallService
                {
                    ServiceName = collectionConfig.ServiceName
                };
                frm.ShowDialog();
                lastServiceActivity = DateTime.Now;
                RefreshServiceStatus();
                ValidateDestination(); // DB could be upgraded on service start so refresh destination
            }
        }

        private static ServiceLog ServiceLogForm;

        private void BttnViewServiceLog_Click(object sender, EventArgs e)
        {
            if (ServiceLogForm == null)
            {
                ServiceLogForm = new();
                ServiceLogForm.FormClosed += delegate { ServiceLogForm = null; };
            }

            ServiceLogForm.Show();
            ServiceLogForm.Focus();
        }

        private void BttnAbout_Click(object sender, EventArgs e)
        {
            if (collectionConfig.DestinationConnection.Type == ConnectionType.SQL)
            {
                CommonShared.ShowAbout(collectionConfig.DestinationConnection.ConnectionString, this, false);
            }
            else
            {
                CommonShared.ShowAbout(this, false);
            }
        }

        private void ChkDefaultIdentityCollection_CheckedChanged(object sender, EventArgs e)
        {
            numIdentityCollectionThreshold.Enabled = !chkDefaultIdentityCollection.Checked;
            collectionConfig.IdentityCollectionThreshold = chkDefaultIdentityCollection.Checked
                ? null
                : (int)numIdentityCollectionThreshold.Value;
            SetJson();
        }

        private void NumIdentityCollectionThreshold_ValueChanged(object sender, EventArgs e)
        {
            collectionConfig.IdentityCollectionThreshold = chkDefaultIdentityCollection.Checked
                ? null
                : (int)numIdentityCollectionThreshold.Value;
            SetJson();
        }

        private void BttnAWS_Click(object sender, EventArgs e)
        {
            var frm = new AWSCreds()
            {
                AWSAccessKey = collectionConfig.AccessKey,
                AWSSecretKet = collectionConfig.SecretKey,
                AWSProfile = collectionConfig.AWSProfile
            };
            frm.ShowDialog();
            if (frm.DialogResult == DialogResult.OK)
            {
                collectionConfig.AWSProfile = frm.AWSProfile;
                collectionConfig.SecretKey = frm.AWSSecretKet;
                collectionConfig.AccessKey = frm.AWSAccessKey;
            }

            SetJson();
            RefreshEncryption();
        }

        private void BttnEncryption_Click(object sender, EventArgs e)
        {
            var frm = new EncryptionConfig()
            {
                EncryptionOption = collectionConfig.EncryptionOption,
                EncryptionPassword = collectionConfig.EncryptionOption == BasicConfig.EncryptionOptions.Encrypt
                    ? EncryptedConfig.GetPassword()
                    : null
            };
            frm.ShowDialog();
            if (frm.DialogResult == DialogResult.OK)
            {
                collectionConfig.EncryptionOption = frm.EncryptionOption;
                if (frm.EncryptionOption == BasicConfig.EncryptionOptions.Encrypt)
                {
                    EncryptedConfig.SetPassword(frm.EncryptionPassword, true);
                }
                else
                {
                    EncryptedConfig.ClearPassword();
                }

                SaveChanges();
                RefreshEncryption();
            }
        }

        private void RefreshEncryption()
        {
            lblEncryptionStatus.Text = collectionConfig.EncryptionOption == BasicConfig.EncryptionOptions.Encrypt
                ? "Encrypted"
                : "Not Encrypted";
            lblEncryptionStatus.ForeColor = collectionConfig.EncryptionOption == BasicConfig.EncryptionOptions.Encrypt
                ? DashColors.Success
                : collectionConfig.ContainsSensitive()
                    ? DashColors.Fail
                    : DashColors.Warning;
            tabJson.Text = collectionConfig.EncryptionOption == BasicConfig.EncryptionOptions.Encrypt
                ? "Json (Decrypted)"
                : "Json";
        }

        private void NumBackupRetention_ValueChanged(object sender, EventArgs e)
        {
            collectionConfig.ConfigBackupRetentionDays = (int)numBackupRetention.Value;
            SetJson();
        }

        private void LnkDeleteConfigBackups_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (MessageBox.Show("Delete ALL config file backups?", "Delete", MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) ==
                DialogResult.Yes)
            {
                BasicConfig.ClearOldConfigBackups(0);
            }
        }

        private void ChkSummaryRefresh_CheckedChanged(object sender, EventArgs e)
        {
            txtSummaryRefreshCron.Enabled = chkSummaryRefresh.Checked;
            if (!IsSetFromJson)
            {
                txtSummaryRefreshCron.Text = chkSummaryRefresh.Checked ? "120" : string.Empty;
                UpdateSummaryCron();
                SetJson();
            }
        }

        private void TxtSummaryRefreshCron_Validated(object sender, EventArgs e)
        {
            UpdateSummaryCron();
        }

        private void UpdateSummaryCron()
        {
            try
            {
                lblSummaryRefreshCron.Text = ScheduleConfig.GetScheduleDescription(txtSummaryRefreshCron.Text);
                lblSummaryRefreshCron.ForeColor = DashColors.TrimbleBlue;
                collectionConfig.SummaryRefreshCron = txtSummaryRefreshCron.Text;
                SetJson();
            }
            catch
            {
                lblSummaryRefreshCron.Text = "Error with cron expression";
                lblSummaryRefreshCron.ForeColor = DashColors.Fail;
            }
        }

        private void BttnCustomCollections_Click(object sender, EventArgs e)
        {
            var connectionString = collectionConfig.SourceConnections
                .Where(c => c.SourceConnection.Type == ConnectionType.SQL)
                .Select(c => c.SourceConnection.ConnectionString).FirstOrDefault("");
            var dlg = new DBConnection() { ConnectionString = connectionString };
            dlg.ShowDialog();
            if (dlg.DialogResult != DialogResult.OK) return;

            var frm = new ManageCustomCollections()
            { CustomCollections = collectionConfig.CustomCollections, ConnectionString = dlg.ConnectionString };
            if (frm.ShowDialog() != DialogResult.OK) return;
            collectionConfig.CustomCollections = frm.CustomCollections;
            SetJson();
            UpdateCustomCollectionCount();
        }

        private void BttnCustomCollectionsNew_Click(object sender, EventArgs e)
        {
            var connectionString = NewSourceConnections[0];
            var src = new DBADashSource(connectionString);
            if (src.SourceConnection.Type == ConnectionType.SQL)
            {
                try
                {
                    src.SourceConnection.Validate();
                    connectionString = src.SourceConnection.ConnectionString;
                }
                catch
                {
                    var dlg = new DBConnection() { ConnectionString = connectionString };
                    dlg.ShowDialog();
                    if (dlg.DialogResult != DialogResult.OK) return;
                }
            }
            else
            {
                MessageBox.Show("Invalid connection type for custom collections", "Error", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            var frm = new ManageCustomCollections()
            { CustomCollections = CustomCollectionsNew, ConnectionString = connectionString };
            if (frm.ShowDialog() != DialogResult.OK) return;
            CustomCollectionsNew = frm.CustomCollections;
        }

        private void DgvConnections_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            if (e.Control is not TextBox textBox) return;
            textBox.KeyPress -= NumericKeyPress;
            if ((dgvConnections.CurrentCell.OwningColumn.ValueType == typeof(int) ||
                 dgvConnections.CurrentCell.OwningColumn.ValueType == typeof(int?)))
            {
                textBox.KeyPress += NumericKeyPress;
            }
        }

        private static void NumericKeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != '-')
            {
                e.Handled = true; // Suppress the key press if it's not a digit or control character
            }
        }

        private async void ChkEnableMessaging_CheckedChanged(object sender, EventArgs e)
        {
            if (IsSetFromJson) return;
            collectionConfig.EnableMessaging = chkEnableMessaging.Checked;
            SetJson();

            if (chkEnableMessaging.Checked && collectionConfig.SourceConnections.Exists(src =>
                    string.IsNullOrEmpty(src.ConnectionID) && src.SourceConnection.Type == ConnectionType.SQL))
            {
                if (MessageBox.Show(
                        "Messaging requires an explicit ConnectionID to be defined in the config file. One or more connections do not have a ConnectionID defined.\nWould you like to automatically populate this now?\n\nNote:This might take a while depending on the number of connections and their availability.",
                        "Messaging", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                {
                    await PopulateConnectionIDAsync();
                }
            }
        }

        private async Task PopulateConnectionIDAsync()
        {
            var errors = new ConcurrentBag<Exception>();

            var tasks = collectionConfig.SourceConnections
                .Where(src => string.IsNullOrEmpty(src.ConnectionID) && src.SourceConnection.Type == ConnectionType.SQL)
                .Select(async src =>
                {
                    try
                    {
                        var collector = await DBCollector.CreateAsync(src, collectionConfig.ServiceName, true);
                        src.ConnectionID = await src.GetGeneratedConnectionIDAsync();
                    }
                    catch (Exception ex)
                    {
                        errors.Add(new Exception($"Error getting ConnectionID for {src.SourceConnection.ConnectionForPrint}: {ex.Message}", ex));
                    }
                });

            await Task.WhenAll(tasks);

            if (errors.Count > 0)
            {
                CommonShared.ShowExceptionDialog($"{errors.Count} errors occurred populating ConnectionID", string.Join("\n", errors.Select(e => e.Message)).Truncate(300), string.Join("\n", errors.Select(ex => ex.ToString())));
            }
            else
            {
                MessageBox.Show("ConnectionID populated successfully", "Messaging", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        public void ApplyTheme(BaseTheme theme)
        {
            foreach (Control c in Controls)
            {
                c.ApplyTheme(theme);
            }

            lblServerNameWarning.ForeColor = theme.WarningForeColor;
            lblServerNameWarning.BackColor = theme.WarningBackColor;
        }

        private void TxtSQS_Validated(object sender, EventArgs e)
        {
            if (IsSetFromJson) return;
            collectionConfig.ServiceSQSQueueUrl = txtSQS.Text;
            SetJson();
        }

        private void TxtSQS_Validating(object sender, CancelEventArgs e)
        {
            if (!string.IsNullOrEmpty(txtSQS.Text) &&
                !txtSQS.Text.StartsWith("https://sqs.", StringComparison.OrdinalIgnoreCase))
            {
                errorProvider1.SetError(txtSQS, "Invalid SQS Url");
            }
            else
            {
                errorProvider1.SetError(txtSQS, "");
            }
        }

        private void ChkAllowPlanForcing_CheckedChanged(object sender, EventArgs e)
        {
            collectionConfig.AllowPlanForcing = chkAllowPlanForcing.Checked;
            SetJson();
        }

        private Dictionary<string, DateTime> SnapshotDates = null;
        private ConcurrentDictionary<string, (string status, Color statusColor, int statusValue)> ConnectionStatuses;

        private async void bttnCheckConnections_Click(object sender, EventArgs e)
        {
            if (collectionConfig.SourceConnections.Where(src => src.SourceConnection.Type == ConnectionType.SQL)
                .Any(src => string.IsNullOrEmpty(src.ConnectionID)))
            {
                if (MessageBox.Show(
                        "One or more connections do not have a ConnectionID defined. Would you like to automatically populate this now?",
                        "Connection ID", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                {
                    await PopulateConnectionIDAsync();
                }
            }

            bttnCheckConnections.Text = "Checking...";
            bttnCheckConnections.Enabled = false;

            List<Task> tasks = new List<Task>();
            if (collectionConfig.DestinationConnection.Type == ConnectionType.SQL)
            {
                tasks.Add(TryGetLastSnapshotDates());
            }
            var connectionStatusTask = GetConnectionStatus();
            tasks.Add(connectionStatusTask);
            await Task.WhenAll(tasks);

            ConnectionStatuses = connectionStatusTask.Result;

            if (!dgvConnections.Columns.Contains("ConnectionStatus"))
            {
                dgvConnections.Columns.Insert(0,
                    new DataGridViewTextBoxColumn() { Name = "ConnectionStatus", HeaderText = "Connection Status" });
            }
            if (SnapshotDates != null && !dgvConnections.Columns.Contains("SnapshotAge"))
            {
                dgvConnections.Columns.Insert(1,
                    new DataGridViewTextBoxColumn() { Name = "SnapshotAge", HeaderText = "Last Snapshot Age" });
            }

            SetDgv();

            bttnCheckConnections.Text = "Check Connections";
            bttnCheckConnections.Enabled = true;
        }

        private DateTime SnapshotDatesLastChecked = DateTime.MinValue;

        private async Task TryGetLastSnapshotDates()
        {
            try
            {
                SnapshotDates = await GetLastSnapshotDates();
                SnapshotDatesLastChecked = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex, "Error getting snapshot dates");
            }
        }

        private async Task<ConcurrentDictionary<string, (string status, Color statusColor, int statusValue)>> GetConnectionStatus()
        {
            var tasks = new List<Task>();
            ConcurrentDictionary<string, (string status, Color statusColor, int statusValue)> connectionStatus =
                new();

            foreach (var row in dgvConnections.Rows.Cast<DataGridViewRow>())
            {
                async Task CheckConnectionAsync()
                {
                    var src = (DBADashSource)row.DataBoundItem;
                    switch (src.SourceConnection.Type)
                    {
                        case ConnectionType.SQL:
                            try
                            {
                                await using var cn = new SqlConnection(src.SourceConnection.ConnectionString);
                                await cn.OpenAsync();
                                connectionStatus.TryAdd(src.ConnectionString,
                                    ("OK", DashColors.Success, 0));
                            }
                            catch (Exception ex)
                            {
                                connectionStatus.TryAdd(src.ConnectionString,
                                    ($"Error: {ex.Message}", DashColors.Fail, 99));
                            }

                            break;

                        case ConnectionType.Directory:
                            try
                            {
                                src.SourceConnection.Validate();
                                connectionStatus.TryAdd(src.ConnectionString,
                                    ("OK", DashColors.Success, 0));
                            }
                            catch (Exception ex)
                            {
                                connectionStatus.TryAdd(src.ConnectionString,
                                    ($"Error: {ex.Message}", DashColors.Fail, 99));
                            }

                            break;

                        default:
                            connectionStatus.TryAdd(src.ConnectionString,
                                ("N/A", DashColors.NotApplicable, 1));
                            break;
                    }
                }

                tasks.Add(CheckConnectionAsync());
            }
            await Task.WhenAll(tasks);
            return connectionStatus;
        }

        private async Task<Dictionary<string, DateTime>> GetLastSnapshotDates()
        {
            await using var cn = new SqlConnection(collectionConfig.DestinationConnection.ConnectionString);
            await cn.OpenAsync();
            await using var cmd = new SqlCommand("dbo.MaxCollectionDateByInstance_Get", cn) { CommandType = CommandType.StoredProcedure };
            await using var rdr = await cmd.ExecuteReaderAsync();
            var snapshotDates = new Dictionary<string, DateTime>();
            while (await rdr.ReadAsync())
            {
                snapshotDates.Add(rdr.GetString(0), rdr.GetDateTime(1));
            }
            return snapshotDates;
        }

        private void lnkAllowAll_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            txtAllowScripts.Text = "*";
        }

        private void lnkAllowNone_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            txtAllowScripts.Text = string.Empty;
        }

        private void lnkAllowExplicit_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            txtAllowScripts.Text = Enum.GetValues(typeof(ProcedureExecutionMessage.CommandNames)).Cast<DBADash.Messaging.ProcedureExecutionMessage.CommandNames>()
                .Select(c => c.ToString())
                .Aggregate((a, b) => a + "," + b);
        }

        private void TxtAllowScripts_TextChanged(object sender, EventArgs e)
        {
            collectionConfig.AllowedScripts = txtAllowScripts.Text;
        }

        private void ChkProcessNotifications_CheckedChanged(object sender, EventArgs e)
        {
            if (IsSetFromJson) return;
            collectionConfig.ProcessAlerts = chkProcessAlerts.Checked;
            numAlertPollingFrequency.Enabled = chkAlertPollingFrequency.Checked && chkProcessAlerts.Checked;
            numAlertStartupDelay.Enabled = chkAlertStartupDelay.Checked && chkProcessAlerts.Checked;
            chkAlertPollingFrequency.Enabled = chkProcessAlerts.Checked;
            chkAlertStartupDelay.Enabled = chkProcessAlerts.Checked;
            SetJson();
        }

        private void ChangeAlertPollingFrequency(object sender, EventArgs e)
        {
            if (IsSetFromJson) return;
            collectionConfig.AlertProcessingFrequencySeconds =
                chkAlertPollingFrequency.Checked && numAlertPollingFrequency.Value > 0 ? Convert.ToInt32(numAlertPollingFrequency.Value) : null;
            numAlertPollingFrequency.Enabled = chkAlertPollingFrequency.Checked && chkProcessAlerts.Checked;
            numAlertPollingFrequency.Value = chkAlertPollingFrequency.Checked
                ? numAlertPollingFrequency.Value
                : CollectionConfig.DefaultAlertProcessingFrequencySeconds;
            SetJson();
        }

        private void ChangeAlertStartupDelay(object sender, EventArgs e)
        {
            if (IsSetFromJson) return;
            collectionConfig.AlertProcessingStartupDelaySeconds =
                chkAlertStartupDelay.Checked && numAlertStartupDelay.Value > 0
                    ? Convert.ToInt32(numAlertStartupDelay.Value)
                    : null;
            numAlertStartupDelay.Value = chkAlertStartupDelay.Checked
                ? numAlertStartupDelay.Value
                : CollectionConfig.DefaultAlertProcessingStartupDelaySeconds;
            numAlertStartupDelay.Enabled = chkAlertStartupDelay.Checked && chkProcessAlerts.Checked;
            SetJson();
        }

        private void lnkAllowAllJobs_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            txtAllowedJobs.Text = "*";
        }

        private void lnkAllowNoJobs_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            txtAllowedJobs.Text = string.Empty;
        }

        private void AllowedJobs_TextChanged(object sender, EventArgs e)
        {
            collectionConfig.AllowedJobs = txtAllowedJobs.Text;
        }

        private void bttnPermissionsHelper_Click(object sender, EventArgs e)
        {
            ShowPermissionsHelper(collectionConfig.SourceConnections);
        }

        private void ShowPermissionsHelper(List<DBADashSource> sourceConnections)
        {
            var frm = new PermissionsHelper() { Connections = sourceConnections, Config = collectionConfig };
            frm.ShowDialog();
        }

        private void lnkGrant_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var connections = GetNewSourceConnections();
            if (connections.Count == 0)
            {
                MessageBox.Show(
                    "Enter a connection string then click this link to provision access to the service account. Or click the Permissions Helper button to provision access to existing connections", "Permissions Helper", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            ShowPermissionsHelper(GetNewSourceConnections());
        }

        private void bttnGrantAccessToServiceAccount_Click(object sender, EventArgs e)
        {
            ShowPermissionsHelper(collectionConfig.SourceConnections);
        }
    }
}