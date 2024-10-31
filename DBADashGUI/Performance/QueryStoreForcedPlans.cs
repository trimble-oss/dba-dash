using DBADash.Messaging;
using DBADashGUI.Theme;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Polly;
using DBADashGUI.CustomReports;
using DBADashGUI.Messaging;
using DocumentFormat.OpenXml.Presentation;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Dmf;
using DBADashGUI.SchemaCompare;
using DocumentFormat.OpenXml.ExtendedProperties;
using System.Security.AccessControl;
using DBADash;
using Microsoft.Data.Tools.Schema.Sql.SchemaModel.Parameterization;

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
            ColumnAlias = new Dictionary<string, string>
            {
                { "DB", "DB" },
                { "log_date", "Date" },
                { "log_type", "Type" },
                { "query_id", "Query ID" },
                { "plan_id", "Plan ID" },
                { "object_name", "Object Name" },
                { "query_sql_text", "Text" },
                { "query_hash", "Query Hash" },
                { "query_plan_hash", "Plan Hash" },
                { "user_name", "User" },
                { "notes", "Notes" },
                { "status", "Status" }
            },
            ColumnLayout = new List<KeyValuePair<string, PersistedColumnLayout>>()
            {
                new("DB", new PersistedColumnLayout() { Width = 100, Visible = true }),
                new("log_date", new PersistedColumnLayout() { Width = 160, Visible = true }),
                new("log_type", new PersistedColumnLayout() { Width = 70, Visible = true }),
                new("query_id", new PersistedColumnLayout() { Width = 70, Visible = true }),
                new("plan_id", new PersistedColumnLayout() { Width = 70, Visible = true }),
                new("object_name", new PersistedColumnLayout() { Width = 180, Visible = true }),
                new("query_sql_text", new PersistedColumnLayout() { Width = 250, Visible = true }),
                new("query_hash", new PersistedColumnLayout() { Width = 160, Visible = true }),
                new("query_plan_hash", new PersistedColumnLayout() { Width = 160, Visible = true }),
                new("user_name", new PersistedColumnLayout() { Width = 100, Visible = true }),
                new("notes", new PersistedColumnLayout() { Width = 250, Visible = true }),
                new("status", new PersistedColumnLayout() { Width = 120, Visible = true }),
                new("Undo", new PersistedColumnLayout() { Width = 120, Visible = true })
            },
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
            },
            CellHighlightingRules =
            {
                {
                    "status",
                    new CellHighlightingRuleSet("status")
                    {
                        Rules = new List<CellHighlightingRule>()
                        {
                            new()
                            {
                                ConditionType = CellHighlightingRule.ConditionTypes.Equals, Value1 = "REQUEST",
                                Status = DBADashStatus.DBADashStatusEnum.Information
                            },
                            new()
                            {
                                ConditionType = CellHighlightingRule.ConditionTypes.Equals, Value1 = "SUCCEEDED",
                                Status = DBADashStatus.DBADashStatusEnum.OK
                            },
                            new()
                            {
                                ConditionType = CellHighlightingRule.ConditionTypes.All,
                                Status = DBADashStatus.DBADashStatusEnum.Warning
                            }
                        }
                    }
                }
            }
        };

        private readonly CustomReportResult forcedPlansResult = new()
        {
            ColumnAlias = new Dictionary<string, string>
            {
                { "DB", "DB" },
                { "query_id", "Query ID" },
                { "object_id", "Object ID" },
                { "plan_id", "Plan ID" },
                { "query_hash", "Query Hash" },
                { "query_plan_hash", "Plan Hash" },
                { "object_name", "Object Name" },
                { "query_sql_text", "Text" },
                { "total_cpu_time_ms", "Total CPU (ms)" },
                { "plan_forcing_type_desc", "Plan Forcing" },
                { "force_failure_count", "Force Failure count" },
                { "last_force_failure_reason_desc", "Last Forced Failure" },
                { "is_parallel_plan", "Parallel" },
                { "last_execution_time_plan", "Last Execution Time (Plan)" },
                { "last_execution_time_query", "Last Execution Time (Query)" },
                { "last_compile_start_time", "Last Compile Time" },
                { "num_plans", "Plan Count" },
                { "query_parameterization_type_desc", "Parameterization type" },
            },
            ColumnLayout = new List<KeyValuePair<string, PersistedColumnLayout>>()
            {
                new("DB", new PersistedColumnLayout() { Width = 170, Visible = true }),
                new("query_hash", new PersistedColumnLayout() { Width = 160, Visible = true }),
                new("query_plan_hash", new PersistedColumnLayout() { Width = 160, Visible = true }),
                new("query_id", new PersistedColumnLayout() { Width = 80, Visible = true }),
                new("plan_id", new PersistedColumnLayout() { Width = 70, Visible = true }),
                new("object_id", new PersistedColumnLayout() { Width = 100, Visible = false }),
                new("object_name", new PersistedColumnLayout() { Width = 180, Visible = true }),
                new("query_sql_text", new PersistedColumnLayout() { Width = 250, Visible = true }),
                new("num_plans", new PersistedColumnLayout() { Width = 60, Visible = true }),
                new("plan_forcing_type_desc", new PersistedColumnLayout() { Width = 100, Visible = true }),
                new("force_failure_count", new PersistedColumnLayout() { Width = 70, Visible = true }),
                new("last_force_failure_reason_desc", new PersistedColumnLayout() { Width = 100, Visible = true }),
                new("last_execution_time_plan", new PersistedColumnLayout() { Width = 160, Visible = true }),
                new("last_execution_time_query", new PersistedColumnLayout() { Width = 160, Visible = true }),
                new("last_compile_start_time", new PersistedColumnLayout() { Width = 160, Visible = true }),
                new("is_parallel_plan", new PersistedColumnLayout() { Width = 70, Visible = true }),
                new("query_parameterization_type_desc", new PersistedColumnLayout() { Width = 100, Visible = false })
            },
            CellHighlightingRules =
            {
                {
                    "force_failure_count",
                    new CellHighlightingRuleSet("force_failure_count")
                    {
                        Rules = new List<CellHighlightingRule>()
                        {
                            new()
                            {
                                ConditionType = CellHighlightingRule.ConditionTypes.Equals, Value1 = "0",
                                Status = DBADashStatus.DBADashStatusEnum.OK
                            },
                            new()
                            {
                                ConditionType = CellHighlightingRule.ConditionTypes.All,
                                Status = DBADashStatus.DBADashStatusEnum.Warning
                            }
                        }
                    }
                },
                {
                    "last_force_failure_reason_desc",
                    new CellHighlightingRuleSet("last_force_failure_reason_desc")
                    {
                        Rules = new List<CellHighlightingRule>()
                        {
                            new()
                            {
                                ConditionType = CellHighlightingRule.ConditionTypes.Equals, Value1 = "NONE",
                                Status = DBADashStatus.DBADashStatusEnum.OK
                            },
                            new()
                            {
                                ConditionType = CellHighlightingRule.ConditionTypes.All,
                                Status = DBADashStatus.DBADashStatusEnum.Warning
                            }
                        }
                    }
                }
            },
            LinkColumns = new Dictionary<string, LinkColumnInfo>
            {
                {
                    "query_sql_text",
                    new TextLinkColumnInfo()
                    {
                        TargetColumn = "query_sql_text",
                        TextHandling = CodeEditor.CodeEditorModes.SQL
                    }
                },
            },
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
                    linkColumnInfo?.Navigate(CurrentContext, _dgv.Rows[e.RowIndex], 0);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(@"Error navigating to link: " + ex.Message, @"Error", MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
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