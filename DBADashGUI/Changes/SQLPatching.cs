using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using DBADash;
using DBADashGUI.Changes;
using DBADashGUI.Theme;

namespace DBADashGUI
{
    public partial class SQLPatching : UserControl, ISetContext
    {
        public SQLPatching()
        {
            InitializeComponent();
            dgvVersion.RegisterClearFilter(tsClearFilter);
            dgvHistory.RegisterClearFilter(tsClearFilterHistory);
        }

        private List<int> InstanceIDs;

        private readonly List<DataGridViewColumn> ColumnsList = new()
        {
            new DataGridViewTextBoxColumn() { DataPropertyName = "InstanceDisplayName", HeaderText = "Instance", ReadOnly = true, Width = 150, Name = "colInstance" },
            new DataGridViewTextBoxColumn() { DataPropertyName = "SQLVersion", HeaderText = "Version", ReadOnly = true, Width = 300, Name = "colSQLVersion", SortMode = DataGridViewColumnSortMode.Programmatic},
            new DataGridViewTextBoxColumn() { DataPropertyName = "PatchDate", HeaderText = "SQL Patch Date", ReadOnly = true, Width = 100, Name = "colPatchDate", ToolTipText = "Date when DBA Dash detected a version change"},
            new DataGridViewTextBoxColumn() { DataPropertyName = "ProductVersion", HeaderText = "Product Version", ReadOnly = true, Width = 100, Name = "colProductVersion", SortMode = DataGridViewColumnSortMode.Programmatic},
            new DataGridViewTextBoxColumn() { DataPropertyName = "ProductMajorVersion", HeaderText = "Major", ReadOnly = true, Width = 60, Name = "colProductMajorVersion", Visible = false},
            new DataGridViewTextBoxColumn() { DataPropertyName = "ProductMinorVersion", HeaderText = "Minor", ReadOnly = true, Width = 60, Name = "colProductMinorVersion",Visible = false},
            new DataGridViewTextBoxColumn() { DataPropertyName = "ProductBuild", HeaderText = "Build", ReadOnly = true, Width = 60, Name = "colProductBuild", Visible = false},
            new DataGridViewTextBoxColumn() { DataPropertyName = "ProductRevision", HeaderText = "Revision", ReadOnly = true, Width = 70, Name = "colProductRevision", Visible = false},
            new DataGridViewTextBoxColumn() { DataPropertyName = "Edition", HeaderText = "Edition", ReadOnly = true, Width = 180, Name = "colEdition" },
            new DataGridViewTextBoxColumn() { DataPropertyName = "EngineEdition", HeaderText = "Engine Edition", ReadOnly = true, Width = 60, Name = "colEngineEdition", Visible = false},
            new DataGridViewTextBoxColumn() { DataPropertyName = "EditionID", HeaderText = "Edition ID", ReadOnly = true, Width = 90, Name = "colEditionID", Visible = false},
            new DataGridViewTextBoxColumn() { DataPropertyName = "ProductLevel", HeaderText = "Product Level", ReadOnly = true, Width = 60, Name = "colProductLevel" },
            new DataGridViewTextBoxColumn() { DataPropertyName = "ProductUpdateLevel", HeaderText = "Product Update Level", ReadOnly = true, Width = 60, Name = "colProductUpdateLevel" },
            new DataGridViewLinkColumn() { DataPropertyName = "ProductUpdateReference", HeaderText = "Product Update Reference", ReadOnly = true, Width = 100, Name = "colProductUpdateReference" },
            new DataGridViewTextBoxColumn() { DataPropertyName = "ProductBuildType", HeaderText = "Build Type", ReadOnly = true, Width = 96, Name = "colBuildType", Visible = false},
            new DataGridViewTextBoxColumn() { DataPropertyName = "ResourceVersion", HeaderText = "Resource Version", ReadOnly = true, Width = 137, Name = "colResourceVersion", Visible =false},
            new DataGridViewTextBoxColumn() { DataPropertyName = "ResourceLastUpdateDateTime", HeaderText = "Resource Last Update", ReadOnly = true, Width = 122, Name = "colResourceLastUpdateDateTime", Visible = false },
            new DataGridViewTextBoxColumn() { DataPropertyName = "LicenseType", HeaderText = "License Type", ReadOnly = true, Width = 112, Name = "colLicenseType", Visible = false},
            new DataGridViewTextBoxColumn() { DataPropertyName = "NumLicenses", HeaderText = "Num Licenses", ReadOnly = true, Width = 70, Name = "colNumLicences",Visible = false },
            new DataGridViewTextBoxColumn() { DataPropertyName = "WindowsCaption", HeaderText = "Windows Caption", ReadOnly = true, Width = 100, Name = "colWindowsCaption", Visible = false},
            new DataGridViewTextBoxColumn() { DataPropertyName = "WindowsRelease", HeaderText = "Windows Release", ReadOnly = true, Width = 80, Name = "colWindowsRelease", Visible = false},
            new DataGridViewTextBoxColumn() { DataPropertyName = "WindowsSKU", HeaderText = "Windows SKU", ReadOnly = true, Width = 60, Name = "colWindowsSKU", Visible = false },
            new DataGridViewLinkColumn()  { DataPropertyName = "SupportedUntil", HeaderText = "Supported Until (Extended)", ReadOnly = true,Width=100, Name = "colSupportedUntil", DefaultCellStyle = new DataGridViewCellStyle() { Font = new Font(DefaultFont, FontStyle.Italic)  }, SortMode = DataGridViewColumnSortMode.Automatic, ToolTipText = "Date extended support expires for this version/build of SQL Server" },
            new DataGridViewTextBoxColumn() { DataPropertyName = "DaysUntilSupportExpires", HeaderText = "Days Until Support Expires", ReadOnly = true, Width = 60, Name = "colDaysUntilSupportExpires", Visible = true,DefaultCellStyle = new DataGridViewCellStyle() { Font = new Font(DefaultFont, FontStyle.Italic)  }, ToolTipText = "Days until extended support expires for this version/build of SQL Server" },
            new DataGridViewLinkColumn()  { DataPropertyName = "MainstreamEndDate", HeaderText = "Supported Until (Mainstream)", Width=100, ReadOnly = true, Name = "colMainstreamEndDate", DefaultCellStyle = new DataGridViewCellStyle() { Font = new Font(DefaultFont, FontStyle.Italic)  }, SortMode = DataGridViewColumnSortMode.Automatic, ToolTipText = "Date mainstream support expires for this version of SQL Server"},
            new DataGridViewTextBoxColumn() { DataPropertyName = "DaysUntilMainstreamSupportExpires", HeaderText = "Days Until Mainstream Support Expires", ReadOnly = true, Width = 80, Name = "colDaysUntilMainstreamSupportExpires", Visible = true,DefaultCellStyle = new DataGridViewCellStyle() { Font = new Font(DefaultFont, FontStyle.Italic)  }, ToolTipText = "Days until mainstream support expires for this version of SQL Server"},
            new DataGridViewLinkColumn() { DataPropertyName = "KBList", HeaderText = "KB List", ReadOnly = true, Name = "colKBList", DefaultCellStyle = new DataGridViewCellStyle() { Font = new Font(DefaultFont, FontStyle.Italic)} },
            new DataGridViewCheckBoxColumn() { DataPropertyName = "IsUpdateAvailable", HeaderText = "Update Available?", ReadOnly = true,Width = 80, Name = "colIsUpdateAvailable", DefaultCellStyle = new DataGridViewCellStyle() { Font = new Font(DefaultFont, FontStyle.Italic)  }, ToolTipText = "Value is true when a later build is available."},
            new DataGridViewTextBoxColumn()  { DataPropertyName = "SPBehind", HeaderText = "SP Behind", ReadOnly = true, Name = "colSPBehind",Width=60, DefaultCellStyle = new DataGridViewCellStyle() { Font = new Font(DefaultFont, FontStyle.Italic)  }, ToolTipText = "Service packs behind latest build"},
            new DataGridViewTextBoxColumn()  { DataPropertyName = "CUBehind", HeaderText = "CU Behind", ReadOnly = true, Name = "colCUBehind",Width=60, DefaultCellStyle = new DataGridViewCellStyle() { Font = new Font(DefaultFont, FontStyle.Italic)  }, ToolTipText = "Cumulative updates behind latest build within this service pack"},
            new DataGridViewTextBoxColumn()  { DataPropertyName = "LatestVersion", HeaderText = "Latest Version", ReadOnly = true,Width=100, Name = "colLatestVersion", DefaultCellStyle = new DataGridViewCellStyle() { Font = new Font(DefaultFont, FontStyle.Italic)  }, ToolTipText = "Latest build available for this version of SQL"},
            new DataGridViewTextBoxColumn()  { DataPropertyName = "LatestVersionPatchLevel", HeaderText = "Latest Version Patch Level",Width=90, ReadOnly = true, Name = "colLatestVersionPatchLevel", DefaultCellStyle = new DataGridViewCellStyle() { Font = new Font(DefaultFont, FontStyle.Italic)  }, ToolTipText = "Latest build available for this version of SQL" },
            new DataGridViewCheckBoxColumn() { DataPropertyName = "IsWindowsUpdate", HeaderText = "Is Windows Update", ReadOnly = true,Width = 80, Name = "colIsWindowsUpdate",   DefaultCellStyle = new DataGridViewCellStyle() { Font = new Font(DefaultFont, FontStyle.Italic)  }, ToolTipText = "SQL Server is patched through Windows update. Checks 'Receive updates for other Microsoft products' setting of Windows update", IndeterminateValue = DBNull.Value, ThreeState = true, SortMode = DataGridViewColumnSortMode.Automatic},
        };

        public void SetContext(DBADashContext _context)
        {
            InstanceIDs = _context.RegularInstanceIDs.ToList();
            RefreshData();
        }

        public void RefreshData()
        {
            dgvVersion.Columns[0].Frozen = Common.FreezeKeyColumn;
            dgvHistory.Columns[0].Frozen = Common.FreezeKeyColumn;
            RefreshHistory();
            RefreshVersion();
        }

        private void RefreshVersion()
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using SqlCommand cmd = new("dbo.InstanceVersionInfo_Get", cn) { CommandType = CommandType.StoredProcedure };
            cn.Open();
            cmd.Parameters.AddWithValue("@InstanceIDs", string.Join(",", InstanceIDs));
            cmd.Parameters.AddWithValue("ShowHidden", InstanceIDs.Count == 1 || Common.ShowHidden);
            SqlDataAdapter da = new(cmd);
            DataTable dt = new();
            da.Fill(dt);
            DateHelper.ConvertUTCToAppTimeZone(ref dt, new List<string>() { "PatchDate" });
            dgvVersion.AutoGenerateColumns = false;
            dgvVersion.DataSource = new DataView(dt);

            if (dt.Rows.Count <= 0) return;
            var versionDate = Convert.ToDateTime(dt.Rows[0]["BuildReferenceVersion"].DBNullToNull());
            var updatedDate = Convert.ToDateTime(dt.Rows[0]["BuildReferenceUpdated"].DBNullToNull());
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

        private void RefreshHistory()
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.SQLPatching_Get", cn) { CommandType = CommandType.StoredProcedure };
            cn.Open();
            cmd.Parameters.AddWithValue("@InstanceIDs", string.Join(",", InstanceIDs));
            cmd.Parameters.AddWithValue("ShowHidden", InstanceIDs.Count == 1 || Common.ShowHidden);
            SqlDataAdapter da = new(cmd);
            DataTable dt = new();
            da.Fill(dt);
            DateHelper.ConvertUTCToAppTimeZone(ref dt);
            dgvHistory.AutoGenerateColumns = false;
            dgvHistory.DataSource = new DataView(dt);
            dgvHistory.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
        }

        private void TsCopyVersion_Click(object sender, EventArgs e)
        {
            dgvVersion.CopyGrid();
        }

        private void TsCopyHistory_Click(object sender, EventArgs e)
        {
            dgvHistory.CopyGrid();
        }

        private void TsRefreshVersion_Click(object sender, EventArgs e)
        {
            RefreshVersion();
        }

        private void TsRefreshHistory_Click(object sender, EventArgs e)
        {
            RefreshHistory();
        }

        private void TsExcel_Click(object sender, EventArgs e)
        {
            dgvVersion.ExportToExcel();
        }

        private void TsExcelHistory_Click(object sender, EventArgs e)
        {
            dgvHistory.ExportToExcel();
        }

        private void TsCols_Click(object sender, EventArgs e)
        {
            dgvVersion.PromptColumnSelection();
        }

        private async void TsUpdateBuildReference_Click(object sender, EventArgs e)
        {
            try
            {
                await DBADash.BuildReference.Update(Common.ConnectionString);
                RefreshVersion();
                MessageBox.Show("Build reference updated", "Updated", MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error updating build reference\n" + ex.Message, "Error", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void SQLPatching_Load(object sender, EventArgs e)
        {
            dgvVersion.Columns.Clear();
            dgvVersion.Columns.AddRange(ColumnsList.ToArray());
            dgvVersion.ApplyTheme();
            SetThresholdsMenuText();
        }

        private void DgvVersion_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            for (var idx = e.RowIndex; idx < e.RowIndex + e.RowCount; idx += 1)
            {
                var gRow = dgvVersion.Rows[idx];
                var SPBehind = (int?)gRow.Cells["colSPBehind"].Value.DBNullToNull();
                var CUBehind = (int?)gRow.Cells["colCUBehind"].Value.DBNullToNull();
                var daysUntilSupportEnds = (int?)gRow.Cells["colDaysUntilSupportExpires"].Value.DBNullToNull();
                var daysUntilMainstreamEnds = (int?)gRow.Cells["colDaysUntilMainstreamSupportExpires"].Value.DBNullToNull();
                var supportedUntilStatus = daysUntilSupportEnds == null ? DBADashStatus.DBADashStatusEnum.NA :
                    daysUntilSupportEnds < Config.DaysUntilSupportEndsCriticalThreshold ? DBADashStatus.DBADashStatusEnum.Critical :
                    daysUntilSupportEnds <= Config.DaysUntilSupportEndsWarningThreshold ? DBADashStatus.DBADashStatusEnum.Warning :
                    DBADashStatus.DBADashStatusEnum.OK;
                var mainstreamStatus = daysUntilMainstreamEnds == null ? DBADashStatus.DBADashStatusEnum.NA :
                    daysUntilMainstreamEnds < Config.DaysUntilMainstreamSupportEndsCriticalThreshold ? DBADashStatus.DBADashStatusEnum.Critical :
                    daysUntilMainstreamEnds <= Config.DaysUntilMainstreamSupportEndsWarningThreshold ? DBADashStatus.DBADashStatusEnum.Warning :
                    DBADashStatus.DBADashStatusEnum.OK;
                var colSPBehindStatus = SPBehind == null
                    ? DBADashStatus.DBADashStatusEnum.NA
                    : SPBehind >= Config.SPBehindCriticalThreshold ? DBADashStatus.DBADashStatusEnum.Critical :
                        SPBehind >= Config.SPBehindWarningThreshold ? DBADashStatus.DBADashStatusEnum.Warning : DBADashStatus.DBADashStatusEnum.NA;
                var colCUBehindStatus = CUBehind == null
                    ? DBADashStatus.DBADashStatusEnum.NA
                    : CUBehind >= Config.CUBehindCriticalThreshold ? DBADashStatus.DBADashStatusEnum.Critical :
                        CUBehind >= Config.CUBehindWarningThreshold ? DBADashStatus.DBADashStatusEnum.Warning : DBADashStatus.DBADashStatusEnum.NA;

                gRow.Cells["colDaysUntilMainstreamSupportExpires"].SetStatusColor(mainstreamStatus);
                gRow.Cells["colDaysUntilSupportExpires"].SetStatusColor(supportedUntilStatus);
                gRow.Cells["colMainstreamEndDate"].SetStatusColor(mainstreamStatus);
                gRow.Cells["colSupportedUntil"].SetStatusColor(supportedUntilStatus);
                gRow.Cells["colCUBehind"].SetStatusColor(colCUBehindStatus);
                gRow.Cells["colSPBehind"].SetStatusColor(colSPBehindStatus);
                if (gRow.Cells["colIsWindowsUpdate"].Value == DBNull.Value)
                {
                    gRow.Cells["colIsWindowsUpdate"].SetStatusColor(DBADashStatus.DBADashStatusEnum.NA);
                }
                else
                {
                    gRow.Cells["colIsWindowsUpdate"].Style = gRow.DefaultCellStyle;
                }
            }
        }

        private void DgvVersion_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            switch (dgvVersion.Columns[e.ColumnIndex].Name)
            {
                case "colKBList":
                    {
                        if (dgvVersion.Rows[e.RowIndex].Cells[e.ColumnIndex].Value is not string KBs) return;
                        foreach (var KB in KBs.Split(","))
                        {
                            CommonShared.OpenURL("https://support.microsoft.com/help/" + KB.Trim());
                        }

                        break;
                    }
                case "colProductUpdateReference":
                    {
                        if (dgvVersion.Rows[e.RowIndex].Cells[e.ColumnIndex].Value is not string KB) return;
                        CommonShared.OpenURL("https://support.microsoft.com/help/" + KB.Replace("KB", "").Trim());
                        break;
                    }
                case "colSupportedUntil":
                case "colMainstreamEndDate":
                    var row = (DataRowView)dgvVersion.Rows[e.RowIndex].DataBoundItem;
                    var url = row["LifecycleUrl"] as string;
                    if (string.IsNullOrEmpty(url)) return;
                    CommonShared.OpenURL(url);
                    break;
            }
        }

        private static BuildReferenceViewer BuildViewer;

        private void TsViewBuildReference_Click(object sender, EventArgs e)
        {
            if (BuildViewer == null)
            {
                BuildViewer = new();
                BuildViewer.FormClosed += delegate { BuildViewer = null; };
            }
            BuildViewer.Show();
            BuildViewer.Focus();
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
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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

        private void SetThreshold_Click(object sender, EventArgs e)
        {
            var item = (ToolStripMenuItem)sender;
            var setting = item.Tag!.ToString();
            var value = Convert.ToString(RepositorySettings.GetIntSetting(setting, Common.ConnectionString));
            PromptUpdateThreshold(item.Tag.ToString(), value, item.Text);
        }

        private void ResetToDefaultToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Reset thresholds to default values?", "Reset", MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) != DialogResult.Yes) return;
            foreach (var itm in thresholdsToolStripMenuItem.DropDownItems.OfType<ToolStripMenuItem>())
            {
                if (itm.Tag == null) continue;
                var setting = itm.Tag.ToString();
                RepositorySettings.UpdateSetting(setting, null, Common.ConnectionString);
                Config.RefreshConfig();
                RefreshData();
                SetThresholdsMenuText();
            }
        }

        private void DgvVersion_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (dgvVersion.Columns[e.ColumnIndex].Name is "colSQLVersion" or "colProductVersion")
            {
                var sort = "ProductMajorVersion DESC,ProductMinorVersion DESC,ProductBuild DESC,ProductRevision DESC";
                var dv = (DataView)dgvVersion.DataSource;
                dv.Sort = dv.Sort == sort ? sort.Replace(" DESC", " ASC") : sort;
            }
        }
    }
}