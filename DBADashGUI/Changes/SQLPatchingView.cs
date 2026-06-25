using DBADash;
using DBADashGUI.CustomReports;
using DBADashGUI.Theme;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using static DBADashGUI.DBADashStatus;

namespace DBADashGUI.Changes
{
    internal class SQLPatchingView : CustomReportView
    {
        private ToolStripMenuItem thresholdsToolStripMenuItem;
        private ToolStripMenuItem sPBehindWarningToolStripMenuItem;
        private ToolStripMenuItem sPBehindCriticalToolStripMenuItem;
        private ToolStripMenuItem cUBehindWarningToolStripMenuItem;
        private ToolStripMenuItem cUBehindCriticalToolStripMenuItem;
        private ToolStripMenuItem daysUntilSupportEndsWarningToolStripMenuItem;
        private ToolStripMenuItem daysUntilSupportEndsCriticalToolStripMenuItem;
        private ToolStripMenuItem daysUntilMainstreamSupportEndsWarningToolStripMenuItem;
        private ToolStripMenuItem daysUntilMainstreamSupportEndsCriticalToolStripMenuItem;
        private ToolStripMenuItem buildReferenceAgeWarningThresholdToolStripMenuItem;
        private ToolStripMenuItem buildReferenceAgeCriticalThresholdToolStripMenuItem;
        private ToolStripMenuItem buildReferenceUpdateExclusionPeriodToolStripMenuItem;
        private ToolStripButton tsUpdateBuildReference;

        public SQLPatchingView()
        {
            Report = Instance;
            PreventReportOverwrite = true;
            AddToolbarButtons();
        }

        #region Toolbar

        private void AddToolbarButtons()
        {
            var tsConfig = new ToolStripDropDownButton("Configure")
            {
                Name = "tsConfigureSQLPatching",
                ToolTipText = "Configure SQL Patching thresholds",
                Image = Properties.Resources.SettingsOutline_16x,
                DisplayStyle = ToolStripItemDisplayStyle.Image
            };

            thresholdsToolStripMenuItem = new ToolStripMenuItem("Thresholds");

            sPBehindWarningToolStripMenuItem = new ToolStripMenuItem { Tag = "GUISPBehindWarningThreshold" };
            sPBehindCriticalToolStripMenuItem = new ToolStripMenuItem { Tag = "GUISPBehindCriticalThreshold" };
            cUBehindWarningToolStripMenuItem = new ToolStripMenuItem { Tag = "GUICUBehindWarningThreshold" };
            cUBehindCriticalToolStripMenuItem = new ToolStripMenuItem { Tag = "GUICUBehindCriticalThreshold" };
            daysUntilSupportEndsWarningToolStripMenuItem = new ToolStripMenuItem { Tag = "GUIDaysUntilSupportEndsWarningThreshold" };
            daysUntilSupportEndsCriticalToolStripMenuItem = new ToolStripMenuItem { Tag = "GUIDaysUntilSupportEndsCriticalThreshold" };
            daysUntilMainstreamSupportEndsWarningToolStripMenuItem = new ToolStripMenuItem { Tag = "GUIDaysUntilMainstreamSupportEndsWarningThreshold" };
            daysUntilMainstreamSupportEndsCriticalToolStripMenuItem = new ToolStripMenuItem { Tag = "GUIDaysUntilMainstreamSupportEndsCriticalThreshold" };
            buildReferenceAgeWarningThresholdToolStripMenuItem = new ToolStripMenuItem { Tag = "GUIBuildReferenceAgeWarningThreshold" };
            buildReferenceAgeCriticalThresholdToolStripMenuItem = new ToolStripMenuItem { Tag = "GUIBuildReferenceAgeCriticalThreshold" };
            buildReferenceUpdateExclusionPeriodToolStripMenuItem = new ToolStripMenuItem
            {
                Tag = "GUIBuildReferenceUpdateExclusionPeriod",
                ToolTipText = "Don't show a build reference is out of date warning if we have checked for updates in X days"
            };

            var resetToDefault = new ToolStripMenuItem("Reset to Default");
            resetToDefault.Click += ResetToDefaultToolStripMenuItem_Click;

            foreach (var item in new[]
            {
                sPBehindWarningToolStripMenuItem, sPBehindCriticalToolStripMenuItem,
                cUBehindWarningToolStripMenuItem, cUBehindCriticalToolStripMenuItem,
                daysUntilSupportEndsWarningToolStripMenuItem, daysUntilSupportEndsCriticalToolStripMenuItem,
                daysUntilMainstreamSupportEndsWarningToolStripMenuItem, daysUntilMainstreamSupportEndsCriticalToolStripMenuItem,
                buildReferenceAgeWarningThresholdToolStripMenuItem, buildReferenceAgeCriticalThresholdToolStripMenuItem,
                buildReferenceUpdateExclusionPeriodToolStripMenuItem
            })
            {
                item.Click += SetThreshold_Click;
            }

            thresholdsToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[]
            {
                sPBehindWarningToolStripMenuItem, sPBehindCriticalToolStripMenuItem,
                cUBehindWarningToolStripMenuItem, cUBehindCriticalToolStripMenuItem,
                daysUntilSupportEndsWarningToolStripMenuItem, daysUntilSupportEndsCriticalToolStripMenuItem,
                daysUntilMainstreamSupportEndsWarningToolStripMenuItem, daysUntilMainstreamSupportEndsCriticalToolStripMenuItem,
                buildReferenceAgeWarningThresholdToolStripMenuItem, buildReferenceAgeCriticalThresholdToolStripMenuItem,
                buildReferenceUpdateExclusionPeriodToolStripMenuItem,
                new ToolStripSeparator(),
                resetToDefault
            });

            tsConfig.DropDownItems.Add(thresholdsToolStripMenuItem);

            var tsViewBuildReference = new ToolStripButton("Build Reference")
            {
                Name = "tsViewBuildReference",
                Image = Properties.Resources.Table_16x,
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText
            };
            tsViewBuildReference.Click += (_, _) =>
            {
                BuildReferenceViewer buildViewer = new();
                buildViewer.ShowSingleInstance();
            };

            tsUpdateBuildReference = new ToolStripButton("Update Build Reference")
            {
                Name = "tsUpdateBuildReference",
                Image = Properties.Resources.CloudDownload_16x,
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText
            };
            tsUpdateBuildReference.Click += TsUpdateBuildReference_Click;

            var insertAt = ToolStrip.Items.IndexOfKey("tsParams");
            insertAt = insertAt >= 0 ? insertAt + 1 : ToolStrip.Items.Count;
            ToolStrip.Items.Insert(insertAt, tsConfig);
            ToolStrip.Items.Insert(insertAt + 1, tsViewBuildReference);
            ToolStrip.Items.Insert(insertAt + 2, tsUpdateBuildReference);

            SetThresholdsMenuText();
        }

        private void SetThresholdsMenuText()
        {
            sPBehindCriticalToolStripMenuItem.Text = $"SP Behind Critical ({Config.SPBehindCriticalThreshold})";
            sPBehindWarningToolStripMenuItem.Text = $"SP Behind Warning ({Config.SPBehindWarningThreshold})";
            cUBehindCriticalToolStripMenuItem.Text = $"CU Behind Critical ({Config.CUBehindCriticalThreshold})";
            cUBehindWarningToolStripMenuItem.Text = $"CU Behind Warning ({Config.CUBehindWarningThreshold})";
            daysUntilMainstreamSupportEndsCriticalToolStripMenuItem.Text = $"Days Until Mainstream Support Ends Critical ({Config.DaysUntilMainstreamSupportEndsCriticalThreshold})";
            daysUntilMainstreamSupportEndsWarningToolStripMenuItem.Text = $"Days Until Mainstream Support Ends Warning ({Config.DaysUntilMainstreamSupportEndsWarningThreshold})";
            daysUntilSupportEndsCriticalToolStripMenuItem.Text = $"Days Until Support Ends Critical ({Config.DaysUntilSupportEndsCriticalThreshold})";
            daysUntilSupportEndsWarningToolStripMenuItem.Text = $"Days Until Support Ends Warning ({Config.DaysUntilSupportEndsWarningThreshold})";
            buildReferenceAgeCriticalThresholdToolStripMenuItem.Text = $"Build Reference Age Critical Threshold ({Config.BuildReferenceAgeCriticalThreshold})";
            buildReferenceAgeWarningThresholdToolStripMenuItem.Text = $"Build Reference Age Warning Threshold ({Config.BuildReferenceAgeWarningThreshold})";
            buildReferenceUpdateExclusionPeriodToolStripMenuItem.Text = $"Build Reference Update Exclusion Period ({Config.BuildReferenceUpdateExclusionPeriod})";
        }

        #endregion Toolbar

        #region Toolbar event handlers

        private void SetThreshold_Click(object sender, EventArgs e)
        {
            var item = (ToolStripMenuItem)sender;
            var setting = item.Tag!.ToString();
            var value = Convert.ToString(RepositorySettings.GetIntSetting(setting, Common.ConnectionString));
            PromptUpdateThreshold(setting, value, item.Text);
        }

        private void PromptUpdateThreshold(string setting, string currentValue, string title)
        {
            try
            {
                if (CommonShared.ShowInputDialog(ref currentValue, title) != DialogResult.OK) return;
                if (string.IsNullOrEmpty(currentValue))
                {
                    RepositorySettings.UpdateSetting(setting, null, Common.ConnectionString);
                }
                else if (int.TryParse(currentValue, out var intValue))
                {
                    RepositorySettings.UpdateSetting(setting, intValue, Common.ConnectionString);
                }
                else
                {
                    MessageBox.Show("Invalid value", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                Config.RefreshConfig();
                RefreshData();
                SetThresholdsMenuText();
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex);
            }
        }

        private void ResetToDefaultToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Reset thresholds to default values?", "Reset", MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) != DialogResult.Yes) return;
            foreach (var itm in thresholdsToolStripMenuItem.DropDownItems.OfType<ToolStripMenuItem>())
            {
                if (itm.Tag == null) continue;
                RepositorySettings.UpdateSetting(itm.Tag.ToString(), null, Common.ConnectionString);
            }
            Config.RefreshConfig();
            RefreshData();
            SetThresholdsMenuText();
        }

        private async void TsUpdateBuildReference_Click(object sender, EventArgs e)
        {
            try
            {
                await DBADash.BuildReference.UpdateAsync(Common.ConnectionString);
                RefreshData();
                MessageBox.Show("Build reference updated", "Updated", MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex, "Error updating build reference");
            }
        }

        #endregion Toolbar event handlers

        #region Grid post-processing

        protected override void OnPostGridRefresh()
        {
            base.OnPostGridRefresh();
            foreach (var grid in Grids)
            {
                if (grid.ResultSetID == 0)
                {
                    ApplyVersionInfoStyling(grid);
                    UpdateBuildReferenceStatus(grid);
                }
            }
        }

        private static DBADashStatusEnum GetThresholdStatus(int? value, int criticalThreshold, int warningThreshold)
        {
            if (value == null) return DBADashStatusEnum.NA;
            if (value < criticalThreshold) return DBADashStatusEnum.Critical;
            if (value <= warningThreshold) return DBADashStatusEnum.Warning;
            return DBADashStatusEnum.OK;
        }

        private static DBADashStatusEnum GetBehindStatus(int? value, int criticalThreshold, int warningThreshold)
        {
            if (value == null) return DBADashStatusEnum.NA;
            if (value >= criticalThreshold) return DBADashStatusEnum.Critical;
            if (value >= warningThreshold) return DBADashStatusEnum.Warning;
            return DBADashStatusEnum.NA;
        }

        private static void ApplyVersionInfoStyling(DBADashDataGridView grid)
        {
            var hasWindowsUpdateCol = grid.Columns["IsWindowsUpdate"] is not null;

            foreach (DataGridViewRow gRow in grid.Rows)
            {
                if (gRow.DataBoundItem is not DataRowView drv) continue;

                var supportedUntilStatus = GetThresholdStatus(
                    drv["DaysUntilSupportExpires"].DBNullToNull() as int?,
                    Config.DaysUntilSupportEndsCriticalThreshold,
                    Config.DaysUntilSupportEndsWarningThreshold);

                var mainstreamStatus = GetThresholdStatus(
                    drv["DaysUntilMainstreamSupportExpires"].DBNullToNull() as int?,
                    Config.DaysUntilMainstreamSupportEndsCriticalThreshold,
                    Config.DaysUntilMainstreamSupportEndsWarningThreshold);

                var spBehindStatus = GetBehindStatus(
                    drv["SPBehind"].DBNullToNull() as int?,
                    Config.SPBehindCriticalThreshold,
                    Config.SPBehindWarningThreshold);

                var cuBehindStatus = GetBehindStatus(
                    drv["CUBehind"].DBNullToNull() as int?,
                    Config.CUBehindCriticalThreshold,
                    Config.CUBehindWarningThreshold);

                SetCellStatus(gRow, "DaysUntilMainstreamSupportExpires", mainstreamStatus);
                SetCellStatus(gRow, "DaysUntilSupportExpires", supportedUntilStatus);
                SetCellStatus(gRow, "MainstreamEndDate", mainstreamStatus);
                SetCellStatus(gRow, "SupportedUntil", supportedUntilStatus);
                SetCellStatus(gRow, "CUBehind", cuBehindStatus);
                SetCellStatus(gRow, "SPBehind", spBehindStatus);

                if (hasWindowsUpdateCol)
                {
                    if (gRow.Cells["IsWindowsUpdate"].Value == DBNull.Value)
                    {
                        gRow.Cells["IsWindowsUpdate"].SetStatusColor(DBADashStatusEnum.NA);
                    }
                    else
                    {
                        gRow.Cells["IsWindowsUpdate"].Style = gRow.DefaultCellStyle;
                    }
                }
            }
        }

        private static void SetCellStatus(DataGridViewRow row, string columnName, DBADashStatusEnum status)
        {
            if (row.DataGridView?.Columns[columnName] is null) return;
            row.Cells[columnName].SetStatusColor(status);
        }

        private void UpdateBuildReferenceStatus(DBADashDataGridView grid)
        {
            if (grid.Rows.Count <= 0) return;
            if (grid.Rows[0].DataBoundItem is not DataRowView drv) return;
            if (!drv.Row.Table.Columns.Contains("BuildReferenceVersion")) return;

            if (drv["BuildReferenceVersion"].DBNullToNull() is not DateTime versionDate
                || drv["BuildReferenceUpdated"].DBNullToNull() is not DateTime updatedDate)
            {
                tsUpdateBuildReference.Text = "Update Build Reference";
                tsUpdateBuildReference.ForeColor = DashColors.TrimbleBlue;
                return;
            }

            tsUpdateBuildReference.Text = "Update Build Reference (" + versionDate.ToShortDateString() + ")";
            tsUpdateBuildReference.ToolTipText = "Build Reference provided by dbatools (https://dbatools.io/). Click to update.\nBuild Reference Version: " + versionDate.ToShortDateString() + "\nLast Update Check: " + updatedDate.ToShortDateString();
            var buildReferenceAge = DateTime.UtcNow.Subtract(versionDate).TotalDays;
            var buildReferenceDaysSinceUpdate = DateTime.UtcNow.Subtract(updatedDate).TotalDays;

            if (buildReferenceAge > Config.BuildReferenceAgeCriticalThreshold)
            {
                tsUpdateBuildReference.ForeColor = buildReferenceDaysSinceUpdate <= Config.BuildReferenceUpdateExclusionPeriod ? DashColors.TrimbleBlue : DashColors.Fail;
            }
            else if (buildReferenceAge > Config.BuildReferenceAgeWarningThreshold)
            {
                tsUpdateBuildReference.ForeColor = buildReferenceDaysSinceUpdate <= Config.BuildReferenceUpdateExclusionPeriod ? DashColors.TrimbleBlue : DashColors.Warning;
            }
            else
            {
                tsUpdateBuildReference.ForeColor = DashColors.Success;
            }
        }

        #endregion Grid post-processing

        #region Custom links

        private sealed class KBListLink : LinkColumnInfo
        {
            public override void Navigate(DBADashContext context, DataGridViewRow row, int selectedTableIndex, ContainerControl sender)
            {
                if (row.Cells["KBList"].Value is not string kbs) return;
                foreach (var kb in kbs.Split(","))
                {
                    CommonShared.OpenURL("https://support.microsoft.com/help/" + kb.Trim());
                }
            }
        }

        private sealed class ProductUpdateReferenceLink : LinkColumnInfo
        {
            public override void Navigate(DBADashContext context, DataGridViewRow row, int selectedTableIndex, ContainerControl sender)
            {
                if (row.Cells["ProductUpdateReference"].Value is not string kb) return;
                CommonShared.OpenURL("https://support.microsoft.com/help/" + kb.Replace("KB", "").Trim());
            }
        }

        private sealed class LifecycleUrlLink : LinkColumnInfo
        {
            public override void Navigate(DBADashContext context, DataGridViewRow row, int selectedTableIndex, ContainerControl sender)
            {
                if (row.DataBoundItem is not DataRowView drv) return;
                var url = drv["LifecycleUrl"] as string;
                if (string.IsNullOrEmpty(url)) return;
                CommonShared.OpenURL(url);
            }
        }

        #endregion Custom links

        #region Report definition

        private static ColumnMetadata Hidden(int displayIndex) =>
            new() { Visible = false, DisplayIndex = displayIndex };

        public static SystemReport Instance => new()
        {
            ViewType = typeof(SQLPatchingView),
            ReportName = "SQL Patching",
            SchemaName = "dbo",
            ProcedureName = "SQLPatchingReport_Get",
            QualifiedProcedureName = "dbo.SQLPatchingReport_Get",
            CanEditReport = false,
            TriggerCollectionTypes = new List<string>
            {
                CollectionType.ServerProperties.ToString()
            },
            CustomReportResults = new Dictionary<int, CustomReportResult>
            {
                [0] = new CustomReportResult
                {
                    ResultName = "Version Info",
                    Columns = new Dictionary<string, ColumnMetadata>
                    {
                        ["InstanceID"] = Hidden(0),
                        ["ConnectionID"] = Hidden(1),
                        ["InstanceDisplayName"] = new ColumnMetadata { DisplayIndex = 2, Alias = "Instance" },
                        ["SQLVersion"] = new ColumnMetadata { DisplayIndex = 3, Alias = "Version" },
                        ["PatchDate"] = new ColumnMetadata { DisplayIndex = 4, Alias = "SQL Patch Date", Description = "Date when DBA Dash detected a version change" },
                        ["ProductVersion"] = new ColumnMetadata { DisplayIndex = 5, Alias = "Product Version" },
                        ["ProductMajorVersion"] = Hidden(6),
                        ["ProductMinorVersion"] = Hidden(7),
                        ["ProductBuild"] = Hidden(8),
                        ["ProductRevision"] = Hidden(9),
                        ["Edition"] = new ColumnMetadata { DisplayIndex = 10, Alias = "Edition" },
                        ["EngineEdition"] = Hidden(11),
                        ["EditionID"] = Hidden(12),
                        ["ProductLevel"] = new ColumnMetadata { DisplayIndex = 13, Alias = "Product Level" },
                        ["ProductUpdateLevel"] = new ColumnMetadata { DisplayIndex = 14, Alias = "Product Update Level" },
                        ["ProductUpdateReference"] = new ColumnMetadata { DisplayIndex = 15, Alias = "Product Update Reference", Link = new ProductUpdateReferenceLink() },
                        ["ProductBuildType"] = Hidden(16),
                        ["ResourceVersion"] = Hidden(17),
                        ["ResourceLastUpdateDateTime"] = Hidden(18),
                        ["LicenseType"] = Hidden(19),
                        ["NumLicenses"] = Hidden(20),
                        ["WindowsCaption"] = Hidden(21),
                        ["WindowsRelease"] = Hidden(22),
                        ["WindowsSKU"] = Hidden(23),
                        ["SupportedUntil"] = new ColumnMetadata { DisplayIndex = 24, Alias = "Supported Until (Extended)", Description = "Date extended support expires for this version/build of SQL Server", Link = new LifecycleUrlLink() },
                        ["DaysUntilSupportExpires"] = new ColumnMetadata { DisplayIndex = 25, Alias = "Days Until Support Expires", Description = "Days until extended support expires for this version/build of SQL Server" },
                        ["MainstreamEndDate"] = new ColumnMetadata { DisplayIndex = 26, Alias = "Supported Until (Mainstream)", Description = "Date mainstream support expires for this version of SQL Server", Link = new LifecycleUrlLink() },
                        ["DaysUntilMainstreamSupportExpires"] = new ColumnMetadata { DisplayIndex = 27, Alias = "Days Until Mainstream Support Expires", Description = "Days until mainstream support expires for this version of SQL Server" },
                        ["KBList"] = new ColumnMetadata { DisplayIndex = 28, Alias = "KB List", Link = new KBListLink() },
                        ["IsUpdateAvailable"] = new ColumnMetadata { DisplayIndex = 29, Alias = "Update Available?", Description = "Value is true when a later build is available." },
                        ["SPBehind"] = new ColumnMetadata { DisplayIndex = 30, Alias = "SP Behind", Description = "Service packs behind latest build" },
                        ["CUBehind"] = new ColumnMetadata { DisplayIndex = 31, Alias = "CU Behind", Description = "Cumulative updates behind latest build within this service pack" },
                        ["LatestVersion"] = new ColumnMetadata { DisplayIndex = 32, Alias = "Latest Version", Description = "Latest build available for this version of SQL" },
                        ["LatestVersionPatchLevel"] = new ColumnMetadata { DisplayIndex = 33, Alias = "Latest Version Patch Level", Description = "Latest build available for this version of SQL" },
                        ["IsWindowsUpdate"] = new ColumnMetadata { DisplayIndex = 34, Alias = "Is Windows Update", Description = "SQL Server is patched through Windows update" },
                        ["BuildReferenceVersion"] = Hidden(35),
                        ["BuildReferenceUpdated"] = Hidden(36),
                        ["LifecycleUrl"] = Hidden(37),
                    }
                },
                [1] = new CustomReportResult
                {
                    ResultName = "Patch History",
                    Columns = new Dictionary<string, ColumnMetadata>
                    {
                        ["InstanceDisplayName"] = new ColumnMetadata { DisplayIndex = 0, Alias = "Instance" },
                        ["ChangedDate"] = new ColumnMetadata { DisplayIndex = 1, Alias = "Changed Date" },
                        ["OldVersion"] = new ColumnMetadata { DisplayIndex = 2, Alias = "Old Version" },
                        ["NewVersion"] = new ColumnMetadata { DisplayIndex = 3, Alias = "New Version" },
                        ["OldProductLevel"] = new ColumnMetadata { DisplayIndex = 4, Alias = "Old Product Level" },
                        ["NewProductLevel"] = new ColumnMetadata { DisplayIndex = 5, Alias = "New Product Level" },
                        ["OldProductUpdateLevel"] = new ColumnMetadata { DisplayIndex = 6, Alias = "Old Product Update Level" },
                        ["NewProductUpdateLevel"] = new ColumnMetadata { DisplayIndex = 7, Alias = "New Product Update Level" },
                        ["OldEdition"] = new ColumnMetadata { DisplayIndex = 8, Alias = "Old Edition" },
                        ["NewEdition"] = new ColumnMetadata { DisplayIndex = 9, Alias = "New Edition" },
                        ["Instance"] = Hidden(10),
                    }
                }
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

        #endregion Report definition
    }
}
