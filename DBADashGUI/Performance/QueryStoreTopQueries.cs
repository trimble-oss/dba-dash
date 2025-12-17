using DBADash;
using DBADash.Messaging;
using DBADashGUI.CustomReports;
using DBADashGUI.Interface;
using DBADashGUI.Messaging;
using DBADashGUI.SchemaCompare;
using DBADashGUI.Theme;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using static DBADashGUI.Messaging.MessagingHelper;

namespace DBADashGUI.Performance
{
    public partial class QueryStoreTopQueries : UserControl, ISetContext, IRefreshData, ISetStatus
    {
        private DBADashContext CurrentContext;

        public QueryStoreTopQueries()
        {
            InitializeComponent();
            Reset();
            dgv.RowsAdded += (sender, args) => topQueriesResult.CellHighlightingRules.FormatRowsAdded((DataGridView)sender, args);
            dgvDrillDown.RowsAdded += (sender, args) => topQueriesResult.CellHighlightingRules.FormatRowsAdded((DataGridView)sender, args);
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public byte[] QueryHash { get; set; } = null;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public byte[] PlanHash { get; set; } = null;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public long? QueryId { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public long? PlanId { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool UseGlobalTime { get => !tsDateRange.Visible; set => tsDateRange.Visible = !value; }

        public void SetContext(DBADashContext _context)
        {
            if (_context != CurrentContext)
            {
                CurrentContext = _context;
                SetContext();
            }
        }

        private void SetContext()
        {
            if (CurrentContext == null) return;
            var objectName = QueryHash != null ? QueryHash.ToHexString(true) : QueryId.HasValue ? QueryId.ToString() : CurrentContext.Type.IsQueryStoreObjectType() ? CurrentContext.ObjectName : string.Empty;
            var plan = PlanHash != null ? PlanHash.ToHexString(true) : PlanId.HasValue ? PlanId.ToString() : string.Empty;
            dgv.DataSource = null;
            txtObjectName.Text = objectName;
            txtObjectName.Enabled = objectName == string.Empty;
            txtPlan.Text = plan;
            txtPlan.Enabled = plan == string.Empty;
            splitContainer1.Panel2Collapsed = true;
            dgvDrillDown.DataSource = null;
            includeWaitsToolStripMenuItem.Enabled = CurrentContext.ProductVersion?.Major >= 14 || CurrentContext.AzureInstanceIDs.Count > 0;
            lblStatus.Text = string.Empty;
            tsExecute.Text = string.IsNullOrEmpty(CurrentContext.DatabaseName) ? "Execute (ALL Databases)" : "Execute";
        }

        private int top = 25;
        private int minimumPlanCount = 1;

        private int GetMinimumPlanCount() => minimumPlanCountToolStripMenuItem.Enabled ? minimumPlanCount : 1;

        private string sortColumn = "total_cpu_time_ms";
        private QueryStoreTopQueriesMessage.QueryStoreGroupByEnum groupBy = QueryStoreTopQueriesMessage.QueryStoreGroupByEnum.query_id;
        private bool IncludeWaits => includeWaitsToolStripMenuItem.Checked && includeWaitsToolStripMenuItem.Enabled;
        private const string messageSentMessage = "Message sent...";

        private DateTimeOffset FromOffset => UseGlobalTime
            ? new DateTimeOffset(DateRange.FromUTC, TimeSpan.Zero)
            : new DateTimeOffset(tsDateRange.DateFromUtc, TimeSpan.Zero);

        private DateTimeOffset ToOffset => UseGlobalTime
            ? new DateTimeOffset(DateRange.ToUTC, TimeSpan.Zero)
            : new DateTimeOffset(tsDateRange.DateToUtc, TimeSpan.Zero);

        public async void RefreshData()
        {
            if (lblStatus.Text == messageSentMessage)
            {
                MessageBox.Show(@"Please wait for the current operation to complete", "Busy", MessageBoxButtons.OK,
                    MessageBoxIcon.Asterisk);
                return;
            }
            splitContainer1.Panel2Collapsed = true;
            var objectName = string.Empty;
            int? queryId = null;
            byte[] queryHash = null;
            byte[] planHash = null;
            int? planId = null;

            if (QueryHash != null || PlanHash != null)
            {
                // Don't parse
            }
            else if (txtObjectName.Text.IsHex())
            {
                queryHash = txtObjectName.Text.HexStringToByteArray();
            }
            else if (!int.TryParse(txtObjectName.Text, out var queryIdOut))
            {
                objectName = txtObjectName.Text;
            }
            else
            {
                queryId = queryIdOut;
            }
            if (txtPlan.Text.IsHex())
            {
                planHash = txtPlan.Text.HexStringToByteArray();
            }
            else if (int.TryParse(txtPlan.Text, out var planIdOut))
            {
                planId = planIdOut;
            }

            dgv.DataSource = null;

            try
            {
                var message = new QueryStoreTopQueriesMessage
                {
                    CollectAgent = CurrentContext.CollectAgent,
                    ImportAgent = CurrentContext.ImportAgent,
                    Top = top,
                    SortColumn = sortColumn,
                    ObjectName = objectName,
                    QueryID = queryId,
                    PlanID = planId,
                    NearestInterval = tsNearestInterval.Checked,
                    QueryHash = QueryHash ?? queryHash,
                    QueryPlanHash = PlanHash ?? planHash,
                    GroupBy = groupBy,
                    ConnectionID = CurrentContext.ConnectionID,
                    DatabaseName = CurrentContext.DatabaseName,
                    ParallelPlans = parallelPlansOnlyToolStripMenuItem.Checked,
                    MinimumPlanCount = GetMinimumPlanCount(),
                    From = FromOffset,
                    To = ToOffset,
                    IncludeWaits = IncludeWaits,
                    Lifetime = Config.DefaultCommandTimeout
                };
                lblStatus.InvokeSetStatus(messageSentMessage, string.Empty, DashColors.Information);
                await MessagingHelper.SendMessageAndProcessReply(message, CurrentContext, lblStatus,
                    ProcessCompletedTopQueriesMessage, Guid.NewGuid());
            }
            catch (Exception ex)
            {
                lblStatus.InvokeSetStatus(ex.Message.Truncate(200), ex.ToString(), DashColors.Fail);
            }
        }

        private async Task ProcessCompletedDrillDownTask(ResponseMessage reply, Guid messageGroup, SetStatusDelegate setStatus)
        {
            await ProcessCompletedTopQueriesOrDrillDownMessage(reply, dgvDrillDown, null);
        }

        private async Task ProcessCompletedTopQueriesMessage(ResponseMessage reply, Guid messageGroup, SetStatusDelegate setStatus)
        {
            await ProcessCompletedTopQueriesOrDrillDownMessage(reply, dgv, UserColumnLayout);
            if ((QueryHash != null || PlanHash != null) && dgv.Rows.Count == 1 && !string.IsNullOrEmpty(CurrentContext.DatabaseName))
            {
                var queryId = (long)dgv.Rows[0].Cells["query_id"].Value;
                QueryDrillDown(queryId, CurrentContext.DatabaseName);
            }
        }

        private Task ProcessCompletedTopQueriesOrDrillDownMessage(ResponseMessage reply, DataGridView _dgv, List<KeyValuePair<string, PersistedColumnLayout>> layout)
        {
            lblStatus.InvokeSetStatus(reply.Message, reply.Exception?.ToString(), reply.Type switch
            {
                ResponseMessage.ResponseTypes.Success => DashColors.Success,
                ResponseMessage.ResponseTypes.Progress => DashColors.Information,
                _ => DashColors.Fail
            });
            if (reply.Type != ResponseMessage.ResponseTypes.Success) return Task.CompletedTask;
            Invoke(() =>
            {
                var ds = reply.Data;
                if (ds == null || ds.Tables.Count == 0 || ds.Tables[0].Columns.Count == 0)
                {
                    MessageBox.Show(@"No data returned");
                    return;
                }

                var dt = ds.Tables[0];
                DateHelper.ConvertUTCToAppTimeZone(ref dt);

                _dgv.Columns.Clear();
                _dgv.AddColumns(dt, topQueriesResult);
                try
                {
                    _dgv.LoadColumnLayout(layout ?? topQueriesResult.ColumnLayout);
                }
                catch (Exception ex)
                {
                    lblStatus.InvokeSetStatus("Error loading column layout", ex.Message, DashColors.Fail);
                }
                if (_dgv.Columns.Contains("plan_id") && _dgv == dgvDrillDown)
                {
                    _dgv.Columns.Add(new DataGridViewButtonColumn()
                    { Name = "ForceUnforce", Text = "Force Plan", HeaderText = "", Visible = true });
                }

                _dgv.ApplyTheme();
                _dgv.DataSource = new DataView(dt, null, $"{sortColumn} DESC",
                    DataViewRowState.CurrentRows);
                if (_dgv == dgvDrillDown)
                {
                    splitContainer1.Panel2Collapsed = false;
                }
            });
            return Task.CompletedTask;
        }

        private readonly CustomReportResult topQueriesResult = new()
        {
            ResultName = "Top Queries",
            Columns = new Dictionary<string, ColumnMetadata>
            {
                { "DB", new ColumnMetadata { Alias = "DB", Width = 170, Visible = true } },
                { "query_id", new ColumnMetadata
                    {
                        Alias = "Query ID",
                        Width = 80,
                        Visible = true,
                        Link = new DrillDownLinkColumnInfo()
                    }
                },
                { "query_parameterization_type_desc", new ColumnMetadata { Alias = "Parameterization type", Width = 100, Visible = true } },
                { "object_id", new ColumnMetadata { Alias = "Object ID", Width = 100, Visible = false } },
                { "plan_id", new ColumnMetadata
                    {
                        Alias = "Plan ID",
                        Width = 70,
                        Visible = true,
                        Link = new PlanIdLinkColumnInfo
                        {
                            DatabaseNameColumn = "DB",
                            PlanIdColumn = "plan_id"
                        }
                    }
                },
                { "query_hash", new ColumnMetadata
                    {
                        Alias = "Query Hash",
                        Width = 160,
                        Visible = true,
                        Link = new QueryStoreLinkColumnInfo()
                        {
                            TargetColumn = "query_hash",
                            DatabaseNameColumn = "DB",
                            TargetColumnLinkType = QueryStoreLinkColumnInfo.QueryStoreLinkColumnType.QueryHash
                        }
                    }
                },
                { "query_plan_hash", new ColumnMetadata
                    {
                        Alias = "Plan Hash",
                        Width = 160,
                        Visible = true,
                        Link = new QueryStoreLinkColumnInfo()
                        {
                            TargetColumn = "query_plan_hash",
                            DatabaseNameColumn = "DB",
                            TargetColumnLinkType = QueryStoreLinkColumnInfo.QueryStoreLinkColumnType.PlanHash
                        }
                    }
                },
                { "object_name", new ColumnMetadata
                    {
                        Alias = "Object Name",
                        Width = 180,
                        Visible = true,
                        Link = new QueryStoreLinkColumnInfo()
                        {
                            TargetColumn = "object_name",
                            DatabaseNameColumn = "DB",
                            TargetColumnLinkType = QueryStoreLinkColumnInfo.QueryStoreLinkColumnType.ObjectName
                        }
                    }
                },
                { "query_sql_text", new ColumnMetadata
                    {
                        Alias = "Text",
                        Width = 250,
                        Visible = true,
                        Link = new TextLinkColumnInfo()
                        {
                            TargetColumn = "query_sql_text",
                            TextHandling = CodeEditor.CodeEditorModes.SQL
                        }
                    }
                },
                { "total_cpu_time_ms", new ColumnMetadata { Alias = "Total CPU (ms)", FormatString = "N0", Width = 70, Visible = true } },
                { "avg_cpu_time_ms", new ColumnMetadata { Alias = "Avg CPU (ms)", FormatString = "N1", Width = 70, Visible = true } },
                { "max_cpu_time_ms", new ColumnMetadata { Alias = "Max CPU (ms)", FormatString = "N1", Width = 70, Visible = true } },
                { "total_duration_ms", new ColumnMetadata { Alias = "Total Duration (ms)", FormatString = "N0", Width = 70, Visible = true } },
                { "avg_duration_ms", new ColumnMetadata { Alias = "Avg Duration (ms)", FormatString = "N1", Width = 70, Visible = true } },
                { "max_duration_ms", new ColumnMetadata { Alias = "Max Duration (ms)", FormatString = "N1", Width = 70, Visible = true } },
                { "count_executions", new ColumnMetadata { Alias = "Execs", FormatString = "N0", Width = 70, Visible = true } },
                { "abort_count", new ColumnMetadata
                    {
                        Alias = "Abort",
                        FormatString = "N0",
                        Width = 60,
                        Visible = true,
                        Highlighting = new CellHighlightingRuleSet("abort_count")
                        {
                            Rules = new List<CellHighlightingRule>
                            {
                                new() { ConditionType = CellHighlightingRule.ConditionTypes.Equals, Value1 = "0", Status = DBADashStatus.DBADashStatusEnum.OK },
                                new() { ConditionType = CellHighlightingRule.ConditionTypes.GreaterThan, Value1 = "0", Status = DBADashStatus.DBADashStatusEnum.Warning }
                            }
                        }
                    }
                },
                { "exception_count", new ColumnMetadata
                    {
                        Alias = "Fail",
                        FormatString = "N0",
                        Width = 60,
                        Visible = true,
                        Highlighting = new CellHighlightingRuleSet("exception_count")
                        {
                            Rules = new List<CellHighlightingRule>
                            {
                                new() { ConditionType = CellHighlightingRule.ConditionTypes.Equals, Value1 = "0", Status = DBADashStatus.DBADashStatusEnum.OK },
                                new() { ConditionType = CellHighlightingRule.ConditionTypes.GreaterThan, Value1 = "0", Status = DBADashStatus.DBADashStatusEnum.Warning }
                            }
                        }
                    }
                },
                { "executions_per_min", new ColumnMetadata { Alias = "Execs/min", FormatString = "N2", Width = 70, Visible = true } },
                { "avg_memory_grant_kb", new ColumnMetadata { Alias = "Avg Memory Grant KB", FormatString = "N0", Width = 70, Visible = true } },
                { "max_memory_grant_kb", new ColumnMetadata { Alias = "Max Memory Grant KB", FormatString = "N0", Width = 70, Visible = true } },
                { "total_physical_io_reads_kb", new ColumnMetadata { Alias = "Total Physical Reads KB", FormatString = "N0", Width = 70, Visible = true } },
                { "avg_physical_io_reads_kb", new ColumnMetadata { Alias = "Avg Physical Reads KB", FormatString = "N0", Width = 70, Visible = true } },
                { "avg_rowcount", new ColumnMetadata { Alias = "Avg Rows", FormatString = "N0", Width = 70, Visible = true } },
                { "max_rowcount", new ColumnMetadata { Alias = "Max Rows", FormatString = "N0", Width = 70, Visible = true } },
                { "max_dop", new ColumnMetadata { Alias = "Max DOP", Width = 60, Visible = true } },
                { "avg_tempdb_space_used_kb", new ColumnMetadata { Alias = "Avg TempDB KB", FormatString = "N0", Width = 80, Visible = true } },
                { "max_tempdb_space_used_kb", new ColumnMetadata { Alias = "Max TempDB KB", FormatString = "N0", Width = 80, Visible = true } },
                { "num_plans", new ColumnMetadata { Alias = "Plan Count", FormatString = "N0", Width = 60, Visible = true } },
                { "num_queries", new ColumnMetadata { Alias = "Query Count", FormatString = "N0", Width = 60, Visible = true } },
                { "top_waits", new ColumnMetadata { Alias = "Top Waits", Width = 200, Visible = true } },
                { "plan_forcing_type_desc", new ColumnMetadata
                    {
                        Alias = "Plan Forcing",
                        Width = 100,
                        Visible = true,
                        Link = new DrillDownLinkColumnInfo(),
                        Highlighting = new CellHighlightingRuleSet("plan_forcing_type_desc")
                        {
                            Rules = new List<CellHighlightingRule>
                            {
                                new() { ConditionType = CellHighlightingRule.ConditionTypes.Equals, Value1 = "NONE", Status = DBADashStatus.DBADashStatusEnum.NA },
                                new() { ConditionType = CellHighlightingRule.ConditionTypes.Equals, Value1 = "MANUAL", Status = DBADashStatus.DBADashStatusEnum.Information },
                                new() { ConditionType = CellHighlightingRule.ConditionTypes.Equals, Value1 = "AUTO", Status = DBADashStatus.DBADashStatusEnum.Information }
                            }
                        }
                    }
                },
                { "force_failure_count", new ColumnMetadata
                    {
                        Alias = "Force Failure count",
                        Width = 70,
                        Visible = true,
                        Highlighting = new CellHighlightingRuleSet("force_failure_count")
                        {
                            Rules = new List<CellHighlightingRule>
                            {
                                new() { ConditionType = CellHighlightingRule.ConditionTypes.Equals, Value1 = "0", Status = DBADashStatus.DBADashStatusEnum.OK },
                                new() { ConditionType = CellHighlightingRule.ConditionTypes.All, Status = DBADashStatus.DBADashStatusEnum.Warning }
                            }
                        }
                    }
                },
                { "last_force_failure_reason_desc", new ColumnMetadata
                    {
                        Alias = "Last Forced Failure",
                        Width = 100,
                        Visible = true,
                        Highlighting = new CellHighlightingRuleSet("last_force_failure_reason_desc")
                        {
                            Rules = new List<CellHighlightingRule>
                            {
                                new() { ConditionType = CellHighlightingRule.ConditionTypes.Equals, Value1 = "NONE", Status = DBADashStatus.DBADashStatusEnum.OK },
                                new() { ConditionType = CellHighlightingRule.ConditionTypes.All, Status = DBADashStatus.DBADashStatusEnum.Warning }
                            }
                        }
                    }
                },
                { "is_parallel_plan", new ColumnMetadata { Alias = "Parallel", Width = 70, Visible = true } },
                { "interval_start", new ColumnMetadata { Alias = "Interval Start", Width = 150, Visible = false } },
                { "interval_end", new ColumnMetadata { Alias = "Interval End", Width = 150, Visible = false } },
                { "has_regressed_plan", new ColumnMetadata { Alias = "Has Regressed Plan", Width = 70, Visible = true, Description = "Indicates if there is a regressed plan from sys.dm_db_tuning_recommendations." }  },
                { "has_recommended_plan", new ColumnMetadata { Alias = "Has Recommended Plan", Width = 70, Visible = true, Description = "Indicates if there is a recommended plan from sys.dm_db_tuning_recommendations." } },
                { "is_regressed_plan", new ColumnMetadata { Alias = "Is Regressed Plan", Width = 70, Visible = true, Description = "Indicates if the plan is marked as regressed from sys.dm_db_tuning_recommendations." }  },
                { "is_recommended_plan", new ColumnMetadata { Alias = "Is Recommended Plan", Width = 70, Visible = true, Description = "Indicates if the plan is the recommended plan from sys.dm_db_tuning_recommendations." } },
            }
        };

        private void TsRefresh(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void QueryDrillDown(DataGridView _dgv, DataGridViewCellEventArgs e)
        {
            var queryId = (long)_dgv.Rows[e.RowIndex].Cells["query_id"].Value;
            var db = _dgv.Rows[e.RowIndex].Cells["DB"].Value.ToString();
            QueryDrillDown(queryId, db);
        }

        private long DrillDownQueryId;
        private string DrillDownDB;
        private bool IsChartLoaded;
        private bool IsDrillDownLoaded;

        private void QueryDrillDown(long queryId, string db)
        {
            IsChartLoaded = false;
            IsDrillDownLoaded = false;
            DrillDownQueryId = queryId;
            DrillDownDB = db;
            QueryDrillDown();
        }

        private void QueryDrillDown()
        {
            if (tabDrillDown.SelectedTab == tabChart && !IsChartLoaded)
            {
                Task.Run(() => queryStorePlanChart1.ShowChart(CurrentContext, DrillDownDB, DrillDownQueryId, tsNearestInterval.Checked, FromOffset, ToOffset));
                splitContainer1.Panel2Collapsed = false;
                IsChartLoaded = true;
            }
            else if (tabDrillDown.SelectedTab == tabSummary && !IsDrillDownLoaded)
            {
                IsDrillDownLoaded = true;
                var message = new QueryStoreTopQueriesMessage
                {
                    CollectAgent = CurrentContext.CollectAgent,
                    ImportAgent = CurrentContext.ImportAgent,
                    Top = top,
                    SortColumn = sortColumn,
                    QueryID = DrillDownQueryId,
                    NearestInterval = tsNearestInterval.Checked,
                    GroupBy = QueryStoreTopQueriesMessage.QueryStoreGroupByEnum.plan_id,
                    ConnectionID = CurrentContext.ConnectionID,
                    DatabaseName = DrillDownDB,
                    From = FromOffset,
                    To = ToOffset,
                    IncludeWaits = IncludeWaits,
                    Lifetime = Config.DefaultCommandTimeout
                };
                Task.Run(() => MessagingHelper.SendMessageAndProcessReply(message, CurrentContext, lblStatus,
                    ProcessCompletedDrillDownTask, Guid.NewGuid()));
            }
        }

        private void DefaultDrillDown(DataGridView _dgv, DataGridViewCellEventArgs e)
        {
            var colName = _dgv.Columns[e.ColumnIndex].DataPropertyName;
            LinkColumnInfo linkColumnInfo = null;
            topQueriesResult.LinkColumns?.TryGetValue(colName, out linkColumnInfo);
            try
            {
                linkColumnInfo?.Navigate(CurrentContext, _dgv.Rows[e.RowIndex], 0, this);
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex, "Error navigating to link");
            }
        }

        private async void Dgv_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            var _dgv = (DataGridView)sender;
            if (e.RowIndex < 0) return;
            switch (_dgv.Columns[e.ColumnIndex].DataPropertyName)
            {
                case "query_id":
                    QueryDrillDown(_dgv, e);
                    break;

                case "plan_forcing_type_desc":
                    PlanForcingDrillDown(_dgv, e);
                    break;

                case "" when _dgv.Columns[e.ColumnIndex].Name == "ForceUnforce":
                    await MessagingHelper.ForcePlanDrillDown(_dgv, e, CurrentContext, lblStatus, ProcessPlanForcingMessage);
                    break;

                default:
                    DefaultDrillDown(_dgv, e);
                    break;
            }
        }

        private async Task ProcessPlanForcingMessage(ResponseMessage reply, Guid messageGroup, SetStatusDelegate setStatus)
        {
            if (reply.Type == ResponseMessage.ResponseTypes.Success)
            {
                lblStatus.InvokeSetStatus("Plan Forcing Operation Completed", string.Empty, DashColors.Success);
                await MessagingHelper.UpdatePlanForcingLog(messageGroup, "SUCCEEDED");
                if (dgvDrillDown.Rows.Count > 0) // Refresh
                {
                    QueryDrillDown((long)dgvDrillDown.Rows[0].Cells["query_id"].Value, dgvDrillDown.Rows[0].Cells["DB"].Value.ToString());
                }
            }
            else
            {
                lblStatus.InvokeSetStatus("Plan Forcing Operation Failed", reply.Message, DashColors.Fail);
                await MessagingHelper.UpdatePlanForcingLog(messageGroup, "FAIL:" + reply.Message);
            }
        }

        private void PlanForcingDrillDown(DataGridView _dgv, DataGridViewCellEventArgs e)
        {
            var planId = (long)_dgv.Rows[e.RowIndex].Cells["plan_id"].Value;
            var queryId = (long)_dgv.Rows[e.RowIndex].Cells["query_id"].Value;
            var db = _dgv.Rows[e.RowIndex].Cells["DB"].Value.ToString();
            var planForcingSQL = $"/* Plan forcing script for {CurrentContext.InstanceName} */\n\n" +
                                    $"USE [{db?.Replace("]", "]]")}]\n\n" +
                                    $"/*Force Plan */\n" +
                                    $"EXEC sp_query_store_force_plan @query_id={queryId}, @plan_id={planId}\n\n" +
                                    $"/* Unforce Plan */\n" +
                                    $"EXEC sp_query_store_unforce_plan @query_id={queryId}, @plan_id={planId}";
            Common.ShowCodeViewer(planForcingSQL, "Plan Force/Unforce SQL");
        }

        private void Top_Select(object sender, EventArgs e)
        {
            var menuItem = (ToolStripMenuItem)sender;
            var topString = menuItem.Tag?.ToString();
            if (string.IsNullOrEmpty(topString))
            {
                CommonShared.ShowInputDialog(ref topString, "Enter top value");
            }

            if (!int.TryParse(topString, out top)) return;
            SetTop(top);
        }

        private void SetTop(int _top)
        {
            top = _top;
            foreach (var item in tsTop.DropDownItems.OfType<ToolStripMenuItem>())
            {
                item.Checked = false;
            }
            tsTop.Text = $@"Top {top}";
            var menuItem = tsTop.DropDownItems.OfType<ToolStripMenuItem>().FirstOrDefault(x => x.Tag != null && x.Tag.ToString() == top.ToString());
            if (menuItem == null) return;
            menuItem.Checked = true;
        }

        private void Sort_Select(object sender, EventArgs e)
        {
            var menuItem = (ToolStripMenuItem)sender;
            SetSort(menuItem.Tag?.ToString());
        }

        private void SetSort(string sortCol)
        {
            sortColumn = sortCol;
            foreach (var item in tsSort.DropDownItems.OfType<ToolStripMenuItem>())
            {
                item.Checked = false;
            }
            var menuItem = tsSort.DropDownItems.OfType<ToolStripMenuItem>().FirstOrDefault(x => x.Tag != null && x.Tag.ToString() == sortCol);
            if (menuItem == null) return;
            menuItem.Checked = true;
            tsSort.Text = $@"Sort by {menuItem.Text}";
        }

        private void TsExcel_Click(object sender, EventArgs e)
        {
            dgv.ExportToExcel();
        }

        private void TsCopy_Click(object sender, EventArgs e)
        {
            dgv.CopyGrid();
        }

        private void Select_GroupBy(object sender, EventArgs e)
        {
            var menuItem = (ToolStripMenuItem)sender;
            groupBy = Enum.Parse<QueryStoreTopQueriesMessage.QueryStoreGroupByEnum>(menuItem.Tag?.ToString() ?? "query_id", true);
            SetGroupBy(groupBy);
        }

        private void SetGroupBy(QueryStoreTopQueriesMessage.QueryStoreGroupByEnum groupBy)
        {
            this.groupBy = groupBy;
            foreach (var item in tsGroupBy.DropDownItems.OfType<ToolStripMenuItem>())
            {
                item.Checked = false;
            }
            var menuItem = tsGroupBy.DropDownItems.OfType<ToolStripMenuItem>().FirstOrDefault(x => x.Tag != null && x.Tag.ToString() == groupBy.ToString());
            if (menuItem == null) return;
            menuItem.Checked = true;
            tsGroupBy.Text = $@"Group by {menuItem.Text}";
            minimumPlanCountToolStripMenuItem.Enabled = groupBy == QueryStoreTopQueriesMessage.QueryStoreGroupByEnum.query_id;
            UserColumnLayout = null;
        }

        private void Dgv_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.Value is not byte[] bytes) return;
            e.Value = "0x" + BitConverter.ToString(bytes).Replace("-", string.Empty);
            e.FormattingApplied = true; // Indicate that formatting was applied
        }

        private void TsReset_Click(object sender, EventArgs e)
        {
            Reset();
            RefreshData();
        }

        private void Reset()
        {
            SetGroupBy(QueryStoreTopQueriesMessage.QueryStoreGroupByEnum.query_id);
            SetSort("total_cpu_time_ms");
            SetTop(25);
            SetContext();
            tsNearestInterval.Checked = true;
            parallelPlansOnlyToolStripMenuItem.Checked = false;
            SetMinimumPlanCount(1);
            UserColumnLayout = null;
        }

        private void ParallelPlans_CheckChanged(object sender, EventArgs e)
        {
            SetFilterBold();
        }

        private void SetFilterBold()
        {
            tsFilter.Font = IsFilterCustomized() ? new System.Drawing.Font(tsFilter.Font, System.Drawing.FontStyle.Bold) : new System.Drawing.Font(tsFilter.Font, System.Drawing.FontStyle.Regular);
            parallelPlansOnlyToolStripMenuItem.Font = parallelPlansOnlyToolStripMenuItem.Checked ? new System.Drawing.Font(tsFilter.Font, System.Drawing.FontStyle.Bold) : new System.Drawing.Font(tsFilter.Font, System.Drawing.FontStyle.Regular);
            minimumPlanCountToolStripMenuItem.Font = minimumPlanCount > 1 ? new System.Drawing.Font(tsFilter.Font, System.Drawing.FontStyle.Bold) : new System.Drawing.Font(tsFilter.Font, System.Drawing.FontStyle.Regular);
        }

        private bool IsFilterCustomized()
        {
            return parallelPlansOnlyToolStripMenuItem.Checked || minimumPlanCount > 1;
        }

        private void MinimumPlanCountToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var countString = minimumPlanCount.ToString();
            if (CommonShared.ShowInputDialog(ref countString, "Enter minimum plan count:") == DialogResult.OK)
            {
                if (!int.TryParse(countString, out var count)) return;
                SetMinimumPlanCount(count);
            }
        }

        private void SetMinimumPlanCount(int count)
        {
            minimumPlanCountToolStripMenuItem.Text = @"Minimum Plan Count: " + count;
            minimumPlanCount = count;
            SetFilterBold();
        }

        private List<KeyValuePair<string, PersistedColumnLayout>> UserColumnLayout;

        private void TsColumns_Click(object sender, EventArgs e)
        {
            dgv.PromptColumnSelection();
            UserColumnLayout = dgv.GetColumnLayout();
        }

        private void IncludeWaitsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UserColumnLayout = null;
        }

        private void DgvDrillDown_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            if (!dgvDrillDown.Columns.Contains("ForceUnForce")) return;
            for (var idx = e.RowIndex; idx < e.RowIndex + e.RowCount; idx += 1)
            {
                dgvDrillDown.Rows[idx].Cells["ForceUnForce"].Value = (string)dgvDrillDown.Rows[idx].Cells["plan_forcing_type_desc"].Value == "NONE" ? "Force" : "Unforce";
            }
        }

        private void DrillDownTabIndexChanged(object sender, EventArgs e)
        {
            QueryDrillDown();
        }

        private void RefreshOn_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                RefreshData();
            }
        }

        private void QueryStoreTopQueries_Load(object sender, EventArgs e)
        {
            if (UseGlobalTime) return;
            if (DateRange.SelectedTimeSpan.HasValue)
            {
                tsDateRange.SetTimeSpan(DateRange.SelectedTimeSpan.Value);
            }
            else
            {
                tsDateRange.SetDateRangeUtc(DateRange.FromUTC, DateRange.ToUTC);
            }
        }

        private void DateRangeChanged(object sender, EventArgs e)
        {
            RefreshData();
        }

        public void SetStatus(string message, string tooltip, Color color)
        {
            lblStatus.InvokeSetStatus(message, tooltip, color);
        }
    }
}