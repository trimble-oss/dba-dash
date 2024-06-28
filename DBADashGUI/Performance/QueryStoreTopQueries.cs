using DBADash.Messaging;
using DBADashGUI.CustomReports;
using DBADashGUI.SchemaCompare;
using DBADashGUI.Theme;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using Amazon.Runtime.Internal.Transform;
using System.Xml.Linq;
using Serilog;

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

        public void SetContext(DBADashContext context)
        {
            if (context != CurrentContext)
            {
                dgv.DataSource = null;
                txtObjectName.Text = string.Empty;
                txtPlan.Text = string.Empty;
                splitContainer1.Panel2Collapsed = true;
                dgvDrillDown.DataSource = null;
                includeWaitsToolStripMenuItem.Enabled = context.ProductVersion?.Major >= 14 || context.AzureInstanceIDs.Count > 0;
                lblStatus.Text = string.Empty;
            }
            tsExecute.Text = string.IsNullOrEmpty(context.DatabaseName) ? "Execute (ALL Databases)" : "Execute";
            CurrentContext = context;
        }

        private int top = 25;
        private int minimumPlanCount = 1;

        private int GetMinimumPlanCount()=>minimumPlanCountToolStripMenuItem.Enabled ? minimumPlanCount : 1;

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
            };
            lblStatus.InvokeSetStatus(messageSentMessage, string.Empty, DashColors.Information);
            await SendMessageAndProcessReply(message, dgv);
        }

        private async Task SendMessageAndProcessReply(MessageBase message, DataGridView _dgv)
        {
            var messageGroup = Guid.NewGuid();
            if (CurrentContext.ImportAgentID == null)
            {
                lblStatus.InvokeSetStatus("No Import Agent", string.Empty, DashColors.Fail);
                return;
            }
            await MessageProcessing.SendMessageToService(message.Serialize(), (int)CurrentContext.ImportAgentID, messageGroup, Common.ConnectionString, Config.DefaultCommandTimeout);
            var completed = false;
            while (!completed)
            {
                ResponseMessage reply;
                try
                {
                    reply = await Messaging.CollectionMessaging.ReceiveReply(messageGroup, Config.DefaultCommandTimeout * 1000);
                }
                catch (Exception ex)
                {
                    lblStatus.InvokeSetStatus(ex.Message, string.Empty, DashColors.Fail);
                    completed = true;
                    return;
                }

                switch (reply.Type)
                {
                    case ResponseMessage.ResponseTypes.Progress:
                        completed = false;
                        lblStatus.InvokeSetStatus(reply.Message, string.Empty, DashColors.Information);
                        break;

                    case ResponseMessage.ResponseTypes.Failure:
                        completed = true;
                        lblStatus.InvokeSetStatus(reply.Message, reply.Exception?.ToString(), DashColors.Fail);
                        break;

                    case ResponseMessage.ResponseTypes.Success:
                        {
                            completed = false; // It's done but wait for end dialog
                            lblStatus.InvokeSetStatus(reply.Message, string.Empty, DashColors.Success);
                            this.Invoke(() =>
                            {
                                var ds = reply.Data;
                                if (ds == null || ds.Tables.Count == 0 || ds.Tables[0].Columns.Count == 0)
                                {
                                    MessageBox.Show(@"No data returned");
                                    return;
                                }

                                var dt = ds.Tables[0];
                                if (message is PlanCollectionMessage)
                                {
                                    LoadQueryPlan(dt);
                                    return;
                                }

                                _dgv.Columns.Clear();
                                _dgv.AddColumns(dt, topQueriesResult);
                                _dgv.DataSource = new DataView(dt, null, $"{sortColumn} DESC",
                                    DataViewRowState.CurrentRows);
                                _dgv.LoadColumnLayout(topQueriesResult.ColumnLayout);
                                _dgv.ApplyTheme();
                                if (_dgv == dgvDrillDown)
                                {
                                    splitContainer1.Panel2Collapsed = false;
                                }
                            });
                            break;
                        }
                    case ResponseMessage.ResponseTypes.EndConversation:
                        completed = true;
                        break;
                }
            }
        }

        private void LoadQueryPlan(DataTable dt)
        {
            if (dt.Rows.Count == 0)
            {
                lblStatus.InvokeSetStatus("No query plan", string.Empty, DashColors.Fail);
                return;
            }
            var plan = dt.Rows[0]["query_plan"].ToString();
            lblStatus.InvokeSetStatus("Loading Query Plan...", string.Empty, DashColors.Success);
            Common.ShowQueryPlan(plan);
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
                { "top_waits", "Top Waits" },
                { "plan_forcing_type_desc","Plan Forcing"},
                { "force_failure_count","Force Failure count"},
                { "last_force_failure_reason_desc", "Last Forced Failure"},
                 {"is_parallel_plan","Parallel"}
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
                new("top_waits", new PersistedColumnLayout() { Width = 200, Visible = true }),
                new ("plan_forcing_type_desc", new PersistedColumnLayout() { Width = 100, Visible = true }),
                new ("force_failure_count", new PersistedColumnLayout() { Width = 70, Visible = true }),
                new ("last_force_failure_reason_desc", new PersistedColumnLayout() { Width = 100, Visible = true }),
                new ("is_parallel_plan", new PersistedColumnLayout() { Width = 70, Visible = true }),
                new("query_parameterization_type_desc", new PersistedColumnLayout() { Width = 100, Visible = true}),
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
            var message = new QueryStoreTopQueriesMessage
            {
                CollectAgent = CurrentContext.CollectAgent,
                ImportAgent = CurrentContext.ImportAgent,
                Top = top,
                SortColumn = sortColumn,
                QueryID = queryId,
                NearestInterval = tsNearestInterval.Checked,
                GroupBy = QueryStoreTopQueriesMessage.QueryStoreGroupByEnum.plan_id,
                ConnectionID = CurrentContext.ConnectionID,
                DatabaseName = db,
                From = new DateTimeOffset(DateRange.FromUTC, TimeSpan.Zero),
                To = new DateTimeOffset(DateRange.ToUTC, TimeSpan.Zero),
                IncludeWaits = IncludeWaits,
            };
            Task.Run(() => SendMessageAndProcessReply(message, dgvDrillDown));
        }

        private void PlanDrillDown(DataGridView _dgv, DataGridViewCellEventArgs e)
        {
            var planId = (long)_dgv.Rows[e.RowIndex].Cells["plan_id"].Value;
            var db = _dgv.Rows[e.RowIndex].Cells["DB"].Value.ToString();
            var message = new PlanCollectionMessage()
            {
                CollectAgent = CurrentContext.CollectAgent,
                ImportAgent = CurrentContext.ImportAgent,
                ConnectionID = CurrentContext.ConnectionID,
                DatabaseName = db,
                PlanID = planId,
            };
            Task.Run(() => SendMessageAndProcessReply(message, null));
        }

        private void QueryHashDrillDown(DataGridView _dgv, DataGridViewCellEventArgs e)
        {
            var hash = (string)_dgv.Rows[e.RowIndex].Cells["query_hash"].FormattedValue;
            txtObjectName.Text = hash ?? string.Empty;
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

        private void Dgv_CellContentClick(object sender, DataGridViewCellEventArgs e)
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

                default:
                    DefaultDrillDown(_dgv, e);
                    break;
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
            Common.ShowCodeViewer(planForcingSQL, "Plan Force/Unforce SQL", CodeEditor.CodeEditorModes.SQL);
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
            Common.PromptSaveDataGridView(ref dgv);
        }

        private void TsCopy_Click(object sender, EventArgs e)
        {
            Common.CopyDataGridViewToClipboard(dgv);
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
            if (CommonShared.ShowInputDialog(ref countString,"Enter minimum plan count:")== DialogResult.OK)
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
    }
}