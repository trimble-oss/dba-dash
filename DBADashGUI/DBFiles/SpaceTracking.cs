using DBADash;
using DBADashGUI.Charts;
using DBADashGUI.CustomReports;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI.DBFiles
{
    internal class SpaceTracking : CustomReportView
    {
        private enum DrillLevel { Instance, Database, File }

        /// <summary>
        /// Returns the level currently displayed, mirroring the proc's Grp logic:
        /// File when @DBName is set, Database when @InstanceGroupName is set or a single instance
        /// is in context, otherwise Instance.
        /// </summary>
        private DrillLevel CurrentLevel
        {
            get
            {
                if (GetCurrentParam("@DBName") != null || context?.DatabaseID > 0) return DrillLevel.File;
                if (GetCurrentParam("@InstanceGroupName") != null
                    || context?.InstanceID > 0
                    || context?.InstanceIDs.Count == 1) return DrillLevel.Database;
                return DrillLevel.Instance;
            }
        }

        private ToolStripDropDownButton tsUnits;
        private ToolStripDropDownButton tsDecimalPlaces;
        private ToolStripButton tsHistory;
        private const string HistoryColumnName = "colHistory";
        private static readonly string[] Units = { "MB", "GB", "TB" };
        private const string DefaultUnit = "GB";
        private const int DefaultDecimalPlaces = 1;
        private const string UnitPrefixText = "Unit: ";
        private const string DecimalPlacesPrefixText = "Decimal Places: ";
        private static readonly (string Label, string Format)[] DecimalOptions =
        {
            ("0", "N0"), ("1", "N1"), ("2", "N2"), ("3", "N3")
        };

        public SpaceTracking()
        {
            Report = Instance;
            PreventReportOverwrite = true;
            AddToolbarButtons();
            PostGridRefresh += SpaceTrackingReport_PostGridRefresh;
        }

        private void AddToolbarButtons()
        {
            tsHistory = new ToolStripButton("History")
            {
                ToolTipText = "View space history for current level",
                Image = Properties.Resources.LineChart_16x,
                DisplayStyle = ToolStripItemDisplayStyle.Image
            };
            tsHistory.Click += (_, _) => ShowHistory(null);

            tsUnits = new ToolStripDropDownButton(UnitPrefixText + DefaultUnit) { ToolTipText = "Select unit", Tag = DefaultUnit };
            foreach (var unit in Units)
            {
                var u = unit;
                var item = new ToolStripMenuItem(unit) { Tag = unit, Checked = unit == DefaultUnit };
                item.Click += (_, _) => SetUnit(u);
                tsUnits.DropDownItems.Add(item);
            }

            tsDecimalPlaces = new ToolStripDropDownButton(DecimalPlacesPrefixText + DefaultDecimalPlaces.ToString()) { ToolTipText = "Decimal places" };
            foreach (var (label, format) in DecimalOptions)
            {
                var f = format;
                var item = new ToolStripMenuItem(label) { Tag = format, Checked = label == DefaultDecimalPlaces.ToString() };
                item.Click += (_, _) => SetDecimalPlaces(f);
                tsDecimalPlaces.DropDownItems.Add(item);
            }

            var insertAt = ToolStrip.Items.IndexOfKey("tsCols");
            insertAt = insertAt >= 0 ? insertAt + 1 : ToolStrip.Items.Count;
            ToolStrip.Items.Insert(insertAt++, tsHistory);
            ToolStrip.Items.Insert(insertAt++, tsUnits);
            ToolStrip.Items.Insert(insertAt, tsDecimalPlaces);
        }

        private void SetUnit(string unit)
        {
            tsUnits.Text = UnitPrefixText + unit;
            tsUnits.Tag = unit;
            foreach (ToolStripMenuItem item in tsUnits.DropDownItems)
                item.Checked = (string)item.Tag == unit;

            if (Grids.Count == 0) return;
            var grid = Grids[0];
            foreach (var u in Units)
            {
                if (grid.Columns["Allocated" + u] is { } allocCol) allocCol.Visible = u == unit;
                if (grid.Columns["Used" + u] is { } usedCol) usedCol.Visible = u == unit;
            }
        }

        private void SpaceTrackingReport_PostGridRefresh(object sender, EventArgs e)
        {
            ToolStrip.Items["tsParams"].Visible = false;

            if (Grids.Count == 0) return;
            var grid = Grids[0];

            AddHistoryColumn(grid);
            grid.CellContentClick -= Grid_HistoryClick;
            grid.CellContentClick += Grid_HistoryClick;

            SetUnit(tsUnits.Tag.ToString());
            ApplyDecimalFormat(grid);
        }

        private string CurrentFormat =>
            tsDecimalPlaces.DropDownItems.Cast<ToolStripMenuItem>().FirstOrDefault(i => i.Checked)?.Tag as string ?? "N1";

        private void SetDecimalPlaces(string format)
        {
            var label = tsDecimalPlaces.DropDownItems.Cast<ToolStripMenuItem>()
                .FirstOrDefault(i => (string)i.Tag == format)?.Text ?? DefaultDecimalPlaces.ToString();
            tsDecimalPlaces.Text = DecimalPlacesPrefixText + label;
            foreach (ToolStripMenuItem item in tsDecimalPlaces.DropDownItems)
                item.Checked = (string)item.Tag == format;

            if (Grids.Count == 0) return;
            ApplyDecimalFormat(Grids[0]);
        }

        private void ApplyDecimalFormat(DataGridView grid)
        {
            var fmt = CurrentFormat;
            foreach (var u in Units)
            {
                if (grid.Columns["Allocated" + u] is { } ac) ac.DefaultCellStyle = Common.DataGridViewCellStyle(fmt);
                if (grid.Columns["Used" + u] is { } uc) uc.DefaultCellStyle = Common.DataGridViewCellStyle(fmt);
            }
        }

        private static void AddHistoryColumn(DataGridView grid)
        {
            if (grid.Columns[HistoryColumnName] != null) return;
            grid.Columns.Add(new DataGridViewLinkColumn
            {
                Name = HistoryColumnName,
                HeaderText = "History",
                Text = "View",
                UseColumnTextForLinkValue = true,
                ReadOnly = true
            });
        }

        private string GetCurrentParam(string paramName) =>
            customParams.FirstOrDefault(p =>
                string.Equals(p.Param.ParameterName, paramName, StringComparison.OrdinalIgnoreCase)
                && !p.UseDefaultValue)?.Param.Value as string;

        private void SetParam(string paramName, string value)
        {
            var idx = customParams.FindIndex(p =>
                string.Equals(p.Param.ParameterName, paramName, StringComparison.OrdinalIgnoreCase));
            if (idx < 0) return;
            var existing = customParams[idx];
            // Replace the item (not mutate it) so the shallow-copied navigation state retains the original reference
            customParams[idx] = new CustomSqlParameter
            {
                Param = new Microsoft.Data.SqlClient.SqlParameter
                {
                    ParameterName = existing.Param.ParameterName,
                    SqlDbType = existing.Param.SqlDbType,
                    Value = value
                },
                UseDefaultValue = false
            };
        }

        private void Grid_HistoryClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || sender is not DataGridView grid) return;
            if (e.ColumnIndex != grid.Columns[HistoryColumnName]?.Index) return;
            var grp = grid.Rows[e.RowIndex].Cells["Grp"]?.Value as string;
            ShowHistory(grp);
        }

        private void ShowHistory(string grp)
        {
            // Fall back to the instance from context when not drilled down from an instance-group
            // level (e.g. a single instance in context where @InstanceGroupName is unset).
            string instance = GetCurrentParam("@InstanceGroupName") ?? context?.InstanceName ?? string.Empty;
            string db = GetCurrentParam("@DBName") ?? string.Empty;
            string fileName = string.Empty;

            switch (CurrentLevel)
            {
                case DrillLevel.Instance:  instance  = grp ?? string.Empty; break;
                case DrillLevel.Database:  db        = grp ?? string.Empty; break;
                case DrillLevel.File:      fileName  = grp ?? string.Empty; break;
            }

            var dbId = context?.DatabaseID ?? -1;
            if (dbId < 1 && !string.IsNullOrEmpty(instance) && !string.IsNullOrEmpty(db))
                dbId = CommonData.GetDatabaseID(instance, db);

            var form = new DBSpaceHistoryView
            {
                DatabaseID = dbId,
                InstanceGroupName = instance,
                DBName = db,
                NumberFormat = CurrentFormat,
                Unit = tsUnits.Tag?.ToString() ?? DefaultUnit,
                FileName = fileName
            };
            form.ShowSingleInstance();
        }

        protected override void OnContextChanged(bool isDrillDown)
        {
            if (!isDrillDown)
            {
                foreach (ToolStripMenuItem item in tsUnits.DropDownItems)
                    item.Checked = (string)item.Tag == DefaultUnit;
                tsUnits.Text = UnitPrefixText + DefaultUnit;

                foreach (ToolStripMenuItem item in tsDecimalPlaces.DropDownItems)
                    item.Checked = item.Text == DefaultDecimalPlaces.ToString();
                tsDecimalPlaces.Text = DecimalPlacesPrefixText + DefaultDecimalPlaces.ToString();
            }
        }

        /// <summary>
        /// Custom drill-down link that reads level context directly from the owning
        /// SpaceTrackingReport rather than from hidden grid columns.
        /// </summary>
        private sealed class SpaceTrackingDrillDownLink : LinkColumnInfo
        {
            public override void Navigate(DBADashContext context, DataGridViewRow row, int selectedTableIndex, ContainerControl sender)
            {
                if (sender is not SpaceTracking view) return;
                var grp = row.Cells["Grp"]?.Value as string;
                if (string.IsNullOrEmpty(grp)) return;

                if (view.CurrentLevel == DrillLevel.File)
                {
                    view.ShowHistory(grp);
                    return;
                }

                view.PushNavigationState();
                view.DrillDownGridFilters = null;

                if (view.CurrentLevel == DrillLevel.Database)
                    view.SetParam("@DBName", grp);
                else
                    view.SetParam("@InstanceGroupName", grp);

                view.RefreshData();
            }
        }

        public static SystemReport Instance => new()
        {
            ViewType = typeof(SpaceTracking),
            ReportName = "DB Space",
            SchemaName = "dbo",
            ProcedureName = "DBSpace_Get",
            QualifiedProcedureName = "dbo.DBSpace_Get",
            CanEditReport = false,
            TriggerCollectionTypes = new List<string> { CollectionType.DBFiles.ToString() },
            ChartLocation = CustomReport.ChartLocations.Right,
            ChartSplitPercentage = 0.7,
            Charts = new List<CustomReportChart>
            {
                new()
                {
                    TableIndex = 0,
                    Config = new PieChartConfiguration
                    {
                        CategoryColumn = "Grp",
                        ValueColumn = "AllocatedGB",
                        MinSlicePercent = 2,
                        OtherLabel = "{Other}",
                        DataLabelMode = PieLabelMode.ValueAndPercent,
                        DataLabelsValueFormat = "N1",
                        DataLabelsValueSuffix = " GB"
                    }
                }
            },
            CustomReportResults = new Dictionary<int, CustomReportResult>
            {
                {
                    0, new CustomReportResult
                    {
                        ResultName = "DB Space",
                        Columns = new Dictionary<string, ColumnMetadata>
                        {
                            { "Grp", new ColumnMetadata { Alias = "Name", Link = new SpaceTrackingDrillDownLink(), Description = "Click to drill down" } },
                            { "AllocatedGB", new ColumnMetadata { Alias = "Allocated (GB)", FormatString = "N1", Description = "Allocated space in GB" } },
                            { "UsedGB", new ColumnMetadata { Alias = "Used (GB)", FormatString = "N1", Description = "Used space in GB" } },
                            { "AllocatedMB", new ColumnMetadata { Alias = "Allocated (MB)", FormatString = "N1", Visible = false, Description = "Allocated space in MB" } },
                            { "UsedMB", new ColumnMetadata { Alias = "Used (MB)", FormatString = "N1", Visible = false, Description = "Used space in MB" } },
                            { "AllocatedTB", new ColumnMetadata { Alias = "Allocated (TB)", FormatString = "N3", Visible = false, Description = "Allocated space in TB" } },
                            { "UsedTB", new ColumnMetadata { Alias = "Used (TB)", FormatString = "N3", Visible = false, Description = "Used space in TB" } },
                            { "Pct", new ColumnMetadata { Alias = "Pct", FormatString = "P2", Visible = false, Description = "Percentage of total allocated space" } },
                        }
                    }
                }
            },
            Params = new Params
            {
                ParamList = new List<Param>
                {
                    new() { ParamName = "@InstanceIDs", ParamType = "IDS" },
                    new() { ParamName = "@DatabaseID", ParamType = "INT" },
                    new() { ParamName = "@InstanceGroupName", ParamType = "NVARCHAR" },
                    new() { ParamName = "@DBName", ParamType = "NVARCHAR" },
                    new() { ParamName = "@ShowHidden", ParamType = "BIT" },
                }
            }
        };
    }
}
