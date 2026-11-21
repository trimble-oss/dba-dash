using DBADash;
using DBADashGUI.AgentJobs;
using DBADashGUI.CustomReports;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using static DBADashGUI.DBADashStatus;

namespace DBADashGUI.ElasticJobs
{
    /// <summary>
    /// Elastic jobs with their status, from the job database of an elastic job agent - the counterpart of <see cref="AgentJobsView"/>.
    ///
    /// <para>Status uses the SQL Agent job thresholds and exclusions (dbo.AgentJobThresholds and dbo.AgentJobExclusions): set for the
    /// job, for the instance - the agent's job database - or at root level, which also applies to SQL Agent jobs.  So the same dialogs
    /// configure them.</para>
    /// </summary>
    internal class ElasticJobsView : CustomReportView
    {
        private ToolStripMenuItem mnuThresholdInstance;
        private ToolStripMenuItem mnuExclusionsInstance;

        public ElasticJobsView()
        {
            Report = Instance;
            PreventReportOverwrite = true;
            AddToolbarButtons();
        }

        #region Toolbar

        private void AddToolbarButtons()
        {
            var tsConfigure = new ToolStripDropDownButton("Configure")
            {
                Name = "tsConfigure",
                ToolTipText = "Configure job thresholds, exclusions & acknowledge errors.  Shared with SQL Agent jobs.",
                Image = Properties.Resources.SettingsOutline_16x,
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText
            };

            var mnuThresholdRoot = new ToolStripMenuItem("Configure Root Thresholds") { ToolTipText = "Root thresholds also apply to SQL Agent jobs" };
            mnuThresholdRoot.Click += (_, _) => ConfigureThresholds(-1, Guid.Empty);
            mnuThresholdInstance = new ToolStripMenuItem("Configure Instance Thresholds") { ToolTipText = "Thresholds for the elastic jobs of this job database" };
            mnuThresholdInstance.Click += (_, _) =>
            {
                if (TryGetSingleInstanceId(out var id)) ConfigureThresholds(id, Guid.Empty);
            };
            var mnuExclusionsRoot = new ToolStripMenuItem("Configure Root Exclusions") { ToolTipText = "Root exclusions also apply to SQL Agent jobs" };
            mnuExclusionsRoot.Click += (_, _) => ConfigureExclusions(-1);
            mnuExclusionsInstance = new ToolStripMenuItem("Configure Instance Exclusions");
            mnuExclusionsInstance.Click += (_, _) =>
            {
                if (TryGetSingleInstanceId(out var id)) ConfigureExclusions(id);
            };
            var mnuAcknowledgeErrors = new ToolStripMenuItem("Acknowledge Errors");
            mnuAcknowledgeErrors.Click += AcknowledgeErrors_Click;
            tsConfigure.DropDownItems.AddRange(new ToolStripItem[] { mnuThresholdInstance, mnuThresholdRoot, mnuExclusionsInstance, mnuExclusionsRoot, mnuAcknowledgeErrors });

            var insertAt = ToolStrip.Items.IndexOfKey("tsParams");
            insertAt = insertAt >= 0 ? insertAt + 1 : ToolStrip.Items.Count;
            ToolStrip.Items.Insert(insertAt, tsConfigure);
        }

        private bool TryGetSingleInstanceId(out int id)
        {
            id = -1;
            if (context == null) return false;
            if (context.InstanceID > 0) { id = context.InstanceID; return true; }
            if (context.InstanceIDs.Count == 1) { id = context.InstanceIDs.First(); return true; }
            return false;
        }

        #endregion Toolbar

        #region Configuration

        internal void ConfigureThresholds(int instanceID, Guid jobID)
        {
            AgentJobsView.ConfigureThresholds(instanceID, jobID, this);
        }

        private void ConfigureExclusions(int instanceID)
        {
            var levelName = instanceID == -1 ? "{Root}" : CommonData.GetDBADashContext(instanceID).InstanceName;
            using var frm = new AgentJobExclusionsConfig
            {
                InstanceID = instanceID,
                ConnectionString = Common.ConnectionString,
                LevelName = levelName
            };
            frm.ShowDialog();
            if (frm.DialogResult == DialogResult.OK)
            {
                RefreshData();
            }
        }

        private void AcknowledgeErrors_Click(object sender, EventArgs e)
        {
            var grid = Grids.FirstOrDefault();
            if (grid?.DataSource is not DataView { Table: not null } dv) return;
            var warningsAndFailures = dv.Table.Select("JobStatus IN(1,2)");
            if (warningsAndFailures.Length == 0)
            {
                MessageBox.Show("No warnings/failures to acknowledge", "Acknowledge Failures", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else if (MessageBox.Show($"Are you sure you want to acknowledge {warningsAndFailures.Length} job failure(s)?", "Acknowledge Failures",
                         MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                foreach (var row in warningsAndFailures)
                {
                    AcknowledgeJobErrors((int)row["InstanceID"], (Guid)row["job_id"], false);
                }
                RefreshData();
            }
        }

        private static void AcknowledgeJobErrors(int instanceID, Guid jobId, bool clear)
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.ElasticJobErrorAck", cn) { CommandType = CommandType.StoredProcedure };
            cn.Open();
            cmd.Parameters.AddWithValue("InstanceID", instanceID);
            cmd.Parameters.AddGuidIfNotEmpty("job_id", jobId);
            cmd.Parameters.AddWithValue("Clear", clear);
            cmd.ExecuteNonQuery();
        }

        #endregion Configuration

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
                if (subscribedGrids.Add(grid))
                {
                    grid.RowsAdded += JobsGrid_RowsAdded;
                }
                JobsGrid_RowsAdded(grid, new DataGridViewRowsAddedEventArgs(0, grid.RowCount));
            }

            mnuThresholdInstance.Enabled = TryGetSingleInstanceId(out _);
            mnuExclusionsInstance.Enabled = TryGetSingleInstanceId(out _);
        }

        private readonly HashSet<DBADashDataGridView> subscribedGrids = new();

        /// <summary>The Acknowledge link says what it will do - acknowledge a warning/failure, or clear an acknowledgement</summary>
        private static void JobsGrid_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            if (sender is not DBADashDataGridView grid) return;
            var ackCol = grid.Columns["Acknowledge"];
            if (ackCol == null) return;
            for (var idx = e.RowIndex; idx < e.RowIndex + e.RowCount && idx < grid.RowCount; idx++)
            {
                if (grid.Rows[idx].DataBoundItem is not DataRowView drv) continue;
                if (!drv.Row.Table.Columns.Contains("JobStatus")) continue;
                grid.Rows[idx].Cells[ackCol.Index].Value = (DBADashStatusEnum)drv["JobStatus"] switch
                {
                    DBADashStatusEnum.Critical or DBADashStatusEnum.Warning => "Acknowledge",
                    DBADashStatusEnum.Acknowledged => "Clear",
                    _ => "",
                };
            }
        }

        #endregion Grid post-processing

        #region Custom links

        private sealed class JobConfigureLink : LinkColumnInfo
        {
            public override void Navigate(DBADashContext context, DataGridViewRow row, int selectedTableIndex, ContainerControl sender)
            {
                if (sender is not ElasticJobsView view) return;
                var instanceId = row.Cells["InstanceID"].Value as int? ?? -1;
                var jobId = row.Cells["job_id"].Value is Guid g ? g : Guid.Empty;
                view.ConfigureThresholds(instanceId, jobId);
            }
        }

        private sealed class AcknowledgeLink : LinkColumnInfo
        {
            public override void Navigate(DBADashContext context, DataGridViewRow row, int selectedTableIndex, ContainerControl sender)
            {
                if (sender is not ElasticJobsView view || row.DataBoundItem is not DataRowView drv) return;
                var status = (DBADashStatusEnum)drv["JobStatus"];
                if (status is not (DBADashStatusEnum.Critical or DBADashStatusEnum.Warning or DBADashStatusEnum.Acknowledged)) return;
                try
                {
                    AcknowledgeJobErrors((int)drv["InstanceID"], (Guid)drv["job_id"], status == DBADashStatusEnum.Acknowledged);
                    view.RefreshData();
                }
                catch (Exception ex)
                {
                    CommonShared.ShowExceptionDialog(ex);
                }
            }
        }

        private sealed class MessageLink : LinkColumnInfo
        {
            public override void Navigate(DBADashContext context, DataGridViewRow row, int selectedTableIndex, ContainerControl sender)
            {
                if (row.DataBoundItem is not DataRowView drv) return;
                Convert.ToString(drv["LastMessage"]).OpenAsTextFile();
            }
        }

        #endregion Custom links

        #region Report definition

        private static CellHighlightingRuleSet StatusHighlight(string targetColumn) =>
            new(targetColumn, true) { IsStatusColumn = true };

        private static ColumnMetadata Hidden() => new() { Visible = false };

        private static Dictionary<string, string> JobParameters => new()
        {
            { "@InstanceID", "InstanceID" },
            { "@JobID", "job_id" },
        };

        public static SystemReport Instance => new()
        {
            ViewType = typeof(ElasticJobsView),
            ReportName = "Elastic Jobs",
            Description = "Elastic jobs of an elastic job agent, with their status.  Thresholds are shared with SQL Agent jobs.",
            SchemaName = "dbo",
            ProcedureName = "ElasticJobsReport_Get",
            QualifiedProcedureName = "dbo.ElasticJobsReport_Get",
            CanEditReport = false,
            ShowStatusFilter = true,
            TriggerCollectionTypes = new List<string>
            {
                CollectionType.ElasticJobs.ToString()
            },
            CustomReportResults = new Dictionary<int, CustomReportResult>
            {
                [0] = new CustomReportResult
                {
                    ResultName = "Elastic Jobs",
                    Columns = new Dictionary<string, ColumnMetadata>
                    {
                        ["History"] = new()
                        {
                            Alias = "History",
                            Link = new SystemDrillDownLinkColumnInfo
                            {
                                ReportFactory = () => ElasticJobHistoryReport.Instance,
                                DrillDownMode = DrillDownMode.ChildPanel,
                                ColumnToParameterMap = JobParameters,
                                ChildPanelTitle = "{InstanceDisplayName} | {name}",
                            },
                            Description = "View job execution history"
                        },
                        ["Steps"] = new()
                        {
                            Alias = "Steps",
                            Link = new SystemDrillDownLinkColumnInfo
                            {
                                ReportFactory = () => ElasticJobStepsReport.Instance,
                                DrillDownMode = DrillDownMode.ChildPanel,
                                ColumnToParameterMap = JobParameters,
                                ChildPanelTitle = "{InstanceDisplayName} | {name} | Steps",
                            },
                            Description = "View the job's steps and the members of the target groups they run against"
                        },
                        ["Acknowledge"] = new() { Alias = "Acknowledge", Link = new AcknowledgeLink(), Description = "Acknowledge or clear job errors" },
                        ["InstanceDisplayName"] = new() { Alias = "Job Database" },
                        ["name"] = new() { Alias = "Job Name", Highlighting = StatusHighlight("JobStatus") },
                        ["enabled"] = new() { Alias = "Enabled" },
                        ["description"] = new() { Alias = "Description" },
                        ["Schedule"] = new() { Description = "How often the job runs, from its schedule start time" },
                        ["schedule_start_time"] = new() { Alias = "Schedule Start", Visible = false },
                        ["schedule_end_time"] = new() { Alias = "Schedule End", Visible = false },
                        ["IsRunning"] = new() { Alias = "Running", Description = "The job's most recent execution is still active" },
                        ["LastLifecycle"] = new() { Alias = "Last Outcome", Description = "Lifecycle of the job's most recent execution" },
                        ["LastExecutionStart"] = new() { Alias = "Last Execution" },
                        ["LastMessage"] = new() { Alias = "Last Message", Link = new MessageLink() },
                        ["LastFailed"] = new() { Alias = "Last Failed", Highlighting = StatusHighlight("LastFailStatus") },
                        ["IsLastFail"] = new() { Alias = "Is Last Fail?", Highlighting = StatusHighlight("LastFailStatus") },
                        ["TimeSinceLastFailed"] = new() { Alias = "Time Since Last Fail", Highlighting = StatusHighlight("TimeSinceLastFailureStatus") },
                        ["StepLastFailed"] = new() { Alias = "Step Last Failed", Description = "Last time a step failed against one of its targets" },
                        ["LastSucceeded"] = new() { Alias = "Last Succeeded" },
                        ["TimeSinceLastSucceeded"] = new() { Alias = "Time Since Last Succeeded", Highlighting = StatusHighlight("TimeSinceLastSucceededStatus") },
                        ["AckDate"] = new() { Alias = "Acknowledged Date" },
                        ["FailCount24Hrs"] = new() { Alias = "Fail Count (24Hrs)", Highlighting = StatusHighlight("FailCount24HrsStatus") },
                        ["SucceededCount24Hrs"] = new() { Alias = "Succeed Count (24Hrs)" },
                        ["FailCount7Days"] = new() { Alias = "Fail Count (7 Days)", Highlighting = StatusHighlight("FailCount7DaysStatus") },
                        ["SucceededCount7Days"] = new() { Alias = "Succeed Count (7 Days)" },
                        ["JobStepFails24Hrs"] = new() { Alias = "Target Fails (24Hrs)", Description = "Executions of the job's steps against their targets that failed", Highlighting = StatusHighlight("JobStepFail24HrsStatus") },
                        ["JobStepFails7Days"] = new() { Alias = "Target Fails (7 Days)", Description = "Executions of the job's steps against their targets that failed", Highlighting = StatusHighlight("JobStepFail7DaysStatus") },
                        ["SkippedCount7Days"] = new() { Alias = "Target Skips (7 Days)", Description = "Executions of the job's steps that were skipped because the previous execution was still running against the target" },
                        ["RetryCount7Days"] = new() { Alias = "Retries (7 Days)", Description = "Retries of the job's steps against their targets" },
                        ["MaxDurationSec"] = new() { Alias = "Max Duration (sec)", Visible = false },
                        ["MaxDuration"] = new() { Alias = "Max Duration" },
                        ["AvgDurationSec"] = new() { Alias = "Avg Duration (sec)", Visible = false },
                        ["AvgDuration"] = new() { Alias = "Avg Duration" },
                        ["ConfiguredLevel"] = new() { Alias = "Configured Level" },
                        ["Configure"] = new()
                        {
                            Alias = "Configure",
                            Link = new JobConfigureLink(),
                            Description = "Configure job thresholds",
                            Highlighting = new CellHighlightingRuleSet("ConfiguredLevel", true)
                            {
                                Rules = new List<CellHighlightingRule>
                                {
                                    new() { ConditionType = CellHighlightingRule.ConditionTypes.Equals, Value1 = "Job", Font = new Font("Segoe UI", 9F, FontStyle.Bold) },
                                }
                            }
                        },
                        ["IsExcluded"] = Hidden(),
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
                }
            }
        };

        #endregion Report definition
    }
}
