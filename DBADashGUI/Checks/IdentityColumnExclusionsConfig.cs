using DBADashGUI.Theme;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI.Checks
{
    /// <summary>
    /// Configure identity column table exclusions at root, instance or database level.  Excluded tables are reported
    /// with N/A status, noting the exclusion that matched.  See issue #2021.
    /// </summary>
    public class IdentityColumnExclusionsConfig : Form
    {
        /// <summary>-1 for root level</summary>
        public int InstanceID = -1;

        /// <summary>-1 for root or instance level</summary>
        public int DatabaseID = -1;

        /// <summary>Friendly name of the level being configured.  Shown in the title.</summary>
        public string LevelName;

        private TextBox txtExclusions;
        private Label lblInheritedExclusions;
        private Button bttnSave;

        public IdentityColumnExclusionsConfig()
        {
            BuildLayout();
            this.ApplyTheme();
            Load += IdentityColumnExclusionsConfig_Load;
            // Avoid the existing exclusions all being selected (and easily overwritten) when the dialog opens
            Shown += (_, _) => txtExclusions.Select(txtExclusions.TextLength, 0);
        }

        private void BuildLayout()
        {
            Text = "Identity Column Exclusions";
            Width = 560;
            Height = 460;
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(420, 320);

            var lblInfo = new Label
            {
                Dock = DockStyle.Top,
                AutoSize = false,
                Height = 100,
                Padding = new Padding(8, 6, 8, 6),
                Text = "Excluded tables are reported with N/A status, noting the exclusion that matched.  " +
                       "Enter one table per line as table or schema.table (without brackets).  % can be used as a wildcard.  " +
                       "e.g. MyTable, dbo.MyTable, Staging.%"
            };

            lblInheritedExclusions = new Label
            {
                Dock = DockStyle.Bottom,
                AutoSize = false,
                Height = 64,
                Padding = new Padding(8, 4, 8, 4),
                Visible = false
            };

            txtExclusions = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                AcceptsReturn = true,
                ScrollBars = ScrollBars.Vertical
            };
            var pnlText = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 0, 8, 0) };
            pnlText.Controls.Add(txtExclusions);

            var pnlButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                Height = 52,
                Padding = new Padding(8, 8, 8, 16)
            };
            // Disabled until the existing exclusions load.  Saving after a failed load would replace them with an empty list.
            bttnSave = new Button { Text = "Save", AutoSize = true, Enabled = false };
            var bttnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, AutoSize = true };
            bttnSave.Click += BttnSave_Click;
            pnlButtons.Controls.Add(bttnSave);
            pnlButtons.Controls.Add(bttnCancel);

            Controls.Add(pnlText);
            Controls.Add(lblInfo);
            Controls.Add(lblInheritedExclusions);
            Controls.Add(pnlButtons);

            CancelButton = bttnCancel;
        }

        private void IdentityColumnExclusionsConfig_Load(object sender, EventArgs e)
        {
            Text = "Identity Column Exclusions - " + (InstanceID == -1 ? "{Root}" : string.IsNullOrEmpty(LevelName) ? InstanceID.ToString() : LevelName);
            try
            {
                txtExclusions.Lines = IdentityColumnExclusions.Get(InstanceID, DatabaseID).ToArray();
                if (InstanceID != -1)
                {
                    var inherited = new List<string> { "Root: " + FormatExclusions(IdentityColumnExclusions.Get(-1, -1)) };
                    if (DatabaseID != -1)
                    {
                        inherited.Add("Instance: " + FormatExclusions(IdentityColumnExclusions.Get(InstanceID, -1)));
                    }
                    lblInheritedExclusions.Text = "Exclusions also apply from:" + Environment.NewLine + string.Join(Environment.NewLine, inherited);
                    lblInheritedExclusions.Visible = true;
                }
                bttnSave.Enabled = true;
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex);
            }
        }

        private static string FormatExclusions(List<string> exclusions) => exclusions.Count == 0 ? "{None}" : string.Join(", ", exclusions);

        private void BttnSave_Click(object sender, EventArgs e)
        {
            var exclusions = txtExclusions.Lines.Select(l => l.Trim()).Where(l => l.Length > 0).ToList();
            if (exclusions.Any(x => x.Length > 257))
            {
                MessageBox.Show("Excluded table names can't exceed 257 characters", "Identity Column Exclusions", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                IdentityColumnExclusions.Save(InstanceID, DatabaseID, exclusions);
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex);
                return;
            }
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
