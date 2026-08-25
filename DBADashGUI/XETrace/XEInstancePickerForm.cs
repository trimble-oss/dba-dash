using DBADashGUI.Theme;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI.XETrace
{
    /// <summary>One instance the trace picker can offer.  Regular instances are leaves; Azure SQL databases are grouped
    /// under their (logical) server so each database - a distinct monitored instance in its own right - can be picked
    /// individually rather than the whole server at once.</summary>
    internal sealed class XEInstanceCandidate
    {
        public int InstanceID { get; init; }
        public bool IsAzure { get; init; }
        public string ServerName { get; init; }    // grouping key / node text for Azure
        public string DatabaseName { get; init; }   // leaf text for an Azure database
        public string DisplayName { get; init; }    // leaf text for a regular instance

        /// <summary>Label used when the picked instance is added to the trace's instance list.</summary>
        public string ListLabel => IsAzure ? $"{ServerName} / {DatabaseName}" : DisplayName;
    }

    /// <summary>
    /// Modal tree picker for adding instances to a multi-instance XE trace.  Regular instances are flat, checkable
    /// leaves; Azure SQL databases hang off an expandable server node (checking the server toggles all its databases).
    /// A filter box keeps large estates navigable.  Returns the checked instance IDs, or null if cancelled.
    /// </summary>
    internal static class XEInstancePickerForm
    {
        public static HashSet<int> Pick(IWin32Window owner, string title, IEnumerable<XEInstanceCandidate> candidates)
        {
            var all = (candidates ?? Enumerable.Empty<XEInstanceCandidate>())
                .Where(c => c is { InstanceID: > 0 })
                .ToList();
            var checkedIds = new HashSet<int>();

            using var form = new Form
            {
                Text = title,
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.SizableToolWindow,
                MinimizeBox = false,
                MaximizeBox = false,
                ClientSize = new Size(360, 500),
                ShowInTaskbar = false
            };

            var filter = new TextBox { Dock = DockStyle.Top, PlaceholderText = "Filter..." };
            var tree = new TreeView
            {
                Dock = DockStyle.Fill,
                CheckBoxes = true,
                HideSelection = false,
                ShowLines = true,
                ShowRootLines = true
            };

            var suppress = false; // guards the AfterCheck cascade against re-entrancy / programmatic checks

            void Rebuild()
            {
                var text = filter.Text.Trim();
                suppress = true;
                tree.BeginUpdate();
                tree.Nodes.Clear();

                foreach (var c in all.Where(c => !c.IsAzure && Matches(c.DisplayName, text))
                             .OrderBy(c => c.DisplayName, StringComparer.OrdinalIgnoreCase))
                {
                    tree.Nodes.Add(new TreeNode(c.DisplayName)
                    { Tag = c.InstanceID, Checked = checkedIds.Contains(c.InstanceID) });
                }

                foreach (var g in all.Where(c => c.IsAzure)
                             .GroupBy(c => c.ServerName ?? string.Empty)
                             .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase))
                {
                    // Show the whole server when its name matches, else only the databases that match.
                    var serverMatches = Matches(g.Key, text);
                    var dbs = g.Where(c => serverMatches || Matches(c.DatabaseName, text))
                        .OrderBy(c => c.DatabaseName, StringComparer.OrdinalIgnoreCase).ToList();
                    if (dbs.Count == 0) continue;

                    var parent = new TreeNode(g.Key); // group node - no Tag, never a result on its own
                    foreach (var c in dbs)
                    {
                        parent.Nodes.Add(new TreeNode(c.DatabaseName)
                        { Tag = c.InstanceID, Checked = checkedIds.Contains(c.InstanceID) });
                    }
                    parent.Checked = parent.Nodes.Cast<TreeNode>().All(n => n.Checked);
                    tree.Nodes.Add(parent);
                    parent.Expand();
                }

                tree.EndUpdate();
                suppress = false;
            }

            tree.AfterCheck += (_, e) =>
            {
                if (suppress) return;
                suppress = true;
                try
                {
                    if (e.Node.Tag is int id)
                    {
                        if (e.Node.Checked) checkedIds.Add(id); else checkedIds.Remove(id);
                        if (e.Node.Parent is { } p)
                            p.Checked = p.Nodes.Cast<TreeNode>().All(n => n.Checked);
                    }
                    else
                    {
                        // Server group: cascade the check to every database under it.
                        foreach (TreeNode child in e.Node.Nodes)
                        {
                            child.Checked = e.Node.Checked;
                            if (child.Tag is int cid)
                            {
                                if (e.Node.Checked) checkedIds.Add(cid); else checkedIds.Remove(cid);
                            }
                        }
                    }
                }
                finally { suppress = false; }
            };

            filter.TextChanged += (_, _) => Rebuild();

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                Height = 44,
                Padding = new Padding(6)
            };
            var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Width = 80, Height = 30 };
            var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Width = 80, Height = 30 };
            var none = new Button { Text = "None", Width = 60, Height = 30 };
            var allBtn = new Button { Text = "All", Width = 60, Height = 30 };
            allBtn.Click += (_, _) => { foreach (var c in all) checkedIds.Add(c.InstanceID); Rebuild(); };
            none.Click += (_, _) => { checkedIds.Clear(); Rebuild(); };
            buttons.Controls.AddRange(new Control[] { ok, cancel, none, allBtn });

            form.Controls.Add(tree);
            form.Controls.Add(filter);
            form.Controls.Add(buttons);
            form.AcceptButton = ok;
            form.CancelButton = cancel;

            Rebuild();
            form.ApplyTheme();

            return form.ShowDialog(owner) == DialogResult.OK ? checkedIds : null;
        }

        private static bool Matches(string value, string filter) =>
            string.IsNullOrEmpty(filter) ||
            (value != null && value.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0);
    }
}
