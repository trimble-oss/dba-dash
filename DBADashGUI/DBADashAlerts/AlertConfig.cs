using DBADash.Alert;
using DBADashGUI.CustomReports;
using DBADashGUI.DBADashAlerts.Rules;
using DBADashGUI.Performance;
using DBADashGUI.SchemaCompare;
using DBADashGUI.Theme;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using System.Windows.Forms;
using DBADash;

namespace DBADashGUI.DBADashAlerts
{
    public partial class AlertConfig : UserControl, IRefreshData, ISetContext
    {
        private DBADashContext CurrentContext;

        public AlertConfig()
        {
            InitializeComponent();
            var tsAdd = new ToolStripButton("Add Rule", Properties.Resources.NewRule_16x);
            tsAdd.Click += AddRule_Click;
            customReportView1.ToolStrip.Items.Add(tsAdd);

            var tsAddBlackout = new ToolStripButton("Add Blackout", Properties.Resources.NotificationAlertMute_16x);
            tsAddBlackout.Click += AddBlackout_Click;
            customReportView1.ToolStrip.Items.Add(tsAddBlackout);

            var tsAddChannel = new ToolStripMenuItem("Add Channel", Properties.Resources.AddUserGroup_16x);
            foreach (var typeName in Enum.GetNames<NotificationChannelBase.NotificationChannelTypes>())
            {
                var tsAddChannelType = new ToolStripMenuItem(typeName);
                tsAddChannel.DropDownItems.Add(tsAddChannelType);
                tsAddChannelType.Click += AddChannel_Click;
            }
            customReportView1.ToolStrip.Items.Add(tsAddChannel);
            customReportView1.PostGridRefresh += OnPostGridRefresh;
        }

        private void AddChannel_Click(object sender, EventArgs e)
        {
            try
            {
                var menuItem = (ToolStripMenuItem)sender;
                var type = Enum.Parse<NotificationChannelBase.NotificationChannelTypes>(menuItem.Text!);
                var channel = NotificationChannelBase.CreateChannel(type);
                channel.Schedules[0].TimeZone = DateHelper.AppTimeZone;
                EditChannel(channel);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void EditChannel(NotificationChannelBase channel)
        {
            using var edit = new PropertyGridDialog();
            edit.Text = channel.ChannelID > 0 ? "Edit Channel" : "Add Channel";
            edit.SelectedObject = channel;
            if (edit.ShowDialog() != DialogResult.OK) return;
            try
            {
                channel.Save(Common.ConnectionString);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            RefreshData();
        }

        private async void AddBlackout_Click(object sender, EventArgs e)
        {
            var blackout = new BlackoutPeriod() { StartDate = DateHelper.AppNow, EndDate = DateHelper.AppNow.AddHours(1), TimeZone = DateHelper.AppTimeZone };
            await EditBlackout(blackout);
        }

        private async Task EditBlackout(BlackoutPeriod blackout)
        {
            using var frm = new PropertyGridDialog() { Text = blackout.BlackoutPeriodID.HasValue ? "Edit Blackout" : "Add Blackout", SelectedObject = blackout };
            if (frm.ShowDialog() != DialogResult.OK) return;
            try
            {
                blackout.AdjustDatesToUtc();
                await blackout.Save();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, @"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            RefreshData();
        }

        private void AddRule_Click(object sender, EventArgs e)
        {
            using var addRule = new EditRule() { ConnectionID = CurrentContext.ConnectionID };
            addRule.ShowDialog();
            RefreshData();
        }

        protected void OnPostGridRefresh(object sender, EventArgs e)
        {
            DBADashDataGridView ruleGrid = null;
            DBADashDataGridView blackoutGrid = null;
            DBADashDataGridView channelGrid = null;
            if (customReportView1.Grids.Count == 3)
            {
                ruleGrid = customReportView1.Grids[0];
                blackoutGrid = customReportView1.Grids[1];
                channelGrid = customReportView1.Grids[2];
            }
            else if (customReportView1.Grids.Count == 1)
            {
                switch (customReportView1.Grids[0].ResultSetID)
                {
                    case 0:
                        ruleGrid = customReportView1.Grids[0];
                        break;

                    case 1:
                        blackoutGrid = customReportView1.Grids[0];
                        break;

                    case 2:
                        channelGrid = customReportView1.Grids[0];
                        break;
                }
            }

            if (ruleGrid != null && !ruleGrid.Columns.Contains("colEdit"))
            {
                ruleGrid.Columns.Add(new DataGridViewLinkColumn()
                {
                    Name = "colCopy",
                    HeaderText = @"Copy",
                    Text = "Copy",
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader,
                    UseColumnTextForLinkValue = true,
                });
                ruleGrid.Columns.Add(new DataGridViewLinkColumn()
                {
                    Name = "colEdit",
                    HeaderText = @"Edit",
                    Text = "Edit",
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader,
                    UseColumnTextForLinkValue = true,
                });
                ruleGrid.Columns.Add(new DataGridViewLinkColumn()
                {
                    Name = "colDelete",
                    HeaderText = @"Delete",
                    Text = "Delete",
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader,
                    UseColumnTextForLinkValue = true,
                });
                ruleGrid.Columns.Add(new DataGridViewLinkColumn()
                {
                    Name = "colBlackout",
                    HeaderText = @"Blackout",
                    Text = "Add Blackout",
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader,
                    UseColumnTextForLinkValue = true,
                });
                ruleGrid.CellContentClick -= RuleGrid_CellContentClick;
                ruleGrid.CellContentClick += RuleGrid_CellContentClick;
                ruleGrid.ApplyTheme();
            }
            ruleGrid?.AutoResizeColumnsWithMaxColumnWidth();
            if (blackoutGrid != null && !blackoutGrid.Columns.Contains("colDelete"))
            {
                blackoutGrid.Columns.Add(new DataGridViewLinkColumn()
                {
                    Name = "colCopy",
                    HeaderText = @"Copy",
                    Text = "Copy",
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader,
                    UseColumnTextForLinkValue = true,
                });
                blackoutGrid.Columns.Add(new DataGridViewLinkColumn()
                {
                    Name = "colEdit",
                    HeaderText = @"Edit",
                    Text = "Edit",
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader,
                    UseColumnTextForLinkValue = true,
                });
                blackoutGrid.Columns.Add(new DataGridViewLinkColumn()
                {
                    Name = "colDelete",
                    HeaderText = @"Delete",
                    Text = "Delete",
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader,
                    UseColumnTextForLinkValue = true,
                });

                blackoutGrid.CellContentClick -= BlackoutGrid_CellContentClick;
                blackoutGrid.CellContentClick += BlackoutGrid_CellContentClick;
                blackoutGrid.ApplyTheme();
            }
            blackoutGrid?.AutoResizeColumnsWithMaxColumnWidth();
            if (channelGrid != null && !channelGrid.Columns.Contains("colEdit"))
            {
                channelGrid.Columns.Add(new DataGridViewLinkColumn()
                {
                    Name = "colEnableDisable",
                    HeaderText = @"Enable/Disable",
                    Text = "Disable",
                    UseColumnTextForLinkValue = true,
                });
                channelGrid.Columns.Add(new DataGridViewLinkColumn()
                {
                    Name = "colCopy",
                    HeaderText = @"Copy",
                    Text = "Copy",
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader,
                    UseColumnTextForLinkValue = true,
                });
                channelGrid.Columns.Add(new DataGridViewLinkColumn()
                {
                    Name = "colEdit",
                    HeaderText = @"Edit",
                    Text = "Edit",
                    UseColumnTextForLinkValue = true,
                });
                channelGrid.Columns.Add(new DataGridViewLinkColumn()
                {
                    Name = "colDelete",
                    HeaderText = @"Delete",
                    Text = "Delete",
                    UseColumnTextForLinkValue = true,
                });
                channelGrid.Columns.Add(new DataGridViewLinkColumn()
                {
                    Name = "colTest",
                    HeaderText = @"Test",
                    Text = "Test",
                    UseColumnTextForLinkValue = true,
                });
                channelGrid.CellContentClick -= ChannelGrid_CellContentClick;
                channelGrid.CellContentClick += ChannelGrid_CellContentClick;
                channelGrid.CellFormatting -= ChannelGrid_CellFormatting;
                channelGrid.CellFormatting += ChannelGrid_CellFormatting;
                channelGrid.ApplyTheme();
            }
            channelGrid?.AutoResizeColumnsWithMaxColumnWidth();
        }

        private static void ChannelGrid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var grid = (DBADashDataGridView)sender;
            var isActive = (bool)grid.Rows[e.RowIndex].Cells["IsActive"].Value;
            switch (grid.Columns[e.ColumnIndex].Name)
            {
                case "colEnableDisable":
                    e.Value = isActive ? "Disable" : "Enable";
                    break;
            }
        }

        private async void ChannelGrid_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var grid = (DBADashDataGridView)sender;
            var channelID = (int)grid.Rows[e.RowIndex].Cells["NotificationChannelID"].Value;
            try
            {
                switch (grid.Columns[e.ColumnIndex].Name)
                {
                    case "colCopy":
                        {
                            var channel = NotificationChannelBase.GetChannel(channelID, Common.ConnectionString);
                            channel.ChannelID = null;
                            EditChannel(channel);

                            RefreshData();
                            break;
                        }
                    case "colEdit":

                        {
                            var channel = NotificationChannelBase.GetChannel(channelID, Common.ConnectionString);
                            EditChannel(channel);

                            RefreshData();
                            break;
                        }

                    case "colEnableDisable":

                        {
                            var isActive = (bool)grid.Rows[e.RowIndex].Cells["IsActive"].Value;
                            var channel = NotificationChannelBase.GetChannel(channelID, Common.ConnectionString);
                            if (isActive) // Disable
                            {
                                channel.DisableFrom = DateTime.UtcNow;
                                channel.DisableTo = null;
                            }
                            else // Enable
                            {
                                channel.DisableFrom = null;
                                channel.DisableTo = null;
                            }

                            channel.Save(Common.ConnectionString);
                            RefreshData();
                            break;
                        }

                    case "colDelete":
                        {
                            var channel = NotificationChannelBase.GetChannel(channelID, Common.ConnectionString);
                            if (MessageBox.Show(@$"Delete {channel.ChannelName}?", @"Delete", MessageBoxButtons.YesNo,
                                    MessageBoxIcon.Question) != DialogResult.Yes) return;

                            channel.Delete(Common.ConnectionString);
                            RefreshData();
                            break;
                        }

                    case "colTest":
                        {
                            var channel = NotificationChannelBase.GetChannel(channelID, Common.ConnectionString);
                            if (MessageBox.Show(@$"Send a test notification to {channel.ChannelName}?", @"Send",
                                    MessageBoxButtons.YesNo,
                                    MessageBoxIcon.Question) != DialogResult.Yes) return;

                            await channel.SendNotificationAsync(Alert.GetTestAlert(), string.Empty);
                            RefreshData();
                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void BlackoutGrid_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            try
            {
                var grid = (DBADashDataGridView)sender;
                var row = grid.Rows[e.RowIndex];
                var blackout = new BlackoutPeriod()
                {
                    AlertKey = (string)row.Cells["AlertKey"].Value,
                    ApplyToInstance = (string)row.Cells["ConnectionID"].Value.DBNullToNull(),
                    ApplyToInstanceID = (int)row.Cells["ApplyToInstanceID"].Value,
                    ApplyToTag = DBADashTag.GetTag(Common.ConnectionString, (int)row.Cells["ApplyToTagID"].Value),
                    StartDate = (DateTime?)row.Cells["StartDate"].Value.DBNullToNull(),
                    EndDate = (DateTime?)row.Cells["EndDate"].Value.DBNullToNull(),
                    BlackoutPeriodID = (int)row.Cells["BlackoutPeriodId"].Value,
                    TimeZone = ScheduleBase.TimeZoneFromString((string)row.Cells["TimeZone"].Value),
                    Monday = (bool)row.Cells["Monday"].Value,
                    Tuesday = (bool)row.Cells["Tuesday"].Value,
                    Wednesday = (bool)row.Cells["Wednesday"].Value,
                    Thursday = (bool)row.Cells["Thursday"].Value,
                    Friday = (bool)row.Cells["Friday"].Value,
                    Saturday = (bool)row.Cells["Saturday"].Value,
                    Sunday = (bool)row.Cells["Sunday"].Value,
                    TimeFrom = row.Cells["TimeFrom"].Value == DBNull.Value
                        ? null
                        : TimeOnly.FromTimeSpan((TimeSpan)row.Cells["TimeFrom"].Value),
                    TimeTo = row.Cells["TimeTo"].Value == DBNull.Value
                        ? null
                        : TimeOnly.FromTimeSpan((TimeSpan)row.Cells["TimeTo"].Value),
                    Notes = (string)row.Cells["Notes"].Value.DBNullToNull()
                };
                switch (grid.Columns[e.ColumnIndex].Name)
                {
                    case "colCopy":
                        blackout.BlackoutPeriodID = null;
                        await EditBlackout(blackout);
                        break;

                    case "colDelete":

                        if (MessageBox.Show("Are you sure you want to delete this?", "Delete",
                                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                        {
                            return;
                        }

                        await blackout.Delete();
                        RefreshData();

                        break;

                    case "colEdit":

                        await EditBlackout(blackout);

                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void RuleGrid_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var grid = (DBADashDataGridView)sender;
            var ruleID = (int)grid.Rows[e.RowIndex].Cells["RuleID"].Value;
            try
            {
                switch (grid.Columns[e.ColumnIndex].Name)
                {
                    case "colDelete":
                        if (MessageBox.Show("Are you sure you want to delete this rule?", "Delete Rule",
                                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                        {
                            return;
                        }

                        DeleteRule(ruleID);
                        RefreshData();

                        break;

                    case "colEdit":

                        EditRule(ruleID);
                        RefreshData();

                        break;

                    case "colCopy":
                        var rule = AlertRuleBase.GetRule(ruleID);
                        rule.CreateAsNew();
                        EditRule(rule);
                        RefreshData();

                        break;

                    case "colBlackout":
                        var blackout = AlertRuleBase.GetRule(ruleID).CreateBlackout();
                        blackout.StartDate = DateHelper.AppNow;
                        blackout.EndDate = DateHelper.AppNow.AddHours(1);
                        blackout.TimeZone = DateHelper.AppTimeZone;
                        await EditBlackout(blackout);
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void DeleteRule(int ruleID)
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("Alert.Rule_Del", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@RuleID", ruleID);
            cn.Open();
            cmd.ExecuteNonQuery();
        }

        public static void EditRule(int ruleID) => EditRule(AlertRuleBase.GetRule(ruleID));

        public static void EditRule(AlertRuleBase rule)
        {
            using var editRule = new EditRule { AlertRule = rule };
            editRule.ShowDialog();
        }

        public void RefreshData()
        {
            customReportView1.RefreshData();
        }

        public void SetContext(DBADashContext _context)
        {
            _context.Report = AlertRulesReport;
            CurrentContext = _context;
            customReportView1.SetContext(_context);
            customReportView1.RefreshData();
        }

        public static SystemReport AlertRulesReport => new()
        {
            ReportName = "Alert Rules",
            SchemaName = "Alert",
            ProcedureName = "Rules_Get",
            QualifiedProcedureName = "Alert.Config_Get",
            CanEditReport = false,
            CustomReportResults = new Dictionary<int, CustomReportResult>
            {
                {
                    0, new CustomReportResult
                    {
                        ResultName = "Rules",
                        ColumnAlias = new Dictionary<string, string>
                        {
                            { "RuleID", "Rule#" },
                            { "Type", "Type" },
                            { "AlertKey", "Alert Key" },
                            { "Priority", "Priority ID" },
                            { "PriorityDescription", "Priority" },
                            { "ApplyToTagID", "Apply To Tag ID" },
                            { "ApplyToTag", "Apply To Tag" },
                            { "ApplyToInstanceID", "Apply To Instance ID" },
                            { "ApplyToInstance", "Apply To Instance" },
                            { "ApplyToHidden","Apply To Hidden"},
                            { "Threshold", "Threshold" },
                            { "EvaluationPeriodMins", "Evaluation Period (Mins)" },
                            { "IsActive", "Is Active" },
                            { "Details", "Details" },
                            { "Notes", "Notes"}
                        },
                        ColumnLayout = new List<KeyValuePair<string, PersistedColumnLayout>>()
                        {
                            new("RuleID", new PersistedColumnLayout { Visible = true }),
                            new("Type", new PersistedColumnLayout { Visible = true }),
                            new("AlertKey", new PersistedColumnLayout { Visible = true }),
                            new("Priority", new PersistedColumnLayout { Visible = false }),
                            new("PriorityDescription", new PersistedColumnLayout { Visible = true }),
                            new("ApplyToTag", new PersistedColumnLayout { Visible = true }),
                            new("ApplyToInstanceID", new PersistedColumnLayout { Visible = false }),
                            new("ApplyToInstance", new PersistedColumnLayout { Visible = true }),
                            new("ApplyToHidden", new PersistedColumnLayout { Visible = true }),
                            new("Threshold", new PersistedColumnLayout { Visible = true }),
                            new("EvaluationPeriodMins", new PersistedColumnLayout { Visible = true }),
                            new("IsActive", new PersistedColumnLayout { Visible = true }),
                            new("Details", new PersistedColumnLayout { Visible = true }),
                            new("ApplyToTagID", new PersistedColumnLayout { Visible = false }),
                            new("Notes", new PersistedColumnLayout { Visible = true }),
                        },
                        CellFormatString = new Dictionary<string, string>()
                        {
                            {"Threshold","#,0.######"}
                        },
                        CellHighlightingRules =
                        {
                            {
                            "PriorityDescription",
                            ActiveAlerts.GetPriorityCellHighlightingRuleSet()
                            },
                        },
                        LinkColumns = new()
                        {
                            {
                                "Details",
                                new TextLinkColumnInfo()
                                {
                                    TextHandling = CodeEditor.CodeEditorModes.Json,
                                    TargetColumn = "Details"
                                }
                            },
                            {
                                "Notes",
                                new TextLinkColumnInfo()
                                {
                                    TextHandling = CodeEditor.CodeEditorModes.Markdown,
                                    TargetColumn = "Notes"
                                }
                            }
                        }
                    }
                },
                {
                    1, new CustomReportResult
                    {
                        ResultName = "Blackout Periods",
                        ColumnAlias = new Dictionary<string, string>
                        {
                            { "BlackoutPeriodID", "Blackout ID" },
                            { "ApplyToInstanceID", "Apply To Instance ID" },
                            { "ApplyToTagID", "Apply To Tag ID" },
                            { "ApplyToTag", "Apply To Tag" },
                            { "ApplyToInstance", "Apply To Instance" },
                            { "ConnectionID", "ConnectionID" },
                            { "AlertKey", "Alert Key" },
                            { "StartDate", "From" },
                            { "EndDate", "To" },
                            { "Hours", "Hours" },
                            { "Monday", "MON" },
                            { "Tuesday", "TUE" },
                            { "Wednesday", "WED" },
                            { "Thursday", "THU" },
                            { "Friday", "FRI" },
                            { "Saturday", "SAT" },
                            { "Sunday", "SUN" },
                            { "TimeZone", "Time Zone" },
                            { "TimeFrom", "Time From" },
                            { "TimeTo", "Time To" },
                            { "InEffect", "In Effect" },
                            { "CurrentStatus","Current Status"},
                            { "StartsIn","Starts In"},
                            { "EndsIn","Ends In"},
                            {"Notes","Notes"}
                        },
                        ColumnLayout = new List<KeyValuePair<string, PersistedColumnLayout>>()
                        {
                            new("BlackoutPeriodID", new PersistedColumnLayout { Visible = false }),
                            new("ApplyToInstanceID", new PersistedColumnLayout { Visible = false }),
                            new("ApplyToInstance", new PersistedColumnLayout { Visible = true }),
                            new("ConnectionID", new PersistedColumnLayout { Visible = false }),
                            new("ApplyToTag", new PersistedColumnLayout { Visible = true }),
                            new("ApplyToTagID", new PersistedColumnLayout { Visible = false }),
                            new("AlertKey", new PersistedColumnLayout { Visible = true }),
                            new("StartDate", new PersistedColumnLayout { Visible = true }),
                            new("EndDate", new PersistedColumnLayout { Visible = true }),
                            new("Hours",new PersistedColumnLayout(){Visible = true}),
                            new("Monday",new PersistedColumnLayout(){Visible = true}),
                            new("Tuesday",new PersistedColumnLayout(){Visible = true}),
                            new("Wednesday",new PersistedColumnLayout(){Visible = true}),
                            new("Thursday",new PersistedColumnLayout(){Visible = true}),
                            new("Friday",new PersistedColumnLayout(){Visible = true}),
                            new("Saturday",new PersistedColumnLayout(){Visible = true}),
                            new("Sunday",new PersistedColumnLayout(){Visible = true}),
                            new("TimeZone",new PersistedColumnLayout(){Visible = true}),
                            new("TimeFrom",new PersistedColumnLayout(){Visible = true}),
                            new("TimeTo",new PersistedColumnLayout(){Visible = true}),
                            new("InEffect", new PersistedColumnLayout { Visible = false }),
                            new("CurrentStatus", new PersistedColumnLayout { Visible = true }),
                            new("StartsIn", new PersistedColumnLayout { Visible = false }),
                            new("EndsIn", new PersistedColumnLayout { Visible = false }),
                            new("Notes", new PersistedColumnLayout { Visible = true }),
                        },
                        CellHighlightingRules =
                        {
                            {
                                "CurrentStatus",
                                new CellHighlightingRuleSet("CurrentStatus")
                                {
                                    Rules = new List<CellHighlightingRule>
                                    {
                                        new()
                                        {
                                            ConditionType = CellHighlightingRule.ConditionTypes.Like,
                                            Value1 = "Active*",
                                            Status = DBADashStatus.DBADashStatusEnum.OK
                                        },
                                        new()
                                        {
                                            ConditionType = CellHighlightingRule.ConditionTypes.Equals,
                                            Value1 = "Ended",
                                            Status = DBADashStatus.DBADashStatusEnum.NA
                                        },
                                        new()
                                        {
                                            ConditionType = CellHighlightingRule.ConditionTypes.All,
                                            Status = DBADashStatus.DBADashStatusEnum.Information
                                        },
                                    }
                                }
                            },
                            {
                                "StartDate",
                                new CellHighlightingRuleSet("InEffect")
                                {
                                    Rules = new List<CellHighlightingRule>
                                    {
                                        new()
                                        {
                                            ConditionType = CellHighlightingRule.ConditionTypes.Equals,
                                            Value1 = "true",
                                            Status = DBADashStatus.DBADashStatusEnum.OK
                                        },
                                        new()
                                        {
                                            ConditionType = CellHighlightingRule.ConditionTypes.Equals,
                                            Value1 = "false",
                                            Status = DBADashStatus.DBADashStatusEnum.NA
                                        },
                                    }
                                }
                            },
                            {
                                "EndDate",
                                new CellHighlightingRuleSet("InEffect")
                                {
                                    Rules = new List<CellHighlightingRule>
                                    {
                                        new()
                                        {
                                            ConditionType = CellHighlightingRule.ConditionTypes.Equals,
                                            Value1 = "true",
                                            Status = DBADashStatus.DBADashStatusEnum.OK
                                        },
                                        new()
                                        {
                                            ConditionType = CellHighlightingRule.ConditionTypes.Equals,
                                            Value1 = "false",
                                            Status = DBADashStatus.DBADashStatusEnum.NA
                                        },
                                    }
                                }
                            },
                        },
                        LinkColumns = new()
                        {
                            {
                                "Notes",
                                new TextLinkColumnInfo()
                                {
                                    TextHandling = CodeEditor.CodeEditorModes.Markdown,
                                    TargetColumn = "Notes"
                                }
                            }
                        }
                    }
                },
                  {
                    2, new CustomReportResult
                    {
                        ResultName = "Notification Channels",
                        ColumnAlias = new Dictionary<string, string>
                        {
                            { "NotificationChannelID", "Notification Channel ID" },
                            { "NotificationChannelTypeID", "Notification Channel Type ID" },
                            { "NotificationChannelType", "Notification Channel Type" },
                            { "ChannelName", "Name" },
                            { "DisableFrom", "Disable From" },
                            { "DisableTo", "Disable To" },
                            { "IsActive","Is Active"},
                            {"ScheduleCount","Schedules"},
                            {"Has247Schedule","24x7 Schedule"},
                            {"Tags","Tags"},
                            {"LastFailedNotification","Last Failed Notification"},
                            {"LastSucceededNotification","Last Succeeded Notification"},
                            {"FailedNotificationCount","Failed Notification Count"},
                            {"SucceededNotificationCount","Succeeded Notification Count"},
                            {"LastFailure","Last Failure"},
                            {"LastFailureStatus","Last Failure Status"},
                        },
                         ColumnLayout = new List<KeyValuePair<string, PersistedColumnLayout>>()
                            {
                                new("NotificationChannelID", new PersistedColumnLayout { Visible = false }),
                                new("NotificationChannelTypeID", new PersistedColumnLayout { Visible = false }),
                                new("NotificationChannelType", new PersistedColumnLayout { Visible = true }),
                                new("ChannelName", new PersistedColumnLayout { Visible = true }),
                                new("DisableFrom", new PersistedColumnLayout { Visible = true }),
                                new("DisableTo", new PersistedColumnLayout { Visible = true}),
                                new("IsActive", new PersistedColumnLayout { Visible = true}),
                                new("ScheduleCount", new PersistedColumnLayout { Visible = true}),
                                new("Has247Schedule", new PersistedColumnLayout { Visible = true}),
                                new("Tags", new PersistedColumnLayout { Visible = true}),
                                new("LastFailedNotification", new PersistedColumnLayout(){Visible = true}),
                                new("LastSucceededNotification", new PersistedColumnLayout(){Visible = true}),
                                new("FailedNotificationCount", new PersistedColumnLayout(){Visible = true}),
                                new("SucceededNotificationCount", new PersistedColumnLayout(){Visible = true}),
                                new("LastFailure", new PersistedColumnLayout(){Visible = true}),
                                new("LastFailureStatus", new PersistedColumnLayout(){Visible = false}),
                            },
                        CellHighlightingRules =
                        {
                            {
                                "IsActive",
                                new CellHighlightingRuleSet("IsActive")
                                {
                                    Rules = new List<CellHighlightingRule>
                                    {
                                        new()
                                        {
                                            ConditionType = CellHighlightingRule.ConditionTypes.Equals,
                                            Value1 = true.ToString(),
                                            Status = DBADashStatus.DBADashStatusEnum.OK
                                        },
                                        new()
                                        {
                                            ConditionType = CellHighlightingRule.ConditionTypes.Equals,
                                            Value1 = false.ToString(),
                                            Status = DBADashStatus.DBADashStatusEnum.NA
                                        }
                                    }
                                }
                            },
                            {
                                "ChannelName",
                                new CellHighlightingRuleSet("IsActive")
                                {
                                    Rules = new List<CellHighlightingRule>
                                    {
                                        new()
                                        {
                                            ConditionType = CellHighlightingRule.ConditionTypes.Equals,
                                            Value1 = true.ToString(),
                                            Status = DBADashStatus.DBADashStatusEnum.OK
                                        },
                                        new()
                                        {
                                            ConditionType = CellHighlightingRule.ConditionTypes.Equals,
                                            Value1 = false.ToString(),
                                            Status = DBADashStatus.DBADashStatusEnum.NA
                                        }
                                    }
                                }
                            },
                            {
                                "LastFailedNotification",
                                new CellHighlightingRuleSet("LastFailureStatus")
                                {
                                    IsStatusColumn = true
                                }
                            },
                        }
                    }
                }
            }
        };
    }
}