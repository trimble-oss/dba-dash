using DBADash.Messaging;
using DBADashGUI.CustomReports;
using DBADashGUI.SchemaCompare;
using DBADashGUI.Theme;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Azure;
using DBADashGUI.Messaging;
using Microsoft.Data.Tools.Schema.Sql.SchemaModel.Parameterization;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace DBADashGUI.Performance
{
    public partial class QueryStoreTopQueries : UserControl, ISetContext, IRefreshData
    {
        private DBADashContext CurrentContext;

        public QueryStoreTopQueries()
        {
            InitializeComponent();
            Reset();
            dgv.RowsAdded += (sender, args) => topQueriesResult.CellHighlightingRules.FormatRowsAdded((DataGridView)sender, args);
            dgvDrillDown.RowsAdded += (sender, args) => topQueriesResult.CellHighlightingRules.FormatRowsAdded((DataGridView)sender, args);
        }

        public void SetContext(DBADashContext _context)
        {
            if (_context != CurrentContext)
            {
                dgv.DataSource = null;
                txtObjectName.Text = string.Empty;
                txtPlan.Text = string.Empty;
                splitContainer1.Panel2Collapsed = true;
                dgvDrillDown.DataSource = null;
                includeWaitsToolStripMenuItem.Enabled = _context.ProductVersion?.Major >= 14 || _context.AzureInstanceIDs.Count > 0;
                lblStatus.Text = string.Empty;
            }
            tsExecute.Text = string.IsNullOrEmpty(_context.DatabaseName) ? "Execute (ALL Databases)" : "Execute";
            CurrentContext = _context;
        }

        private int top = 25;
        private int minimumPlanCount = 1;

        private int GetMinimumPlanCount() => minimumPlanCountToolStripMenuItem.Enabled ? minimumPlanCount : 1;

        private string sortColumn = "total_cpu_time_ms";
        private QueryStoreTopQueriesMessage.QueryStoreGroupByEnum groupBy = QueryStoreTopQueriesMessage.QueryStoreGroupByEnum.query_id;
        private bool IncludeWaits => includeWaitsToolStripMenuItem.Checked && includeWaitsToolStripMenuItem.Enabled;
        private const string messageSentMessage = "Message sent...";

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
            if (txtObjectName.Text.IsHex())
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
                    QueryHash = queryHash,
                    QueryPlanHash = planHash,
                    GroupBy = groupBy,
                    ConnectionID = CurrentContext.ConnectionID,
                    DatabaseName = CurrentContext.DatabaseName,
                    ParallelPlans = parallelPlansOnlyToolStripMenuItem.Checked,
                    MinimumPlanCount = GetMinimumPlanCount(),
                    From = new DateTimeOffset(DateRange.FromUTC, TimeSpan.Zero),
                    To = new DateTimeOffset(DateRange.ToUTC, TimeSpan.Zero),
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

        private Task ProcessCompletedPlanCollectionMessage(ResponseMessage reply, Guid messageGroup)
        {
            var ds = reply.Data;
            if (ds == null || ds.Tables.Count == 0 || ds.Tables[0].Columns.Count == 0)
            {
                MessageBox.Show(@"No data returned");
                return Task.CompletedTask;
            }

            var dt = ds.Tables[0];
            LoadQueryPlan(dt);
            return Task.CompletedTask;
        }

        private Task ProcessCompletedDrillDownTask(ResponseMessage reply, Guid messageGroup)
        {
            return ProcessCompletedTopQueriesOrDrillDownMessage(reply, dgvDrillDown, null);
        }

        private Task ProcessCompletedTopQueriesMessage(ResponseMessage reply, Guid messageGroup)
        {
            return ProcessCompletedTopQueriesOrDrillDownMessage(reply, dgv, UserColumnLayout);
        }

        private Task ProcessCompletedTopQueriesOrDrillDownMessage(ResponseMessage reply, DataGridView _dgv, List<KeyValuePair<string, PersistedColumnLayout>> layout)
        {
            lblStatus.InvokeSetStatus(reply.Message, string.Empty, DashColors.Success);
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

        private void LoadQueryPlan(DataTable dt)
        {
            if (dt.Rows.Count == 0)
            {
                lblStatus.InvokeSetStatus("No query plan", string.Empty, DashColors.Fail);
                return;
            }
            var plan = dt.Rows[0]["query_plan"].ToString();
            string fileName = null;
            if (dt.Columns.Contains("plan_id"))
            {
                fileName = $"Plan_{dt.Rows[0]["plan_id"]}_{DateTime.Now:yyyyMMddHHmmss}.sqlplan";
            }
            lblStatus.InvokeSetStatus("Loading Query Plan...", string.Empty, DashColors.Success);
            Common.ShowQueryPlan(plan, fileName);
            lblStatus.InvokeSetStatus("Query plan loaded in associated app", string.Empty, DashColors.Success);
        }

        private readonly CustomReportResult topQueriesResult = new()
        {
            ColumnAlias = new Dictionary<string, string>
            {
                { "DB", "DB" },
                { "query_id", "Query ID" },
                { "query_parameterization_type_desc","Parameterization type"},
                { "object_id", "Object ID" },
                { "plan_id", "Plan ID" },
                { "query_hash", "Query Hash" },
                { "query_plan_hash", "Plan Hash" },
                { "object_name", "Object Name" },
                { "query_sql_text", "Text" },
                { "total_cpu_time_ms", "Total CPU (ms)" },
                { "avg_cpu_time_ms", "Avg CPU (ms)" },
                { "max_cpu_time_ms", "Max CPU (ms)" },
                { "total_duration_ms", "Total Duration (ms)" },
                { "avg_duration_ms", "Avg Duration (ms)" },
                { "max_duration_ms", "Max Duration (ms)" },
                { "count_executions", "Execs" },
                { "abort_count", "Abort" },
                { "exception_count", "Fail" },
                { "executions_per_min", "Execs/min" },
                { "avg_memory_grant_kb", "Avg Memory Grant KB" },
                { "max_memory_grant_kb", "Max Memory Grant KB" },
                { "total_physical_io_reads_kb", "Total Physical Reads KB" },
                { "avg_physical_io_reads_kb", "Avg Physical Reads KB" },
                { "avg_rowcount","Avg Rows"},
                { "max_rowcount","Max Rows"},
                { "max_dop","Max DOP"},
                { "avg_tempdb_space_used_kb","Avg TempDB KB"},
                { "max_tempdb_space_used_kb","Max TempDB KB"},
                { "num_plans", "Plan Count" },
                { "num_queries", "Query Count" },
                { "top_waits", "Top Waits" },
                { "plan_forcing_type_desc","Plan Forcing"},
                { "force_failure_count","Force Failure count"},
                { "last_force_failure_reason_desc", "Last Forced Failure"},
                 {"is_parallel_plan","Parallel"},
                 {"interval_start","Interval Start"},
                 {"interval_end","Interval End"}
            },
            CellFormatString = new Dictionary<string, string>
            {
                { "total_cpu_time_ms", "N0" },
                { "avg_cpu_time_ms", "N1" },
                { "max_cpu_time_ms", "N1" },
                { "total_duration_ms", "N0" },
                { "avg_duration_ms", "N1" },
                { "max_duration_ms", "N1" },
                { "count_executions", "N0" },
                { "executions_per_min", "N2" },
                { "max_memory_grant_kb", "N0" },
                { "avg_memory_grant_kb", "N0" },
                { "total_physical_io_reads_kb", "N0" },
                { "avg_physical_io_reads_kb", "N0" },
                { "num_plans","N0"},
                { "avg_rowcount","N0"},
                { "max_rowcount","N0"},
                { "abort_count", "N0" },
                { "exception_count", "N0" },
                { "avg_tempdb_space_used_kb","N0"},
                { "max_tempdb_space_used_kb","N0"},
            },
            ResultName = "Top Queries",
            LinkColumns = new Dictionary<string, LinkColumnInfo>
            {
                {
                    "query_sql_text",
                    new TextLinkColumnInfo()
                    {
                        TargetColumn = "query_sql_text",
                        TextHandling = CodeEditor.CodeEditorModes.SQL
                    }
                }
                ,
                {
                    "query_id",
                    new DrillDownLinkColumnInfo()
                    {
                    }
                },
                {
                    "plan_id",
                    new DrillDownLinkColumnInfo()
                    {
                    }
                }
                ,
                {
                    "query_hash",
                    new DrillDownLinkColumnInfo()
                    {
                    }
                },
                {
                    "query_plan_hash",
                    new DrillDownLinkColumnInfo()
                    {
                    }
                },
                {
                    "plan_forcing_type_desc",
                    new DrillDownLinkColumnInfo()
                    {
                    }
                },
                {
                    "object_name",
                    new DrillDownLinkColumnInfo()
                    {
                    }
                }
            },
            ColumnLayout = new List<KeyValuePair<string, PersistedColumnLayout>>()
            {
                new("DB", new PersistedColumnLayout() { Width = 170, Visible = true }),
                new("query_hash", new PersistedColumnLayout() { Width = 160, Visible = true}),
                new("query_plan_hash", new PersistedColumnLayout() { Width = 160, Visible = true }),
                new("query_id", new PersistedColumnLayout() { Width = 80, Visible = true }),
                new("plan_id", new PersistedColumnLayout() { Width = 70, Visible = true }),
                new("object_id", new PersistedColumnLayout() { Width = 100, Visible = false }),
                new("object_name", new PersistedColumnLayout() { Width = 180, Visible = true }),
                new("query_sql_text", new PersistedColumnLayout() { Width = 250, Visible = true }),
                new("total_cpu_time_ms", new PersistedColumnLayout() { Width = 70, Visible = true }),
                new("avg_cpu_time_ms", new PersistedColumnLayout() { Width = 70, Visible = true }),
                new("max_cpu_time_ms", new PersistedColumnLayout() { Width = 70, Visible = true }),
                new("total_duration_ms", new PersistedColumnLayout() { Width = 70, Visible = true }),
                new("avg_duration_ms", new PersistedColumnLayout() { Width = 70, Visible = true }),
                new("max_duration_ms", new PersistedColumnLayout() { Width = 70, Visible = true }),
                new("count_executions", new PersistedColumnLayout() { Width = 70, Visible = true }),
                new("abort_count", new PersistedColumnLayout() { Width = 60, Visible = true }),
                new("exception_count", new PersistedColumnLayout() { Width = 60, Visible = true }),
                new("executions_per_min", new PersistedColumnLayout() { Width = 70, Visible = true }),
                new("avg_memory_grant_kb", new PersistedColumnLayout() { Width = 70, Visible = true }),
                new("max_memory_grant_kb", new PersistedColumnLayout() { Width = 70, Visible = true }),
                new("total_physical_io_reads_kb", new PersistedColumnLayout() { Width = 70, Visible = true }),
                new("avg_physical_io_reads_kb", new PersistedColumnLayout() { Width = 70, Visible = true }),
                new("avg_rowcount", new PersistedColumnLayout() { Width = 70, Visible = true }),
                new("max_rowcount", new PersistedColumnLayout() { Width = 70, Visible = true }),
                new("max_dop", new PersistedColumnLayout() { Width = 60, Visible = true }),
                new("avg_tempdb_space_used_kb", new PersistedColumnLayout() { Width = 80, Visible = true }),
                new("max_tempdb_space_used_kb", new PersistedColumnLayout() { Width = 80, Visible = true }),
                new("num_plans", new PersistedColumnLayout() { Width = 60, Visible = true }),
                new("num_queries", new PersistedColumnLayout() { Width = 60, Visible = true }),
                new("top_waits", new PersistedColumnLayout() { Width = 200, Visible = true }),
                new("plan_forcing_type_desc", new PersistedColumnLayout() { Width = 100, Visible = true }),
                new("force_failure_count", new PersistedColumnLayout() { Width = 70, Visible = true }),
                new("last_force_failure_reason_desc", new PersistedColumnLayout() { Width = 100, Visible = true }),
                new("is_parallel_plan", new PersistedColumnLayout() { Width = 70, Visible = true }),
                new("query_parameterization_type_desc", new PersistedColumnLayout() { Width = 100, Visible = true}),
                new("interval_start", new PersistedColumnLayout() { Width = 150, Visible = false}),
                new("interval_end", new PersistedColumnLayout() { Width = 150, Visible = false})
            },
            CellHighlightingRules =
            {
                {
                    "abort_count",
                    new CellHighlightingRuleSet("abort_count")
                    {
                        Rules = new List<CellHighlightingRule>()
                        {
                            new(){ ConditionType = CellHighlightingRule.ConditionTypes.Equals, Value1 = "0", Status =  DBADashStatus.DBADashStatusEnum.OK},
                            new(){ ConditionType = CellHighlightingRule.ConditionTypes.GreaterThan, Value1 = "0", Status =  DBADashStatus.DBADashStatusEnum.Warning}
                        }
                    }
                },
                {
                    "exception_count",
                    new CellHighlightingRuleSet("exception_count")
                    {
                        Rules = new List<CellHighlightingRule>()
                        {
                            new(){ ConditionType = CellHighlightingRule.ConditionTypes.Equals, Value1 = "0", Status =  DBADashStatus.DBADashStatusEnum.OK},
                            new(){ ConditionType = CellHighlightingRule.ConditionTypes.GreaterThan, Value1 = "0", Status =  DBADashStatus.DBADashStatusEnum.Warning}
                        }
                    }
                },
                {
                    "plan_forcing_type_desc",
                    new CellHighlightingRuleSet("plan_forcing_type_desc")
                    {
                        Rules = new List<CellHighlightingRule>()
                        {
                            new(){ ConditionType = CellHighlightingRule.ConditionTypes.Equals, Value1 = "NONE", Status =  DBADashStatus.DBADashStatusEnum.NA},
                            new(){ ConditionType = CellHighlightingRule.ConditionTypes.GreaterThan, Value1 = "MANUAL", Status = DBADashStatus.DBADashStatusEnum.Information},
                            new(){ ConditionType = CellHighlightingRule.ConditionTypes.GreaterThan, Value1 = "AUTO", Status = DBADashStatus.DBADashStatusEnum.Information}
                        }
                    }
                },
                {
                    "force_failure_count",
                    new CellHighlightingRuleSet("force_failure_count")
                    {
                        Rules = new List<CellHighlightingRule>()
                        {
                            new(){ ConditionType = CellHighlightingRule.ConditionTypes.Equals, Value1 = "0", Status =  DBADashStatus.DBADashStatusEnum.OK},
                            new(){ ConditionType = CellHighlightingRule.ConditionTypes.All, Status = DBADashStatus.DBADashStatusEnum.Warning}
                        }
                    }
                },
                {
                    "last_force_failure_reason_desc",
                    new CellHighlightingRuleSet("last_force_failure_reason_desc")
                    {
                        Rules = new List<CellHighlightingRule>()
                        {
                            new(){ ConditionType = CellHighlightingRule.ConditionTypes.Equals, Value1 = "NONE", Status =  DBADashStatus.DBADashStatusEnum.OK},
                            new(){ ConditionType = CellHighlightingRule.ConditionTypes.All, Status = DBADashStatus.DBADashStatusEnum.Warning}
                        }
                    }
                }
            },
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
                Task.Run(() => queryStorePlanChart1.ShowChart(CurrentContext, DrillDownDB, DrillDownQueryId, tsNearestInterval.Checked, new DateTimeOffset(DateRange.FromUTC, TimeSpan.Zero), new DateTimeOffset(DateRange.ToUTC, TimeSpan.Zero)));
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
                    From = new DateTimeOffset(DateRange.FromUTC, TimeSpan.Zero),
                    To = new DateTimeOffset(DateRange.ToUTC, TimeSpan.Zero),
                    IncludeWaits = IncludeWaits,
                    Lifetime = Config.DefaultCommandTimeout
                };
                Task.Run(() => MessagingHelper.SendMessageAndProcessReply(message, CurrentContext, lblStatus,
                    ProcessCompletedDrillDownTask, Guid.NewGuid()));
            }
        }

        private void PlanDrillDown(DataGridView _dgv, DataGridViewCellEventArgs e)
        {
            var planId = (long)_dgv.Rows[e.RowIndex].Cells["plan_id"].Value;
            var db = _dgv.Rows[e.RowIndex].Cells["DB"].Value.ToString();
            PlanDrillDown(planId, db);
        }

        private void PlanDrillDown(long planId, string db)
        {
            var message = new PlanCollectionMessage()
            {
                CollectAgent = CurrentContext.CollectAgent,
                ImportAgent = CurrentContext.ImportAgent,
                ConnectionID = CurrentContext.ConnectionID,
                DatabaseName = db,
                PlanID = planId,
            };
            Task.Run(() => MessagingHelper.SendMessageAndProcessReply(message, CurrentContext, lblStatus, ProcessCompletedPlanCollectionMessage, Guid.NewGuid()));
        }

        private void QueryHashDrillDown(DataGridView _dgv, DataGridViewCellEventArgs e)
        {
            var hash = (string)_dgv.Rows[e.RowIndex].Cells["query_hash"].FormattedValue;
            txtObjectName.Text = hash ?? string.Empty;
            SetGroupBy(QueryStoreTopQueriesMessage.QueryStoreGroupByEnum.query_id);
            RefreshData();
        }

        private void ObjectDrillDown(DataGridView _dgv, DataGridViewCellEventArgs e)
        {
            var objectName = Convert.ToString(_dgv.Rows[e.RowIndex].Cells["object_name"].Value);
            txtObjectName.Text = objectName ?? string.Empty;
            SetGroupBy(QueryStoreTopQueriesMessage.QueryStoreGroupByEnum.query_id);
            RefreshData();
        }

        private void PlanHashDrillDown(DataGridView _dgv, DataGridViewCellEventArgs e)
        {
            var hash = (string)_dgv.Rows[e.RowIndex].Cells["query_plan_hash"].FormattedValue;
            txtPlan.Text = hash ?? string.Empty;
            SetGroupBy(QueryStoreTopQueriesMessage.QueryStoreGroupByEnum.query_id);
            RefreshData();
        }

        private void DefaultDrillDown(DataGridView _dgv, DataGridViewCellEventArgs e)
        {
            var colName = _dgv.Columns[e.ColumnIndex].DataPropertyName;
            LinkColumnInfo linkColumnInfo = null;
            topQueriesResult.LinkColumns?.TryGetValue(colName, out linkColumnInfo);
            try
            {
                linkColumnInfo?.Navigate(CurrentContext, _dgv.Rows[e.RowIndex], 0);
            }
            catch (Exception ex)
            {
                MessageBox.Show(@"Error navigating to link: " + ex.Message, @"Error", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
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

                case "plan_id":
                    PlanDrillDown(_dgv, e);
                    break;

                case "query_hash":
                    QueryHashDrillDown(_dgv, e);
                    break;

                case "query_plan_hash":
                    PlanHashDrillDown(_dgv, e);
                    break;

                case "plan_forcing_type_desc":
                    PlanForcingDrillDown(_dgv, e);
                    break;

                case "object_name":
                    ObjectDrillDown(_dgv, e);
                    break;

                case "" when _dgv.Columns[e.ColumnIndex].Name == "ForceUnforce":
                    await MessagingHelper.ForcePlanDrillDown(_dgv, e, CurrentContext, lblStatus, ProcessPlanForcingMessage);
                    break;

                default:
                    DefaultDrillDown(_dgv, e);
                    break;
            }
        }

        private async Task ProcessPlanForcingMessage(ResponseMessage reply, Guid messageGroup)
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
            txtPlan.Text = string.Empty;
            txtObjectName.Text = string.Empty;
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
    }
}