using DBADashGUI.Theme;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI.LogShipping
{
    /// <summary>
    /// Configure log shipping database exclusions at root or (secondary) instance level.  Excluded databases are reported
    /// with N/A status, noting the exclusion that matched.  Applies to restoring/standby databases and to databases missing
    /// from the secondary (log shipping pairs).
    /// </summary>
    public class LogShippingExclusionsConfig : Form
    {
        /// <summary>-1 for root level</summary>
        public int InstanceID = -1;

        /// <summary>Friendly name of the level being configured.  Shown in the title.</summary>
        public string LevelName;

        private TextBox txtExclusions;
        private Label lblRootExclusions;
        private Button bttnSave;

        public LogShippingExclusionsConfig()
        {
            BuildLayout();
            this.ApplyTheme();
            Load += LogShippingExclusionsConfig_Load;
            // Avoid the existing exclusions all being selected (and easily overwritten) when the dialog opens
            Shown += (_, _) => txtExclusions.Select(txtExclusions.TextLength, 0);
        }

        private void BuildLayout()
        {
            Text = "Log Shipping Exclusions";
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
                Text = "Excluded databases are reported with N/A status, noting the exclusion that matched.  " +
                       "Applies to restoring/standby databases and to databases missing from the secondary (log shipping pairs).  " +
                       "Enter one database name per line.  % can be used as a wildcard."
            };

            lblRootExclusions = new Label
            {
                Dock = DockStyle.Bottom,
                AutoSize = false,
                Height = 44,
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
            Controls.Add(lblRootExclusions);
            Controls.Add(pnlButtons);

            CancelButton = bttnCancel;
        }

        private void LogShippingExclusionsConfig_Load(object sender, EventArgs e)
        {
            Text = "Log Shipping Exclusions - " + (InstanceID == -1 ? "{Root}" : string.IsNullOrEmpty(LevelName) ? InstanceID.ToString() : LevelName);
            try
            {
                txtExclusions.Lines = LogShippingExclusions.Get(InstanceID).ToArray();
                if (InstanceID != -1)
                {
                    var rootExclusions = LogShippingExclusions.Get(-1);
                    lblRootExclusions.Text = "Root exclusions also apply: " + (rootExclusions.Count == 0 ? "{None}" : string.Join(", ", rootExclusions));
                    lblRootExclusions.Visible = true;
                }
                bttnSave.Enabled = true;
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex);
            }
        }

        private void BttnSave_Click(object sender, EventArgs e)
        {
            var exclusions = txtExclusions.Lines.Select(l => l.Trim()).Where(l => l.Length > 0).ToList();
            if (exclusions.Any(x => x.Length > 128))
            {
                MessageBox.Show("Excluded database names can't exceed 128 characters", "Log Shipping Exclusions", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                LogShippingExclusions.Save(InstanceID, exclusions);
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
