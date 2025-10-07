using DBADash;
using DBADashGUI.CustomReports;
using DBADashGUI.DBADashAlerts.Rules;
using DBADashGUI.SchemaCompare;
using DBADashGUI.Theme;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
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
            var tsDefaultSort = new ToolStripMenuItem("Default Sort", Properties.Resources.SortByColumn_16x) { DisplayStyle = ToolStripItemDisplayStyle.Image, ToolTipText = "Apply default sort order" };
            tsDefaultSort.Click += DefaultSort_Click;
            customReportView1.ToolStrip.Items.Add(tsDefaultSort);

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

        private void DefaultSort_Click(object sender, EventArgs e)
        {
            if (customReportView1.Grids.Count > 0)
            {
                var grid = customReportView1.Grids[0];
                var sortCol = grid.Columns["DefaultSortOrder"];
                if (sortCol == null) return;
                grid.Sort(sortCol, ListSortDirection.Ascending);
            }
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
                CommonShared.ShowExceptionDialog(ex);
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
                CommonShared.ShowExceptionDialog(ex);
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
            var configGrid = new AlertConfig() { Dock = DockStyle.Fill };
            frm.Controls.Add(configGrid);
            frm.Load += (_, _) =>
            {
                configGrid.SetContext(CurrentContext);
            };
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
                            AlertRuleBase.RuleTypes.DriveSpace => Tabs.Drives,
                            AlertRuleBase.RuleTypes.AGHealth => Tabs.AG,
                            AlertRuleBase.RuleTypes.Counter => Tabs.Metrics,
                            AlertRuleBase.RuleTypes.CollectionDates => Tabs.CollectionDates,
                            AlertRuleBase.RuleTypes.AgentJob => Tabs.Jobs,
                            AlertRuleBase.RuleTypes.SQLAgentAlert => Tabs.SQLAgentAlerts,
                            AlertRuleBase.RuleTypes.Offline => Tabs.OfflineInstances,
                            _ => Tabs.Performance
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
                CommonShared.ShowExceptionDialog(ex, "Insufficient permissions", default, TaskDialogIcon.ShieldWarningYellowBar);
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex);
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
                        Columns = new Dictionary<string, ColumnMetadata>
                        {
                            { "NotificationDate", new ColumnMetadata { Alias = "Date/Time" } },
                            { "NotificationChannelType", new ColumnMetadata { Alias = "Channel Type" } },
                            { "ChannelName", new ColumnMetadata { Alias = "Channel Name" } },
                            { "Status", new ColumnMetadata {
                                Alias = "Status",
                                Highlighting = new CellHighlightingRuleSet("Status")
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
                            } },
                            { "ErrorMessage", new ColumnMetadata { Alias = "Error Message" } },
                            { "NotificationMessage", new ColumnMetadata { Alias = "Notification Message" } }
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
                        Columns = new Dictionary<string, ColumnMetadata>
                        {
                            { "AlertID", new ColumnMetadata { Alias = "Alert ID", Visible = false } },
                            { "InstanceID", new ColumnMetadata { Alias = "Instance ID", Visible = false } },
                            { "InstanceDisplayName", new ColumnMetadata { Alias = "Instance", Link = new PlaceholderLinkInfo() } },
                            { "Priority", new ColumnMetadata { Alias = "Priority ID", Visible = false } },
                            { "PriorityDescription", new ColumnMetadata {
                                Alias = "Priority",
                                Highlighting = new CellHighlightingRuleSet("AlertStatus") { IsStatusColumn = true }
                            }},
                            { "AlertType", new ColumnMetadata { Alias = "Alert Type" } },
                            { "AlertKey", new ColumnMetadata { Alias = "Alert Key" } },
                            { "FirstMessage", new ColumnMetadata { Alias = "First Message" } },
                            { "LastMessage", new ColumnMetadata { Alias = "Last Message" } },
                            { "TriggerDate", new ColumnMetadata { Alias = "Trigger Date" } },
                            { "AlertDuration", new ColumnMetadata { Alias = "Alert Duration" } },
                            { "UpdatedDate", new ColumnMetadata { Alias = "Updated Date", Visible = false } },
                            { "TimeSinceLastUpdate", new ColumnMetadata { Alias = "Time since last update" } },
                            { "FirstNotification", new ColumnMetadata { Alias = "First Notification", Visible = false } },
                            { "LastNotification", new ColumnMetadata { Alias = "Last Notification", Visible = false } },
                            { "TimeSinceLastNotification", new ColumnMetadata { Alias = "Time since last notification" } },
                            { "UpdateCount", new ColumnMetadata { Alias = "Update Count" } },
                            { "ResolvedCount", new ColumnMetadata { Alias = "Resolved Count" } },
                            { "NotificationCount", new ColumnMetadata { Alias = "Notification Count", Link = new PlaceholderLinkInfo() } },
                            { "FailedNotificationCount", new ColumnMetadata {
                                Alias = "Failed Notification Count",
                                Link = new PlaceholderLinkInfo(),
                                Highlighting = new CellHighlightingRuleSet("FailedNotificationCount")
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
                            } },
                            { "IsAcknowledged", new ColumnMetadata { Alias = "Acknowledged?" } },
                            { "IsResolved", new ColumnMetadata { Alias = "Resolved?" } },
                            { "ResolvedDate", new ColumnMetadata { Alias = "Resolved Date" } },
                            { "IsBlackout", new ColumnMetadata { Alias = "Is Blackout" } },
                            { "Notes", new ColumnMetadata {
                                Alias = "Notes",
                                NullValue = "Add Notes...",
                                Link = new EditNotesLinkColumnInfo()
                            } },
                            { "RuleNotes", new ColumnMetadata {
                                Alias = "Rule Notes",
                                Link = new TextLinkColumnInfo()
                                {
                                    TextHandling = CodeEditor.CodeEditorModes.Markdown,
                                    TargetColumn = "RuleNotes"
                                }
                            } },
                            { "RuleID", new ColumnMetadata { Alias = "Rule#", Link = new PlaceholderLinkInfo() } },
                            { "DefaultSortOrder", new ColumnMetadata { Alias = "Default Sort Order", Visible = false } }
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
                        Columns = new Dictionary<string, ColumnMetadata>
                        {
                            { "AlertID", new ColumnMetadata { Alias = "Alert ID", Visible = false } },
                            { "InstanceID", new ColumnMetadata { Alias = "Instance ID", Visible = false } },
                            { "InstanceDisplayName", new ColumnMetadata { Alias = "Instance", Link = new PlaceholderLinkInfo() } },
                            { "Priority", new ColumnMetadata { Alias = "Priority ID", Visible = false } },
                            { "PriorityDescription", new ColumnMetadata { Alias = "Priority" } },
                            { "AlertType", new ColumnMetadata { Alias = "Alert Type" } },
                            { "AlertKey", new ColumnMetadata { Alias = "Alert Key" } },
                            { "FirstMessage", new ColumnMetadata { Alias = "First Message" } },
                            { "LastMessage", new ColumnMetadata { Alias = "Last Message" } },
                            { "TriggerDate", new ColumnMetadata { Alias = "Trigger Date" } },
                            { "AlertDuration", new ColumnMetadata { Alias = "Alert Duration" } },
                            { "UpdatedDate", new ColumnMetadata { Alias = "Updated Date", Visible = false } },
                            { "TimeSinceLastUpdate", new ColumnMetadata { Alias = "Time since last update" } },
                            { "FirstNotification", new ColumnMetadata { Alias = "First Notification", Visible = false } },
                            { "LastNotification", new ColumnMetadata { Alias = "Last Notification", Visible = false } },
                            { "TimeSinceLastNotification", new ColumnMetadata { Alias = "Time since last notification" } },
                            { "UpdateCount", new ColumnMetadata { Alias = "Update Count" } },
                            { "ResolvedCount", new ColumnMetadata { Alias = "Resolved Count" } },
                            { "NotificationCount", new ColumnMetadata { Alias = "Notification Count" } },
                            { "FailedNotificationCount", new ColumnMetadata {
                                Alias = "Failed Notification Count",
                                Highlighting = new CellHighlightingRuleSet("FailedNotificationCount")
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
                            } },
                            { "IsAcknowledged", new ColumnMetadata { Alias = "Acknowledged?" } },
                            { "IsResolved", new ColumnMetadata { Alias = "Resolved?" } },
                            { "ResolvedDate", new ColumnMetadata { Alias = "Resolved Date" } },
                            { "IsBlackout", new ColumnMetadata { Alias = "IsBlackout" } },
                            { "Notes", new ColumnMetadata {
                                Alias = "Notes",
                                NullValue = "Add Notes...",
                                Link = new EditNotesLinkColumnInfo()
                            } },
                            { "RuleNotes", new ColumnMetadata {
                                Alias = "Rule Notes",
                                Link = new TextLinkColumnInfo()
                                {
                                    TextHandling = CodeEditor.CodeEditorModes.Markdown,
                                    TargetColumn = "RuleNotes"
                                }
                            } },
                            { "RuleID", new ColumnMetadata { Alias = "Rule#" } }
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