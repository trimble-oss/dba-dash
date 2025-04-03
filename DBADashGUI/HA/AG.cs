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
                        ColumnAlias = new Dictionary<string, string>
                        {
                            {
                                "Primary Replicas",
                                "Primary\nReplicas"
                            },
                            {
                                "Secondary Replicas",
                                "Secondary\nReplicas"
                            },
                            {
                                "Readable Secondaries",
                                "Readable\nSecondaries"
                            },
                            {
                                "Async Commit",
                                "Async\nCommit"
                            },
                            {
                                "Sync Commit",
                                "Sync\nCommit"
                            },
                            {
                                "Not Synchronizing",
                                "Not\nSynchronizing"
                            },
                            {
                                "Remote Not Synchronizing",
                                "Remote\nNot\nSynchronizing"
                            },
                            {
                                "Remote Synchronizing",
                                "Remote\nSynchronizing"
                            },
                            {
                                "Remote Synchronized",
                                "Remote\nSynchronized"
                            },
                            {
                                "Remote Reverting",
                                "Remote\nReverting"
                            },
                            {
                                "Remote Initializing",
                                "Remote\nInitializing"
                            },
                            {
                                "Sync Health",
                                "Sync\nHealth"
                            },
                            {
                                "Max Secondary Lag (sec)",
                                "Max\nSecondary\nLag\n(sec)"
                            },
                            {
                                "Max Secondary Lag",
                                "Max\nSecondary\nLag"
                            },
                            {
                                "Max Estimated Data Loss (sec)",
                                "Max\nEstimated\nData\nLoss\n(sec)"
                            },
                            {
                                "Max Estimated Data Loss",
                                "Max\nEstimated\nData\nLoss"
                            },
                            {
                                "Max Estimated Recovery Time (sec)",
                                "Max\nEstimated\nRecovery\nTime (sec)"
                            },
                            {
                                "Max Estimated Recovery Time",
                                "Max\nEstimated\nRecovery\nTime"
                            },
                            {
                                "Total Redo Queue Size (KB)",
                                "Total\nRedo\nQueue\nSize\n(KB)"
                            },
                            {
                                "Avg Redo Queue Size (KB)",
                                "Avg\nRedo\nQueue\nSize\n(KB)"
                            },
                            {
                                "Total Log Send Queue Size (KB)",
                                "Total\nLog\nSend\nQueue\nSize\n(KB)"
                            },
                            {
                                "Avg Log Send Queue Size (KB)",
                                "Avg\nLog\nSend\nQueue\nSize\n(KB)"
                            },
                            {
                                "Snapshot Date",
                                "Snapshot\nDate"
                            },
                            {
                                "Snapshot Age",
                                "Snapshot\nAge"
                            },
                            {
                                "Synchronized Status",
                                "Synchronized\nStatus"
                            }
                        },
                        ColumnLayout = new List<KeyValuePair<string, PersistedColumnLayout>>
                        {
                            new("InstanceID", new PersistedColumnLayout(){Visible = false}),
                            new("Instance", new PersistedColumnLayout() { Visible = true }) ,
                            new("Primary Replicas", new PersistedColumnLayout(){ Visible = true }),
                            new("Secondary Replicas", new PersistedColumnLayout(){ Visible = true }),
                            new("Readable Secondaries", new PersistedColumnLayout(){ Visible = true }),
                            new("Async Commit", new PersistedColumnLayout(){ Visible = true }),
                            new("Sync Commit", new PersistedColumnLayout(){ Visible = true }),
                            new("Not Synchronizing", new PersistedColumnLayout(){ Visible = true }),
                            new("Synchronizing", new PersistedColumnLayout(){ Visible = true }),
                            new("Synchronized", new PersistedColumnLayout() { Visible = true }),
                            new("Reverting", new PersistedColumnLayout() { Visible = true }),
                            new("Initializing", new PersistedColumnLayout() { Visible = true }),
                            new("Remote Not Synchronizing", new PersistedColumnLayout() { Visible = true }),
                            new("Remote Synchronizing", new PersistedColumnLayout() { Visible = true }),
                            new("Remote Synchronized", new PersistedColumnLayout() { Visible = true }),
                            new("Remote Reverting", new PersistedColumnLayout() { Visible = true }),
                            new("Remote Initializing", new PersistedColumnLayout() { Visible = true }),
                            new("Sync Health", new PersistedColumnLayout() { Visible = true }),
                            new("Snapshot Date", new PersistedColumnLayout() { Visible = true }),
                            new("Snapshot Status", new PersistedColumnLayout() {Visible = false}),
                            new("Snapshot Age", new PersistedColumnLayout() { Visible = true }),
                            new("Synchronized Status", new PersistedColumnLayout() { Visible = false }),
                            new("Max Secondary Lag (sec)", new PersistedColumnLayout() { Visible = false }),
                            new("Max Estimated Data Loss (sec)", new PersistedColumnLayout() { Visible = false }),
                            new("Max Estimated Recovery Time (sec)", new PersistedColumnLayout() { Visible = false }),
                            new("Max Secondary Lag", new PersistedColumnLayout() { Visible = true }),
                            new("Max Estimated Data Loss", new PersistedColumnLayout() { Visible = true }),
                            new("Max Estimated Recovery Time", new PersistedColumnLayout() { Visible = true }),
                            new("Total Redo Queue Size (KB)", new PersistedColumnLayout() { Visible = false }),
                            new("Avg Redo Queue Size (KB)", new PersistedColumnLayout() { Visible = true }),
                            new("Total Log Send Queue Size (KB)", new PersistedColumnLayout() { Visible = false }),
                            new("Avg Log Send Queue Size (KB)", new PersistedColumnLayout() { Visible = true }),
                        },
                        CellFormatString = new Dictionary<string, string>(),
                        CellNullValue = new Dictionary<string, string>(),
                        DoNotConvertToLocalTimeZone = new List<string>(),
                        ResultName = "Availability Group Summary",
                        LinkColumns = new Dictionary<string, LinkColumnInfo>
                        {
                            {
                                "Instance",
                                new DrillDownLinkColumnInfo()
                            }
                        },
                        CellHighlightingRules = new CellHighlightingRuleSetCollection
                        {
                            {
                                "Not Synchronizing",
                                new CellHighlightingRuleSet
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
                            },
                            {
                                "Remote Not Synchronizing",
                                new CellHighlightingRuleSet
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
                            },
                            {
                                "Reverting",
                                new CellHighlightingRuleSet
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
                            },
                            {
                                "Remote Reverting",
                                new CellHighlightingRuleSet
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
                            },
                            {
                                "Initializing",
                                new CellHighlightingRuleSet
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
                            },
                            {
                                "Remote Initializing",
                                new CellHighlightingRuleSet
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
                            },
                            {
                                "Sync Health",
                                new CellHighlightingRuleSet
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
                            },
                            {
                                "Snapshot Date",
                                new CellHighlightingRuleSet
                                {
                                    IsStatusColumn = true,
                                    TargetColumn = "Snapshot Status"
                                }
                            },
                            {
                                "Snapshot Age",
                                new CellHighlightingRuleSet
                                {
                                    TargetColumn = "Snapshot Status",
                                    IsStatusColumn = true
                                }
                            },
                            {
                                "Snapshot Status",
                                new CellHighlightingRuleSet
                                {
                                    TargetColumn = "Snapshot Status",
                                    IsStatusColumn = true
                                }
                            },
                            {
                                "Synchronized",
                                new CellHighlightingRuleSet
                                {
                                    IsStatusColumn = true,
                                    TargetColumn = "Synchronized Status"
                                }
                            },
                            {
                                "Synchronized Status",
                                new CellHighlightingRuleSet
                                {
                                    IsStatusColumn = true,
                                    TargetColumn = "Synchronized Status"
                                }
                            },
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
                            ColumnAlias = new Dictionary<string, string>(),
                            CellFormatString = new Dictionary<string, string>
                            {
                                {
                                    "Log Redo Rate (KB/s)",
                                    "N0"
                                },
                                {
                                    "Log Redo Queue Size (KB)",
                                    "N0"
                                },
                                {
                                    "Log Send Rate (KB/s)",
                                    "N0"
                                },
                                {
                                    "Log Send Queue Size (KB)",
                                    "N0"
                                },
                                {
                                    "Estimated Recovery Time",
                                    "N0"
                                },
                                {
                                    "Estimated Data Loss (sec)",
                                    "N0"
                                },
                                {
                                    "Secondary Lag (sec)",
                                    "N0"
                                },
                                {
                                    "Last Sent Time",
                                    "G"
                                },
                                {
                                    "Last Received Time",
                                    "G"
                                },
                                {
                                    "Last Hardened Time",
                                    "G"
                                },
                                {
                                    "Last Redone Time",
                                    "G"
                                },
                                {
                                    "Last Commit Time",
                                    "G"
                                }
                            },
                            CellNullValue = new Dictionary<string, string>(),
                            DoNotConvertToLocalTimeZone = new List<string>(),
                            ColumnLayout = new List<KeyValuePair<string, PersistedColumnLayout>>
                            {
                                new("Database", new PersistedColumnLayout() { Visible = true }),
                                new("Availability Group", new PersistedColumnLayout(){ Visible = true }),
                                new("Replica Server", new PersistedColumnLayout(){ Visible = true }),
                                new("Sync State", new PersistedColumnLayout(){ Visible = true }),
                                new("Sync Health", new PersistedColumnLayout(){ Visible = true }),
                                new("Suspend Reason", new PersistedColumnLayout(){ Visible = true }),
                                new("Database State", new PersistedColumnLayout(){ Visible = true }),
                                new("Is Local", new PersistedColumnLayout(){ Visible = true }),
                                new("Availability Mode", new PersistedColumnLayout(){ Visible = true }),
                                new("Failover Mode", new PersistedColumnLayout(){ Visible = true }),
                                new("Is Primary", new PersistedColumnLayout(){ Visible = true }),
                                new("Primary Connections", new PersistedColumnLayout(){ Visible = true }),
                                new("Secondary Connections", new PersistedColumnLayout(){ Visible = true }),
                                new("Snapshot Status", new PersistedColumnLayout(){ Visible = false }),
                                new("Snapshot Age", new PersistedColumnLayout(){ Visible = true }),
                                new("Estimated Data Loss (sec)", new PersistedColumnLayout(){ Visible = false }),
                                new("Estimated Recovery Time (sec)", new PersistedColumnLayout(){ Visible = false }),
                                new("Secondary Lag (sec)", new PersistedColumnLayout(){ Visible = false }),
                                new("Estimated Data Loss", new PersistedColumnLayout(){ Visible = true }),
                                new("Estimated Recovery Time", new PersistedColumnLayout(){ Visible = true }),
                                new("Secondary Lag", new PersistedColumnLayout(){ Visible = true }),
                                new("Log Send Queue Size (KB)", new PersistedColumnLayout(){ Visible = true }),
                                new("Log Send Rate (KB/s)", new PersistedColumnLayout(){ Visible = false }),
                                new("Log Redo Queue Size (KB)", new PersistedColumnLayout(){ Visible = true }),
                                new("Log Redo Rate (KB/s)", new PersistedColumnLayout(){ Visible = false }),
                                new("Last Sent Time", new PersistedColumnLayout(){ Visible = false }),
                                new("Last Received Time", new PersistedColumnLayout(){ Visible = false }),
                                new("Last Hardened Time", new PersistedColumnLayout(){ Visible = false }),
                                new("Last Redone Time", new PersistedColumnLayout(){ Visible = false }),
                                new("Last Commit Time", new PersistedColumnLayout(){ Visible = false }),
                                new("Snapshot Date", new PersistedColumnLayout(){ Visible = true }),
                            },
                            ResultName = "Availability Group Detail",
                            LinkColumns = new Dictionary<string, LinkColumnInfo>(),
                            CellHighlightingRules = new CellHighlightingRuleSetCollection
                            {
                                {
                                    "Sync Health",
                                    new CellHighlightingRuleSet
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
                                },
                                {
                                    "Sync State",
                                    new CellHighlightingRuleSet
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
                                },
                                {
                                    "Snapshot Date",
                                    new CellHighlightingRuleSet
                                    {
                                        Rules = new List<CellHighlightingRule>(),
                                        TargetColumn = "Snapshot Status",
                                        IsStatusColumn = true
                                    }
                                },
                                {
                                    "Snapshot Age",
                                    new CellHighlightingRuleSet
                                    {
                                        Rules = new List<CellHighlightingRule>(),
                                        TargetColumn = "Snapshot Status",
                                        IsStatusColumn = true
                                    }
                                },
                                {
                                    "Snapshot Status",
                                    new CellHighlightingRuleSet
                                    {
                                        Rules = new List<CellHighlightingRule>(),
                                        TargetColumn = "Snapshot Status",
                                        IsStatusColumn = true
                                    }
                                }
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
            using var metricsConfig = new AGMetricsConfig() { InstanceID = instanceId };
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