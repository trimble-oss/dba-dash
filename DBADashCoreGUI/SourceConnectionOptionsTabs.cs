using DBADash;
using DBADashGUI.Theme;
using System.ComponentModel;
using System.Runtime.Versioning;

namespace DBADashGUI
{
    /// <summary>
    /// The options for new source connections, as tabs: slow queries, running queries, deadlocks, query stats and other.
    /// Shared by the service configuration tool and the GUI's Add Connections dialog so connections get the same settings
    /// whichever adds them.  Hosts add their own tabs (e.g. the connection strings) with <see cref="TabControl.TabPages"/>.
    /// Laid out for AutoScaleDimensions 8x20 - the host form scales it.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class SourceConnectionOptionsTabs : ThemedTabControl
    {
        public const string SlowQueryCaptureModeToolTip =
            "RingBuffer (default) - DBADash_1/DBADash_2 sessions with a ring buffer, read and then emptied by stopping and starting the session.\r\n" +
            $"EventFile - a {DBADashSource.ManagedSlowQueryXESessionName} session with an event file, read from a saved position.  Never stopped to be emptied, so nothing is lost while it is.  Not available on Azure SQL Database or Managed Instance, which use RingBuffer.\r\n" +
            "ExistingSession - a session you create and manage.  DBA Dash reads it and never alters it.";

        public const string KeepSlowQuerySessionToolTip =
            $"Leave the {DBADashSource.ManagedSlowQueryXESessionName} session running when the service stops, and start it with the instance, so slow queries are captured while the service is down and collected when it starts.\r\n" +
            "EventFile mode only.  Without it, the session is stopped or removed on service stop like the other DBA Dash sessions.";

        private const string QueryStatsTopNToolTip =
            "The number of query families kept per ranking measure each collection.  Seven measures are ranked and the results combined, so the number of queries stored is usually close to this but can reach seven times it, plus any query kept for one slow execution.\r\n" +
            "Everything below the cut is rolled up rather than discarded, so the totals for an interval stay correct whatever this is set to.  Raising it buys detail, not accuracy.  50 suits most instances.";

        private const string QueryStatsPlansToolTip =
            "0 - query stats collects no plans.  Plans can still be fetched from the plan cache from the grid, with messaging enabled.\r\n" +
            "Any other value - the most plans one collection fetches, for the plan shapes of the queries it stored.  50 by default.\r\n" +
            "Each plan shape's plan is sent once a day rather than every collection, so once the plans in use have been sent a collection only fetches the ones that are new.  The cap bites after a restart, and the heaviest plans are fetched first.";

        /// <summary>Pages are laid out at this size, so controls anchored right or bottom keep their distance from the edge as the page is resized.</summary>
        private static readonly Size DesignPageSize = new(1059, 178);

        private readonly ToolTip toolTip = new() { AutoPopDelay = 30000 };
        private readonly ErrorProvider errorProvider = new();

        public readonly TabPage TabSlowQueries = new("Slow Queries") { Padding = new Padding(3, 4, 3, 4), UseVisualStyleBackColor = true, Size = DesignPageSize };
        public readonly TabPage TabRunningQueries = new("Running Queries") { Padding = new Padding(3), UseVisualStyleBackColor = true, Size = DesignPageSize };
        public readonly TabPage TabOther = new("Other") { Padding = new Padding(3, 4, 3, 4), UseVisualStyleBackColor = true, Size = DesignPageSize };
        public readonly TabPage TabDeadlocks = new("Deadlocks") { Padding = new Padding(3), UseVisualStyleBackColor = true, Size = DesignPageSize };
        public readonly TabPage TabQueryStats = new("Query Stats") { Padding = new Padding(3), UseVisualStyleBackColor = true, Size = DesignPageSize };

        // Slow queries
        private readonly Panel pnlExtendedEvents = new() { Dock = DockStyle.Fill };
        private readonly CheckBox chkSlowQueryThreshold = Check("Capture Slow Queries (Extended Event)", 6, 4);
        private readonly CheckBox chkPersistXESession = Check("Persist XE sessions (to allow for manual configuration)", 6, 37);
        private readonly CheckBox chkDualSession = Check("Use dual session", 6, 71);
        private readonly NumericUpDown numSlowQueryThreshold = new() { Location = new Point(117, 108), Size = new Size(173, 27), Minimum = -1, Maximum = 604800000, Increment = 1000, Value = -1, Enabled = false };
        private readonly Label lblSlow = Caption("Extended events trace to capture slow rpc and batch completed events is NOT enabled", 6, 145);
        private readonly ComboBox cboSlowQueryCaptureMode = new() { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(760, 4), Size = new Size(250, 28) };
        private readonly TextBox txtSlowQueryXESessionName = new() { Location = new Point(760, 38), Size = new Size(250, 27) };
        private readonly CheckBox chkKeepSlowQuerySessionRunning = Check("Keep session running when service stops", 620, 71);

        // Running queries
        private readonly CheckBox chkCollectSessionWaits = Check("Collect Session Waits", 9, 8, true);
        private readonly CheckBox chkCollectTaskWaits = Check("Collect Task Waits", 9, 40);
        private readonly CheckBox chkCollectPlans = Check("Collect Plans for Running Queries", 9, 72);
        private readonly CheckBox chkCollectTempDB = Check("Collect TempDB allocations", 9, 103);
        private readonly CheckBox chkCollectCursors = Check("Collect Cursors", 9, 134);
        private readonly GroupBox grpLivePlanThreshold = new() { Text = "Live Plan Capture", Location = new Point(270, 8), Size = new Size(265, 160), Enabled = false };
        private readonly CheckBox chkCaptureLivePlans = Check("Capture long running queries", 9, 24);
        private readonly TextBox txtLivePlanCPUThreshold = Number(150, 55);
        private readonly TextBox txtLivePlanDurationThreshold = Number(150, 89);
        private readonly TextBox txtLivePlanMaxPerCollection = Number(150, 123);
        private readonly GroupBox grpRunningQueryThreshold = new() { Text = "Running Queries Plan Collection Thresholds", Location = new Point(545, 16), Size = new Size(509, 132), Enabled = false, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
        private readonly TextBox txtDurationThreshold = Number(146, 37);
        private readonly TextBox txtCPUThreshold = Number(146, 79);
        private readonly TextBox txtGrantThreshold = Number(385, 37);
        private readonly TextBox txtCountThreshold = Number(385, 79);

        // Other
        private readonly CheckBox chkNoWMI = Check("Don't use WMI", 9, 8);
        private readonly CheckBox chkScriptJobs = Check("Script Jobs", 146, 8, true);
        private readonly CheckBox chkWriteToSecondaryDestinations = Check("Write to secondary destinations", 264, 8, true);
        private readonly Label lblIOCollectionLevel = Caption("IO Collection Level:", 9, 49);
        private readonly ComboBox cboIOLevel = new() { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(224, 45), Size = new Size(135, 28) };
        private readonly Label lblSchemaSnapshotDBs = Caption("Schema Snapshot Databases:", 9, 91);
        private readonly TextBox txtSnapshotDBs = new() { Location = new Point(224, 88), Size = new Size(449, 27) };
        private readonly LinkLabel lnkALL = Link("All", 535, 115);
        private readonly LinkLabel lnkNone = Link("None", 565, 115);
        private readonly LinkLabel lnkExample = Link("Example", 613, 115);
        private readonly Button bttnCustomCollections = new() { Text = "Custom Collections", Location = new Point(859, 8), Size = new Size(193, 37), Visible = false };

        // Deadlocks
        private readonly RadioButton optDisableDeadlock = Option("Disable Capture", 23, 16);
        private readonly RadioButton optSystemManagedDeadlock = Option("Dedicated System Managed Session (recommended)", 23, 46);
        private readonly RadioButton optSystemHealthDeadlock = Option("Use system health", 23, 76);
        private readonly RadioButton optCustomDeadlockSession = Option("Use existing session:", 23, 106);
        private readonly TextBox txtDeadlockSessionName = new() { Location = new Point(321, 106), Size = new Size(207, 27) };
        private readonly CheckBox chkBackfillDeadlocks = Check("Backfill history from system health", 23, 142, true);
        private readonly LinkLabel lnkApplyDeadlockConfigurationToAll = Link("Apply deadlock configuration to all existing connections", 650, 17);

        // Query stats
        private readonly CheckBox chkCollectQueryStats = Check("Collect query stats (statement level, from the plan cache)", 23, 16);
        private readonly NumericUpDown numQueryStatsTopN = new() { Location = new Point(230, 54), Size = new Size(100, 27), Minimum = 1, Maximum = 10000, Value = 50, Enabled = false };
        private readonly NumericUpDown numQueryStatsPlans = new() { Location = new Point(230, 90), Size = new Size(100, 27), Minimum = 0, Maximum = 10000, Value = 50, Enabled = false };
        private readonly LinkLabel lnkApplyQueryStatsConfigurationToAll = Link("Apply query stats configuration to all existing connections", 650, 17);

        private Dictionary<string, CustomCollection> customCollections = new();

        /// <summary>The Custom Collections button was clicked - only shown when <see cref="ShowCustomCollections"/>.</summary>
        public event EventHandler CustomCollectionsClick;

        /// <summary>The "Apply deadlock configuration to all existing connections" link was clicked (<see cref="ShowApplyToAll"/>).  The options are valid.</summary>
        public event EventHandler ApplyDeadlockConfigurationToAll;

        /// <summary>The "Apply query stats configuration to all existing connections" link was clicked (<see cref="ShowApplyToAll"/>).</summary>
        public event EventHandler ApplyQueryStatsConfigurationToAll;

        public SourceConnectionOptionsTabs()
        {
            pnlExtendedEvents.Controls.AddRange(new Control[]
            {
                chkSlowQueryThreshold, chkPersistXESession, chkDualSession, Caption("Threshold (ms):", 6, 109), numSlowQueryThreshold, lblSlow,
                Caption("Capture mode:", 620, 8), cboSlowQueryCaptureMode, Caption("Session name:", 620, 41), txtSlowQueryXESessionName, chkKeepSlowQuerySessionRunning
            });
            TabSlowQueries.Controls.Add(pnlExtendedEvents);

            grpLivePlanThreshold.Controls.AddRange(new Control[]
            {
                chkCaptureLivePlans, Caption("CPU (ms):", 6, 58), txtLivePlanCPUThreshold, Caption("Duration (ms):", 6, 92), txtLivePlanDurationThreshold,
                Caption("Max per collection:", 6, 126), txtLivePlanMaxPerCollection
            });
            grpRunningQueryThreshold.Controls.AddRange(new Control[]
            {
                Caption("Duration Threshold:", 6, 40), txtDurationThreshold, Caption("CPU Threshold:", 6, 83), txtCPUThreshold,
                Caption("Grant Threshold:", 263, 40), txtGrantThreshold, Caption("Count Threshold:", 263, 83), txtCountThreshold
            });
            TabRunningQueries.Controls.AddRange(new Control[] { chkCollectSessionWaits, chkCollectTaskWaits, chkCollectPlans, chkCollectTempDB, chkCollectCursors, grpLivePlanThreshold, grpRunningQueryThreshold });

            var picSnapshotWarning = new PictureBox
            {
                Image = new Icon(SystemIcons.Warning, 16, 16).ToBitmap(),
                Location = new Point(679, 91),
                Size = new Size(16, 16),
                SizeMode = PictureBoxSizeMode.Zoom
            };
            TabOther.Controls.AddRange(new Control[] { chkNoWMI, chkScriptJobs, chkWriteToSecondaryDestinations, lblIOCollectionLevel, cboIOLevel, lblSchemaSnapshotDBs, txtSnapshotDBs, picSnapshotWarning, lnkALL, lnkNone, lnkExample, bttnCustomCollections });

            lnkApplyDeadlockConfigurationToAll.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lnkApplyDeadlockConfigurationToAll.Visible = false;
            TabDeadlocks.Controls.AddRange(new Control[] { optDisableDeadlock, optSystemManagedDeadlock, optSystemHealthDeadlock, optCustomDeadlockSession, txtDeadlockSessionName, chkBackfillDeadlocks, lnkApplyDeadlockConfigurationToAll });

            lnkApplyQueryStatsConfigurationToAll.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lnkApplyQueryStatsConfigurationToAll.Visible = false;
            TabQueryStats.Controls.AddRange(new Control[]
            {
                chkCollectQueryStats, Caption("Top N:", 43, 56), numQueryStatsTopN, Caption("Plans per collection:", 43, 92), numQueryStatsPlans,
                Caption("Totals are correct at any Top N - queries below the cut are rolled up.  Raising it adds detail and storage.", 23, 132),
                lnkApplyQueryStatsConfigurationToAll
            });

            TabPages.AddRange(new[] { TabSlowQueries, TabRunningQueries, TabOther, TabDeadlocks, TabQueryStats });

            cboIOLevel.DataSource = Enum.GetValues(typeof(DBADashSource.IOCollectionLevels));
            cboSlowQueryCaptureMode.DataSource = Enum.GetValues(typeof(DBADashSource.SlowQueryCaptureModes));

            SetToolTips();
            WireEvents();
            // Not on the tab pages - the error icon is shown beside the control
            errorProvider.BlinkStyle = ErrorBlinkStyle.NeverBlink;
            Options = new SourceConnectionOptions();
        }

        private void SetToolTips()
        {
            toolTip.SetToolTip(chkDualSession, "Uses overlapping event sessions to try to capture events that occur during the brief period where the session is stopped to flush the ring buffer.");
            toolTip.SetToolTip(cboSlowQueryCaptureMode, SlowQueryCaptureModeToolTip);
            toolTip.SetToolTip(chkKeepSlowQuerySessionRunning, KeepSlowQuerySessionToolTip);
            toolTip.SetToolTip(chkCollectSessionWaits, "Collect Session Waits for Running Queries (sys.dm_exec_session_wait_stats)");
            toolTip.SetToolTip(chkCollectTaskWaits, "Collect Task Waits for Running Queries (sys.dm_os_waiting_tasks)\r\nUseful for parallel queries that can have several tasks waiting on different things");
            toolTip.SetToolTip(chkCollectCursors, "sys.dm_exec_cursors capture for Running Queries.  Enable if your application uses cursors and you need to identify the SQL query associated with the cursor.");
            toolTip.SetToolTip(chkScriptJobs, "If Jobs collection is enabled, the job definition will be scripted via SMO");
            toolTip.SetToolTip(txtSnapshotDBs, "Warning: Avoid using this feature on databases with very large numbers of objects or selecting all databases on servers with very large numbers of databases.");
            toolTip.SetToolTip(optSystemManagedDeadlock, $"Uses a system managed \"{DBADashSource.ManagedDeadlockXESessionName}\" session.");
            toolTip.SetToolTip(optSystemHealthDeadlock, "Uses existing system_health session.  This option has additional overhead compared to using a dedicated session.");
            toolTip.SetToolTip(optCustomDeadlockSession, "Option to use your own custom session.  e.g. You already have a suitable session created & running for other purposes.\r\nIdeally it captures only sqlserver.xml_deadlock_report to avoid unnecessary overhead.");
            toolTip.SetToolTip(chkBackfillDeadlocks, "After the first collection, read the system_health session once in the background for the deadlocks from before the dedicated session started.  Later collections use the dedicated session only.");
            toolTip.SetToolTip(chkCollectQueryStats,
                "Statement level resource usage, read from sys.dm_exec_query_stats every collection and diffed against the previous read.\r\n" +
                "Off by default: reading the plan cache costs in proportion to the size of the cache, so it's a decision to make per connection.");
            toolTip.SetToolTip(numQueryStatsTopN, QueryStatsTopNToolTip);
            toolTip.SetToolTip(numQueryStatsPlans, QueryStatsPlansToolTip);
        }

        private void WireEvents()
        {
            chkSlowQueryThreshold.CheckedChanged += (_, _) =>
            {
                numSlowQueryThreshold.Enabled = chkSlowQueryThreshold.Checked;
                if (chkSlowQueryThreshold.Checked)
                {
                    if (numSlowQueryThreshold.Value < 0) numSlowQueryThreshold.Value = 1000;
                    lblSlow.Text = "Extended events trace to capture slow rpc and batch completed events IS enabled";
                }
                else
                {
                    numSlowQueryThreshold.Value = -1;
                    lblSlow.Text = "Extended events trace to capture slow rpc and batch completed events is NOT enabled";
                }
                UpdateSlowQueryOptionsEnabled();
            };
            cboSlowQueryCaptureMode.SelectedIndexChanged += (_, _) => UpdateSlowQueryOptionsEnabled();

            chkCollectPlans.CheckedChanged += (_, _) =>
            {
                grpRunningQueryThreshold.Enabled = chkCollectPlans.Checked && chkCollectPlans.Enabled;
                // Live plans are only captured for queries whose plans are collected
                grpLivePlanThreshold.Enabled = chkCollectPlans.Checked && chkCollectPlans.Enabled;
                if (!chkCollectPlans.Checked) chkCaptureLivePlans.Checked = false;
                if (chkCollectPlans.Checked && !isLoading)
                {
                    SetPlanThreshold(PlanCollectionThreshold.DefaultThreshold);
                }
            };
            chkCaptureLivePlans.CheckedChanged += (_, _) =>
            {
                UpdateLivePlanOptionsEnabled();
                if (chkCaptureLivePlans.Checked && !isLoading)
                {
                    SetLiveProfileThreshold(LiveProfileThreshold.DefaultThreshold);
                }
            };

            lnkALL.LinkClicked += (_, _) => txtSnapshotDBs.Text = "*";
            lnkNone.LinkClicked += (_, _) => txtSnapshotDBs.Text = string.Empty;
            lnkExample.LinkClicked += (_, _) => txtSnapshotDBs.Text = "Database1,Database2,Database3";
            bttnCustomCollections.Click += (_, _) => CustomCollectionsClick?.Invoke(this, EventArgs.Empty);

            foreach (var opt in new[] { optDisableDeadlock, optSystemManagedDeadlock, optSystemHealthDeadlock, optCustomDeadlockSession })
            {
                opt.CheckedChanged += (_, _) => UpdateDeadlockOptionsEnabled();
            }
            lnkApplyDeadlockConfigurationToAll.LinkClicked += (_, _) =>
            {
                if (ValidateDeadlockOptions()) ApplyDeadlockConfigurationToAll?.Invoke(this, EventArgs.Empty);
            };

            chkCollectQueryStats.CheckedChanged += (_, _) => UpdateQueryStatsOptionsEnabled();
            lnkApplyQueryStatsConfigurationToAll.LinkClicked += (_, _) => ApplyQueryStatsConfigurationToAll?.Invoke(this, EventArgs.Empty);
        }

        private bool isLoading;

        /// <summary>Show the Custom Collections button - config tool only, as custom collections can't be added from the GUI.</summary>
        // Kept apart from the controls' Visible, which is false whenever their tab isn't the one selected
        private bool showCustomCollections;
        private bool showApplyToAll;

        [DefaultValue(false)]
        public bool ShowCustomCollections
        {
            get => showCustomCollections;
            set
            {
                showCustomCollections = value;
                bttnCustomCollections.Visible = value;
            }
        }

        /// <summary>Show the links that apply the deadlock and query stats configuration to every existing connection.</summary>
        [DefaultValue(false)]
        public bool ShowApplyToAll
        {
            get => showApplyToAll;
            set
            {
                showApplyToAll = value;
                lnkApplyDeadlockConfigurationToAll.Visible = value;
                lnkApplyQueryStatsConfigurationToAll.Visible = value;
            }
        }

        /// <summary>Custom collections for new connections (<see cref="ShowCustomCollections"/>).  Shown as a count on the button.</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Dictionary<string, CustomCollection> CustomCollections
        {
            get => customCollections;
            set
            {
                customCollections = value;
                bttnCustomCollections.Text = $"Custom Collections ({customCollections?.Count ?? 0})";
                bttnCustomCollections.Font = new Font(Font, customCollections?.Count > 0 ? FontStyle.Bold : FontStyle.Regular);
            }
        }

        /// <summary>The deadlock session chosen on the Deadlocks tab - see <see cref="DBADashSource.DeadlockXESessionName"/>.  Blank disables deadlock collection.</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string DeadlockXESessionName
        {
            get
            {
                if (optSystemManagedDeadlock.Checked) return DBADashSource.ManagedDeadlockXESessionName;
                if (optSystemHealthDeadlock.Checked) return DBADashSource.SystemHealthXESessionName;
                if (optCustomDeadlockSession.Checked) return txtDeadlockSessionName.Text.Trim();
                return string.Empty;
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    optDisableDeadlock.Checked = true;
                    txtDeadlockSessionName.Text = string.Empty;
                }
                else if (string.Equals(value, DBADashSource.ManagedDeadlockXESessionName, StringComparison.OrdinalIgnoreCase))
                {
                    optSystemManagedDeadlock.Checked = true;
                    txtDeadlockSessionName.Text = string.Empty;
                }
                else if (string.Equals(value, DBADashSource.SystemHealthXESessionName, StringComparison.OrdinalIgnoreCase))
                {
                    optSystemHealthDeadlock.Checked = true;
                    txtDeadlockSessionName.Text = string.Empty;
                }
                else
                {
                    optCustomDeadlockSession.Checked = true;
                    txtDeadlockSessionName.Text = value;
                }

                UpdateDeadlockOptionsEnabled();
            }
        }

        /// <summary>Backfill deadlocks from system health - read without the other options, so an invalid threshold doesn't stop it.</summary>
        [Browsable(false)]
        public bool BackfillDeadlocksFromSystemHealth => chkBackfillDeadlocks.Checked;

        /// <summary>The query stats top N - zero when query stats aren't collected.  Read without the other options.</summary>
        [Browsable(false)]
        public int QueryStatsTopN => chkCollectQueryStats.Checked ? (int)numQueryStatsTopN.Value : 0;

        [Browsable(false)]
        public int QueryStatsPlansPerCollection => (int)numQueryStatsPlans.Value;

        /// <summary>The options chosen.  Call <see cref="ValidateOptions"/> first - the thresholds must be numbers.</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public SourceConnectionOptions Options
        {
            get => new()
            {
                NoWMI = chkNoWMI.Checked,
                UseDualEventSession = chkDualSession.Checked,
                PersistXESessions = chkPersistXESession.Checked,
                SlowQueryThresholdMs = chkSlowQueryThreshold.Checked ? (int)numSlowQueryThreshold.Value : -1,
                SlowQueryCaptureMode = SelectedSlowQueryCaptureMode,
                SlowQueryXESessionName = txtSlowQueryXESessionName.Text.Trim(),
                KeepSlowQueryXESessionRunning = chkKeepSlowQuerySessionRunning.Checked,
                DeadlockXESessionName = DeadlockXESessionName,
                BackfillDeadlocksFromSystemHealth = chkBackfillDeadlocks.Checked,
                QueryStatsTopN = QueryStatsTopN,
                QueryStatsPlansPerCollection = QueryStatsPlansPerCollection,
                RunningQueryPlanThreshold = chkCollectPlans.Checked
                    ? new PlanCollectionThreshold
                    {
                        CountThreshold = int.Parse(txtCountThreshold.Text),
                        CPUThreshold = int.Parse(txtCPUThreshold.Text),
                        DurationThreshold = int.Parse(txtDurationThreshold.Text),
                        MemoryGrantThreshold = int.Parse(txtGrantThreshold.Text)
                    }
                    : null,
                RunningQueryLiveProfileThreshold = chkCollectPlans.Checked && chkCaptureLivePlans.Checked
                    ? new LiveProfileThreshold
                    {
                        CPUThreshold = int.Parse(txtLivePlanCPUThreshold.Text),
                        DurationThreshold = int.Parse(txtLivePlanDurationThreshold.Text),
                        MaxPerCollection = int.Parse(txtLivePlanMaxPerCollection.Text)
                    }
                    : null,
                SchemaSnapshotDBs = txtSnapshotDBs.Text.Trim(),
                CollectSessionWaits = chkCollectSessionWaits.Checked,
                CollectTaskWaits = chkCollectTaskWaits.Checked,
                CollectCursors = chkCollectCursors.Checked,
                ScriptAgentJobs = chkScriptJobs.Checked,
                IOCollectionLevel = cboIOLevel.SelectedItem is DBADashSource.IOCollectionLevels level ? level : DBADashSource.IOCollectionLevels.Full,
                WriteToSecondaryDestinations = chkWriteToSecondaryDestinations.Checked,
                CollectTempDB = chkCollectTempDB.Checked,
                CustomCollections = ShowCustomCollections ? CustomCollections : null
            };
            set
            {
                isLoading = true;
                try
                {
                    chkNoWMI.Checked = value.NoWMI;
                    chkPersistXESession.Checked = value.PersistXESessions;
                    chkDualSession.Checked = value.UseDualEventSession;
                    chkSlowQueryThreshold.Checked = value.SlowQueryThresholdMs != -1;
                    numSlowQueryThreshold.Value = Math.Clamp(value.SlowQueryThresholdMs, (int)numSlowQueryThreshold.Minimum, (int)numSlowQueryThreshold.Maximum);
                    cboSlowQueryCaptureMode.SelectedItem = value.SlowQueryCaptureMode;
                    txtSlowQueryXESessionName.Text = value.SlowQueryXESessionName;
                    chkKeepSlowQuerySessionRunning.Checked = value.KeepSlowQueryXESessionRunning;
                    DeadlockXESessionName = value.DeadlockXESessionName;
                    chkBackfillDeadlocks.Checked = value.BackfillDeadlocksFromSystemHealth;
                    chkCollectQueryStats.Checked = value.QueryStatsTopN > 0;
                    numQueryStatsTopN.Value = value.QueryStatsTopN > 0
                        ? Math.Clamp(value.QueryStatsTopN, (int)numQueryStatsTopN.Minimum, (int)numQueryStatsTopN.Maximum)
                        : 50;
                    numQueryStatsPlans.Value = Math.Clamp(value.QueryStatsPlansPerCollection, (int)numQueryStatsPlans.Minimum, (int)numQueryStatsPlans.Maximum);
                    chkScriptJobs.Checked = value.ScriptAgentJobs;
                    txtSnapshotDBs.Text = value.SchemaSnapshotDBs;
                    chkCollectPlans.Checked = value.RunningQueryPlanThreshold is { PlanCollectionEnabled: true };
                    SetPlanThreshold(value.RunningQueryPlanThreshold ?? PlanCollectionThreshold.DefaultThreshold);
                    // After the plan collection check box, which clears this one when it changes
                    chkCaptureLivePlans.Checked = value.RunningQueryLiveProfileThreshold is { Enabled: true };
                    SetLiveProfileThreshold(value.RunningQueryLiveProfileThreshold ?? LiveProfileThreshold.DefaultThreshold);
                    chkCollectTaskWaits.Checked = value.CollectTaskWaits;
                    chkCollectCursors.Checked = value.CollectCursors;
                    chkCollectSessionWaits.Checked = value.CollectSessionWaits;
                    cboIOLevel.SelectedItem = value.IOCollectionLevel;
                    chkCollectTempDB.Checked = value.CollectTempDB;
                    chkWriteToSecondaryDestinations.Checked = value.WriteToSecondaryDestinations;
                    CustomCollections = value.CustomCollections ?? new Dictionary<string, CustomCollection>();
                }
                finally
                {
                    isLoading = false;
                }
                UpdateSlowQueryOptionsEnabled();
                UpdateLivePlanOptionsEnabled();
                UpdateQueryStatsOptionsEnabled();
            }
        }

        /// <summary>Load the options of an existing connection - e.g. to edit it.</summary>
        public void LoadFromSource(DBADashSource src) => Options = SourceConnectionOptions.FromSource(src);

        /// <summary>Most options only apply to a SQL connection - not a folder or S3 path.</summary>
        public void SetSqlOptionsEnabled(bool isSql)
        {
            pnlExtendedEvents.Enabled = isSql;
            TabDeadlocks.Enabled = isSql; // Reading an XE session needs a SQL connection
            TabQueryStats.Enabled = isSql;
            chkCollectPlans.Enabled = isSql;
            grpRunningQueryThreshold.Enabled = isSql && chkCollectPlans.Checked;
            grpLivePlanThreshold.Enabled = isSql && chkCollectPlans.Checked;
            foreach (var c in new Control[] { chkNoWMI, chkScriptJobs, cboIOLevel, txtSnapshotDBs, lblIOCollectionLevel, lblSchemaSnapshotDBs, chkCollectSessionWaits, chkCollectTaskWaits, chkCollectCursors, chkCollectTempDB, lnkALL, lnkExample, lnkNone, bttnCustomCollections })
            {
                c.Enabled = isSql;
            }
        }

        /// <summary>
        /// Checks the options can be used, showing an error beside the first problem and selecting its tab.  The same
        /// checks as <see cref="SourceConnectionOptions.Validate"/>, plus that the thresholds are numbers.
        /// </summary>
        public bool ValidateOptions() => ValidateDeadlockOptions() && ValidateSlowQueryOptions() && ValidateThresholds();

        private bool ValidateDeadlockOptions()
        {
            errorProvider.SetError(txtDeadlockSessionName, null);
            if (!optCustomDeadlockSession.Checked || !string.IsNullOrWhiteSpace(txtDeadlockSessionName.Text))
            {
                return true;
            }

            ShowError(TabDeadlocks, txtDeadlockSessionName, "Enter the name of the extended events session to read deadlocks from");
            return false;
        }

        /// <summary>The existing session mode is the only one that needs a name typed - and not the name of a session DBA Dash manages itself.</summary>
        private bool ValidateSlowQueryOptions()
        {
            errorProvider.SetError(txtSlowQueryXESessionName, null);
            if (!txtSlowQueryXESessionName.Enabled)
            {
                return true;
            }

            string error = null;
            if (string.IsNullOrWhiteSpace(txtSlowQueryXESessionName.Text))
            {
                error = "Enter the name of the extended events session to read slow queries from";
            }
            else if (DBADashSource.IsReservedSlowQueryXESessionName(txtSlowQueryXESessionName.Text))
            {
                error = "This is a session DBA Dash creates and removes itself.  Use the EventFile or RingBuffer capture mode, or a session of your own with a different name.";
            }

            if (error == null)
            {
                return true;
            }

            ShowError(TabSlowQueries, txtSlowQueryXESessionName, error);
            return false;
        }

        private bool ValidateThresholds()
        {
            var thresholds = new List<TextBox>();
            if (chkCollectPlans.Checked) thresholds.AddRange(new[] { txtDurationThreshold, txtCPUThreshold, txtGrantThreshold, txtCountThreshold });
            if (chkCollectPlans.Checked && chkCaptureLivePlans.Checked) thresholds.AddRange(new[] { txtLivePlanCPUThreshold, txtLivePlanDurationThreshold, txtLivePlanMaxPerCollection });
            foreach (var txt in new[] { txtDurationThreshold, txtCPUThreshold, txtGrantThreshold, txtCountThreshold, txtLivePlanCPUThreshold, txtLivePlanDurationThreshold, txtLivePlanMaxPerCollection })
            {
                errorProvider.SetError(txt, null);
            }

            var invalid = thresholds.FirstOrDefault(t => !int.TryParse(t.Text, out var v) || v < 0);
            if (invalid == null)
            {
                return true;
            }

            ShowError(TabRunningQueries, invalid, "Enter a whole number");
            return false;
        }

        private void ShowError(TabPage tab, Control control, string error)
        {
            errorProvider.SetError(control, error);
            SelectedTab = tab;
        }

        private DBADashSource.SlowQueryCaptureModes SelectedSlowQueryCaptureMode =>
            cboSlowQueryCaptureMode.SelectedItem is DBADashSource.SlowQueryCaptureModes mode
                ? mode
                : DBADashSource.SlowQueryCaptureModes.RingBuffer;

        /// <summary>Each option on the Slow Queries tab only applies to some modes.</summary>
        private void UpdateSlowQueryOptionsEnabled()
        {
            var enabled = chkSlowQueryThreshold.Checked;
            var mode = SelectedSlowQueryCaptureMode;
            numSlowQueryThreshold.Enabled = enabled;
            cboSlowQueryCaptureMode.Enabled = enabled;
            chkDualSession.Enabled = enabled && mode == DBADashSource.SlowQueryCaptureModes.RingBuffer;
            chkPersistXESession.Enabled = enabled && mode != DBADashSource.SlowQueryCaptureModes.ExistingSession;
            txtSlowQueryXESessionName.Enabled = enabled && mode == DBADashSource.SlowQueryCaptureModes.ExistingSession;
            chkKeepSlowQuerySessionRunning.Enabled = enabled && mode == DBADashSource.SlowQueryCaptureModes.EventFile;
            lblSlow.Text = enabled
                ? "Extended events trace to capture slow rpc and batch completed events IS enabled"
                : "Extended events trace to capture slow rpc and batch completed events is NOT enabled";
            if (!txtSlowQueryXESessionName.Enabled)
            {
                errorProvider.SetError(txtSlowQueryXESessionName, null);
            }
        }

        private void UpdateLivePlanOptionsEnabled()
        {
            txtLivePlanCPUThreshold.Enabled = chkCaptureLivePlans.Checked;
            txtLivePlanDurationThreshold.Enabled = chkCaptureLivePlans.Checked;
            txtLivePlanMaxPerCollection.Enabled = chkCaptureLivePlans.Checked;
        }

        private void UpdateDeadlockOptionsEnabled()
        {
            txtDeadlockSessionName.Enabled = optCustomDeadlockSession.Checked;
            // Only a dedicated session has anything to backfill - system_health is the backfill source itself
            chkBackfillDeadlocks.Enabled = optSystemManagedDeadlock.Checked || optCustomDeadlockSession.Checked;
            if (!optCustomDeadlockSession.Checked)
            {
                errorProvider.SetError(txtDeadlockSessionName, null);
            }
        }

        private void UpdateQueryStatsOptionsEnabled()
        {
            numQueryStatsTopN.Enabled = chkCollectQueryStats.Checked;
            numQueryStatsPlans.Enabled = chkCollectQueryStats.Checked;
        }

        private void SetPlanThreshold(PlanCollectionThreshold threshold)
        {
            txtCPUThreshold.Text = threshold.CPUThreshold.ToString();
            txtCountThreshold.Text = threshold.CountThreshold.ToString();
            txtDurationThreshold.Text = threshold.DurationThreshold.ToString();
            txtGrantThreshold.Text = threshold.MemoryGrantThreshold.ToString();
        }

        private void SetLiveProfileThreshold(LiveProfileThreshold threshold)
        {
            txtLivePlanCPUThreshold.Text = threshold.CPUThreshold.ToString();
            txtLivePlanDurationThreshold.Text = threshold.DurationThreshold.ToString();
            txtLivePlanMaxPerCollection.Text = threshold.MaxPerCollection.ToString();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                toolTip.Dispose();
                errorProvider.Dispose();
            }
            base.Dispose(disposing);
        }

        private static CheckBox Check(string text, int x, int y, bool isChecked = false) =>
            new() { Text = text, Location = new Point(x, y), AutoSize = true, Checked = isChecked };

        private static RadioButton Option(string text, int x, int y) =>
            new() { Text = text, Location = new Point(x, y), AutoSize = true };

        private static Label Caption(string text, int x, int y) =>
            new() { Text = text, Location = new Point(x, y), AutoSize = true };

        private static LinkLabel Link(string text, int x, int y) =>
            new() { Text = text, Location = new Point(x, y), AutoSize = true, TabStop = true };

        private static TextBox Number(int x, int y) =>
            new() { Location = new Point(x, y), Size = new Size(100, 27) };
    }
}
