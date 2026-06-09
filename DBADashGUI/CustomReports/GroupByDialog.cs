using DBADashGUI.Theme;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI.CustomReports
{
    /// <summary>
    /// Represents a column's configuration within a Group By operation.
    /// </summary>
    public class GroupByColumnConfig
    {
        public int ColumnIndex { get; set; }
        public string HeaderText { get; set; }
        public bool IsGroupKey { get; set; }
        public bool IsNumeric { get; set; }
        public bool IsComparable { get; set; }
        public Type ValueType { get; set; }
        public bool IncludeCountDistinct { get; set; }
        public bool IncludeSum { get; set; }
        public bool IncludeSumPercent { get; set; }
        public bool IncludeAvg { get; set; }
        public bool IncludeMin { get; set; }
        public bool IncludeMax { get; set; }
    }

    /// <summary>
    /// Dialog that allows the user to configure Group By columns and numeric aggregations.
    /// </summary>
    public class GroupByDialog : Form, IThemedControl
    {
        private DataGridView dgv;
        private Button btnOK;
        private Button btnCancel;
        private CheckBox chkIncludeCount;
        private CheckBox chkPercentOfTotal;
        private Panel pnlBottom;
        private Panel pnlTop;
        private Label lblInfo;
        private DataTable dtConfig;

        // DataTable column indices
        private const int DtColGroupBy = 0;
        private const int DtColColumnName = 1;
        private const int DtColIsNumeric = 2;
        private const int DtColIsComparable = 3;
        private const int DtColCountDistinct = 4;
        private const int DtColSum = 5;
        private const int DtColSumPercent = 6;
        private const int DtColAvg = 7;
        private const int DtColMin = 8;
        private const int DtColMax = 9;

        // DataGridView column indices (_IsNumeric and _IsComparable are in the DataTable but NOT shown in the grid)
        private const int DgvColGroupBy = 0;
        private const int DgvColCountDistinct = 2;
        private const int DgvColSum = 3;
        private const int DgvColSumPercent = 4;
        private const int DgvColAvg = 5;
        private const int DgvColMin = 6;
        private const int DgvColMax = 7;

        public bool IncludeCount => chkIncludeCount.Checked;
        public bool IncludePercentOfTotal => chkPercentOfTotal.Checked;

        public List<GroupByColumnConfig> Columns { get; private set; }

        public GroupByDialog(IEnumerable<GroupByColumnConfig> columns)
        {
            Columns = columns.ToList();
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Text = "Group By";
            Size = new Size(700, 480);
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(500, 350);

            // Top info label
            pnlTop = new Panel { Dock = DockStyle.Top, Height = 44, Padding = new Padding(8, 8, 8, 4) };
            lblInfo = new Label
            {
                Dock = DockStyle.Fill,
                Text = "Check columns to group by. For numeric columns, select aggregations (Sum, Avg, Min, Max, Sum %). Min and Max are also available for dates and other comparable types.",
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlTop.Controls.Add(lblInfo);

            // Bottom panel: count check + OK/Cancel
            pnlBottom = new Panel { Dock = DockStyle.Bottom, Height = 52, Padding = new Padding(8, 8, 8, 8) };
            chkIncludeCount = new CheckBox
            {
                Text = "Include Count",
                Checked = true,
                AutoSize = true,
                Anchor = AnchorStyles.Left | AnchorStyles.Bottom
            };
            chkIncludeCount.Location = new Point(8, 14);
            chkPercentOfTotal = new CheckBox
            {
                Text = "Count % of Total",
                Checked = false,
                AutoSize = true,
                Anchor = AnchorStyles.Left | AnchorStyles.Bottom
            };
            chkPercentOfTotal.Location = new Point(130, 14);
            btnOK = new Button
            {
                Text = "&OK",
                DialogResult = DialogResult.OK,
                Size = new Size(88, 30),
                Anchor = AnchorStyles.Right | AnchorStyles.Bottom
            };
            btnCancel = new Button
            {
                Text = "&Cancel",
                DialogResult = DialogResult.Cancel,
                Size = new Size(88, 30),
                Anchor = AnchorStyles.Right | AnchorStyles.Bottom
            };
            pnlBottom.Controls.Add(chkIncludeCount);
            pnlBottom.Controls.Add(chkPercentOfTotal);
            pnlBottom.Controls.Add(btnCancel);
            pnlBottom.Controls.Add(btnOK);

            // Position OK/Cancel on the right
            pnlBottom.Resize += (_, _) =>
            {
                btnCancel.Location = new Point(pnlBottom.Width - btnCancel.Width - 8, 11);
                btnOK.Location = new Point(pnlBottom.Width - btnOK.Width - btnCancel.Width - 16, 11);
            };

            // Grid
            dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = false,
                RowHeadersVisible = false,
                AutoGenerateColumns = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize
            };

            BuildDataTable();
            BuildGridColumns();
            dgv.DataSource = dtConfig;
            dgv.CellValueChanged += Dgv_CellValueChanged;
            dgv.CurrentCellDirtyStateChanged += Dgv_CurrentCellDirtyStateChanged;

            AcceptButton = btnOK;
            CancelButton = btnCancel;

            Controls.Add(dgv);
            Controls.Add(pnlTop);
            Controls.Add(pnlBottom);

            btnOK.Click += BtnOK_Click;
            Load += GroupByDialog_Load;
        }

        private void BuildDataTable()
        {
            dtConfig = new DataTable();
            dtConfig.Columns.Add("GroupBy", typeof(bool));
            dtConfig.Columns.Add("Column", typeof(string));
            dtConfig.Columns.Add("_IsNumeric", typeof(bool));
            dtConfig.Columns.Add("_IsComparable", typeof(bool));
            dtConfig.Columns.Add("CountDistinct", typeof(bool));
            dtConfig.Columns.Add("Sum", typeof(bool));
            dtConfig.Columns.Add("SumPercent", typeof(bool));
            dtConfig.Columns.Add("Avg", typeof(bool));
            dtConfig.Columns.Add("Min", typeof(bool));
            dtConfig.Columns.Add("Max", typeof(bool));

            foreach (var col in Columns)
            {
                dtConfig.Rows.Add(
                    col.IsGroupKey,
                    col.HeaderText,
                    col.IsNumeric,
                    col.IsComparable,
                    col.IncludeCountDistinct,
                    col.IncludeSum,
                    col.IncludeSumPercent,
                    col.IncludeAvg,
                    col.IncludeMin,
                    col.IncludeMax);
            }
        }

        private void BuildGridColumns()
        {
            dgv.Columns.Add(new DataGridViewCheckBoxColumn
            {
                DataPropertyName = "GroupBy",
                HeaderText = "Group By",
                Name = "colGroupBy",
                Width = 70,
                SortMode = DataGridViewColumnSortMode.Automatic,
                ToolTipText = "Check to include this column in the GROUP BY clause"
            });

            dgv.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Column",
                HeaderText = "Column",
                Name = "colColumn",
                ReadOnly = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                SortMode = DataGridViewColumnSortMode.Automatic
            });

            dgv.Columns.Add(new DataGridViewCheckBoxColumn
            {
                DataPropertyName = "CountDistinct",
                HeaderText = "Count Distinct",
                Name = "colCountDistinct",
                Width = 95,
                SortMode = DataGridViewColumnSortMode.Automatic,
                ToolTipText = "Include Count Distinct aggregation (available for all non-group-key columns)"
            });

            dgv.Columns.Add(new DataGridViewCheckBoxColumn
            {
                DataPropertyName = "Sum",
                HeaderText = "Sum",
                Name = "colSum",
                Width = 50,
                SortMode = DataGridViewColumnSortMode.Automatic,
                ToolTipText = "Include Sum aggregation"
            });

            dgv.Columns.Add(new DataGridViewCheckBoxColumn
            {
                DataPropertyName = "SumPercent",
                HeaderText = "Sum %",
                Name = "colSumPercent",
                Width = 60,
                SortMode = DataGridViewColumnSortMode.Automatic,
                ToolTipText = "Include Sum as a percentage of the grand total"
            });

            dgv.Columns.Add(new DataGridViewCheckBoxColumn
            {
                DataPropertyName = "Avg",
                HeaderText = "Avg",
                Name = "colAvg",
                Width = 50,
                SortMode = DataGridViewColumnSortMode.Automatic,
                ToolTipText = "Include Average aggregation"
            });

            dgv.Columns.Add(new DataGridViewCheckBoxColumn
            {
                DataPropertyName = "Min",
                HeaderText = "Min",
                Name = "colMin",
                Width = 50,
                SortMode = DataGridViewColumnSortMode.Automatic,
                ToolTipText = "Include Minimum aggregation (numeric, dates and other comparable types)"
            });

            dgv.Columns.Add(new DataGridViewCheckBoxColumn
            {
                DataPropertyName = "Max",
                HeaderText = "Max",
                Name = "colMax",
                Width = 50,
                SortMode = DataGridViewColumnSortMode.Automatic,
                ToolTipText = "Include Maximum aggregation (numeric, dates and other comparable types)"
            });
        }

        private void GroupByDialog_Load(object sender, EventArgs e)
        {
            UpdateAggregationColumnReadOnly();
            this.ApplyTheme();
        }

        private void Dgv_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            // Commit checkbox edits immediately so CellValueChanged fires
            if (dgv.IsCurrentCellDirty && dgv.CurrentCell is DataGridViewCheckBoxCell)
            {
                dgv.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        private void Dgv_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (e.ColumnIndex == DgvColGroupBy)
            {
                UpdateAggregationColumnReadOnly();
                return;
            }
            if (e.ColumnIndex is < DgvColCountDistinct or > DgvColMax) return;

            var row = GetDataRow(e.RowIndex);
            // Sum, Sum%, Avg are numeric-only — revert if column is not numeric
            if ((e.ColumnIndex is DgvColSum or DgvColSumPercent or DgvColAvg) && !(bool)row[DtColIsNumeric])
            {
                row[e.ColumnIndex switch { DgvColSum => DtColSum, DgvColSumPercent => DtColSumPercent, _ => DtColAvg }] = false;
            }
            // Min, Max are comparable-only — revert if column is not comparable
            else if ((e.ColumnIndex is DgvColMin or DgvColMax) && !(bool)row[DtColIsComparable])
            {
                row[e.ColumnIndex == DgvColMin ? DtColMin : DtColMax] = false;
            }
        }

        private void UpdateAggregationColumnReadOnly()
        {
            for (int rowIndex = 0; rowIndex < dgv.Rows.Count; rowIndex++)
            {
                var dataRow = GetDataRow(rowIndex);
                bool isNumeric    = (bool)dataRow[DtColIsNumeric];
                bool isComparable = (bool)dataRow[DtColIsComparable];
                bool isGroupKey   = (bool)dataRow[DtColGroupBy];

                // Count Distinct: any non-group-key column
                bool cdEnabled = !isGroupKey;
                SetCellEnabled(dgv.Rows[rowIndex].Cells[DgvColCountDistinct], cdEnabled);
                if (!cdEnabled) dataRow[DtColCountDistinct] = false;

                // Sum, Sum%, Avg: numeric non-group-key columns only
                bool numericEnabled = isNumeric && !isGroupKey;
                var dgvNumericCols = new[] { DgvColSum, DgvColSumPercent, DgvColAvg };
                var dtNumericCols  = new[] { DtColSum,  DtColSumPercent,  DtColAvg  };
                for (int i = 0; i < dgvNumericCols.Length; i++)
                {
                    SetCellEnabled(dgv.Rows[rowIndex].Cells[dgvNumericCols[i]], numericEnabled);
                    if (!numericEnabled) dataRow[dtNumericCols[i]] = false;
                }

                // Min, Max: any comparable non-group-key column (dates, strings, numbers, etc.)
                bool comparableEnabled = isComparable && !isGroupKey;
                var dgvComparableCols = new[] { DgvColMin, DgvColMax };
                var dtComparableCols  = new[] { DtColMin,  DtColMax  };
                for (int i = 0; i < dgvComparableCols.Length; i++)
                {
                    SetCellEnabled(dgv.Rows[rowIndex].Cells[dgvComparableCols[i]], comparableEnabled);
                    if (!comparableEnabled) dataRow[dtComparableCols[i]] = false;
                }
            }
            dgv.Refresh();
        }

        private static void SetCellEnabled(DataGridViewCell cell, bool enabled)
        {
            cell.ReadOnly = !enabled;
            cell.Style.BackColor = enabled ? Color.Empty : SystemColors.ControlLight;
            cell.Style.ForeColor = enabled ? Color.Empty : SystemColors.GrayText;
        }

        /// <summary>
        /// Returns the DataRow bound to a given DGV visual row index.
        /// Using DataRowView.Row instead of dtConfig.Rows[visualIndex] ensures
        /// the correct row is retrieved regardless of sort order.
        /// </summary>
        private DataRow GetDataRow(int dgvRowIndex) =>
            ((DataRowView)dgv.Rows[dgvRowIndex].DataBoundItem).Row;

        private void BtnOK_Click(object sender, EventArgs e)
        {
            // Validate: at least one group-by column selected
            bool anyGroupBy = dtConfig.Rows.Cast<DataRow>().Any(r => (bool)r[DtColGroupBy]);
            if (!anyGroupBy)
            {
                MessageBox.Show("Please select at least one column to group by.", "Group By",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
                return;
            }

            // Write back to Columns list
            for (int i = 0; i < Columns.Count; i++)
            {
                var row = dtConfig.Rows[i];
                Columns[i].IsGroupKey = (bool)row[DtColGroupBy];
                Columns[i].IncludeCountDistinct = (bool)row[DtColCountDistinct];
                Columns[i].IncludeSum = (bool)row[DtColSum];
                Columns[i].IncludeSumPercent = (bool)row[DtColSumPercent];
                Columns[i].IncludeAvg = (bool)row[DtColAvg];
                Columns[i].IncludeMin = (bool)row[DtColMin];
                Columns[i].IncludeMax = (bool)row[DtColMax];
            }
        }

        public void ApplyTheme(BaseTheme theme)
        {
            foreach (System.Windows.Forms.Control control in Controls)
            {
                control.ApplyTheme(theme);
            }
            ForeColor = theme.ForegroundColor;
            BackColor = theme.BackgroundColor;
            dgv.ApplyTheme(theme);
        }
    }
}
