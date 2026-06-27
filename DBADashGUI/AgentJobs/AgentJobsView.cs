using DBADash;
using DBADashGUI.CustomReports;
using DBADashGUI.Performance;
using DBADashGUI.Theme;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using static DBADashGUI.DBADashStatus;

namespace DBADashGUI.AgentJobs
{
    internal class AgentJobsView : CustomReportView
    {
        private ToolStripDropDownButton tsConfigure;
        private ToolStripMenuItem mnuThresholdInstance;
        private ToolStripMenuItem mnuAcknowledgeErrors;

        public AgentJobsView()
        {
            Report = Instance;
            PreventReportOverwrite = true;
            AddToolbarButtons();
        }

        #region Toolbar

        private void AddToolbarButtons()
        {
            tsConfigure = new ToolStripDropDownButton("Configure")
            {
                Name = "tsConfigure",
                ToolTipText = "Configure agent job thresholds",
                Image = Properties.Resources.SettingsOutline_16x,
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText
            };

            var mnuThresholdRoot = new ToolStripMenuItem("Configure Root Thresholds");
            mnuThresholdRoot.Click += (_, _) => ConfigureThresholds(-1, Guid.Empty);
            mnuThresholdInstance = new ToolStripMenuItem("Configure Instance Thresholds");
            mnuThresholdInstance.Click += (_, _) =>
            {
                if (TryGetSingleInstanceId(out var id)) ConfigureThresholds(id, Guid.Empty);
            };
            mnuAcknowledgeErrors = new ToolStripMenuItem("Acknowledge Errors");
            mnuAcknowledgeErrors.Click += AcknowledgeErrorsToolStripMenuItem_Click;
            tsConfigure.DropDownItems.AddRange(new ToolStripItem[] { mnuThresholdInstance, mnuThresholdRoot, mnuAcknowledgeErrors });

            var insertAt = ToolStrip.Items.IndexOfKey("tsParams");
            insertAt = insertAt >= 0 ? insertAt + 1 : ToolStrip.Items.Count;
            ToolStrip.Items.Insert(insertAt, tsConfigure);
        }

        private void UpdateToolbarState()
        {
            mnuThresholdInstance.Enabled = TryGetSingleInstanceId(out _);
        }

        #endregion Toolbar

        #region Parameters

        private bool TryGetSingleInstanceId(out int id)
        {
            id = -1;
            if (context == null) return false;
            if (context.InstanceID > 0) { id = context.InstanceID; return true; }
            if (context.InstanceIDs.Count == 1) { id = context.InstanceIDs.First(); return true; }
            return false;
        }

        protected override void OnBeforeRefresh()
        {
            base.OnBeforeRefresh();
            var pJobID = customParams.FirstOrDefault(p =>
                p.Param.ParameterName.Equals("@JobID", StringComparison.OrdinalIgnoreCase));
            if (pJobID != null)
            {
                pJobID.Param.Value = context?.JobID != Guid.Empty && context?.JobID != null ? context.JobID : DBNull.Value;
                pJobID.UseDefaultValue = false;
            }
        }

        #endregion Parameters

        #region Configuration dialogs

        private void ConfigureThresholds(int instanceID, Guid _jobID)
        {
            ConfigureThresholds(instanceID, _jobID, this);
        }

        internal static void ConfigureThresholds(int instanceID, Guid _jobID, IRefreshData control)
        {
            var frm = new AgentJobThresholdsConfig
            {
                InstanceID = instanceID,
                JobID = _jobID,
                connectionString = Common.ConnectionString
            };
            frm.ShowDialog();
            if (frm.DialogResult == DialogResult.OK)
            {
                control.RefreshData();
            }
        }

        private void AcknowledgeErrorsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var grid = Grids.FirstOrDefault();
            if (grid == null) return;
            var dt = grid.DataSource as DataView;
            if (dt?.Table == null) return;
            var warningsAndFailures = dt.Table.Select("JobStatus IN(1,2)");
            if (warningsAndFailures.Length == 0)
            {
                MessageBox.Show("No warnings/failures to acknowledge", "Acknowledge Failures", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else if (MessageBox.Show(
                         $"Are you sure you want to acknowledge {warningsAndFailures.Length} job failure(s)?", "Acknowledge Failures", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                foreach (var row in warningsAndFailures)
                {
                    var id = (Guid)row["job_id"];
                    var instId = (int)row["InstanceID"];
                    AcknowledgeJobErrors(instId, id, false);
                }
                RefreshData();
            }
        }

        internal static void AcknowledgeJobErrors(int instanceID, Guid jobId, bool clear)
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.JobErrorAck", cn) { CommandType = CommandType.StoredProcedure };
            cn.Open();
            cmd.Parameters.AddWithValue("InstanceID", instanceID);
            cmd.Parameters.AddGuidIfNotEmpty("job_id", jobId);
            cmd.Parameters.AddWithValue("Clear", clear);
            cmd.ExecuteNonQuery();
        }

        #endregion Configuration dialogs

        #region Grid post-processing

        protected override void OnPostGridRefresh()
        {
            base.OnPostGridRefresh();
            foreach (var grid in Grids)
            {
                grid.LoadColumnLayout(Report.CustomReportResults[grid.ResultSetID].ColumnLayout);
                if (grid.Columns["Configure"] != null)
                {
                    grid.Columns["Configure"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                }
                SubscribeRowsAdded(grid);
                AddContextMenuItems(grid, this);
            }

            if (context?.JobID != Guid.Empty && context?.JobID != null)
            {
                var grid = Grids.FirstOrDefault();
                if (grid is { RowCount: 1 })
                {
                    var historyLink = Report.CustomReportResults[0].Columns["History"].Link;
                    historyLink.Navigate(context, grid.Rows[0], 0, this);
                }
            }

            UpdateToolbarState();
        }

        private readonly HashSet<DBADashDataGridView> subscribedGrids = new();

        private void SubscribeRowsAdded(DBADashDataGridView grid)
        {
            if (!subscribedGrids.Add(grid)) return;
            grid.RowsAdded += JobsGrid_RowsAdded;
            JobsGrid_RowsAdded(grid, new DataGridViewRowsAddedEventArgs(0, grid.RowCount));
        }

        private static void JobsGrid_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            if (sender is not DBADashDataGridView grid) return;
            var ackCol = grid.Columns["Acknowledge"];
            if (ackCol == null) return;
            for (var idx = e.RowIndex; idx < e.RowIndex + e.RowCount; idx++)
            {
                if (grid.Rows[idx].DataBoundItem is not DataRowView drv) continue;
                if (!drv.Row.Table.Columns.Contains("JobStatus")) continue;
                var jobStatus = (DBADashStatusEnum)drv["JobStatus"];
                grid.Rows[idx].Cells[ackCol.Index].Value = jobStatus switch
                {
                    DBADashStatusEnum.Critical or DBADashStatusEnum.Warning => "Acknowledge",
                    DBADashStatusEnum.Acknowledged => "Clear",
                    _ => "",
                };
            }
        }

        #endregion Grid post-processing

        #region Shared Context Menu

        public static void AddContextMenuItems(DBADashDataGridView grid, IRefreshData control)
        {
            grid.CellContextMenu ??= new ContextMenuStrip();
            var strip = grid.CellContextMenu;

            if (strip.Items.OfType<ToolStripMenuItem>().Any(i => i.Text == "Agent Job Actions"))
                return;

            var tsStartJob = new ToolStripMenuItem("Start/Stop Job", Properties.Resources.ProjectSystemModelRefresh_16x);
            var tsConfig = new ToolStripMenuItem("Configure Thresholds", Properties.Resources.SettingsOutline_16x);
            var tsConfigJob = new ToolStripMenuItem("Job Level", Properties.Resources.TableScript_16x);
            var tsConfigInstance = new ToolStripMenuItem("Instance Level", Properties.Resources.Database_16x);
            var tsConfigRoot = new ToolStripMenuItem("Root Level", Properties.Resources.StrongHierarchy_16x);
            tsConfig.DropDownItems.AddRange(new ToolStripItem[] { tsConfigJob, tsConfigInstance, tsConfigRoot });
            var tsJobInfo = new ToolStripMenuItem("Job Info", Properties.Resources.Information_blue_6227_16x16);
            var tsAcknowledge = new ToolStripMenuItem("Acknowledge", Properties.Resources.Tick_Blue_32x32_72);
            var lblJobName = new ToolStripLabel();
            lblJobName.Font = new Font(lblJobName.Font, FontStyle.Italic);
            tsAcknowledge.Visible = control is AgentJobsView;
            var tsJobActions = new ToolStripMenuItem("Agent Job Actions", Properties.Resources.MonthCalendar_16x);
            tsJobActions.DropDownItems.AddRange(new ToolStripItem[]
            {
                lblJobName,
                new ToolStripSeparator(),
                tsJobInfo,
                tsAcknowledge,
                tsConfig,
                tsStartJob
            });

            tsStartJob.Click += (_, _) =>
            {
                var row = (DataRowView)grid.Rows[grid.ClickedRowIndex].DataBoundItem;
                StartJob(row);
            };
            tsConfigJob.Click += (_, _) =>
            {
                var row = (DataRowView)grid.Rows[grid.ClickedRowIndex].DataBoundItem;
                ConfigureThresholds(GetInstanceID(row), GetJobId(row), control);
            };
            tsConfigInstance.Click += (_, _) =>
            {
                var row = (DataRowView)grid.Rows[grid.ClickedRowIndex].DataBoundItem;
                ConfigureThresholds(GetInstanceID(row), Guid.Empty, control);
            };
            tsConfigRoot.Click += (_, _) =>
            {
                ConfigureThresholds(-1, Guid.Empty, control);
            };
            tsJobInfo.Click += (_, _) =>
            {
                var row = (DataRowView)grid.Rows[grid.ClickedRowIndex].DataBoundItem;
                ShowJobInfoForm(row);
            };
            tsAcknowledge.Click += (_, _) =>
            {
                var row = (DataRowView)grid.Rows[grid.ClickedRowIndex].DataBoundItem;
                AcknowledgeRow(row, control);
            };
            strip.Opening += (_, _) =>
            {
                var row = (DataRowView)grid.Rows[grid.ClickedRowIndex].DataBoundItem;
                lblJobName.Text = GetJobName(row);
                lblJobName.Visible = !string.IsNullOrEmpty(lblJobName.Text);

                if (tsAcknowledge.Visible)
                {
                    var jobStatus = (DBADashStatusEnum)row["JobStatus"];
                    tsAcknowledge.Text =
                        jobStatus == DBADashStatusEnum.Acknowledged ? "Clear Acknowledgement" : "Acknowledge";
                    tsAcknowledge.Enabled = jobStatus is DBADashStatusEnum.Critical
                        or DBADashStatusEnum.Warning or DBADashStatusEnum.Acknowledged;
                }

                tsStartJob.Visible = DBADashUser.AllowJobExecution;
            };
            strip.Items.Insert(0, new ToolStripSeparator());
            strip.Items.Insert(0, tsJobActions);
        }

        internal static string GetJobName(DataRowView row)
        {
            if (row.Row.Table.Columns.Contains("name"))
                return (string)row["name"];
            if (row.Row.Table.Columns.Contains("JobName"))
                return (string)row["JobName"];
            if (row.Row.Table.Columns.Contains("job_name"))
                return (string)row["job_name"];
            return string.Empty;
        }

        internal static int GetInstanceID(DataRowView row) => (int)row["InstanceID"];

        internal static Guid GetJobId(DataRowView row) => (Guid)row["job_id"];

        private static void StartJob(DataRowView row)
        {
            RunJob(GetJobId(row), GetInstanceID(row), GetJobName(row));
        }

        internal static void ShowJobInfoForm(DataRowView row)
        {
            try
            {
                var jobContext = CommonData.GetDBADashContext(GetInstanceID(row));
                jobContext.Type = SQLTreeItem.TreeType.AgentJob;
                jobContext.JobID = GetJobId(row);
                jobContext.ObjectName = GetJobName(row);
                var frm = new JobInfoForm() { DBADashContext = jobContext };
                frm.ShowSingleInstance();
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex);
            }
        }

        private static void RunJob(Guid jobId, int _instanceId, string jobName)
        {
            var runJobDialog = new JobExecutionDialog
            {
                InstanceId = _instanceId,
                JobId = jobId,
                JobName = jobName,
            };
            runJobDialog.Show();
        }

        private static void AcknowledgeRow(DataRowView row, IRefreshData control)
        {
            try
            {
                var clear = (DBADashStatusEnum)row["JobStatus"] == DBADashStatusEnum.Acknowledged;
                AcknowledgeJobErrors(GetInstanceID(row), GetJobId(row), clear);
                control.RefreshData();
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex);
            }
        }

        #endregion Shared Context Menu

        #region Custom links

        private sealed class JobConfigureLink : LinkColumnInfo
        {
            public override void Navigate(DBADashContext context, DataGridViewRow row, int selectedTableIndex, ContainerControl sender)
            {
                if (sender is not AgentJobsView view) return;
                var instId = row.Cells["InstanceID"].Value as int? ?? -1;
                var jId = row.Cells["job_id"].Value is Guid g ? g : Guid.Empty;
                view.ConfigureThresholds(instId, jId);
            }
        }

        private sealed class JobNameLink : LinkColumnInfo
        {
            public override void Navigate(DBADashContext context, DataGridViewRow row, int selectedTableIndex, ContainerControl sender)
            {
                if (row.DataBoundItem is DataRowView drv)
                {
                    ShowJobInfoForm(drv);
                }
            }
        }

        private sealed class AcknowledgeLink : LinkColumnInfo
        {
            public override void Navigate(DBADashContext context, DataGridViewRow row, int selectedTableIndex, ContainerControl sender)
            {
                if (sender is not AgentJobsView view) return;
                if (row.DataBoundItem is DataRowView drv)
                {
                    AcknowledgeRow(drv, view);
                }
            }
        }

        #endregion Custom links

        #region Report definition

        private static CellHighlightingRuleSet StatusHighlight(string targetColumn) =>
            new(targetColumn, true) { IsStatusColumn = true };

        private static ColumnMetadata Hidden() => new() { Visible = false };

        public static SystemReport Instance => new()
        {
            ViewType = typeof(AgentJobsView),
            ReportName = "Agent Jobs",
            SchemaName = "dbo",
            ProcedureName = "AgentJobsReport_Get",
            QualifiedProcedureName = "dbo.AgentJobsReport_Get",
            CanEditReport = false,
            ShowStatusFilter = true,
            TriggerCollectionTypes = new List<string>
            {
                CollectionType.Jobs.ToString(),
                CollectionType.JobHistory.ToString(),
                CollectionType.RunningJobs.ToString()
            },
            CustomReportResults = new Dictionary<int, CustomReportResult>
            {
                [0] = new CustomReportResult
                {
                    ResultName = "Agent Jobs",
                    Columns = new Dictionary<string, ColumnMetadata>
                    {
                        // Unbound link columns first (matching old layout)
                        ["History"] = new() { Alias = "History", Link = new SystemDrillDownLinkColumnInfo
                        {
                            ReportFactory = () => JobHistoryReport.Instance,
                            DrillDownMode = DrillDownMode.ChildPanel,
                            ColumnToParameterMap = new Dictionary<string, string>
                            {
                                { "@InstanceID", "InstanceID" },
                                { "@JobID", "job_id" },
                            },
                            ChildPanelTitle = "{InstanceDisplayName} | {name}",
                        }, Description = "View job execution history" },
                        ["Acknowledge"] = new() { Alias = "Acknowledge", Link = new AcknowledgeLink(), Description = "Acknowledge or clear job errors" },
                        // Visible data columns
                        ["InstanceDisplayName"] = new() { Alias = "Instance" },
                        ["name"] = new() { Alias = "Job Name", Link = new JobNameLink(), Highlighting = StatusHighlight("JobStatus") },
                        ["enabled"] = new() { Alias = "Enabled" },
                        ["description"] = new() { Alias = "Description" },
                        ["LastFailed"] = new() { Alias = "Last Failed", Highlighting = StatusHighlight("LastFailStatus") },
                        ["IsLastFail"] = new() { Alias = "Is Last Fail?", Highlighting = StatusHighlight("LastFailStatus") },
                        ["TimeSinceLastFailed"] = new() { Alias = "Time Since Last Fail", Highlighting = StatusHighlight("TimeSinceLastFailureStatus") },
                        ["StepLastFailed"] = new() { Alias = "Step Last Failed" },
                        ["LastSucceeded"] = new() { Alias = "Last Succeeded" },
                        ["TimeSinceLastSucceeded"] = new() { Alias = "Time Since Last Succeeded", Highlighting = StatusHighlight("TimeSinceLastSucceededStatus") },
                        ["AckDate"] = new() { Alias = "Acknowledged Date" },
                        ["FailCount24Hrs"] = new() { Alias = "Fail Count (24Hrs)", Highlighting = StatusHighlight("FailCount24HrsStatus") },
                        ["SucceededCount24Hrs"] = new() { Alias = "Succeed Count (24Hrs)" },
                        ["FailCount7Days"] = new() { Alias = "Fail Count (7 Days)", Highlighting = StatusHighlight("FailCount7DaysStatus") },
                        ["SucceededCount7Days"] = new() { Alias = "Succeed Count (7 Days)" },
                        ["JobStepFails24Hrs"] = new() { Alias = "Job Step Fails (24Hrs)", Highlighting = StatusHighlight("JobStepFail24HrsStatus") },
                        ["JobStepFails7Days"] = new() { Alias = "Job Step Fails (7 Days)", Highlighting = StatusHighlight("JobStepFail7DaysStatus") },
                        ["MaxDurationSec"] = new() { Alias = "Max Duration (sec)" },
                        ["MaxDuration"] = new() { Alias = "Max Duration" },
                        ["AvgDurationSec"] = new() { Alias = "Avg Duration (sec)" },
                        ["AvgDuration"] = new() { Alias = "Avg Duration" },
                        ["ConfiguredLevel"] = new() { Alias = "Configured Level" },
                        ["Configure"] = new() { Alias = "Configure", Link = new JobConfigureLink(), Description = "Configure job thresholds",
                            Highlighting = new CellHighlightingRuleSet("ConfiguredLevel", true)
                            {
                                Rules = new List<CellHighlightingRule>
                                {
                                    new() { ConditionType = CellHighlightingRule.ConditionTypes.Equals, Value1 = "Job", Font = new Font("Segoe UI", 9F, FontStyle.Bold) },
                                }
                            }
                        },
                        // Hidden status columns used for highlighting
                        ["InstanceID"] = Hidden(),
                        ["job_id"] = Hidden(),
                        ["JobStatus"] = Hidden(),
                        ["LastFailStatus"] = Hidden(),
                        ["TimeSinceLastFailureStatus"] = Hidden(),
                        ["TimeSinceLastSucceededStatus"] = Hidden(),
                        ["FailCount24HrsStatus"] = Hidden(),
                        ["FailCount7DaysStatus"] = Hidden(),
                        ["JobStepFail24HrsStatus"] = Hidden(),
                        ["JobStepFail7DaysStatus"] = Hidden(),
                        ["Instance"] = Hidden(),
                        // Hidden threshold/config columns
                        ["TimeSinceLastFailureWarning"] = Hidden(),
                        ["TimeSinceLastFailureCritical"] = Hidden(),
                        ["TimeSinceLastSucceededWarning"] = Hidden(),
                        ["TimeSinceLastSucceededCritical"] = Hidden(),
                        ["FailCount24HrsWarning"] = Hidden(),
                        ["FailCount24HrsCritical"] = Hidden(),
                        ["FailCount7DaysCritical"] = Hidden(),
                        ["FailCount7DaysWarning"] = Hidden(),
                        ["JobStepFails24HrsWarning"] = Hidden(),
                        ["JobStepFails24HrsCritical"] = Hidden(),
                        ["JobStepFails7DaysWarning"] = Hidden(),
                        ["JobStepFails7DaysCritical"] = Hidden(),
                        ["LastFailIsCritical"] = Hidden(),
                        ["LastFailIsWarning"] = Hidden(),
                        ["AckStatus"] = Hidden(),
                    }
                }.SetDisplayIndexBasedOnColumnOrder()
            },
            Params = new Params
            {
                ParamList = new List<Param>
                {
                    new() { ParamName = "@InstanceIDs", ParamType = "IDS" },
                    new() { ParamName = "@IncludeCritical", ParamType = "BIT" },
                    new() { ParamName = "@IncludeWarning", ParamType = "BIT" },
                    new() { ParamName = "@IncludeNA", ParamType = "BIT" },
                    new() { ParamName = "@IncludeOK", ParamType = "BIT" },
                    new() { ParamName = "@IncludeACK", ParamType = "BIT" },
                    new() { ParamName = "@ShowHidden", ParamType = "BIT" },
                    new() { ParamName = "@JobID", ParamType = "UNIQUEIDENTIFIER" },
                }
            }
        };

        #endregion Report definition
    }
}
