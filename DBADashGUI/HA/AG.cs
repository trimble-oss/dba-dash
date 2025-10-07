using DBADash;
using DBADashGUI.CustomReports;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using static DBADashGUI.CustomReports.CellHighlightingRule;
using static DBADashGUI.DBADashStatus;

namespace DBADashGUI.HA
{
    public partial class AG : UserControl, ISetContext, INavigation, IRefreshData
    {
        #region "Report Definitions"

        private SystemReport AGSummaryReport = new()
        {
            SchemaName = "dbo",
            ProcedureName = "AvailabilityGroupSummary_Get",
            QualifiedProcedureName = "dbo.AvailabilityGroupSummary_Get",
            ReportName = "Availability Group Summary",
            Params = new Params
            {
                ParamList = new List<Param>
                {
                    new()
                    {
                        ParamName = "@InstanceIDs",
                        ParamType = "IDS"
                    }
                }
            },
            CustomReportResults = new Dictionary<int, CustomReportResult>
            {
                {
                    0,
                    new CustomReportResult
                    {
                        ResultName = "Availability Group Summary",
                        Columns = new Dictionary<string, ColumnMetadata>
                        {
                            { "InstanceID", new ColumnMetadata { Visible = false } },
                            { "Instance", new ColumnMetadata {
                                Link = new DrillDownLinkColumnInfo()
                            }},
                            { "Primary Replicas", new ColumnMetadata {
                                Alias = "Primary\nReplicas"
                            }},
                            { "Secondary Replicas", new ColumnMetadata {
                                Alias = "Secondary\nReplicas"
                            }},
                            { "Readable Secondaries", new ColumnMetadata {
                                Alias = "Readable\nSecondaries"
                            }},
                            { "Async Commit", new ColumnMetadata {
                                Alias = "Async\nCommit"
                            }},
                            { "Sync Commit", new ColumnMetadata {
                                Alias = "Sync\nCommit"
                            }},
                            { "Not Synchronizing", new ColumnMetadata {
                                Alias = "Not\nSynchronizing",
                                Highlighting = new CellHighlightingRuleSet
                                {
                                    Rules = new List<CellHighlightingRule>
                                    {
                                        new()
                                        {
                                            Status = DBADashStatusEnum.Critical,
                                            Value1 = "1",
                                            ConditionType = ConditionTypes.GreaterThan
                                        },
                                        new()
                                        {
                                            Status = DBADashStatusEnum.OK,
                                            Value1 = "0"
                                        }
                                    },
                                    TargetColumn = "Not Synchronizing"
                                }
                            }},
                            { "Synchronizing", new ColumnMetadata() },
                            { "Synchronized", new ColumnMetadata {
                                Highlighting = new CellHighlightingRuleSet
                                {
                                    IsStatusColumn = true,
                                    TargetColumn = "Synchronized Status"
                                }
                            }},
                            { "Reverting", new ColumnMetadata {
                                Highlighting = new CellHighlightingRuleSet
                                {
                                    Rules = new List<CellHighlightingRule>
                                    {
                                        new()
                                        {
                                            Status = DBADashStatusEnum.Critical,
                                            Value1 = "0",
                                            ConditionType = ConditionTypes.GreaterThan
                                        },
                                        new()
                                        {
                                            Status = DBADashStatusEnum.OK,
                                            Value1 = "0"
                                        }
                                    },
                                    TargetColumn = "Reverting"
                                }
                            }},
                            { "Initializing", new ColumnMetadata {
                                Highlighting = new CellHighlightingRuleSet
                                {
                                    Rules = new List<CellHighlightingRule>
                                    {
                                        new()
                                        {
                                            Status = DBADashStatusEnum.Critical,
                                            Value1 = "0",
                                            ConditionType = ConditionTypes.GreaterThan
                                        },
                                        new()
                                        {
                                            Status = DBADashStatusEnum.OK,
                                            Value1 = "0"
                                        }
                                    },
                                    TargetColumn = "Initializing"
                                }
                            }},
                            { "Remote Not Synchronizing", new ColumnMetadata {
                                Alias = "Remote\nNot\nSynchronizing",
                                Highlighting = new CellHighlightingRuleSet
                                {
                                    Rules = new List<CellHighlightingRule>
                                    {
                                        new()
                                        {
                                            Status = DBADashStatusEnum.Critical,
                                            Value1 = "0",
                                            ConditionType = ConditionTypes.GreaterThan
                                        },
                                        new()
                                        {
                                            Status = DBADashStatusEnum.OK,
                                            Value1 = "0"
                                        }
                                    },
                                    TargetColumn = "Remote Not Synchronizing"
                                }
                            }},
                            { "Remote Synchronizing", new ColumnMetadata {
                                Alias = "Remote\nSynchronizing"
                            }},
                            { "Remote Synchronized", new ColumnMetadata {
                                Alias = "Remote\nSynchronized"
                            }},
                            { "Remote Reverting", new ColumnMetadata {
                                Alias = "Remote\nReverting",
                                Highlighting = new CellHighlightingRuleSet
                                {
                                    Rules = new List<CellHighlightingRule>
                                    {
                                        new()
                                        {
                                            Status = DBADashStatusEnum.Critical,
                                            Value1 = "0",
                                            ConditionType = ConditionTypes.GreaterThan
                                        },
                                        new()
                                        {
                                            Status = DBADashStatusEnum.OK,
                                            Value1 = "0"
                                        }
                                    },
                                    TargetColumn = "Remote Reverting"
                                }
                            }},
                            { "Remote Initializing", new ColumnMetadata {
                                Alias = "Remote\nInitializing",
                                Highlighting = new CellHighlightingRuleSet
                                {
                                    Rules = new List<CellHighlightingRule>
                                    {
                                        new()
                                        {
                                            Status = DBADashStatusEnum.Critical,
                                            Value1 = "0",
                                            ConditionType = ConditionTypes.GreaterThan
                                        },
                                        new()
                                        {
                                            Status = DBADashStatusEnum.OK,
                                            Value1 = "0"
                                        }
                                    },
                                    TargetColumn = "Remote Initializing"
                                }
                            }},
                            { "Sync Health", new ColumnMetadata {
                                Alias = "Sync\nHealth",
                                Highlighting = new CellHighlightingRuleSet
                                {
                                    Rules = new List<CellHighlightingRule>
                                    {
                                        new()
                                        {
                                            Status = DBADashStatusEnum.OK,
                                            Value1 = "HEALTHY"
                                        },
                                        new()
                                        {
                                            Status = DBADashStatusEnum.Warning,
                                            Value1 = "PARTIALLY_HEALTHY"
                                        },
                                        new()
                                        {
                                            Status = DBADashStatusEnum.Critical,
                                            ConditionType = ConditionTypes.All
                                        }
                                    },
                                    TargetColumn = "Sync Health"
                                }
                            }},
                            { "Snapshot Date", new ColumnMetadata {
                                Alias = "Snapshot\nDate",
                                Highlighting = new CellHighlightingRuleSet
                                {
                                    IsStatusColumn = true,
                                    TargetColumn = "Snapshot Status"
                                }
                            }},
                            { "Snapshot Status", new ColumnMetadata {
                                Visible = false,
                                Highlighting = new CellHighlightingRuleSet
                                {
                                    IsStatusColumn = true,
                                    TargetColumn = "Snapshot Status"
                                }
                            }},
                            { "Snapshot Age", new ColumnMetadata {
                                Alias = "Snapshot\nAge",
                                Highlighting = new CellHighlightingRuleSet
                                {
                                    IsStatusColumn = true,
                                    TargetColumn = "Snapshot Status"
                                }
                            }},
                            { "Synchronized Status", new ColumnMetadata {
                                Alias = "Synchronized\nStatus",
                                Visible = false,
                                Highlighting = new CellHighlightingRuleSet
                                {
                                    IsStatusColumn = true,
                                    TargetColumn = "Synchronized Status"
                                }
                            }},
                            { "Max Secondary Lag (sec)", new ColumnMetadata {
                                Alias = "Max\nSecondary\nLag\n(sec)",
                                Visible = false
                            }},
                            { "Max Estimated Data Loss (sec)", new ColumnMetadata {
                                Alias = "Max\nEstimated\nData\nLoss\n(sec)",
                                Visible = false
                            }},
                            { "Max Estimated Recovery Time (sec)", new ColumnMetadata {
                                Alias = "Max\nEstimated\nRecovery\nTime (sec)",
                                Visible = false
                            }},
                            { "Max Secondary Lag", new ColumnMetadata {
                                Alias = "Max\nSecondary\nLag"
                            }},
                            { "Max Estimated Data Loss", new ColumnMetadata {
                                Alias = "Max\nEstimated\nData\nLoss"
                            }},
                            { "Max Estimated Recovery Time", new ColumnMetadata {
                                Alias = "Max\nEstimated\nRecovery\nTime"
                            }},
                            { "Total Redo Queue Size (KB)", new ColumnMetadata {
                                Alias = "Total\nRedo\nQueue\nSize\n(KB)",
                                Visible = false
                            }},
                            { "Avg Redo Queue Size (KB)", new ColumnMetadata {
                                Alias = "Avg\nRedo\nQueue\nSize\n(KB)"
                            }},
                            { "Total Log Send Queue Size (KB)", new ColumnMetadata {
                                Alias = "Total\nLog\nSend\nQueue\nSize\n(KB)",
                                Visible = false
                            }},
                            { "Avg Log Send Queue Size (KB)", new ColumnMetadata {
                                Alias = "Avg\nLog\nSend\nQueue\nSize\n(KB)"
                            }}
                        }
                    }
                }
            }
        };

        private SystemReport AGDetailReport = new()
        {
            SchemaName = "dbo",
            ProcedureName = "AvailabilityGroup_Get",
            QualifiedProcedureName = "dbo.AvailabilityGroup_Get",
            ReportName = "Availability Group Detail",
            TriggerCollectionTypes = new List<string>() { CollectionType.AvailabilityGroups.ToString(), CollectionType.AvailabilityReplicas.ToString(), CollectionType.DatabasesHADR.ToString() },
            Params = new Params
            {
                ParamList = new List<Param>
                {
                    new()
                    {
                        ParamName = "@InstanceID",
                        ParamType = "INT"
                    }
                }
            },
            CustomReportResults = new Dictionary<int, CustomReportResult>
            {
                {
                    0,
                    new CustomReportResult
                    {
                        ResultName = "Availability Group Detail",
                        Columns = new Dictionary<string, ColumnMetadata>
                        {
                            { "Database", new ColumnMetadata() },
                            { "Availability Group", new ColumnMetadata() },
                            { "Replica Server", new ColumnMetadata() },
                            { "Sync State", new ColumnMetadata {
                                Highlighting = new CellHighlightingRuleSet
                                {
                                    Rules = new List<CellHighlightingRule>
                                    {
                                        new()
                                        {
                                            Status = DBADashStatusEnum.OK,
                                            Value1 = "HEALTHY"
                                        },
                                        new()
                                        {
                                            Status = DBADashStatusEnum.Warning,
                                            Value1 = "PARTIALLY_HEALTHY"
                                        },
                                        new()
                                        {
                                            Status = DBADashStatusEnum.Critical,
                                            ConditionType = ConditionTypes.All
                                        }
                                    },
                                    TargetColumn = "Sync Health"
                                }
                            }},
                            { "Sync Health", new ColumnMetadata {
                                Highlighting = new CellHighlightingRuleSet
                                {
                                    Rules = new List<CellHighlightingRule>
                                    {
                                        new()
                                        {
                                            Status = DBADashStatusEnum.OK,
                                            Value1 = "HEALTHY"
                                        },
                                        new()
                                        {
                                            Status = DBADashStatusEnum.Warning,
                                            Value1 = "PARTIALLY_HEALTHY"
                                        },
                                        new()
                                        {
                                            Status = DBADashStatusEnum.Critical,
                                            ConditionType = ConditionTypes.All
                                        }
                                    },
                                    TargetColumn = "Sync Health"
                                }
                            }},
                            { "Suspend Reason", new ColumnMetadata() },
                            { "Database State", new ColumnMetadata() },
                            { "Is Local", new ColumnMetadata() },
                            { "Availability Mode", new ColumnMetadata() },
                            { "Failover Mode", new ColumnMetadata() },
                            { "Is Primary", new ColumnMetadata() },
                            { "Primary Connections", new ColumnMetadata() },
                            { "Secondary Connections", new ColumnMetadata() },
                            { "Snapshot Status", new ColumnMetadata {
                                Visible = false,
                                Highlighting = new CellHighlightingRuleSet
                                {
                                    TargetColumn = "Snapshot Status",
                                    IsStatusColumn = true
                                }
                            }},
                            { "Snapshot Age", new ColumnMetadata {
                                Highlighting = new CellHighlightingRuleSet
                                {
                                    TargetColumn = "Snapshot Status",
                                    IsStatusColumn = true
                                }
                            }},
                            { "Estimated Data Loss (sec)", new ColumnMetadata {
                                Visible = false,
                                FormatString = "N0"
                            }},
                            { "Estimated Recovery Time (sec)", new ColumnMetadata {
                                Visible = false
                            }},
                            { "Secondary Lag (sec)", new ColumnMetadata {
                                Visible = false,
                                FormatString = "N0"
                            }},
                            { "Estimated Data Loss", new ColumnMetadata {
                                FormatString = "N0"
                            }},
                            { "Estimated Recovery Time", new ColumnMetadata {
                                FormatString = "N0"
                            }},
                            { "Secondary Lag", new ColumnMetadata {
                                FormatString = "N0"
                            }},
                            { "Log Send Queue Size (KB)", new ColumnMetadata {
                                FormatString = "N0"
                            }},
                            { "Log Send Rate (KB/s)", new ColumnMetadata {
                                Visible = false,
                                FormatString = "N0"
                            }},
                            { "Log Redo Queue Size (KB)", new ColumnMetadata {
                                FormatString = "N0"
                            }},
                            { "Log Redo Rate (KB/s)", new ColumnMetadata {
                                Visible = false,
                                FormatString = "N0"
                            }},
                            { "Last Sent Time", new ColumnMetadata {
                                Visible = false,
                                FormatString = "G"
                            }},
                            { "Last Received Time", new ColumnMetadata {
                                Visible = false,
                                FormatString = "G"
                            }},
                            { "Last Hardened Time", new ColumnMetadata {
                                Visible = false,
                                FormatString = "G"
                            }},
                            { "Last Redone Time", new ColumnMetadata {
                                Visible = false,
                                FormatString = "G"
                            }},
                            { "Last Commit Time", new ColumnMetadata {
                                Visible = false,
                                FormatString = "G"
                            }},
                            { "Snapshot Date", new ColumnMetadata {
                                Highlighting = new CellHighlightingRuleSet
                                {
                                    TargetColumn = "Snapshot Status",
                                    IsStatusColumn = true
                                }
                            }}
                        }
                    }
                }
            }
        };

        #endregion "Report Definitions"

        private readonly ToolStripMenuItem navigateBackMenuItem =
            new("Navigate Back", Properties.Resources.Previous_grey_16x)
            { DisplayStyle = ToolStripItemDisplayStyle.Image };

        private readonly ToolStripMenuItem metricsConfigMenuItem =
            new("Configure Metrics", Properties.Resources.SettingsOutline_16x)
            { DisplayStyle = ToolStripItemDisplayStyle.ImageAndText };

        private readonly ToolStripMenuItem metricsConfigRootMenuItem =
            new("Configure Metrics (Root)")
            { DisplayStyle = ToolStripItemDisplayStyle.Text };

        private readonly ToolStripMenuItem metricsConfigInstanceMenuItem =
            new("Configure Metrics (Instance)")
            { DisplayStyle = ToolStripItemDisplayStyle.Text };

        public AG()
        {
            InitializeComponent();
            metricsConfigMenuItem.DropDownItems.Add(metricsConfigRootMenuItem);
            metricsConfigMenuItem.DropDownItems.Add(metricsConfigInstanceMenuItem);
            customReportView1.PostGridRefresh += PostGridRefresh;
            customReportView1.ToolStrip.Items.Add(navigateBackMenuItem);
            customReportView1.ToolStrip.Items.Add(metricsConfigMenuItem);
            navigateBackMenuItem.Click += (sender, e) => NavigateBack();
            metricsConfigRootMenuItem.Click += (sender, e) => ConfigureMetrics(-1);
            metricsConfigInstanceMenuItem.Click += (sender, e) => ConfigureMetrics(ReportContext.InstanceID);
        }

        private static void ConfigureMetrics(int instanceId)
        {
            using var metricsConfig = new RepositoryMetricsConfig() { InstanceID = instanceId, MetricType = RepositoryMetricsConfig.RepositoryMetricTypes.AG };
            metricsConfig.ShowDialog();
        }

        private void PostGridRefresh(object sender, EventArgs e)
        {
            foreach (var grid in customReportView1.Grids)
            {
                grid.CellContentClick -= Dgv_CellContentClick;
                grid.CellContentClick += Dgv_CellContentClick;
            }
        }

        private List<int> InstanceIDs => CurrentContext?.RegularInstanceIDs.ToList();

        private DBADashContext CurrentContext;
        private DBADashContext ReportContext;

        public void SetContext(DBADashContext _context)
        {
            if (_context == CurrentContext) return;
            CurrentContext = _context;
            // Metrics config visible to App role and admin users.  Not visible to AppReadOnly role.
            metricsConfigMenuItem.Visible = DBADashUser.IsAdmin || DBADashUser.Roles.Contains("App");
            ResetAndRefreshData(_context);
        }

        /// <summary>
        /// Saves report layout when changing context (until app is restarted)
        /// </summary>
        private void SaveLayout()
        {
            if (customReportView1.Grids.Count <= 0) return;
            switch (customReportView1.Report?.ProcedureName)
            {
                case "AvailabilityGroup_Get":
                    AGDetailReport.CustomReportResults[0].ColumnLayout =
                        customReportView1.Grids[0].GetColumnLayout();
                    break;

                case "AvailabilityGroupSummary_Get":
                    AGSummaryReport.CustomReportResults[0].ColumnLayout =
                        customReportView1.Grids[0].GetColumnLayout();
                    break;
            }
        }

        public void ResetAndRefreshData(DBADashContext _context)
        {
            SaveLayout(); // Persist layout before changing context
            customReportView1.Report = _context.InstanceIDs.Count == 1 ? AGDetailReport : AGSummaryReport;
            customReportView1.SetContext(_context);
            navigateBackMenuItem.Enabled = _context != CurrentContext;
            ReportContext = _context;
            metricsConfigInstanceMenuItem.Enabled = _context.InstanceID > 0;
        }

        public void RefreshData()
        {
            customReportView1.RefreshData();
        }

        private void Dgv_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            var dgv = (DataGridView)sender;
            if (e.RowIndex >= 0 && dgv.Columns[e.ColumnIndex].Name == "Instance")
            {
                var row = (DataRowView)dgv.Rows[e.RowIndex].DataBoundItem;
                var instanceId = (int)row["InstanceID"];
                var tempContext = (DBADashContext)CurrentContext.Clone();
                tempContext.InstanceID = instanceId;
                tempContext.RegularInstanceIDsWithHidden = new HashSet<int> { instanceId };
                tempContext.AzureInstanceIDsWithHidden = new HashSet<int>();
                ResetAndRefreshData(tempContext);
            }
        }

        public bool NavigateBack()
        {
            if (CanNavigateBack)
            {
                ResetAndRefreshData(CurrentContext);
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool CanNavigateBack => navigateBackMenuItem.Enabled;
    }
}