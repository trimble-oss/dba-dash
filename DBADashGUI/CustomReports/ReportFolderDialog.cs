using DBADashGUI.Theme;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI.CustomReports
{
    /// <summary>
    /// Move a report (user custom report or system report) to a folder within the Reports folder of the tree.
    /// </summary>
    internal class ReportFolderDialog : Form
    {
        private readonly ComboBox cboFolder;

        public string Folder => CustomReport.NormalizeFolder(cboFolder.Text);

        public ReportFolderDialog(CustomReport report)
        {
            Text = "Move to Folder";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;

            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(10)
            };
            Controls.Add(panel);

            panel.Controls.Add(new Label
            {
                AutoSize = true,
                Margin = new Padding(3, 3, 3, 8),
                MaximumSize = new Size(420, 0),
                Text = $"Folder for \"{report.ReportName}\".  Select an existing folder or type a new one.  Use {CustomReport.FolderSeparator} for nested folders, e.g. Performance{CustomReport.FolderSeparator}Waits.  Leave blank to show the report at the top level.\n\nFolders are shared with all users."
            });

            cboFolder = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDown,
                Dock = DockStyle.Top,
                MinimumSize = new Size(420, 0),
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.ListItems,
                MaxLength = CustomReport.FolderMaxLength
            };
            cboFolder.Items.AddRange(CustomReports.GetCustomReports().FolderPaths.Cast<object>().ToArray());
            cboFolder.Text = report.Folder ?? string.Empty;
            panel.Controls.Add(cboFolder);

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0, 10, 0, 0)
            };
            var bttnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, AutoSize = true };
            var bttnOK = new Button { Text = "OK", DialogResult = DialogResult.OK, AutoSize = true };
            buttons.Controls.Add(bttnCancel);
            buttons.Controls.Add(bttnOK);
            panel.Controls.Add(buttons);

            AcceptButton = bttnOK;
            CancelButton = bttnCancel;
            this.ApplyTheme();
        }

        /// <summary>Prompt for a folder, save it and update the tree</summary>
        public static void MoveReport(CustomReport report)
        {
            using var frm = new ReportFolderDialog(report);
            if (frm.ShowDialog() != DialogResult.OK) return;
            if (string.Equals(frm.Folder, report.Folder, StringComparison.Ordinal)) return;
            try
            {
                report.UpdateFolder(frm.Folder);
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex, "Error moving report to folder");
                return;
            }
            CustomReports.OnReportPlacementChanged(report);
        }
    }
}
