using DBADash.Messaging;
using DBADashGUI.Theme;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using DBADashGUI.CustomReports;
using DBADashGUI.Messaging;
using Microsoft.Data.SqlClient;
using DBADashGUI.SchemaCompare;

namespace DBADashGUI.Performance
{
    public partial class QueryStoreForcedPlans : UserControl, ISetContext, IRefreshData
    {
        private DBADashContext CurrentContext;

        public QueryStoreForcedPlans()
        {
            InitializeComponent();
            dgv.RowsAdded += (sender, args) =>
                forcedPlansResult.CellHighlightingRules.FormatRowsAdded((DataGridView)sender, args);
            dgv.RowsAdded += (sender, args) => forcedPlansResult.CellHighlightingRules.FormatRowsAdded((DataGridView)sender, args);
            dgvLog.RowsAdded += (sender, args) => logReport.CellHighlightingRules.FormatRowsAdded((DataGridView)sender, args);
            dgv.RegisterClearFilter(tsClearFilter);
            dgvLog.RegisterClearFilter(tsClearFilterLog);
        }

        private readonly CustomReportResult logReport = new()
        {
            Columns = new Dictionary<string, ColumnMetadata>
            {
                ["DB"] = new ColumnMetadata { Alias = "DB", Visible = true },
                ["log_date"] = new ColumnMetadata { Alias = "Date", Visible = true },
                ["log_type"] = new ColumnMetadata { Alias = "Type", Visible = true },
                ["query_id"] = new ColumnMetadata
                {
                    Alias = "Query ID",
                    Visible = true,
                    Link = new QueryStoreLinkColumnInfo { TargetColumn = "query_id", TargetColumnLinkType = QueryStoreLinkColumnInfo.QueryStoreLinkColumnType.QueryID, DatabaseNameColumn = "DB" }
                },
                ["plan_id"] = new ColumnMetadata
                {
                    Alias = "Plan ID",
                    Visible = true,
                    Link = new QueryStoreLinkColumnInfo { TargetColumn = "plan_id", TargetColumnLinkType = QueryStoreLinkColumnInfo.QueryStoreLinkColumnType.PlanID, DatabaseNameColumn = "DB" }
                },
                ["object_name"] = new ColumnMetadata
                {
                    Alias = "Object Name",
                    Visible = true,
                    Link = new QueryStoreLinkColumnInfo { TargetColumn = "object_name", TargetColumnLinkType = QueryStoreLinkColumnInfo.QueryStoreLinkColumnType.ObjectName, DatabaseNameColumn = "DB" }
                },
                ["query_sql_text"] = new ColumnMetadata
                {
                    Alias = "Text",
                    Visible = true,
                    Link = new TextLinkColumnInfo { TargetColumn = "query_sql_text", TextHandling = CodeEditor.CodeEditorModes.SQL }
                },
                ["query_hash"] = new ColumnMetadata
                {
                    Alias = "Query Hash",
                    Visible = true,
                    Link = new QueryStoreLinkColumnInfo { TargetColumn = "query_hash", TargetColumnLinkType = QueryStoreLinkColumnInfo.QueryStoreLinkColumnType.QueryHash, DatabaseNameColumn = "DB" }
                },
                ["query_plan_hash"] = new ColumnMetadata
                {
                    Alias = "Plan Hash",
                    Visible = true,
                    Link = new QueryStoreLinkColumnInfo { TargetColumn = "query_plan_hash", TargetColumnLinkType = QueryStoreLinkColumnInfo.QueryStoreLinkColumnType.PlanHash, DatabaseNameColumn = "DB" }
                },
                ["user_name"] = new ColumnMetadata { Alias = "User", Visible = true },
                ["notes"] = new ColumnMetadata { Alias = "Notes", Visible = true },
                ["status"] = new ColumnMetadata
                {
                    Alias = "Status",
                    Visible = true,
                    Highlighting = new CellHighlightingRuleSet("status")
                    {
                        Rules = new List<CellHighlightingRule>
                        {
                            new() { ConditionType = CellHighlightingRule.ConditionTypes.Equals, Value1 = "REQUEST", Status = DBADashStatus.DBADashStatusEnum.Information },
                            new() { ConditionType = CellHighlightingRule.ConditionTypes.Equals, Value1 = "SUCCEEDED", Status = DBADashStatus.DBADashStatusEnum.OK },
                            new() { ConditionType = CellHighlightingRule.ConditionTypes.All, Status = DBADashStatus.DBADashStatusEnum.Warning }
                        }
                    }
                },
                ["Undo"] = new ColumnMetadata { Visible = true }
            }
        };

        private readonly CustomReportResult forcedPlansResult = new()
        {
            Columns = new Dictionary<string, ColumnMetadata>
            {
                ["DB"] = new ColumnMetadata { Alias = "DB", Visible = true },
                ["query_id"] = new ColumnMetadata
                {
                    Alias = "Query ID",
                    Visible = true,
                    Link = new QueryStoreLinkColumnInfo { TargetColumn = "query_id", TargetColumnLinkType = QueryStoreLinkColumnInfo.QueryStoreLinkColumnType.QueryID, DatabaseNameColumn = "DB" }
                },
                ["object_id"] = new ColumnMetadata { Alias = "Object ID", Visible = false },
                ["plan_id"] = new ColumnMetadata
                {
                    Alias = "Plan ID",
                    Visible = true,
                    Link = new QueryStoreLinkColumnInfo { TargetColumn = "plan_id", TargetColumnLinkType = QueryStoreLinkColumnInfo.QueryStoreLinkColumnType.PlanID, DatabaseNameColumn = "DB" }
                },
                ["query_hash"] = new ColumnMetadata
                {
                    Alias = "Query Hash",
                    Visible = true,
                    Link = new QueryStoreLinkColumnInfo { TargetColumn = "query_hash", TargetColumnLinkType = QueryStoreLinkColumnInfo.QueryStoreLinkColumnType.QueryHash, DatabaseNameColumn = "DB" }
                },
                ["query_plan_hash"] = new ColumnMetadata
                {
                    Alias = "Plan Hash",
                    Visible = true,
                    Link = new QueryStoreLinkColumnInfo { TargetColumn = "query_plan_hash", TargetColumnLinkType = QueryStoreLinkColumnInfo.QueryStoreLinkColumnType.PlanHash, DatabaseNameColumn = "DB" }
                },
                ["object_name"] = new ColumnMetadata
                {
                    Alias = "Object Name",
                    Visible = true,
                    Link = new QueryStoreLinkColumnInfo { TargetColumn = "object_name", TargetColumnLinkType = QueryStoreLinkColumnInfo.QueryStoreLinkColumnType.ObjectName, DatabaseNameColumn = "DB" }
                },
                ["query_sql_text"] = new ColumnMetadata
                {
                    Alias = "Text",
                    Visible = true,
                    Link = new TextLinkColumnInfo { TargetColumn = "query_sql_text", TextHandling = CodeEditor.CodeEditorModes.SQL }
                },
                ["total_cpu_time_ms"] = new ColumnMetadata { Alias = "Total CPU (ms)" },
                ["plan_forcing_type_desc"] = new ColumnMetadata { Alias = "Plan Forcing", Visible = true },
                ["force_failure_count"] = new ColumnMetadata
                {
                    Alias = "Force Failure count",
                    Visible = true,
                    Highlighting = new CellHighlightingRuleSet("force_failure_count")
                    {
                        Rules = new List<CellHighlightingRule>
                        {
                            new() { ConditionType = CellHighlightingRule.ConditionTypes.Equals, Value1 = "0", Status = DBADashStatus.DBADashStatusEnum.OK },
                            new() { ConditionType = CellHighlightingRule.ConditionTypes.All, Status = DBADashStatus.DBADashStatusEnum.Warning }
                        }
                    }
                },
                ["last_force_failure_reason_desc"] = new ColumnMetadata
                {
                    Alias = "Last Forced Failure",
                    Visible = true,
                    Highlighting = new CellHighlightingRuleSet("last_force_failure_reason_desc")
                    {
                        Rules = new List<CellHighlightingRule>
                        {
                            new() { ConditionType = CellHighlightingRule.ConditionTypes.Equals, Value1 = "NONE", Status = DBADashStatus.DBADashStatusEnum.OK },
                            new() { ConditionType = CellHighlightingRule.ConditionTypes.All, Status = DBADashStatus.DBADashStatusEnum.Warning }
                        }
                    }
                },
                ["is_parallel_plan"] = new ColumnMetadata { Alias = "Parallel", Visible = true },
                ["last_execution_time_plan"] = new ColumnMetadata { Alias = "Last Execution Time (Plan)", Visible = true },
                ["last_execution_time_query"] = new ColumnMetadata { Alias = "Last Execution Time (Query)", Visible = true },
                ["last_compile_start_time"] = new ColumnMetadata { Alias = "Last Compile Time", Visible = true },
                ["num_plans"] = new ColumnMetadata { Alias = "Plan Count", Visible = true },
                ["query_parameterization_type_desc"] = new ColumnMetadata { Alias = "Parameterization type", Visible = false }
            }
        };

        public void SetContext(DBADashContext _context)
        {
            if (_context == CurrentContext) return;
            dgv.DataSource = null;
            dgvLog.DataSource = null;
            dgvLog.Columns.Clear();
            dgv.Columns.Clear();
            CurrentContext = _context;
            RefreshData();
        }

        public async void RefreshData()
        {
            try
            {
                var message = new QueryStoreForcedPlansMessage()
                {
                    ConnectionID = CurrentContext.ConnectionID,
                    DatabaseName = CurrentContext.DatabaseName,
                    CollectAgent = CurrentContext.CollectAgent,
                    ImportAgent = CurrentContext.ImportAgent,
                };
                var logRefreshTask = RefreshLog();
                var messageTask = MessagingHelper.SendMessageAndProcessReply(message, CurrentContext, lblStatus, ProcessCompletedMessage, Guid.NewGuid());
                await Task.WhenAll(messageTask, logRefreshTask);
            }
            catch (Exception ex)
            {
                lblStatus.InvokeSetStatus(ex.Message.Truncate(200), ex.ToString(), DashColors.Fail);
            }
        }

        private Task ProcessCompletedMessage(ResponseMessage reply, Guid messageGroup)
        {
            if (reply.Data == null || reply.Data.Tables.Count == 0)
            {
                lblStatus.InvokeSetStatus("No data returned", string.Empty, DashColors.Fail);
                return Task.CompletedTask;
            }

            var dt = reply.Data.Tables[0];
            if (dgv.Columns.Count == 0)
            {
                dgv.AddColumns(dt, forcedPlansResult);
                dgv.Columns.Add(new DataGridViewButtonColumn()
                { Name = "Unforce", UseColumnTextForButtonValue = true, Text = "Unforce", HeaderText = "" });
            }

            DateHelper.ConvertUTCToAppTimeZone(ref dt);
            dgv.DataSource = new DataView(dt);
            dgv.ApplyTheme();
            dgv.LoadColumnLayout(forcedPlansResult.ColumnLayout);
            dgv.AutoResizeColumnsWithMaxColumnWidth();
            dgv.Columns["Unforce"]!.Visible = true;
            lblStatus.InvokeSetStatus("Completed", string.Empty, DashColors.Success);
            return Task.CompletedTask;
        }

        private void CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.Value is not byte[] bytes) return;
            e.Value = "0x" + BitConverter.ToString(bytes).Replace("-", string.Empty);
            e.FormattingApplied = true; // Indicate that formatting was applied
        }

        private void TsTriggerCollection_Click(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void TsExcel_Click(object sender, EventArgs e)
        {
            dgv.ExportToExcel();
        }

        private void TsCopy_Click(object sender, EventArgs e)
        {
            dgv.CopyGrid();
        }

        private async void CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            var _dgv = (DataGridView)sender;
            if (e.ColumnIndex == _dgv.Columns["Unforce"]?.Index || e.ColumnIndex == _dgv.Columns["Undo"]?.Index)
            {
                await MessagingHelper.ForcePlanDrillDown(_dgv, e, CurrentContext, lblStatus, ProcessPlanForcingMessage);
            }
            else
            {
                var report = _dgv == dgvLog ? logReport : forcedPlansResult;
                var colName = _dgv.Columns[e.ColumnIndex].DataPropertyName;
                LinkColumnInfo linkColumnInfo = null;
                report.LinkColumns?.TryGetValue(colName, out linkColumnInfo);
                try
                {
                    linkColumnInfo?.Navigate(CurrentContext, _dgv.Rows[e.RowIndex], 0, this);
                }
                catch (Exception ex)
                {
                    CommonShared.ShowExceptionDialog(ex, @"Error navigating to link");
                }
            }
        }

        private async Task ProcessPlanForcingMessage(ResponseMessage reply, Guid messageGroup)
        {
            if (reply.Type == ResponseMessage.ResponseTypes.Success)
            {
                lblStatus.InvokeSetStatus("Plan Forcing Operation Completed", string.Empty, DashColors.Success);
                await MessagingHelper.UpdatePlanForcingLog(messageGroup, "SUCCEEDED");
                RefreshData();
            }
            else
            {
                lblStatus.InvokeSetStatus("Plan Forcing Operation Failed", reply.Message, DashColors.Fail);
                await MessagingHelper.UpdatePlanForcingLog(messageGroup, "FAIL:" + reply.Message);
                await RefreshLog();
            }
        }

        private int TopRows => int.Parse(tsTop.Tag?.ToString() ?? 100.ToString());

        private async Task RefreshLog()
        {
            await using var cn = new SqlConnection(Common.ConnectionString);
            var cmd = new SqlCommand("dbo.PlanForcingLog_Get", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@InstanceID", CurrentContext.InstanceID);
            cmd.Parameters.AddStringIfNotNullOrEmpty("@DB", CurrentContext.DatabaseName);
            cmd.Parameters.AddWithValue("@Top", TopRows);
            using var da = new SqlDataAdapter(cmd);
            var dt = new DataTable();
            await cn.OpenAsync();
            da.Fill(dt);
            DateHelper.ConvertUTCToAppTimeZone(ref dt);
            tsTop.DisplayStyle = dt.Rows.Count == TopRows ? ToolStripItemDisplayStyle.ImageAndText : ToolStripItemDisplayStyle.Text;
            tsTop.ToolTipText = dt.Rows.Count == TopRows ? $"Top {TopRows} (Result is truncated)" : $"Top {TopRows}";

            if (dgvLog.Columns.Count == 0)
            {
                dgvLog.AddColumns(dt, logReport);
                dgvLog.Columns.Add(new DataGridViewButtonColumn() { Name = "Undo", Text = "Undo", HeaderText = "" });
                dgvLog.LoadColumnLayout(logReport.ColumnLayout);
            }
            dgvLog.DataSource = new DataView(dt);
            dgvLog.AutoResizeColumnsWithMaxColumnWidth();
            dgvLog.ApplyTheme();
        }

        private void DgvLog_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            for (var idx = e.RowIndex; idx < e.RowIndex + e.RowCount; idx += 1)
            {
                dgvLog.Rows[idx].Cells["Undo"].Value = (string)dgvLog.Rows[idx].Cells["log_type"].Value == "Force" ? "Undo (Unforce)" : "Undo (Force)";
            }
        }

        private async void Top_Select(object sender, EventArgs e)
        {
            var topMenu = (ToolStripMenuItem)sender;
            int topValue;
            if (topMenu.Tag == null)
            {
                var topValueString = TopRows.ToString();
                CommonShared.ShowInputDialog(ref topValueString, "Enter Top:");
                if (!int.TryParse(topValueString, out topValue)) return;
            }
            else
            {
                topValue = int.Parse(topMenu.Tag.ToString() ?? 100.ToString());
            }
            foreach (var item in tsTop.DropDownItems.OfType<ToolStripMenuItem>())
            {
                item.Checked = false;
            }
            topMenu.Checked = true;
            tsTop.Text = @"Top " + topValue.ToString();
            tsTop.Tag = topValue;
            await RefreshLog();
        }

        private void TsCopyHistory_Click(object sender, EventArgs e)
        {
            dgvLog.CopyGrid();
        }

        private void TsExportHistoryExcel_Click(object sender, EventArgs e)
        {
            dgvLog.ExportToExcel();
        }
    }
}