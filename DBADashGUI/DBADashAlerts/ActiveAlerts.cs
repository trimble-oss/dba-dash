using DBADash;
using DBADashGUI.CustomReports;
using DBADashGUI.SchemaCompare;
using DBADashGUI.Theme;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using DBADashGUI.DBADashAlerts.Rules;
using static DBADashGUI.Main;

namespace DBADashGUI.DBADashAlerts
{
    public partial class ActiveAlerts : UserControl, ISetContext, IRefreshData
    {
        private DBADashContext CurrentContext;

        public event EventHandler<InstanceSelectedEventArgs> Instance_Selected;

        public ActiveAlerts()
        {
            InitializeComponent();
            customReportView1.PostGridRefresh += OnPostGridRefresh;
            CreateMenuItems();
        }

        private void CreateMenuItems()
        {
            var tsConfigure = new ToolStripButton("Configure", Properties.Resources.SettingsOutline_16x);
            tsConfigure.Click += Configure_Click;
            customReportView1.ToolStrip.Items.Add(tsConfigure);
            var tsHistory = new ToolStripMenuItem("History", Properties.Resources.Time_16x);
            tsHistory.Click += TsHistory_Click;
            customReportView1.ToolStrip.Items.Add(tsHistory);
            var tsActions = new ToolStripMenuItem("Actions", Properties.Resources.Table_16x);
            var tsCloseResolved = new ToolStripMenuItem("Close resolved alerts");
            tsCloseResolved.Click += TsCloseResolved_Click;
            var tsAcknowledge = new ToolStripMenuItem("Acknowledge alerts");
            tsAcknowledge.Click += TsAcknowledge_Click;

            var tsAddNotes = new ToolStripMenuItem("Add notes to selected alerts");
            tsAddNotes.Click += TsAddNotes_Click;

            var tsClearAcknowledgement = new ToolStripMenuItem("Clear acknowledgement");
            tsClearAcknowledgement.Click += TsClearAcknowledgement_Click;

            tsActions.DropDownItems.Add(tsAcknowledge);
            tsActions.DropDownItems.Add(tsClearAcknowledgement);
            tsActions.DropDownItems.Add(tsAddNotes);
            tsActions.DropDownItems.Add(tsCloseResolved);
            customReportView1.ToolStrip.Items.Add(tsActions);
        }

        private void TsAddNotes_Click(object sender, EventArgs e)
        {
            if (customReportView1.Grids.Count == 0)
            {
                MessageBox.Show("No grid", "Add Notes", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var selectedRows = customReportView1.Grids[0].SelectedCells.Cast<DataGridViewCell>().Select(cell => cell.OwningRow).Distinct().ToList();
            if (selectedRows.Count == 0)
            {
                MessageBox.Show("No rows selected", "Add Notes", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var notes = (string)selectedRows.Select(r => (string)r.Cells["Notes"].Value.DBNullToNull()).FirstOrDefault(notes => !string.IsNullOrEmpty(notes));
            using var frm = new CodeEditorForm();
            frm.Code = notes;
            frm.Syntax = CodeEditor.CodeEditorModes.Markdown;
            if (frm.ShowDialog() != DialogResult.OK) return;
            if (MessageBox.Show($@"Update notes for {selectedRows.Count} rows?", @"Update Notes", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            foreach (var row in selectedRows.OfType<DataGridViewRow>())
            {
                var alertId = (long)row.Cells["AlertID"].Value;
                EditNotesLinkColumnInfo.UpdateNotes(alertId, frm.Code);
            }
            RefreshData();
        }

        private void TsClearAcknowledgement_Click(object sender, EventArgs e)
        {
            AcknowledgeAll(false);
        }

        private void TsAcknowledge_Click(object sender, EventArgs e)
        {
            AcknowledgeAll(true);
        }

        private void AcknowledgeAll(bool isAck)
        {
            try
            {
                if (customReportView1.Grids.Count == 0) return;
                var dt = ((DataView)customReportView1.Grids[0].DataSource).Table;
                if (dt == null) return;
                var alertIds = dt.Rows.Cast<DataRow>().Where(r => (bool)r["IsAcknowledged"] != isAck)
                    .Select(r => (long)r["AlertID"]).ToList();
                if (alertIds.Count > 0)
                {
                    AckAlert(alertIds, isAck);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, @"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            RefreshData();
        }

        private void TsCloseResolved_Click(object sender, EventArgs e)
        {
            try
            {
                if (customReportView1.Grids.Count == 0) return;

                var dt = ((DataView)customReportView1.Grids[0].DataSource).Table;
                if (dt == null) return;
                var alertIds = dt.Rows.Cast<DataRow>().Where(r => (bool)r["IsResolved"] == true)
                    .Select(r => (long)r["AlertID"]).ToList();
                if (alertIds.Count > 0)
                {
                    if (MessageBox.Show($@"Close {alertIds.Count} alerts?", @"Close Alerts", MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question) != DialogResult.Yes) return;
                    CloseAlert(alertIds);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, @"Error", MessageBoxButtons.OK);
            }

            RefreshData();
        }

        private void TsHistory_Click(object sender, EventArgs e)
        {
            var context = CurrentContext.DeepCopy();
            context.Report = ClosedAlertsReport;
            using var historyViewer = new CustomReportViewer()
            {
                Context = context
            };
            historyViewer.ShowDialog();
        }

        private void Configure_Click(object sender, EventArgs e)
        {
            using var frm = new Form() { Width = this.Width / 2, Height = this.Height / 2, Text = @"Alert Configuration" };
            using var configGrid = new AlertConfig() { Dock = DockStyle.Fill };
            frm.Controls.Add(configGrid);
            configGrid.SetContext(CurrentContext);
            frm.ShowDialog();
        }

        private (string Message, string ToolTip, Color Color) CurrentStatus;

        public void SetContext(DBADashContext _context)
        {
            if (_context == null) return;
            CurrentContext = _context;
            _context.Report = ActiveAlertsReport;
            customReportView1.SetContext(_context);
            if (customReportView1.TimeSinceRefresh.Seconds > 5) // Alerts should show current data on load
            {
                customReportView1.RefreshData();
            }
            SetStatus(CurrentStatus.Message, CurrentStatus.ToolTip, CurrentStatus.Color);
        }

        protected void OnPostGridRefresh(object sender, EventArgs e)
        {
            if (customReportView1.Grids.Count == 0) return;
            var grid = customReportView1.Grids[0];
            if (!grid.Columns.Contains("colAck"))
            {
                grid.Columns.Add(new DataGridViewLinkColumn()
                {
                    Name = "colAck",
                    HeaderText = @"Acknowledge",
                    Text = "Acknowledge",
                    UseColumnTextForLinkValue = true,
                });
                grid.Columns.Add(new DataGridViewLinkColumn()
                {
                    Name = "colClose",
                    HeaderText = @"Close",
                    Text = "Close",
                    UseColumnTextForLinkValue = true,
                });
                grid.CellContentClick -= Grid_CellContentClick;
                grid.CellContentClick += Grid_CellContentClick;
                grid.CellFormatting -= CellFormatting;
                grid.CellFormatting += CellFormatting;
                grid.ApplyTheme();
            }
        }

        private static void CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var grid = (DBADashDataGridView)sender;
            var isAcknowledged = (bool)grid.Rows[e.RowIndex].Cells["IsAcknowledged"].Value;
            e.Value = grid.Columns[e.ColumnIndex].Name switch
            {
                "colAck" => isAcknowledged ? "Clear" : "Acknowledge",
                _ => e.Value
            };
        }

        private void Grid_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var grid = (DBADashDataGridView)sender;
            var instanceID = (int)grid.Rows[e.RowIndex].Cells["InstanceID"].Value;
            var alertID = (long)grid.Rows[e.RowIndex].Cells["AlertID"].Value;
            var instanceName = (string)grid.Rows[e.RowIndex].Cells["InstanceDisplayName"].Value;
            var isAcknowledged = (bool)grid.Rows[e.RowIndex].Cells["IsAcknowledged"].Value;
            var alertTypeString = (string)grid.Rows[e.RowIndex].Cells["AlertType"].Value;
            Enum.TryParse(typeof(AlertRuleBase.RuleTypes), alertTypeString, true, out var alertType);

            var ruleId = (int?)grid.Rows[e.RowIndex].Cells["RuleID"].Value.DBNullToNull();
            try
            {
                switch (grid.Columns[e.ColumnIndex].Name)
                {
                    case "colAck":

                        AckAlert(alertID, !isAcknowledged);
                        RefreshData();

                        break;

                    case "colClose":

                        CloseAlert(alertID);
                        RefreshData();

                        break;

                    case "InstanceDisplayName":
                        var tab = alertType switch
                        {
                            AlertRuleBase.RuleTypes.DriveSpace => "tabDrives",
                            AlertRuleBase.RuleTypes.AGHealth => "tabAG",
                            AlertRuleBase.RuleTypes.Counter => "tabPC",
                            AlertRuleBase.RuleTypes.CollectionDates => "tabCollectionDates",
                            AlertRuleBase.RuleTypes.AgentJob => "tabJobs",
                            AlertRuleBase.RuleTypes.SQLAgentAlert => "tabSQLAgentAlerts",
                            AlertRuleBase.RuleTypes.Offline => "tabOfflineInstances",
                            _ => "tabPerformance"
                        };
                        Instance_Selected?.Invoke(this,
                            new InstanceSelectedEventArgs()
                            { Instance = instanceName, InstanceID = instanceID, Tab = tab });
                        break;

                    case "RuleID":
                        if (ruleId == null) return;
                        AlertConfig.EditRule(ruleId.Value);
                        break;

                    case "NotificationCount":
                        ShowNotificationLog(alertID, false);
                        break;

                    case "FailedNotificationCount":
                        ShowNotificationLog(alertID, true);
                        break;
                }
            }
            catch (SqlException ex) when (ex.Number == 229)
            {
                MessageBox.Show(ex.Message, @"Insufficient permissions", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, @"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowNotificationLog(long alertId, bool failedOnly)
        {
            using var notificationViewer = new CustomReportViewer();
            var reportContext = CurrentContext.DeepCopy();
            reportContext.Report = NotificationLogReport;
            notificationViewer.Context = reportContext;
            notificationViewer.CustomParams = new List<CustomSqlParameter>()
            {
                new() { Param = new SqlParameter("AlertID", alertId) },
                new() { Param = new SqlParameter("FailedOnly",failedOnly) }
            };
            notificationViewer.ShowDialog();
        }

        private static void AckAlert(long alertID, bool isAck) => AckAlert(new List<long>() { alertID }, isAck);

        private static void AckAlert(IEnumerable<long> alertIDs, bool isAck)
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            cn.Open();
            using var cmd = new SqlCommand("Alert.ActiveAlertsAck_Upd", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@AlertIDs", alertIDs.AsDataTable());
            cmd.Parameters.AddWithValue("@IsAcknowledged", isAck);
            cmd.ExecuteNonQuery();
        }

        private static void CloseAlert(long alertID) => CloseAlert(new List<long>() { alertID });

        private static void CloseAlert(IEnumerable<long> alertIDs)
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            cn.Open();
            using var cmd = new SqlCommand("Alert.ClosedAlerts_Add", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@AlertIDs", alertIDs.AsDataTable());
            cmd.ExecuteNonQuery();
        }

        private void SetStatus(string message, string tooltip, Color color)
        {
            CurrentStatus = new(message, tooltip, color);
            customReportView1.StatusLabel.InvokeSetStatus(message ?? string.Empty, tooltip ?? string.Empty, color);
        }

        public static SystemReport NotificationLogReport => new()
        {
            ReportName = "Notification Log",
            SchemaName = "Alert",
            ProcedureName = "NotificationLog_Get",
            QualifiedProcedureName = "Alert.NotificationLog_Get",
            CanEditReport = false,
            Params = new Params()
            {
                ParamList = new List<Param>
                {
                    new() { ParamName = "@AlertID", ParamType = "BIGINT" },
                    new() { ParamName = "@FailedOnly", ParamType = "BIT" }
                }
            },
            CustomReportResults = new Dictionary<int, CustomReportResult>
            {
                {
                    0, new CustomReportResult
                    {
                        ColumnAlias = new Dictionary<string, string>
                        {
                            { "NotificationDate", "Date/Time" },
                            { "NotificationChannelType", "Channel Type" },
                            { "ChannelName", "Channel Name" },
                            { "Status", "Status" },
                            { "ErrorMessage", "Error Message" },
                            { "NotificationMessage","Notification Message"}
                        },
                        CellHighlightingRules =
                        {
                            {
                                "Status",
                                new CellHighlightingRuleSet("Status")
                                {
                                    Rules = new List<CellHighlightingRule>
                                    {
                                        new()
                                        {
                                            ConditionType = CellHighlightingRule.ConditionTypes.Equals,
                                            Value1 = "OK",
                                            Status = DBADashStatus.DBADashStatusEnum.OK
                                        },
                                        new()
                                        {
                                            ConditionType = CellHighlightingRule.ConditionTypes.GreaterThanOrEqual,
                                            Value1 = "Error",
                                            Status = DBADashStatus.DBADashStatusEnum.Critical
                                        }
                                    }
                                }
                            },
                        }
                    }
                }
            }
        };

        public static SystemReport ActiveAlertsReport => new()
        {
            ReportName = "Active Alerts",
            SchemaName = "Alert",
            ProcedureName = "ActiveAlerts_Get",
            QualifiedProcedureName = "Alert.ActiveAlerts_Get",
            CanEditReport = false,
            Params = new Params
            {
                ParamList = new List<Param>
                {
                    new() { ParamName = "@InstanceIDs", ParamType = "IDS" },
                    new() { ParamName = "@InstanceID", ParamType = "INT" },
                }
            },
            CustomReportResults = new Dictionary<int, CustomReportResult>
            {
                {
                    0, new CustomReportResult
                    {
                        ColumnAlias = new Dictionary<string, string>
                        {
                            { "InstanceID", "Instance ID" },
                            { "AlertID", "Alert ID" },
                            { "InstanceDisplayName", "Instance" },
                            { "Priority", "Priority ID" },
                            { "PriorityDescription", "Priority" },
                            { "AlertType", "Alert Type" },
                            { "AlertKey", "Alert Key" },
                            { "FirstMessage", "First Message" },
                            { "LastMessage", "Last Message" },
                            { "TriggerDate", "Trigger Date" },
                            { "UpdatedDate", "Updated Date" },
                            { "TimeSinceLastUpdate", "Time since last update" },
                            { "FirstNotification", "First Notification" },
                            { "LastNotification", "Last Notification" },
                            { "TimeSinceLastNotification", "Time since last notification" },
                            { "UpdateCount", "Update Count" },
                            { "ResolvedCount", "Resolved Count" },
                            { "NotificationCount", "Notification Count" },
                            { "FailedNotificationCount", "Failed Notification Count" },
                            { "IsAcknowledged", "Acknowledged?" },
                            { "IsResolved", "Resolved?" },
                            { "ResolvedDate", "Resolved Date" },
                            { "IsBlackout", "Is Blackout" },
                            { "AlertDuration", "Alert Duration" },
                            {"Notes","Notes"},
                            {"RuleNotes","Rule Notes"},
                            {"RuleID","Rule#"}
                        },
                         ColumnLayout = new List<KeyValuePair<string, PersistedColumnLayout>>()
                            {
                                new("AlertID", new PersistedColumnLayout { Visible = false }),
                                new("InstanceID", new PersistedColumnLayout { Visible = false }),
                                new("InstanceDisplayName", new PersistedColumnLayout { Visible = true }),
                                new("Priority", new PersistedColumnLayout { Visible = false }),
                                new("PriorityDescription", new PersistedColumnLayout { Visible = true}),
                                new("AlertType", new PersistedColumnLayout { Visible = true }),
                                new("AlertKey", new PersistedColumnLayout { Visible = true }),
                                new("FirstMessage", new PersistedColumnLayout { Visible = true }),
                                new("LastMessage", new PersistedColumnLayout { Visible = true }),
                                new("TriggerDate", new PersistedColumnLayout { Visible = true }),
                                new("AlertDuration", new PersistedColumnLayout { Visible = true }),
                                new("UpdatedDate", new PersistedColumnLayout { Visible = false }),
                                new("TimeSinceLastUpdate", new PersistedColumnLayout { Visible = true }),
                                new("FirstNotification", new PersistedColumnLayout { Visible = false }),
                                new("LastNotification", new PersistedColumnLayout { Visible = false }),
                                new("TimeSinceLastNotification", new PersistedColumnLayout { Visible = true }),
                                new("UpdateCount", new PersistedColumnLayout { Visible = true }),
                                new("ResolvedCount", new PersistedColumnLayout { Visible = true }),
                                new("NotificationCount", new PersistedColumnLayout { Visible = true }),
                                new("FailedNotificationCount", new PersistedColumnLayout { Visible = true }),
                                new("IsAcknowledged", new PersistedColumnLayout { Visible = true }),
                                new("IsResolved", new PersistedColumnLayout { Visible = true }),
                                new("ResolvedDate", new PersistedColumnLayout { Visible = true }),
                                new("IsBlackout", new PersistedColumnLayout { Visible = true }),
                                new("Notes", new PersistedColumnLayout { Visible = true }),
                                new("RuleNotes", new PersistedColumnLayout { Visible = true }),
                                new("RuleID", new PersistedColumnLayout { Visible = true }),
                            },
                        LinkColumns = new()
                        {
                            {
                                "InstanceDisplayName",
                                null
                            },
                            {
                                "Notes",
                                new EditNotesLinkColumnInfo()
                            },
                            {
                                "RuleID",
                                null
                            },
                            {
                                "NotificationCount",
                                null
                            },
                            {
                                "FailedNotificationCount",
                                null
                            },
                            {
                                "RuleNotes",
                                new TextLinkColumnInfo()
                                {
                                    TextHandling = CodeEditor.CodeEditorModes.Markdown,
                                    TargetColumn = "RuleNotes"
                                }
                            },
                        },
                        CellNullValue = new Dictionary<string, string>()
                        {
                            {"Notes","Add Notes..."}
                        },
                        CellHighlightingRules =
                        {
                            {
                                "PriorityDescription",
                                new CellHighlightingRuleSet("AlertStatus") {IsStatusColumn = true }
                            },
                            {
                                "FailedNotificationCount",
                                new CellHighlightingRuleSet("FailedNotificationCount")
                                {
                                    Rules = new List<CellHighlightingRule>
                                    {
                                        new()
                                        {
                                            ConditionType = CellHighlightingRule.ConditionTypes.Equals,
                                            Value1 = 0.ToString(),
                                            Status = DBADashStatus.DBADashStatusEnum.OK
                                        },
                                        new()
                                        {
                                            ConditionType = CellHighlightingRule.ConditionTypes.GreaterThanOrEqual,
                                            Value1 = 1.ToString(),
                                            Status = DBADashStatus.DBADashStatusEnum.Critical
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };

        public static SystemReport ClosedAlertsReport => new()
        {
            ReportName = "Closed Alerts",
            SchemaName = "Alert",
            ProcedureName = "ClosedAlerts_Get",
            QualifiedProcedureName = "Alert.ClosedAlerts_Get",
            CanEditReport = false,
            Params = new Params
            {
                ParamList = new List<Param>
                {
                    new() { ParamName = "@InstanceIDs", ParamType = "IDS" },
                    new() { ParamName = "@InstanceID", ParamType = "INT" },
                    new() { ParamName = "@Top", ParamType = "INT" },
                }
            },
            CustomReportResults = new Dictionary<int, CustomReportResult>
            {
                {
                    0, new CustomReportResult
                    {
                        ColumnAlias = new Dictionary<string, string>
                        {
                            { "InstanceID", "Instance ID" },
                            { "AlertID", "Alert ID" },
                            { "InstanceDisplayName", "Instance" },
                            { "Priority", "Priority ID" },
                            { "PriorityDescription", "Priority" },
                            { "AlertType", "Alert Type" },
                            { "AlertKey", "Alert Key" },
                            { "FirstMessage", "First Message" },
                            { "LastMessage", "Last Message" },
                            { "TriggerDate", "Trigger Date" },
                            { "UpdatedDate", "Updated Date" },
                            { "TimeSinceLastUpdate", "Time since last update" },
                            { "FirstNotification", "First Notification" },
                            { "LastNotification", "Last Notification" },
                            { "TimeSinceLastNotification", "Time since last notification" },
                            { "UpdateCount", "Update Count" },
                            { "ResolvedCount", "Resolved Count" },
                            { "NotificationCount", "Notification Count" },
                            { "FailedNotificationCount", "Failed Notification Count" },
                            { "IsAcknowledged", "Acknowledged?" },
                            { "IsResolved", "Resolved?" },
                            { "ResolvedDate", "Resolved Date" },
                            { "IsBlackout", "IsBlackout" },
                            { "AlertDuration", "Alert Duration" },
                            {"Notes","Notes"},
                            {"RuleNotes","Rule Notes"},
                            {"RuleID","Rule#"}
                        },
                         ColumnLayout = new List<KeyValuePair<string, PersistedColumnLayout>>()
                            {
                                new("AlertID", new PersistedColumnLayout { Visible = false }),
                                new("InstanceID", new PersistedColumnLayout { Visible = false }),
                                new("InstanceDisplayName", new PersistedColumnLayout { Visible = true }),
                                new("Priority", new PersistedColumnLayout { Visible = false }),
                                new("PriorityDescription", new PersistedColumnLayout { Visible = true}),
                                new("AlertType", new PersistedColumnLayout { Visible = true }),
                                new("AlertKey", new PersistedColumnLayout { Visible = true }),
                                new("FirstMessage", new PersistedColumnLayout { Visible = true }),
                                new("LastMessage", new PersistedColumnLayout { Visible = true }),
                                new("TriggerDate", new PersistedColumnLayout { Visible = true }),
                                new("AlertDuration", new PersistedColumnLayout { Visible = true }),
                                new("UpdatedDate", new PersistedColumnLayout { Visible = false }),
                                new("TimeSinceLastUpdate", new PersistedColumnLayout { Visible = true }),
                                new("FirstNotification", new PersistedColumnLayout { Visible = false }),
                                new("LastNotification", new PersistedColumnLayout { Visible = false }),
                                new("TimeSinceLastNotification", new PersistedColumnLayout { Visible = true }),
                                new("UpdateCount", new PersistedColumnLayout { Visible = true }),
                                new("ResolvedCount", new PersistedColumnLayout { Visible = true }),
                                new("NotificationCount", new PersistedColumnLayout { Visible = true }),
                                new("FailedNotificationCount", new PersistedColumnLayout { Visible = true }),
                                new("IsAcknowledged", new PersistedColumnLayout { Visible = true }),
                                new("IsResolved", new PersistedColumnLayout { Visible = true }),
                                new("ResolvedDate", new PersistedColumnLayout { Visible = true }),
                                new("Notes", new PersistedColumnLayout { Visible = true }),
                                new("RuleNotes", new PersistedColumnLayout { Visible = true }),
                                new("RuleID", new PersistedColumnLayout { Visible = true }),
                            },
                        LinkColumns = new()
                        {
                            {
                                "InstanceDisplayName",
                                null
                            },
                            {
                                "Notes",
                                new EditNotesLinkColumnInfo()
                            },
                            {
                                "RuleNotes",
                                new TextLinkColumnInfo()
                                {
                                    TextHandling = CodeEditor.CodeEditorModes.Markdown,
                                    TargetColumn = "RuleNotes"
                                }
                            },
                        },
                        CellNullValue = new Dictionary<string, string>()
                        {
                            {"Notes","Add Notes..."}
                        },
                        CellHighlightingRules =
                        {
                            {
                                "FailedNotificationCount",
                                new CellHighlightingRuleSet("FailedNotificationCount")
                                {
                                    Rules = new List<CellHighlightingRule>
                                    {
                                        new()
                                        {
                                            ConditionType = CellHighlightingRule.ConditionTypes.Equals,
                                            Value1 = 0.ToString(),
                                            Status = DBADashStatus.DBADashStatusEnum.OK
                                        },
                                        new()
                                        {
                                            ConditionType = CellHighlightingRule.ConditionTypes.GreaterThanOrEqual,
                                            Value1 = 1.ToString(),
                                            Status = DBADashStatus.DBADashStatusEnum.Critical
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };

        public static CellHighlightingRuleSet GetPriorityCellHighlightingRuleSet() => new("Priority")
        {
            Rules = new List<CellHighlightingRule>
            {
                new()
                {
                    ConditionType = CellHighlightingRule.ConditionTypes.Between,
                    Value1 = "0",
                    Value2 = "10",
                    Status = DBADashStatus.DBADashStatusEnum.Critical,
                },
                new()
                {
                    ConditionType = CellHighlightingRule.ConditionTypes.Between,
                    Value1 = "11",
                    Value2 = "20",
                    Status = DBADashStatus.DBADashStatusEnum.Warning
                },
                new()
                {
                    ConditionType = CellHighlightingRule.ConditionTypes.Between,
                    Value1 = "20",
                    Value2 = "30",
                    Status = DBADashStatus.DBADashStatusEnum.WarningLow
                },
                new()
                {
                    ConditionType = CellHighlightingRule.ConditionTypes.Between,
                    Value1 = "31",
                    Value2 = "40",
                    Status = DBADashStatus.DBADashStatusEnum.Information
                },
                new()
                {
                    ConditionType = CellHighlightingRule.ConditionTypes.Equals,
                    Value1 = "41",
                    Status = DBADashStatus.DBADashStatusEnum.OK
                }
            }
        };

        public void RefreshData()
        {
            customReportView1.RefreshData();
        }
    }
}