using DBADashGUI.Theme;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;

namespace DBADashGUI.CustomReports
{
    /// <summary>
    /// A dictionary of CellHighlightingRuleSets.  Each rule set contains the rules for a single column.  This class has the rules for all columns in the report.  The key value is the name of the column to apply formatting to.
    /// </summary>
    public class CellHighlightingRuleSetCollection : Dictionary<string, CellHighlightingRuleSet>
    {
        /// <summary>
        /// Call this method when a row is added to the DataGridView to apply formatting to the new row
        /// </summary>
        /// <param name="dgv">DataGridView control</param>
        /// <param name="e">DataGridViewRowsAddedEventArgs associated with the RowsAdded event</param>
        public void FormatRowsAdded(DataGridView dgv, DataGridViewRowsAddedEventArgs e)
        {
            if (dgv == null) return;
            var isDark = DBADashUser.SelectedTheme.ThemeIdentifier == ThemeType.Dark;

            if (Count == 0) return;
            for (var idx = e.RowIndex; idx < e.RowIndex + e.RowCount; idx += 1)
            {
                foreach (var rules in this)
                {
                    object value;
                    if (!dgv.Columns.Contains(rules.Key)) break; // Skip if column is not in the grid

                    if (rules.Value.EvaluateConditionAgainstDataSource)
                    {
                        var row = dgv.Rows[idx].DataBoundItem as DataRowView;

                        if (row?.DataView.Table == null || !row.DataView.Table.Columns.Contains(rules.Value.TargetColumn)) return;

                        value = row[rules.Value.TargetColumn];
                    }
                    else
                    {
                        if (!dgv.Columns.Contains(rules.Value.TargetColumn)) continue; // Skip if column is not in the grid
                        var targetCell = dgv.Rows[idx].Cells[rules.Value.TargetColumn];
                        value = targetCell.Value;
                    }
                    var formattedCell = dgv.Rows[idx].Cells[rules.Key];

                    rules.Value.ApplyFormatting(formattedCell, value, isDark, dgv.DefaultCellStyle);
                }
            }
        }
    }
}