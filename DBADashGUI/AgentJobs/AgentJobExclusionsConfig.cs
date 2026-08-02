using DBADashGUI.CustomReports;
using DBADashGUI.Theme;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI.AgentJobs
{
    /// <summary>
    /// Manage name/category/description based agent job exclusions at Root or instance level.  Matching jobs are
    /// removed from job-failure monitoring (forced to N/A status).  See issue #1175.
    /// </summary>
    public class AgentJobExclusionsConfig : Form
    {
        public int InstanceID;
        public string ConnectionString;

        /// <summary>Friendly name of the level being configured (e.g. "{Root}" or the instance name).  Shown in the title.</summary>
        public string LevelName;

        private readonly BindingList<AgentJobExclusion> exclusions = new() { AllowNew = true, AllowRemove = true };
        private List<int> originalIDs = new();
        private DBADashDataGridView grid;

        public AgentJobExclusionsConfig()
        {
            BuildLayout();
            this.ApplyTheme();
            Load += AgentJobExclusionsConfig_Load;
        }

        private void BuildLayout()
        {
            Text = "Agent Job Exclusions";
            Width = 720;
            Height = 420;
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(520, 300);

            var lblInfo = new Label
            {
                Dock = DockStyle.Top,
                AutoSize = false,
                Height = 68,
                Padding = new Padding(8, 6, 8, 6),
                Text = "Exclude jobs from failure monitoring by name, category and/or description.  Filters support wildcards " +
                       "(e.g. %dev%).  A blank filter matches any value; at least one filter is required per row."
            };

            grid = new DBADashDataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                AllowUserToAddRows = true,
                AllowUserToDeleteRows = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2
            };
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(AgentJobExclusion.JobNameFilter),
                HeaderText = "Job Name Filter",
                FillWeight = 35
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(AgentJobExclusion.CategoryFilter),
                HeaderText = "Category Filter",
                FillWeight = 30
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(AgentJobExclusion.DescriptionFilter),
                HeaderText = "Description Filter",
                FillWeight = 35
            });

            var pnlButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                Height = 52,
                Padding = new Padding(8, 8, 8, 16)
            };
            var bttnSave = new Button { Text = "Save", DialogResult = DialogResult.None, AutoSize = true };
            var bttnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, AutoSize = true };
            bttnSave.Click += BttnSave_Click;
            pnlButtons.Controls.Add(bttnSave);
            pnlButtons.Controls.Add(bttnCancel);

            Controls.Add(grid);
            Controls.Add(lblInfo);
            Controls.Add(pnlButtons);

            AcceptButton = bttnSave;
            CancelButton = bttnCancel;
        }

        private void AgentJobExclusionsConfig_Load(object sender, EventArgs e)
        {
            Text = "Agent Job Exclusions - " + (string.IsNullOrEmpty(LevelName) ? (InstanceID == -1 ? "{Root}" : InstanceID.ToString()) : LevelName);
            try
            {
                foreach (var ex in AgentJobExclusion.GetExclusions(InstanceID, ConnectionString))
                {
                    exclusions.Add(ex);
                    originalIDs.Add(ex.AgentJobExclusionID);
                }
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex);
            }
            grid.DataSource = exclusions;
        }

        private void BttnSave_Click(object sender, EventArgs e)
        {
            grid.EndEdit();
            try
            {
                // Insert/update the rows the user kept (ignoring completely blank rows).
                var savedIDs = new List<int>();
                foreach (var ex in exclusions.ToList())
                {
                    if (string.IsNullOrWhiteSpace(ex.JobNameFilter) && string.IsNullOrWhiteSpace(ex.CategoryFilter) && string.IsNullOrWhiteSpace(ex.DescriptionFilter))
                    {
                        continue; // blank row - nothing to save
                    }
                    ex.InstanceID = InstanceID;
                    ex.Save(ConnectionString);
                    savedIDs.Add(ex.AgentJobExclusionID);
                }

                // Delete rows that existed originally but are no longer present.
                foreach (var id in originalIDs.Where(id => id != 0 && !savedIDs.Contains(id)))
                {
                    AgentJobExclusion.Delete(id, ConnectionString);
                }
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
