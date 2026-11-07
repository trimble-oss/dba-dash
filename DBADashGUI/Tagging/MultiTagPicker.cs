using DBADashGUI.Theme;
using DBADashSharedGUI;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI.Tagging
{
    /// <summary>
    /// Pick the tags an alert rule applies to.  Tags with different names must all match (AND), values of the same tag name are alternatives (OR).
    /// Nothing checked = all instances.  Built in code (no designer).
    /// </summary>
    public class MultiTagPicker : Form
    {
        private readonly TreeView tree;
        private readonly TextBox txtSearch;
        private readonly Label lblExpression;
        private readonly Label lblCount;
        private readonly HashSet<int> checkedTagIDs = new();
        private List<DBADashTag> allTags = new();
        private bool updatingChecks;

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public DBADashTagSet SelectedTags { get; set; } = DBADashTagSet.AllInstances();

        public MultiTagPicker()
        {
            Text = @"Apply To Tags";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            MinimumSize = new Size(420, 420);
            Size = new Size(520, 600);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 6,
                Padding = new Padding(8)
            };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));      // help
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));      // search
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));  // tree
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));      // expression
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));      // count
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));  // buttons

            var lblHelp = new Label
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                MaximumSize = new Size(480, 0),
                Margin = new Padding(0, 0, 0, 6),
                Text = @"Instances must have a checked tag for every tag name (AND).  Checking more than one value of the same tag name matches any of them (OR).  Leave everything unchecked to apply to all instances.  For OR across different tag names, create another rule."
            };
            txtSearch = new TextBox { Dock = DockStyle.Fill, PlaceholderText = "Search tags...", Margin = new Padding(0, 0, 0, 6) };
            txtSearch.TextChanged += (_, _) => BuildTree();

            tree = new TreeView { Dock = DockStyle.Fill, CheckBoxes = true, HideSelection = false, Margin = new Padding(0) };
            tree.AfterCheck += Tree_AfterCheck;

            lblExpression = new Label { Dock = DockStyle.Fill, AutoSize = true, MaximumSize = new Size(480, 0), Margin = new Padding(0, 6, 0, 0), Font = new Font(Font, FontStyle.Bold) };
            lblCount = new Label { Dock = DockStyle.Fill, AutoSize = true, Margin = new Padding(0, 2, 0, 0) };

            var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false, Padding = new Padding(0, 6, 0, 0) };
            var bttnCancel = new Button { Text = @"&Cancel", DialogResult = DialogResult.Cancel, AutoSize = true, MinimumSize = new Size(90, 29) };
            var bttnOK = new Button { Text = @"&OK", AutoSize = true, MinimumSize = new Size(90, 29) };
            bttnOK.Click += (_, _) =>
            {
                SelectedTags = DBADashTagSet.FromTagIDs(checkedTagIDs, allTags);
                DialogResult = DialogResult.OK;
            };
            var bttnClear = new Button { Text = @"C&lear (All Instances)", AutoSize = true, MinimumSize = new Size(90, 29) };
            bttnClear.Click += (_, _) =>
            {
                checkedTagIDs.Clear();
                BuildTree();
                UpdatePreview();
            };
            buttons.Controls.Add(bttnCancel);
            buttons.Controls.Add(bttnOK);
            buttons.Controls.Add(bttnClear);
            AcceptButton = bttnOK;
            CancelButton = bttnCancel;

            layout.Controls.Add(lblHelp, 0, 0);
            layout.Controls.Add(txtSearch, 0, 1);
            layout.Controls.Add(tree, 0, 2);
            layout.Controls.Add(lblExpression, 0, 3);
            layout.Controls.Add(lblCount, 0, 4);
            layout.Controls.Add(buttons, 0, 5);
            Controls.Add(layout);

            Load += MultiTagPicker_Load;
        }

        private void MultiTagPicker_Load(object sender, EventArgs e)
        {
            try
            {
                allTags = DBADashTag.GetTags(Common.ConnectionString);
                foreach (var tag in SelectedTags?.Tags ?? new List<DBADashTag>())
                {
                    if (tag.TagID > 0) checkedTagIDs.Add(tag.TagID);
                }
                /* Keep tags that have been deleted visible so they can be unchecked */
                foreach (var missing in SelectedTags?.Tags.Where(t => t.TagID > 0 && allTags.All(a => a.TagID != t.TagID)) ?? Enumerable.Empty<DBADashTag>())
                {
                    allTags.Add(missing);
                }
                BuildTree();
                UpdatePreview();
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex, "Error loading tags");
            }
            this.ApplyTheme();
        }

        private static string NameOf(DBADashTag tag) => tag.TagName ?? "{Deleted tags}";

        private static string ValueOf(DBADashTag tag) => tag.TagName == null && tag.TagValue == null ? $"Tag {tag.TagID}" : tag.TagValue;

        private void BuildTree()
        {
            var search = txtSearch.Text.Trim();
            updatingChecks = true;
            tree.BeginUpdate();
            try
            {
                tree.Nodes.Clear();
                var groups = allTags
                    .Where(t => t.TagID > 0)
                    .Where(t => search.Length == 0
                                || NameOf(t).Contains(search, StringComparison.OrdinalIgnoreCase)
                                || (ValueOf(t) ?? string.Empty).Contains(search, StringComparison.OrdinalIgnoreCase)
                                || checkedTagIDs.Contains(t.TagID))
                    .GroupBy(NameOf, StringComparer.OrdinalIgnoreCase)
                    /* Checked groups and user tags first, then system tags ({Edition} etc) */
                    .OrderByDescending(g => g.Any(t => checkedTagIDs.Contains(t.TagID)))
                    .ThenBy(g => g.Key.StartsWith('{'))
                    .ThenBy(g => g.Key, StringComparer.OrdinalIgnoreCase);

                foreach (var group in groups)
                {
                    var parent = new TreeNode(group.Key);
                    foreach (var tag in group.OrderBy(t => ValueOf(t), StringComparer.OrdinalIgnoreCase))
                    {
                        parent.Nodes.Add(new TreeNode(ValueOf(tag)) { Tag = tag.TagID, Checked = checkedTagIDs.Contains(tag.TagID) });
                    }
                    parent.Checked = parent.Nodes.Count > 0 && parent.Nodes.Cast<TreeNode>().All(n => n.Checked);
                    if (parent.Nodes.Cast<TreeNode>().Any(n => n.Checked) || search.Length > 0)
                    {
                        parent.Expand();
                    }
                    tree.Nodes.Add(parent);
                }
            }
            finally
            {
                tree.EndUpdate();
                updatingChecks = false;
            }
        }

        private void Tree_AfterCheck(object sender, TreeViewEventArgs e)
        {
            if (updatingChecks || e.Node == null) return;
            updatingChecks = true;
            try
            {
                if (e.Node.Tag is int tagID)
                {
                    if (e.Node.Checked) checkedTagIDs.Add(tagID); else checkedTagIDs.Remove(tagID);
                    e.Node.Parent.Checked = e.Node.Parent.Nodes.Cast<TreeNode>().All(n => n.Checked);
                }
                else
                {
                    /* Tag name: check/uncheck all its (visible) values */
                    foreach (TreeNode child in e.Node.Nodes)
                    {
                        child.Checked = e.Node.Checked;
                        if (child.Tag is int id)
                        {
                            if (e.Node.Checked) checkedTagIDs.Add(id); else checkedTagIDs.Remove(id);
                        }
                    }
                    if (e.Node.Checked) e.Node.Expand();
                }
            }
            finally
            {
                updatingChecks = false;
            }
            UpdatePreview();
        }

        private void UpdatePreview()
        {
            var set = DBADashTagSet.FromTagIDs(checkedTagIDs, allTags);
            lblExpression.Text = set.IsAllInstances ? "Applies to: all instances" : "Applies to: " + set;
            try
            {
                using var cn = new SqlConnection(Common.ConnectionString);
                using var cmd = new SqlCommand("Alert.TagScopeMatchingInstanceCount_Get", cn) { CommandType = CommandType.StoredProcedure };
                cmd.Parameters.AddWithValue("@ApplyToTagIDs", set.TagIDs);
                cn.Open();
                using var rdr = cmd.ExecuteReader();
                rdr.Read();
                var count = rdr.GetInt32(0);
                var hidden = rdr.GetInt32(1);
                /* Rules only apply to hidden instances with Apply To Hidden, so count them separately */
                lblCount.Text = $"Matches {count:N0} active instance{(count == 1 ? "" : "s")}" + (hidden > 0 ? $" (+{hidden:N0} hidden)" : "");
                lblCount.ForeColor = count + hidden == 0 ? DashColors.Warning : ForeColor;
            }
            catch (Exception ex)
            {
                lblCount.Text = "Unable to count matching instances: " + ex.Message;
            }
        }
    }
}
