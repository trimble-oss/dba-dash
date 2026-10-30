using DBADash;
using DBADash.CommunityTools;
using DBADashGUI.CustomReports;
using DBADashGUI.Theme;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBADashServiceConfig
{
    /// <summary>
    /// Deploys and updates community tools (sp_WhoIsActive, First Responder Kit etc.) on monitored instances, and shows
    /// which versions are installed.
    ///
    /// <para>Connects as the person running the config tool, or as credentials they enter here - never as the service
    /// account.  Afterwards it offers to allow the tools in the service config and open the permissions helper, as both
    /// are needed before the GUI can run them.</para>
    /// </summary>
    public class CommunityToolsDeploy : Form
    {
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public CollectionConfig Config { get; set; }

        /// <summary>Instances to offer.  Non-SQL connections are ignored.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public List<DBADashSource> Connections { get; set; } = new();

        /// <summary>Set when the dialog added tools to Config.AllowedScripts, so the caller can refresh and save.</summary>
        public bool AllowedScriptsChanged { get; private set; }

        private const int MaxParallelInstances = 4;

        private const string ToolOrderColumn = "ToolOrder";

        private static CommunityToolsManifest Manifest => CommunityToolsManifest.Default;

        private CheckBox chkAccept;
        private CheckBox chkOverrideSafety;
        private CheckBox chkIncludeAzure;
        private ComboBox cboSource;
        private TextBox txtFolder;
        private Button bttnBrowse;
        private TextBox txtDatabase;
        private LinkLabel lnkCredentials;
        private LinkLabel lnkResetCredentials;
        private readonly List<LinkLabel> selectionLinks = new();
        private DBADashDataGridView dgvTools;
        private DBADashDataGridView dgvInstances;
        private DBADashDataGridView dgvResults;
        private Button bttnCheck;
        private Button bttnDeploy;
        private Button bttnCancel;
        private Button bttnAllow;
        private Button bttnPermissions;
        private Label lblStatus;

        private readonly DataTable dtResults = new();

        /// <summary>Credentials to use instead of each connection's own.  Null to use the connection's.</summary>
        private string credentials;

        private CancellationTokenSource cancellation;

        private bool closeRequested;

        /// <summary>Result columns are fitted to the first results only, so widths the user sets are kept.</summary>
        private bool resultsSized;

        private sealed record SourceOption(CommunityToolScriptSource.SourceTypes Type, string Text)
        {
            public override string ToString() => Text;
        }

        public CommunityToolsDeploy()
        {
            Text = "Deploy Community Tools";
            // Same icon as the Community Tools folder in the GUI's tree
            Icon = Properties.Resources.CommunityTools;
            AutoScaleMode = AutoScaleMode.None;
            Width = 1150;
            Height = 850;
            MinimumSize = new Size(850, 650);
            StartPosition = FormStartPosition.CenterParent;
            dtResults.Columns.Add("Instance", typeof(string));
            dtResults.Columns.Add("Tool", typeof(string));
            dtResults.Columns.Add("Database", typeof(string));
            dtResults.Columns.Add("Installed", typeof(string));
            dtResults.Columns.Add("Expected", typeof(string));
            dtResults.Columns.Add("Status", typeof(string));
            dtResults.Columns.Add("Result", typeof(string));
            dtResults.Columns.Add("Message", typeof(string));
            dtResults.Columns.Add(ToolOrderColumn, typeof(int));
            dtResults.PrimaryKey = new[] { dtResults.Columns["Instance"], dtResults.Columns["Tool"] };
            BuildLayout();
            Load += CommunityToolsDeploy_Load;
            FormClosing += (_, e) =>
            {
                if (cancellation == null) return;
                // Stop the running operation first - closing resumes when it finishes.
                e.Cancel = true;
                closeRequested = true;
                cancellation.Cancel();
            };
        }

        private void BuildLayout()
        {
            var tlp = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5, Padding = new Padding(8) };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            tlp.RowStyles.Add(new RowStyle(SizeType.AutoSize));     // 0 disclaimer
            tlp.RowStyles.Add(new RowStyle(SizeType.AutoSize));     // 1 options
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 45));  // 2 tools + instances
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 55));  // 3 results
            tlp.RowStyles.Add(new RowStyle(SizeType.AutoSize));     // 4 buttons

            // Row 0 - disclaimer
            var pnlDisclaimer = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false };
            var lblDisclaimer = new Label { Text = CommunityToolsManifest.Disclaimer, AutoSize = true, MaximumSize = new Size(1080, 0), Margin = new Padding(3, 3, 3, 6) };
            pnlDisclaimer.Controls.Add(lblDisclaimer);
            chkAccept = new CheckBox { Text = "I understand.  Deploy these third-party scripts to the selected instances.", AutoSize = true };
            pnlDisclaimer.Controls.Add(chkAccept);
            pnlDisclaimer.SizeChanged += (_, _) => lblDisclaimer.MaximumSize = new Size(Math.Max(300, pnlDisclaimer.ClientSize.Width - 10), 0);
            tlp.Controls.Add(pnlDisclaimer, 0, 0);

            // Row 1 - source, database and credentials
            // Each label stays with its control when the row wraps.
            var pnlOptions = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true, Margin = new Padding(0, 6, 0, 0) };
            FlowLayoutPanel OptionGroup() => new() { AutoSize = true, WrapContents = false, Margin = new Padding(0, 0, 15, 0) };
            var grpSource = OptionGroup();
            grpSource.Controls.Add(new Label { Text = "Source:", AutoSize = true, Margin = new Padding(3, 8, 3, 3) });
            cboSource = new ComboBox { Width = 300, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(3, 5, 3, 3) };
            cboSource.Items.AddRange(new object[]
            {
                new SourceOption(CommunityToolScriptSource.SourceTypes.Pinned, "GitHub - version tested with DBA Dash"),
                new SourceOption(CommunityToolScriptSource.SourceTypes.Latest, "GitHub - latest release (untested)"),
                new SourceOption(CommunityToolScriptSource.SourceTypes.Folder, "Local folder")
            });
            cboSource.SelectedIndex = 0;
            cboSource.SelectedIndexChanged += (_, _) => UpdateButtons();
            grpSource.Controls.Add(cboSource);
            txtFolder = new TextBox { Width = 250, Margin = new Padding(3, 5, 0, 3), PlaceholderText = "Folder containing the scripts" };
            txtFolder.TextChanged += (_, _) => UpdateButtons();
            grpSource.Controls.Add(txtFolder);
            bttnBrowse = new Button { Text = "...", Width = 32, Height = 27, Margin = new Padding(0, 5, 3, 3) };
            bttnBrowse.Click += Browse_Click;
            grpSource.Controls.Add(bttnBrowse);
            pnlOptions.Controls.Add(grpSource);
            var grpDatabase = OptionGroup();
            grpDatabase.Controls.Add(new Label { Text = "Database:", AutoSize = true, Margin = new Padding(3, 8, 3, 3) });
            txtDatabase = new TextBox { Width = 170, Margin = new Padding(3, 5, 3, 3), PlaceholderText = "Connection's default" };
            grpDatabase.Controls.Add(txtDatabase);
            pnlOptions.Controls.Add(grpDatabase);
            var grpSafety = OptionGroup();
            chkOverrideSafety = new CheckBox { Text = "Override safety checks", AutoSize = true, Margin = new Padding(3, 8, 3, 3) };
            new ToolTip().SetToolTip(chkOverrideSafety, "Replace Ola Hallengren's Maintenance Solution procedures even if they don't match a known release (they may have been customized)\nor the update removes a parameter an Agent job passes.  A rollback script is still saved.  Objects that aren't the tool are never replaced.");
            grpSafety.Controls.Add(chkOverrideSafety);
            chkIncludeAzure = new CheckBox { Text = "Include Azure SQL DB", AutoSize = true, Margin = new Padding(3, 8, 3, 3) };
            new ToolTip().SetToolTip(chkIncludeAzure, "Azure SQL Database connections are skipped by default.  Tools can't be deployed to master there, only to the connected user database.");
            grpSafety.Controls.Add(chkIncludeAzure);
            pnlOptions.Controls.Add(grpSafety);
            var grpCredentials = OptionGroup();
            grpCredentials.Controls.Add(new Label { Text = "Connect as:", AutoSize = true, Margin = new Padding(3, 8, 3, 3) });
            lnkCredentials = new LinkLabel { AutoSize = true, Margin = new Padding(3, 8, 3, 3) };
            lnkCredentials.LinkClicked += Credentials_LinkClicked;
            grpCredentials.Controls.Add(lnkCredentials);
            lnkResetCredentials = new LinkLabel { Text = "Reset", AutoSize = true, Margin = new Padding(3, 8, 3, 3), Visible = false };
            lnkResetCredentials.LinkClicked += (_, _) => SetCredentials(null);
            grpCredentials.Controls.Add(lnkResetCredentials);
            pnlOptions.Controls.Add(grpCredentials);
            tlp.Controls.Add(pnlOptions, 0, 1);

            // Row 2 - tools and instances side by side
            var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Vertical };
            split.Panel1.Controls.Add(BuildToolsPanel());
            split.Panel2.Controls.Add(BuildInstancesPanel());
            // The split container has no real size until the form is shown
            Shown += (_, _) =>
            {
                split.SplitterDistance = split.Width * 3 / 5;
                // Size the columns to their content once, then leave them for the user to resize
                foreach (var dgv in new[] { dgvTools, dgvInstances })
                {
                    dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
                    dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
                }
            };
            tlp.Controls.Add(split, 0, 2);

            // Row 3 - results
            dgvResults = new DBADashDataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoGenerateColumns = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None
            };
            foreach (var column in dtResults.Columns.Cast<DataColumn>().Where(c => c.ColumnName != ToolOrderColumn))
            {
                dgvResults.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = column.ColumnName, DataPropertyName = column.ColumnName, Name = column.ColumnName });
            }
            dgvResults.Columns["Message"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dgvResults.CellFormatting += Results_CellFormatting;
            // Instances complete in parallel - keep each instance's tools together, in manifest order.
            dgvResults.DataSource = new DataView(dtResults) { Sort = "Instance, " + ToolOrderColumn };
            tlp.Controls.Add(dgvResults, 0, 3);

            // Row 4 - buttons
            var pnlButtons = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 2 };
            pnlButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            pnlButtons.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            var pnlLeft = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = false };
            bttnCheck = new Button { Text = "Check Versions", AutoSize = true, Margin = new Padding(3) };
            bttnCheck.Click += Check_Click;
            pnlLeft.Controls.Add(bttnCheck);
            bttnDeploy = new Button { Text = "Deploy", AutoSize = true, Margin = new Padding(3) };
            bttnDeploy.Click += Deploy_Click;
            pnlLeft.Controls.Add(bttnDeploy);
            bttnCancel = new Button { Text = "Cancel", AutoSize = true, Margin = new Padding(3), Visible = false };
            bttnCancel.Click += (_, _) => cancellation?.Cancel();
            pnlLeft.Controls.Add(bttnCancel);
            lblStatus = new Label { AutoSize = true, Margin = new Padding(10, 9, 3, 3) };
            pnlLeft.Controls.Add(lblStatus);
            pnlButtons.Controls.Add(pnlLeft, 0, 0);
            var pnlRight = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Anchor = AnchorStyles.Right };
            bttnAllow = new Button { Text = "Allow in GUI", AutoSize = true, Margin = new Padding(3) };
            bttnAllow.Click += (_, _) => AllowTools(SelectedTools.Where(t => t.RunFromGui).Select(t => t.Name));
            pnlRight.Controls.Add(bttnAllow);
            bttnPermissions = new Button { Text = "Permissions Helper", AutoSize = true, Margin = new Padding(3) };
            bttnPermissions.Click += (_, _) => ShowPermissionsHelper();
            pnlRight.Controls.Add(bttnPermissions);
            var bttnClose = new Button { Text = "Close", AutoSize = true, Margin = new Padding(15, 3, 3, 3), DialogResult = DialogResult.OK };
            pnlRight.Controls.Add(bttnClose);
            CancelButton = bttnClose;
            pnlButtons.Controls.Add(pnlRight, 1, 0);
            tlp.Controls.Add(pnlButtons, 0, 4);

            Controls.Add(tlp);
            this.ApplyTheme();
        }

        private Control BuildToolsPanel()
        {
            var tlp = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
            tlp.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var pnlLinks = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true };
            pnlLinks.Controls.Add(new Label { Text = "Tools:", AutoSize = true, Margin = new Padding(3, 3, 8, 3) });
            pnlLinks.Controls.Add(SelectionLink("All", () => SetToolSelection(_ => true)));
            pnlLinks.Controls.Add(SelectionLink("None", () => SetToolSelection(_ => false)));
            foreach (var family in Manifest.Families)
            {
                pnlLinks.Controls.Add(SelectionLink(family.DisplayName, () => SetToolSelection(t => t.Family == family ? true : null)));
            }
            tlp.Controls.Add(pnlLinks, 0, 0);

            dgvTools = new DBADashDataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                RowHeadersVisible = false
            };
            dgvTools.Columns.Add(new DataGridViewCheckBoxColumn { HeaderText = "", Name = "Selected", Width = 30, AutoSizeMode = DataGridViewAutoSizeColumnMode.None });
            dgvTools.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Tool", Name = "Tool", ReadOnly = true });
            dgvTools.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Family", Name = "Family", ReadOnly = true });
            dgvTools.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Version", Name = "Version", ReadOnly = true });
            dgvTools.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "License", Name = "License", ReadOnly = true });
            dgvTools.Columns.Add(new DataGridViewLinkColumn { HeaderText = "Repository", Name = "Repository", ReadOnly = true, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            foreach (var tool in Manifest.Tools)
            {
                var i = dgvTools.Rows.Add(false, tool.Name, tool.Family.DisplayName,
                    string.Join(", ", tool.Files.Select(f => f.Version).Distinct()), tool.Family.License, tool.Family.RepositoryUrl);
                dgvTools.Rows[i].Tag = tool;
            }
            dgvTools.CellContentClick += (_, e) =>
            {
                if (e.RowIndex >= 0 && dgvTools.Columns[e.ColumnIndex].Name == "Repository")
                {
                    CommonShared.OpenURL((string)dgvTools.Rows[e.RowIndex].Cells[e.ColumnIndex].Value);
                }
            };
            CommitCheckBoxEdits(dgvTools);
            tlp.Controls.Add(dgvTools, 0, 1);
            return tlp;
        }

        private Control BuildInstancesPanel()
        {
            var tlp = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
            tlp.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var pnlLinks = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true };
            pnlLinks.Controls.Add(new Label { Text = "Instances:", AutoSize = true, Margin = new Padding(3, 3, 8, 3) });
            pnlLinks.Controls.Add(SelectionLink("All", () => SetInstanceSelection(true)));
            pnlLinks.Controls.Add(SelectionLink("None", () => SetInstanceSelection(false)));
            tlp.Controls.Add(pnlLinks, 0, 0);

            dgvInstances = new DBADashDataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                RowHeadersVisible = false
            };
            dgvInstances.Columns.Add(new DataGridViewCheckBoxColumn { HeaderText = "", Name = "Selected", Width = 30, AutoSizeMode = DataGridViewAutoSizeColumnMode.None });
            dgvInstances.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Instance", Name = "Instance", ReadOnly = true });
            dgvInstances.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Connect As", Name = "ConnectAs", ReadOnly = true, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            CommitCheckBoxEdits(dgvInstances);
            tlp.Controls.Add(dgvInstances, 0, 1);
            return tlp;
        }

        /// <summary>Tool and instance selection shortcuts.  Disabled while running, like the grids they change.</summary>
        private LinkLabel SelectionLink(string text, Action action)
        {
            var link = new LinkLabel { Text = text, AutoSize = true, Margin = new Padding(3) };
            link.LinkClicked += (_, _) =>
            {
                if (cancellation == null) action();
            };
            selectionLinks.Add(link);
            return link;
        }

        /// <summary>Commits checkbox clicks straight away so the buttons reflect the selection.</summary>
        private void CommitCheckBoxEdits(DataGridView dgv)
        {
            dgv.CurrentCellDirtyStateChanged += (_, _) =>
            {
                if (dgv.IsCurrentCellDirty && dgv.CurrentCell is DataGridViewCheckBoxCell)
                {
                    dgv.CommitEdit(DataGridViewDataErrorContexts.Commit);
                }
            };
            dgv.CellValueChanged += (_, _) => UpdateButtons();
        }

        private void CommunityToolsDeploy_Load(object sender, EventArgs e)
        {
            foreach (var src in Connections.Where(c => c.SourceConnection.Type == DBADashConnection.ConnectionType.SQL))
            {
                var i = dgvInstances.Rows.Add(true, InstanceName(src), "");
                dgvInstances.Rows[i].Tag = src;
            }
            // Tools already allowed in the GUI are the ones the user is most likely to want.  Tools the GUI doesn't run
            // (e.g. the Maintenance Solution) are never selected by default - deploying those is a deliberate choice.
            var allowed = (Config?.AllowedScripts ?? "").Split(',', StringSplitOptions.TrimEntries);
            SetToolSelection(t => t.RunFromGui && (Config?.AllowedScripts == "*" || allowed.Contains(t.Name, StringComparer.OrdinalIgnoreCase)));
            SetCredentials(null);
        }

        private static string InstanceName(DBADashSource src) =>
            string.IsNullOrEmpty(src.ConnectionID) ? src.SourceConnection.DataSource() : src.ConnectionID;

        private List<CommunityTool> SelectedTools => dgvTools.Rows.Cast<DataGridViewRow>()
            .Where(r => r.Cells["Selected"].Value is true)
            .Select(r => (CommunityTool)r.Tag)
            .ToList();

        private List<DBADashSource> SelectedInstances => dgvInstances.Rows.Cast<DataGridViewRow>()
            .Where(r => r.Cells["Selected"].Value is true)
            .Select(r => (DBADashSource)r.Tag)
            .ToList();

        private CommunityToolScriptSource.SourceTypes SourceType => ((SourceOption)cboSource.SelectedItem).Type;

        private string Database => string.IsNullOrWhiteSpace(txtDatabase.Text) ? null : txtDatabase.Text.Trim();

        /// <summary>Selects tools: true to tick, false to untick, null to leave as is.</summary>
        private void SetToolSelection(Func<CommunityTool, bool?> selector)
        {
            foreach (DataGridViewRow row in dgvTools.Rows)
            {
                var selected = selector((CommunityTool)row.Tag);
                if (selected.HasValue)
                {
                    row.Cells["Selected"].Value = selected.Value;
                }
            }
            UpdateButtons();
        }

        private void SetInstanceSelection(bool selected)
        {
            foreach (DataGridViewRow row in dgvInstances.Rows)
            {
                row.Cells["Selected"].Value = selected;
            }
            UpdateButtons();
        }

        private void UpdateButtons()
        {
            if (bttnDeploy == null) return; // Still building the layout
            var running = cancellation != null;
            var isFolder = SourceType == CommunityToolScriptSource.SourceTypes.Folder;
            txtFolder.Visible = isFolder;
            bttnBrowse.Visible = isFolder;
            txtFolder.Enabled = !running;
            bttnBrowse.Enabled = !running;
            var hasSelection = dgvTools.Rows.Cast<DataGridViewRow>().Any(r => r.Cells["Selected"].Value is true)
                               && dgvInstances.Rows.Cast<DataGridViewRow>().Any(r => r.Cells["Selected"].Value is true);
            bttnCheck.Enabled = hasSelection && !running;
            bttnDeploy.Enabled = hasSelection && !running && (!isFolder || !string.IsNullOrWhiteSpace(txtFolder.Text));
            bttnCancel.Visible = running;
            bttnAllow.Enabled = !running && Config != null;
            bttnPermissions.Enabled = !running && Config != null;
            cboSource.Enabled = !running;
            txtDatabase.Enabled = !running;
            chkOverrideSafety.Enabled = !running;
            chkIncludeAzure.Enabled = !running;
            lnkCredentials.Enabled = !running;
            lnkResetCredentials.Enabled = !running;
            selectionLinks.ForEach(l => l.Enabled = !running);
            dgvTools.Enabled = !running;
            dgvInstances.Enabled = !running;
        }

        private void Browse_Click(object sender, EventArgs e)
        {
            using var fbd = new FolderBrowserDialog { Description = "Folder containing the community tool scripts", UseDescriptionForTitle = true, SelectedPath = txtFolder.Text };
            if (fbd.ShowDialog(this) == DialogResult.OK)
            {
                txtFolder.Text = fbd.SelectedPath;
            }
        }

        private void Credentials_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var first = SelectedInstances.FirstOrDefault() ?? Connections.FirstOrDefault();
            using var frm = new DBConnection();
            frm.ConnectionString = credentials != null && first != null
                ? CommunityToolDeployer.WithCredentials(first.SourceConnection.ConnectionString, credentials)
                : first?.SourceConnection.ConnectionString ?? string.Empty;
            if (frm.ShowDialog(this) == DialogResult.OK)
            {
                SetCredentials(frm.ConnectionString);
            }
        }

        private void SetCredentials(string connectionString)
        {
            credentials = connectionString;
            lnkCredentials.Text = credentials == null ? "Each connection's own credentials" : DescribeCredentials(credentials);
            lnkResetCredentials.Visible = credentials != null;
            foreach (DataGridViewRow row in dgvInstances.Rows)
            {
                row.Cells["ConnectAs"].Value = DescribeCredentials(ConnectionString((DBADashSource)row.Tag));
            }
        }

        private string ConnectionString(DBADashSource src) => credentials == null
            ? src.SourceConnection.ConnectionString
            : CommunityToolDeployer.WithCredentials(src.SourceConnection.ConnectionString, credentials);

        private static string DescribeCredentials(string connectionString)
        {
            var builder = new SqlConnectionStringBuilder(connectionString);
            if (builder.IntegratedSecurity || builder.Authentication == SqlAuthenticationMethod.ActiveDirectoryIntegrated)
            {
                return $"Windows - {Environment.UserDomainName}\\{Environment.UserName}";
            }
            return builder.Authentication switch
            {
                SqlAuthenticationMethod.NotSpecified or SqlAuthenticationMethod.SqlPassword => $"SQL login - {builder.UserID}",
                _ => $"{builder.Authentication} - {builder.UserID}"
            };
        }

        private async void Check_Click(object sender, EventArgs e)
        {
            await RunAsync("Checking", async (tools, instances, token) =>
            {
                await CheckVersionsAsync(tools, instances, token);
                return true;
            });
        }

        private async void Deploy_Click(object sender, EventArgs e)
        {
            if (!chkAccept.Checked)
            {
                MessageBox.Show("These are third-party scripts.  Read the notice at the top of this window and tick \"I understand\" to deploy them.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                chkAccept.Focus();
                return;
            }
            var tools = SelectedTools;
            var instances = SelectedInstances;
            var message = $"Deploy {tools.Count} community tool{(tools.Count == 1 ? "" : "s")} to {instances.Count} instance{(instances.Count == 1 ? "" : "s")}?";
            if (SourceType == CommunityToolScriptSource.SourceTypes.Latest)
            {
                message += "\n\nThe latest releases haven't been tested with DBA Dash.";
            }
            if (chkOverrideSafety.Checked)
            {
                message += "\n\nSafety checks are overridden: customized procedures, and procedures whose update would break an Agent job, will be replaced.  A rollback script is still saved.";
            }
            if (MessageBox.Show(message, Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            CommunityToolScriptSource source;
            try
            {
                source = CommunityToolScriptSource.Create(SourceType, txtFolder.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var deployed = new List<CommunityToolDeployResult>();
            var rollbackScripts = new List<string>();
            var completed = await RunAsync("Deploying", async (_, _, token) =>
            {
                var deployer = new CommunityToolDeployer(source) { Database = Database, OverrideSafetyChecks = chkOverrideSafety.Checked, IncludeAzureSqlDb = chkIncludeAzure.Checked };
                // Deploying can add tools to the selection (required tools, and the rest of a set that's updated together)
                var resultTools = new List<CommunityTool>(tools);
                await ForEachInstanceAsync(instances, async src =>
                {
                    var result = await deployer.DeployAsync(ConnectionString(src), tools, token);
                    ShowDeployResult(src, tools, result);
                    deployed.AddRange(result.Tools.Where(t => t.Status == CommunityToolDeployStatus.Deployed));
                    resultTools.AddRange(result.Tools.Select(t => t.Tool).Where(t => !resultTools.Contains(t)));
                    if (result.RollbackScript != null) rollbackScripts.Add(result.RollbackScript);
                }, token);
                // Show the versions now installed alongside the deployment result.
                lblStatus.Text = "Checking versions";
                await CheckVersionsAsync(resultTools, instances, token, clearResults: false);
                return true;
            }, clearResults: true);

            var failed = dtResults.Rows.Cast<DataRow>().Count(r => (r["Result"] as string) == nameof(CommunityToolDeployStatus.Failed));
            var skipped = dtResults.Rows.Cast<DataRow>().Count(r => (r["Result"] as string) == nameof(CommunityToolDeployStatus.Skipped));
            var upToDate = dtResults.Rows.Cast<DataRow>().Count(r => (r["Result"] as string) == nameof(CommunityToolDeployStatus.UpToDate));
            lblStatus.Text = $"{deployed.Count} deployed, {upToDate} already up to date, {failed} failed, {skipped} skipped" + (completed ? "" : " (cancelled)")
                + (rollbackScripts.Count > 0 ? $".  Rollback scripts saved to {System.IO.Path.GetDirectoryName(rollbackScripts[0])}" : "");
            // Only prompt when tools people run their backups with were replaced - routine updates just note it in the status
            if (rollbackScripts.Count > 0 && deployed.Any(d => d.Tool.Family.GuardCustomizations) && MessageBox.Show($"Rollback scripts that put back what was replaced were saved to:\n\n{string.Join("\n", rollbackScripts)}\n\nOpen the folder?",
                    Text, MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
            {
                CommonShared.OpenFolder(System.IO.Path.GetDirectoryName(rollbackScripts[0]));
            }
            if (deployed.Count > 0)
            {
                OfferToAllow(deployed.Where(d => d.Tool.RunFromGui).Select(d => d.Tool.Name).Distinct().ToList());
            }
        }

        /// <summary>
        /// Runs an operation over the selected tools and instances, with the controls disabled and a Cancel button.
        /// False if it was cancelled.
        /// </summary>
        private async Task<bool> RunAsync(string description, Func<List<CommunityTool>, List<DBADashSource>, CancellationToken, Task<bool>> action, bool clearResults = true)
        {
            var tools = SelectedTools;
            var instances = SelectedInstances;
            if (clearResults)
            {
                dtResults.Clear();
            }
            cancellation = new CancellationTokenSource();
            lblStatus.Text = description;
            UpdateButtons();
            try
            {
                return await action(tools, instances, cancellation.Token);
            }
            catch (OperationCanceledException)
            {
                lblStatus.Text = "Cancelled";
                return false;
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex);
                return false;
            }
            finally
            {
                cancellation.Dispose();
                cancellation = null;
                UpdateButtons();
                if (!resultsSized && dtResults.Rows.Count > 0)
                {
                    dgvResults.AutoResizeColumnsWithMaxColumnWidth(DataGridViewAutoSizeColumnsMode.DisplayedCells, 0.4f);
                    resultsSized = true;
                }
                if (closeRequested)
                {
                    BeginInvoke(Close);
                }
            }
        }

        private async Task CheckVersionsAsync(List<CommunityTool> tools, List<DBADashSource> instances, CancellationToken token, bool clearResults = true)
        {
            var done = 0;
            await ForEachInstanceAsync(instances, async src =>
            {
                try
                {
                    var versions = await CommunityToolVersionChecker.CheckAsync(ConnectionString(src), tools, Database, token);
                    foreach (var v in versions)
                    {
                        var row = GetResultRow(src, v.Tool);
                        row["Database"] = v.Database;
                        row["Installed"] = v.InstalledVersion;
                        row["Expected"] = v.ExpectedVersion;
                        row["Status"] = v.Status.ToString();
                        var notes = new[] { v.Message, v.Note }.Where(s => !string.IsNullOrEmpty(s)).ToList();
                        if (notes.Count > 0 && (clearResults || row["Message"] is DBNull))
                        {
                            row["Message"] = string.Join("  ", notes);
                        }
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    foreach (var tool in tools)
                    {
                        var row = GetResultRow(src, tool);
                        row["Status"] = "Error";
                        row["Message"] = ex.Message;
                    }
                }
                lblStatus.Text = $"Checked {Interlocked.Increment(ref done)} of {instances.Count} instances";
            }, token);
        }

        private void ShowDeployResult(DBADashSource src, List<CommunityTool> tools, CommunityToolInstanceDeployResult result)
        {
            if (result.Error != null)
            {
                foreach (var tool in tools)
                {
                    var row = GetResultRow(src, tool);
                    row["Result"] = nameof(CommunityToolDeployStatus.Failed);
                    row["Message"] = result.Error;
                }
                return;
            }
            foreach (var t in result.Tools)
            {
                var row = GetResultRow(src, t.Tool);
                row["Database"] = t.Database ?? result.Database;
                row["Result"] = t.Status.ToString();
                row["Message"] = t.Message;
            }
            lblStatus.Text = $"Deployed to {InstanceName(src)}";
        }

        private DataRow GetResultRow(DBADashSource src, CommunityTool tool)
        {
            var instance = InstanceName(src);
            var row = dtResults.Rows.Find(new object[] { instance, tool.Name });
            if (row != null) return row;
            row = dtResults.NewRow();
            row["Instance"] = instance;
            row["Tool"] = tool.Name;
            row[ToolOrderColumn] = Manifest.Tools.ToList().IndexOf(tool);
            dtResults.Rows.Add(row);
            return row;
        }

        /// <summary>Runs the action for each instance, a few at a time.  Continuations run on the UI thread.</summary>
        private static async Task ForEachInstanceAsync(List<DBADashSource> instances, Func<DBADashSource, Task> action, CancellationToken token)
        {
            using var throttle = new SemaphoreSlim(MaxParallelInstances);
            await Task.WhenAll(instances.Select(async src =>
            {
                await throttle.WaitAsync(token);
                try
                {
                    await action(src);
                }
                finally
                {
                    throttle.Release();
                }
            }));
        }

        private void Results_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var name = dgvResults.Columns[e.ColumnIndex].Name;
            if (name != "Status" && name != "Result") return;
            var (back, fore) = (e.Value as string) switch
            {
                nameof(CommunityToolStatus.UpToDate) or nameof(CommunityToolDeployStatus.Deployed) => (DashColors.Success, Color.White),
                nameof(CommunityToolStatus.Outdated) or nameof(CommunityToolStatus.Conflict) or nameof(CommunityToolStatus.Modified) => (DashColors.Warning, Color.Black),
                nameof(CommunityToolDeployStatus.Failed) or "Error" => (DashColors.Fail, Color.White),
                _ => (Color.Empty, Color.Empty)
            };
            if (back == Color.Empty) return;
            e.CellStyle.BackColor = back;
            e.CellStyle.ForeColor = fore;
            e.CellStyle.SelectionBackColor = back;
            e.CellStyle.SelectionForeColor = fore;
        }

        private List<string> NotYetAllowed(IEnumerable<string> toolNames)
        {
            if (Config == null || Config.AllowedScripts == "*") return new List<string>();
            var allowed = (Config.AllowedScripts ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return toolNames.Where(t => !allowed.Contains(t, StringComparer.OrdinalIgnoreCase)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        private void OfferToAllow(List<string> deployedTools)
        {
            // Only tools the GUI runs need allowing or EXECUTE for the service (e.g. not the Maintenance Solution)
            if (deployedTools.Count == 0) return;
            var notAllowed = NotYetAllowed(deployedTools);
            if (notAllowed.Count > 0)
            {
                if (MessageBox.Show($"These tools aren't allowed to run from the DBA Dash GUI yet:\n\n{string.Join(", ", notAllowed)}\n\nAdd them to Allowed Scripts?",
                        Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                {
                    return;
                }
                AllowTools(notAllowed, prompt: false);
            }
            if (MessageBox.Show("The service account also needs EXECUTE permission on the tools.  Open the Permissions Helper for the selected instances?",
                    Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                ShowPermissionsHelper();
            }
        }

        private void AllowTools(IEnumerable<string> toolNames, bool prompt = true)
        {
            var notAllowed = NotYetAllowed(toolNames);
            if (notAllowed.Count == 0)
            {
                if (prompt)
                {
                    MessageBox.Show("The selected tools are already allowed.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                return;
            }
            if (prompt && MessageBox.Show($"Add these tools to Allowed Scripts?\n\n{string.Join(", ", notAllowed)}", Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }
            Config.AllowedScripts = string.Join(",", (Config.AllowedScripts ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Concat(notAllowed));
            AllowedScriptsChanged = true;
            MessageBox.Show("Allowed Scripts updated.  Save the service configuration and restart the service to apply.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ShowPermissionsHelper()
        {
            var instances = SelectedInstances;
            if (instances.Count == 0)
            {
                MessageBox.Show("Select the instances to grant permissions on.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                using var frm = new PermissionsHelper { Connections = instances, Config = Config };
                frm.ShowDialog(this);
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex, "Error loading Permissions Helper");
            }
        }
    }
}
