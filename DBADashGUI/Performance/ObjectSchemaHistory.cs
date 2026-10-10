using DBADashGUI.CustomReports;
using DBADashGUI.SchemaCompare;
using DBADashGUI.Theme;
using System;
using System.Data;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBADashGUI.Performance
{
    /// <summary>
    /// The schema snapshot history of one object: each time it was created, changed or dropped, with the change shown
    /// as a diff against the version before it.  The same history the Schema tab shows for an object selected in the
    /// tree, for the object detail window.
    /// </summary>
    public sealed class ObjectSchemaHistory : UserControl
    {
        /// <summary>Changes to show - an object changed more often than this is an outlier, and the Schema tab pages.</summary>
        private const int MaxRows = 200;

        private readonly DBADashDataGridView dgv = new()
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            RowHeadersVisible = false,
            AutoGenerateColumns = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false
        };

        private readonly DiffControl diff = new() { Dock = DockStyle.Fill };
        private readonly SplitContainer split = new() { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal };

        private readonly Label lblEmpty = new()
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Visible = false,
            Text = "No schema snapshots include this object.\n\nSchema snapshots are collected on a schedule of their own and are off for an instance until configured."
        };

        public ObjectSchemaHistory()
        {
            dgv.Columns.AddRange(
                new DataGridViewTextBoxColumn { HeaderText = "Action", DataPropertyName = "Action" },
                new DataGridViewTextBoxColumn { HeaderText = "Snapshot Valid From", DataPropertyName = "SnapshotValidFrom" },
                new DataGridViewTextBoxColumn { HeaderText = "Snapshot Valid To", DataPropertyName = "SnapshotValidTo" },
                new DataGridViewTextBoxColumn { HeaderText = "Date Created", DataPropertyName = "ObjectDateCreated" },
                new DataGridViewTextBoxColumn { HeaderText = "Date Modified", DataPropertyName = "ObjectDateModified" });
            dgv.SelectionChanged += (_, _) => ShowSelectedChange();
            split.Panel1.Controls.Add(dgv);
            split.Panel2.Controls.Add(diff);
            Controls.Add(split);
            Controls.Add(lblEmpty);
        }

        public async Task LoadAsync(long objectId)
        {
            var dt = await CommonData.GetDDLHistoryForObjectAsync(objectId, 1, MaxRows);
            split.Visible = dt.Rows.Count > 0;
            lblEmpty.Visible = dt.Rows.Count == 0;
            dgv.DataSource = dt;
            dgv.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);
            try
            {
                split.SplitterDistance = Math.Min(split.Height / 3, dgv.ColumnHeadersHeight + dgv.RowTemplate.Height * (dt.Rows.Count + 1));
            }
            catch (InvalidOperationException)
            {
                // Not sized yet - the default split is fine.
            }
            this.ApplyTheme();
        }

        /// <summary>The selected change, as a diff against the version it replaced.  A creation is all new and a drop all old.</summary>
        private void ShowSelectedChange()
        {
            if (dgv.SelectedRows.Count != 1 || dgv.SelectedRows[0].DataBoundItem is not DataRowView row) return;
            diff.OldText = row["DDLIDOld"] is long oldId ? Common.DDL(oldId) : string.Empty;
            diff.NewText = row["DDLID"] is long newId ? Common.DDL(newId) : string.Empty;
        }
    }
}
