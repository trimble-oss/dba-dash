using DBADash;
using DBADashGUI.CustomReports;
using DBADashGUI.Theme;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using UserControl = System.Windows.Forms.UserControl;

namespace DBADashGUI
{
    public partial class DeletedInstances : UserControl, ISetContext, IRefreshData
    {
        public event EventHandler<int> InstanceRestored;

        private bool IsDeleteInProgress;

        public DeletedInstances()
        {
            InitializeComponent();
            customReportView1.PostGridRefresh += OnPostGridRefresh;
        }

        protected void OnPostGridRefresh(object sender, EventArgs e)
        {
            if (customReportView1.Grids.Count == 0) return;
            var grid = customReportView1.Grids[0];
            if (!grid.Columns.Contains("colRestore"))
            {
                grid.Columns.Add(new DataGridViewLinkColumn()
                {
                    Name = "colRestore",
                    HeaderText = @"Restore",
                    Text = "Restore",
                    UseColumnTextForLinkValue = true,
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells
                });
                grid.Columns.Add(new DataGridViewLinkColumn()
                {
                    Name = "colDelete",
                    HeaderText = @"Delete",
                    Text = "Delete Now",
                    UseColumnTextForLinkValue = true,
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells
                });
                grid.CellContentClick -= Grid_CellContentClick;
                grid.CellContentClick += Grid_CellContentClick;
                grid.ApplyTheme();
            }
        }

        public void RefreshData()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(RefreshData));
                return;
            }

            customReportView1.RefreshData();
        }

        public void SetContext(DBADashContext _context)
        {
            if (_context == null) return;
            _context.Report = DeletedInstancesReport;
            customReportView1.SetContext(_context);

            SetStatus(CurrentStatus.Message, CurrentStatus.ToolTip, CurrentStatus.Color);
        }

        private void Grid_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var grid = (DBADashDataGridView)sender;
            var instanceID = (int)grid.Rows[e.RowIndex].Cells["InstanceID"].Value;
            var instanceName = (string)grid.Rows[e.RowIndex].Cells["InstanceDisplayName"].Value;
            switch (grid.Columns[e.ColumnIndex].Name)
            {
                case "colRestore":
                    try
                    {
                        SharedData.RestoreInstance(instanceID, Common.ConnectionString);
                        RefreshData();
                        OnInstanceRestored(instanceID);
                        SetStatus($"Instance {instanceName} restored.  Refresh the tree to see the instance.", string.Empty, DashColors.Success);
                    }
                    catch (Exception ex)
                    {
                        CommonShared.ShowExceptionDialog(ex);
                    }

                    break;

                case "colDelete":
                    {
                        if (IsDeleteInProgress)
                        {
                            MessageBox.Show(@"Please wait for the current delete operation to complete before starting another.", $@"Delete {instanceName}", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                        var lastCollectionMins = (int?)grid.Rows[e.RowIndex].Cells["LastCollectionMins"].Value.DBNullToNull();
                        if (lastCollectionMins <= 1440)
                        {
                            MessageBox.Show(@"Please wait at least 1 day before deleting this instance.", $@"Delete {instanceName}",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }

                        if (MessageBox.Show(
                                @"Warning:This process can't be undone and might take a while to complete.
If you don't need to delete the instance immediately, the data retention settings will eventually remove most of the data associated with the deleted instance by truncating old partitions.

Are you sure you want to delete this instance?",
                                $@"Delete {instanceName}", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) != DialogResult.Yes) return;

                        Task.Run(() => HardDeleteInstance(instanceID, instanceName));
                        break;
                    }
            }
        }

        private (string Message, string ToolTip, Color Color) CurrentStatus;

        private void SetStatus(string message, string tooltip, Color color)
        {
            CurrentStatus = new(message, tooltip, color);
            customReportView1.StatusLabel.InvokeSetStatus(message ?? string.Empty, tooltip ?? string.Empty, color);
        }

        private void HardDeleteInstance(int instanceID, string instanceName)
        {
            IsDeleteInProgress = true;
            try
            {
                SetStatus($"Deleting instance {instanceName}...", string.Empty,
                    DashColors.Information);
                SharedData.HardDeleteInstance(instanceID, Common.ConnectionString);
                SetStatus($"Instance {instanceName} deleted", string.Empty,
                    DashColors.Success);
            }
            catch (Exception ex)
            {
                SetStatus(ex.Message, ex.ToString(), DashColors.Fail);
            }

            RefreshData();
            IsDeleteInProgress = false;
        }

        protected virtual void OnInstanceRestored(int instanceID)
        {
            InstanceRestored?.Invoke(this, instanceID);
        }

        public static SystemReport DeletedInstancesReport => new()
        {
            ReportName = "DeletedInstances",
            SchemaName = "dbo",
            ProcedureName = "DeletedInstances_Get",
            QualifiedProcedureName = "dbo.DeletedInstances_Get",
            CanEditReport = false,
            ForceRefreshWithoutContextChange = true,
            CustomReportResults = new Dictionary<int, CustomReportResult>
            {
                {
                    0, new CustomReportResult
                    {
                        ColumnAlias = new Dictionary<string, string>
                        {
                            { "InstanceID", "Instance ID" },
                            { "ConnectionID", "Connection ID" },
                            { "InstanceDisplayName", "Instance" },
                            { "SnapshotDate", "Last Collection Date" },
                            { "LastCollection", "Time Since Last Collection" },
                            { "LastCollectionMins", "Time Since Last Collection (Mins)" },
                            { "ScheduledDeletion", "Scheduled Deletion" },
                            { "ScheduledDeletionDays", "Scheduled Deletion (Days)" },
                            { "HardDeleteThresholdDays", "Hard Delete Threshold (Days)" },
                        },
                         ColumnLayout = new List<KeyValuePair<string, PersistedColumnLayout>>()
                            {
                                new ("InstanceID", new PersistedColumnLayout { Visible = false }),
                                new("ConnectionID", new PersistedColumnLayout { Visible = true }),
                                new("InstanceDisplayName", new PersistedColumnLayout {  Visible = true }),
                                new("SnapshotDate", new PersistedColumnLayout {  Visible = true }),
                                new("LastCollection", new PersistedColumnLayout {  Visible = true }),
                                new("LastCollectionMins", new PersistedColumnLayout {  Visible = false }),
                                new("ScheduledDeletion", new PersistedColumnLayout {  Visible = true }),
                                new("ScheduledDeletionDays", new PersistedColumnLayout {  Visible = true }),
                                new("HardDeleteThresholdDays", new PersistedColumnLayout {  Visible = true }),
                                new("colRestore", new PersistedColumnLayout { Visible = true }),
                                new("colDelete", new PersistedColumnLayout {  Visible = true }),
                            },
                        CellHighlightingRules =
                        {
                            {
                                "SnapshotDate",
                                new CellHighlightingRuleSet("LastCollectionMins")
                                {
                                    Rules = new List<CellHighlightingRule>
                                    {
                                        new()
                                        {
                                            ConditionType = CellHighlightingRule.ConditionTypes.LessThan,
                                            Value1 = "10",
                                            Status = DBADashStatus.DBADashStatusEnum.Critical
                                        },
                                        new()
                                        {
                                            ConditionType = CellHighlightingRule.ConditionTypes.LessThan,
                                            Value1 = "1440",
                                            Status = DBADashStatus.DBADashStatusEnum.Warning
                                        }
                                    }
                                }
                            },
                            {
                                "LastCollection",
                                new CellHighlightingRuleSet("LastCollectionMins")
                                {
                                    Rules = new List<CellHighlightingRule>
                                    {
                                        new()
                                        {
                                            ConditionType = CellHighlightingRule.ConditionTypes.LessThan,
                                            Value1 = "10",
                                            Status = DBADashStatus.DBADashStatusEnum.Critical
                                        },
                                        new()
                                        {
                                            ConditionType = CellHighlightingRule.ConditionTypes.LessThan,
                                            Value1 = "1440",
                                            Status = DBADashStatus.DBADashStatusEnum.Warning
                                        }
                                    }
                                }
                            },
                        }
                    }
                }
            }
        };
    }
}