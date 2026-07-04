using DBADash;
using DBADashGUI.CustomReports;
using System.Collections.Generic;
using System.Windows.Forms;

namespace DBADashGUI.Checks
{
    /// <summary>
    /// OS Loaded Modules tab implemented as a system report.  The instance summary (dbo.OSLoadedModuleSummaryReport_Get)
    /// is the default/root view; clicking an instance drills in-place (swapping the same view, with the standard
    /// back button) to the per-instance module list (dbo.OSLoadedModulesReport_Get).  Modelled on
    /// <see cref="Backups.BackupsView"/>'s summary/detail-via-drilldown pattern.
    /// </summary>
    internal class OSLoadedModulesView : CustomReportView
    {
        private ToolStripButton tsConfigureStatus;

        public OSLoadedModulesView()
        {
            Report = Instance;
            PreventReportOverwrite = true;
            AddToolbarButtons();
        }

        #region Toolbar

        private void AddToolbarButtons()
        {
            tsConfigureStatus = new ToolStripButton
            {
                Name = "tsConfigureStatus",
                ToolTipText = "Configure known module statuses",
                Image = Properties.Resources.SettingsOutline_16x,
                DisplayStyle = ToolStripItemDisplayStyle.Image
            };
            tsConfigureStatus.Click += (_, _) =>
            {
                using var frm = new OSLoadedModuleStatus();
                frm.ShowDialog();
                if (frm.DialogResult == DialogResult.OK)
                {
                    RefreshData();
                }
            };

            var insertAt = ToolStrip.Items.IndexOfKey("tsParams");
            insertAt = insertAt >= 0 ? insertAt + 1 : ToolStrip.Items.Count;
            ToolStrip.Items.Insert(insertAt, tsConfigureStatus);
        }

        #endregion Toolbar

        #region Report definition

        private static CellHighlightingRuleSet StatusHighlight(string statusColumn) =>
            new(statusColumn) { IsStatusColumn = true };

        private static ColumnMetadata Hidden() => new() { Visible = false };

        public static SystemReport Instance => new()
        {
            ViewType = typeof(OSLoadedModulesView),
            ReportName = "OS Loaded Modules",
            SchemaName = "dbo",
            ProcedureName = "OSLoadedModuleSummaryReport_Get",
            QualifiedProcedureName = "dbo.OSLoadedModuleSummaryReport_Get",
            CanEditReport = false,
            TriggerCollectionTypes = new List<string>
            {
                CollectionType.OSLoadedModules.ToString()
            },
            CustomReportResults = new Dictionary<int, CustomReportResult>
            {
                [0] = new CustomReportResult
                {
                    ResultName = "OS Loaded Modules",
                    Columns = new Dictionary<string, ColumnMetadata>
                    {
                        ["InstanceDisplayName"] = new()
                        {
                            Alias = "Instance",
                            Link = new SystemDrillDownLinkColumnInfo
                            {
                                ReportFactory = () => DetailReport,
                                ColumnToParameterMap = new Dictionary<string, string> { ["@InstanceID"] = "InstanceID" },
                                DrillDownMode = DrillDownMode.ExistingWindow
                            },
                            Description = "Click to view loaded modules for this instance"
                        },
                        ["StatusDescription"] = new() { Alias = "Status", Highlighting = StatusHighlight("Status") },
                        ["Notes"] = new() { Alias = "Notes" },
                        // Hidden columns
                        ["InstanceID"] = Hidden(),
                        ["EngineEdition"] = Hidden(),
                        ["Status"] = Hidden(),
                    }
                }.SetDisplayIndexBasedOnColumnOrder()
            },
            Params = new Params
            {
                ParamList = new List<Param>
                {
                    new() { ParamName = "@InstanceIDs", ParamType = "IDS" },
                    new() { ParamName = "@ShowHidden", ParamType = "BIT" },
                }
            }
        };

        /// <summary>
        /// Per-instance loaded module list, reached by drilling into an instance from the summary.
        /// </summary>
        private static SystemReport DetailReport => new()
        {
            ViewType = typeof(OSLoadedModulesView),
            ReportName = "OS Loaded Modules",
            SchemaName = "dbo",
            ProcedureName = "OSLoadedModulesReport_Get",
            QualifiedProcedureName = "dbo.OSLoadedModulesReport_Get",
            CanEditReport = false,
            TriggerCollectionTypes = new List<string>
            {
                CollectionType.OSLoadedModules.ToString()
            },
            CustomReportResults = new Dictionary<int, CustomReportResult>
            {
                [0] = new CustomReportResult
                {
                    ResultName = "OS Loaded Modules",
                    Columns = new Dictionary<string, ColumnMetadata>
                    {
                        ["InstanceDisplayName"] = new() { Alias = "Instance" },
                        ["StatusDescription"] = new() { Alias = "Status", Highlighting = StatusHighlight("Status") },
                        ["Notes"] = new() { Alias = "Notes" },
                        ["base_address"] = new() { Alias = "Base Address" },
                        ["file_version"] = new() { Alias = "File Version" },
                        ["product_version"] = new() { Alias = "Product Version" },
                        ["debug"] = new() { Alias = "Debug" },
                        ["patched"] = new() { Alias = "Patched" },
                        ["prerelease"] = new() { Alias = "Pre Release" },
                        ["private_build"] = new() { Alias = "Private Build" },
                        ["special_build"] = new() { Alias = "Special Build" },
                        ["language"] = new() { Alias = "Language" },
                        ["company"] = new() { Alias = "Company" },
                        ["description"] = new() { Alias = "Description" },
                        ["name"] = new() { Alias = "Name" },
                        // Hidden columns
                        ["InstanceID"] = Hidden(),
                        ["Status"] = Hidden(),
                    }
                }.SetDisplayIndexBasedOnColumnOrder()
            },
            Params = new Params
            {
                ParamList = new List<Param>
                {
                    new() { ParamName = "@InstanceID", ParamType = "INT" },
                }
            }
        };

        #endregion Report definition
    }
}
