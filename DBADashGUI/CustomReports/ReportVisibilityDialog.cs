using DBADash;
using DBADashGUI.Theme;
using Microsoft.SqlServer.Management.Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI.CustomReports
{
    /// <summary>
    /// Configure which instances a user custom report applies to: instance type, tags and included/excluded instances.
    /// Instance and database level reports are hidden in the tree for instances that don't apply.  Root level reports
    /// receive only the applicable instances in @InstanceIDs.
    /// </summary>
    internal class ReportVisibilityDialog : Form
    {
        private readonly CustomReport report;
        private readonly CheckBox chkRegular = new() { Text = "SQL Server", AutoSize = true };
        private readonly CheckBox chkMI = new() { Text = "Azure SQL Managed Instance", AutoSize = true };
        private readonly CheckBox chkAzureDB = new() { Text = "Azure SQL DB", AutoSize = true };
        private readonly CheckedListBox lstTags = NewCheckedList();
        private readonly CheckedListBox lstInclude = NewCheckedList();
        private readonly CheckedListBox lstExclude = NewCheckedList();
        private readonly TextBox txtFilter = new() { Dock = DockStyle.Fill, PlaceholderText = "Filter instances" };

        /// <summary>Instance display name/ConnectionID pairs</summary>
        private List<InstanceItem> instances = new();

        private readonly HashSet<string> include = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> exclude = new(StringComparer.OrdinalIgnoreCase);
        private bool isPopulating;

        private class InstanceItem
        {
            public string DisplayName;
            public string ConnectionID;
            public int InstanceID; // 0 if the instance wasn't found
            public DatabaseEngineEdition EngineEdition;
            public override string ToString() => DisplayName;
        }

        private static CheckedListBox NewCheckedList() => new()
        {
            Dock = DockStyle.Fill,
            CheckOnClick = true,
            IntegralHeight = false,
            MinimumSize = new Size(200, 60)
        };

        public ReportVisibilityDialog(CustomReport report)
        {
            this.report = report;
            SuspendLayout();
            // Sizes below are specified at 96 DPI and scaled to the current DPI
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            Text = "Report Visibility - " + report.ReportName;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            ShowInTaskbar = false;
            Size = new Size(900, 680);
            MinimumSize = new Size(700, 500);

            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, Padding = new Padding(10) };
            // Fixed width column so labels wrap rather than widening the form
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 40));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 60));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            Controls.Add(layout);

            layout.Controls.Add(new Label
            {
                AutoSize = true,
                Dock = DockStyle.Fill,
                Margin = new Padding(3, 3, 3, 8),
                Text = "Instance and database level reports are hidden for instances that don't match these rules.  Root level reports only include matching instances.  Settings are shared with all users.\n\nEnable Show Hidden to see reports hidden by these rules in the tree (grayed out).  Tag and instance rules are specific to this repository and are not included when the report is scripted."
            });

            var types = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, Padding = new Padding(0, 5, 0, 0) };
            types.Controls.AddRange(new Control[] { chkRegular, chkMI, chkAzureDB });
            layout.Controls.Add(NewGroup("Instance types", types, autoSize: true));

            var tagPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1 };
            tagPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            tagPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tagPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            tagPanel.Controls.Add(new Label
            {
                AutoSize = true,
                Dock = DockStyle.Fill,
                Text = "Show only for instances with these tags.  Instances must match every tag name, with any of the selected values for each name.  Leave empty for no tag rule."
            });
            tagPanel.Controls.Add(lstTags);
            layout.Controls.Add(NewGroup("Tags", tagPanel));

            var instancePanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
            instancePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            instancePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            instancePanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            instancePanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            instancePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            instancePanel.Controls.Add(txtFilter, 0, 0);
            instancePanel.SetColumnSpan(txtFilter, 2);
            instancePanel.Controls.Add(new Label { AutoSize = true, Text = "Include (in addition to matching tags):", Margin = new Padding(3, 6, 3, 3) }, 0, 1);
            instancePanel.Controls.Add(new Label { AutoSize = true, Text = "Exclude (always hidden):", Margin = new Padding(3, 6, 3, 3) }, 1, 1);
            instancePanel.Controls.Add(lstInclude, 0, 2);
            instancePanel.Controls.Add(lstExclude, 1, 2);
            layout.Controls.Add(NewGroup("Instances", instancePanel));

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                Margin = new Padding(0, 8, 0, 0)
            };
            var bttnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, AutoSize = true };
            var bttnOK = new Button { Text = "OK", AutoSize = true };
            var bttnClear = new Button { Text = "Clear Rules", AutoSize = true };
            bttnOK.Click += OK_Click;
            bttnClear.Click += (_, _) => ClearRules();
            buttons.Controls.AddRange(new Control[] { bttnCancel, bttnOK, bttnClear });
            layout.Controls.Add(buttons);

            AcceptButton = bttnOK;
            CancelButton = bttnCancel;

            lstInclude.ItemCheck += (_, e) => InstanceChecked(lstInclude, include, e);
            lstExclude.ItemCheck += (_, e) => InstanceChecked(lstExclude, exclude, e);
            txtFilter.TextChanged += (_, _) => PopulateInstanceLists();

            Load += ReportVisibilityDialog_Load;
            this.ApplyTheme();
            ResumeLayout(false);
            PerformLayout();
        }

        protected override void OnLoad(EventArgs e)
        {
            // Keep the dialog (and its buttons) within the screen
            var workingArea = Screen.FromControl(Owner ?? this).WorkingArea;
            Size = new Size(Math.Min(Width, workingArea.Width), Math.Min(Height, workingArea.Height));
            base.OnLoad(e);
        }

        private static GroupBox NewGroup(string text, Control content, bool autoSize = false)
        {
            var grp = new GroupBox { Text = text, Dock = DockStyle.Fill, Padding = new Padding(8), AutoSize = autoSize, AutoSizeMode = AutoSizeMode.GrowAndShrink };
            grp.Controls.Add(content);
            return grp;
        }

        private async void ReportVisibilityDialog_Load(object sender, EventArgs e)
        {
            chkRegular.Checked = report.AppliesTo.HasFlag(CustomReport.InstanceApplicability.Regular);
            chkMI.Checked = report.AppliesTo.HasFlag(CustomReport.InstanceApplicability.ManagedInstance);
            chkAzureDB.Checked = report.AppliesTo.HasFlag(CustomReport.InstanceApplicability.AzureSQLDB);
            include.UnionWith(report.IncludeConnectionIDs ?? new List<string>());
            exclude.UnionWith(report.ExcludeConnectionIDs ?? new List<string>());

            try
            {
                // Tags in use plus any selected tags that are no longer assigned to an instance, so they can be removed
                var tags = DBADashTag.GetTags(Common.ConnectionString)
                    .Where(t => t.TagID > 0)
                    .Select(t => new ReportTag { TagName = t.TagName, TagValue = t.TagValue })
                    .ToList();
                foreach (var selected in report.VisibleTags ?? new List<ReportTag>())
                {
                    if (!tags.Any(t => SameTag(t, selected))) tags.Add(selected);
                }
                foreach (var tag in tags.OrderBy(t => t.TagName.StartsWith('{')).ThenBy(t => t.TagName).ThenBy(t => t.TagValue))
                {
                    lstTags.Items.Add(tag, report.VisibleTags?.Any(t => SameTag(t, tag)) == true);
                }

                var dt = await CommonData.GetInstancesAsync();
                instances = dt.Rows.Cast<DataRow>()
                    .Select(r => new InstanceItem
                    {
                        ConnectionID = Convert.ToString(r["ConnectionID"]),
                        InstanceID = (int)r["InstanceID"],
                        EngineEdition = r["EngineEdition"] == DBNull.Value ? DatabaseEngineEdition.Unknown : (DatabaseEngineEdition)Convert.ToInt32(r["EngineEdition"]),
                        DisplayName = (bool)r["IsAzure"] ? $"{r["Instance"]} / {r["AzureDBName"]}" : Convert.ToString(r["InstanceDisplayName"])
                    })
                    .Where(i => !string.IsNullOrEmpty(i.ConnectionID))
                    .DistinctBy(i => i.ConnectionID, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                // Keep selected instances that are no longer active so they can be removed
                foreach (var connectionID in include.Union(exclude).Where(id => !instances.Any(i => string.Equals(i.ConnectionID, id, StringComparison.OrdinalIgnoreCase))).ToList())
                {
                    instances.Add(new InstanceItem { ConnectionID = connectionID, DisplayName = connectionID + " (not found)" });
                }
                instances = instances.OrderBy(i => i.DisplayName, StringComparer.CurrentCultureIgnoreCase).ToList();
                PopulateInstanceLists();
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex, "Error loading tags/instances");
            }
        }

        private static bool SameTag(ReportTag a, ReportTag b) =>
            string.Equals(a.TagName, b.TagName, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(a.TagValue, b.TagValue, StringComparison.OrdinalIgnoreCase);

        private void PopulateInstanceLists()
        {
            isPopulating = true;
            try
            {
                var filtered = instances.Where(i => string.IsNullOrEmpty(txtFilter.Text) ||
                                                   i.DisplayName.Contains(txtFilter.Text, StringComparison.CurrentCultureIgnoreCase) ||
                                                   i.ConnectionID.Contains(txtFilter.Text, StringComparison.CurrentCultureIgnoreCase)).ToList();
                foreach (var (list, selected) in new[] { (lstInclude, include), (lstExclude, exclude) })
                {
                    list.BeginUpdate();
                    list.Items.Clear();
                    foreach (var item in filtered)
                    {
                        list.Items.Add(item, selected.Contains(item.ConnectionID));
                    }
                    list.EndUpdate();
                }
            }
            finally
            {
                isPopulating = false;
            }
        }

        private void InstanceChecked(CheckedListBox list, HashSet<string> selected, ItemCheckEventArgs e)
        {
            if (isPopulating) return;
            var item = (InstanceItem)list.Items[e.Index];
            if (e.NewValue == CheckState.Checked)
            {
                selected.Add(item.ConnectionID);
            }
            else
            {
                selected.Remove(item.ConnectionID);
            }
        }

        private void ClearRules()
        {
            chkRegular.Checked = chkMI.Checked = chkAzureDB.Checked = true;
            for (var i = 0; i < lstTags.Items.Count; i++) lstTags.SetItemChecked(i, false);
            include.Clear();
            exclude.Clear();
            PopulateInstanceLists();
        }

        private void OK_Click(object sender, EventArgs e)
        {
            var appliesTo = (chkRegular.Checked ? CustomReport.InstanceApplicability.Regular : CustomReport.InstanceApplicability.None)
                            | (chkMI.Checked ? CustomReport.InstanceApplicability.ManagedInstance : CustomReport.InstanceApplicability.None)
                            | (chkAzureDB.Checked ? CustomReport.InstanceApplicability.AzureSQLDB : CustomReport.InstanceApplicability.None);
            if (appliesTo == CustomReport.InstanceApplicability.None)
            {
                MessageBox.Show("Select at least one instance type.", "Report Visibility", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var visibleTags = lstTags.CheckedItems.Cast<ReportTag>().ToList();
            if (!MatchesAnyInstance(appliesTo, visibleTags))
            {
                var message = report.IsRootLevel
                    ? "These rules don't match any current instance so the report won't return data for any instance."
                    : "These rules don't match any current instance so the report will be hidden in the tree.  Enable Show Hidden to see it.";
                if (MessageBox.Show(message + "\n\nSave anyway?", "Report Visibility", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            }

            var prev = (report.AppliesTo, report.VisibleTags, report.IncludeConnectionIDs, report.ExcludeConnectionIDs);
            report.AppliesTo = appliesTo;
            report.VisibleTags = visibleTags;
            report.IncludeConnectionIDs = include.OrderBy(id => id).ToList();
            report.ExcludeConnectionIDs = exclude.OrderBy(id => id).ToList();
            try
            {
                report.Update();
            }
            catch (Exception ex)
            {
                (report.AppliesTo, report.VisibleTags, report.IncludeConnectionIDs, report.ExcludeConnectionIDs) = prev;
                CommonShared.ShowExceptionDialog(ex, "Error saving report visibility");
                return;
            }
            DialogResult = DialogResult.OK;
        }

        private bool MatchesAnyInstance(CustomReport.InstanceApplicability appliesTo, List<ReportTag> visibleTags)
        {
            try
            {
                var activeInstances = instances.Where(i => i.InstanceID > 0).ToList();
                if (activeInstances.Count == 0) return true; // Instances not loaded
                return activeInstances.Any(i => ReportVisibility.AppliesTo(appliesTo, visibleTags, include.ToList(), exclude.ToList(),
                    i.EngineEdition, i.ConnectionID, () => ReportInstanceInfo.GetTags(i.InstanceID)));
            }
            catch (Exception ex)
            {
                // Don't block saving if the check fails
                System.Diagnostics.Debug.WriteLine($"Error checking report visibility rules: {ex}");
                return true;
            }
        }

        /// <summary>Prompt for visibility rules, save them and update the tree</summary>
        public static void ConfigureVisibility(CustomReport report)
        {
            using var frm = new ReportVisibilityDialog(report);
            if (frm.ShowDialog() != DialogResult.OK) return;
            CustomReports.OnReportPlacementChanged(report);
        }
    }
}
