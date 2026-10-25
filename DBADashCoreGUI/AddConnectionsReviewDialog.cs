using DBADash;
using DBADashGUI.CustomReports;
using DBADashGUI.Theme;
using DBADashSharedGUI;
using System.Runtime.Versioning;

namespace DBADashGUI
{
    /// <summary>
    /// Shows what checking new connections found (<see cref="SourceConnectionAdder.CheckAsync"/>) and lets the user decide
    /// what happens to each one before anything is added - add, update the existing connection, add one that couldn't be
    /// connected to, or skip.  One grid instead of a question per problem, so a list of connections can be reviewed
    /// together.  Shared by the service configuration tool and the GUI.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public sealed class AddConnectionsReviewDialog : Form
    {
        private const string ActionSkip = "Skip";
        private const string ActionAdd = "Add";
        private const string ActionAddAnyway = "Add anyway";
        private const string ActionUpdate = "Update existing";
        private const string ActionAddExplicitly = "Add explicitly";

        private readonly DBADashDataGridView dgv = new()
        {
            Dock = DockStyle.Fill,
            AutoGenerateColumns = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            RowHeadersVisible = false,
            EditMode = DataGridViewEditMode.EditOnEnter
        };

        private readonly CheckBox chkDefaultDeadlockSchedule = new()
        {
            Text = "Use the default deadlock collection schedule (every 5 minutes) for connections that collect deadlocks but have no schedule for it",
            AutoSize = true,
            Checked = true
        };

        private readonly bool isRestoreOffered;

        private readonly CheckBox chkRestoreDeleted = new()
        {
            Text = "Restore instances marked deleted in the repository",
            AutoSize = true,
            Checked = true
        };

        /// <param name="checks">A check for each connection, in the order they were entered</param>
        /// <param name="checkedFrom">Where the connections were checked from - e.g. "this computer" or the service's name.  With Windows authentication, that's the account that connected.</param>
        public AddConnectionsReviewDialog(IReadOnlyList<SourceConnectionCheck> checks, string checkedFrom)
        {
            Text = "Review New Connections";
            Size = new Size(1300, 520);
            StartPosition = FormStartPosition.CenterParent;
            ShowIcon = false;
            ShowInTaskbar = false;
            MinimizeBox = false;

            var lblSummary = new Label
            {
                Dock = DockStyle.Top,
                Height = 48,
                Padding = new Padding(6),
                Text = $"Connections were checked from {checkedFrom}.  Choose what to do with each one, then click Add.  Nothing is changed until you do."
            };

            dgv.Columns.AddRange(
                new DataGridViewTextBoxColumn { Name = "Connection", HeaderText = "Connection", ReadOnly = true, Width = 300 },
                new DataGridViewTextBoxColumn { Name = "ConnectionID", HeaderText = "Connection ID", ReadOnly = true, Width = 180 },
                new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Status", ReadOnly = true, Width = 260, DefaultCellStyle = { WrapMode = DataGridViewTriState.True } },
                new DataGridViewTextBoxColumn { Name = "Version", HeaderText = "Version", ReadOnly = true, Width = 160 },
                new DataGridViewTextBoxColumn { Name = "Notes", HeaderText = "Notes", ReadOnly = true, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, DefaultCellStyle = { WrapMode = DataGridViewTriState.True } },
                new DataGridViewComboBoxColumn { Name = "Action", HeaderText = "Action", Width = 150, FlatStyle = FlatStyle.Flat }
            );
            dgv.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            foreach (var check in checks)
            {
                AddRow(check);
            }

            var pnlOptions = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.TopDown,
                AutoSize = true,
                Padding = new Padding(6),
                WrapContents = false
            };
            chkDefaultDeadlockSchedule.Visible = checks.Any(c => c.DeadlockScheduleMissing);
            isRestoreOffered = checks.Any(c => c.IsDeletedInRepository);
            chkRestoreDeleted.Visible = isRestoreOffered;
            pnlOptions.Controls.AddRange(new Control[] { chkDefaultDeadlockSchedule, chkRestoreDeleted });

            var bttnAdd = new Button { Text = "Add", DialogResult = DialogResult.OK, Size = new Size(110, 34) };
            var bttnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Size = new Size(110, 34) };
            var pnlButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                Height = 48,
                Padding = new Padding(6)
            };
            pnlButtons.Controls.AddRange(new Control[] { bttnCancel, bttnAdd });
            AcceptButton = bttnAdd;
            CancelButton = bttnCancel;

            Controls.Add(dgv);
            Controls.Add(lblSummary);
            Controls.Add(pnlOptions);
            Controls.Add(pnlButtons);

            dgv.CurrentCellDirtyStateChanged += (_, _) =>
            {
                if (dgv.IsCurrentCellDirty) dgv.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
            dgv.CellValueChanged += (_, _) => bttnAdd.Enabled = Decisions.Any(d => d != null);
            Load += (_, _) =>
            {
                this.ApplyTheme();
                bttnAdd.Enabled = Decisions.Any(d => d != null);
            };
        }

        private void AddRow(SourceConnectionCheck check)
        {
            var idx = dgv.Rows.Add();
            var row = dgv.Rows[idx];
            // The position of its check, which the decisions are returned in - the grid can be sorted
            row.Tag = idx;
            row.Cells["Connection"].Value = check.ConnectionForPrint;
            row.Cells["ConnectionID"].Value = check.ConnectionID;
            row.Cells["Version"].Value = check.Validated && !string.IsNullOrEmpty(check.ProductVersion) ? $"{check.ProductVersion} {check.Edition}" : string.Empty;
            row.Cells["Notes"].Value = GetNotes(check);

            var status = row.Cells["Status"];
            if (check.Validated)
            {
                status.Value = string.IsNullOrEmpty(check.LoginName) ? "Connected" : $"Connected as {check.LoginName}";
                status.Style.ForeColor = DashColors.Success;
            }
            else if (!string.IsNullOrEmpty(check.ValidationError))
            {
                status.Value = (check.IsInvalid ? "Invalid: " : "Couldn't connect: ") + check.ValidationError;
                status.Style.ForeColor = DashColors.Fail;
            }
            else
            {
                status.Value = "Not checked";
                status.Style.ForeColor = DashColors.Warning;
            }

            var action = (DataGridViewComboBoxCell)row.Cells["Action"];
            var (actions, defaultAction) = GetActions(check);
            action.Items.AddRange(actions.Cast<object>().ToArray());
            action.Value = defaultAction;
            if (actions.Count == 1)
            {
                action.ReadOnly = true;
            }
        }

        /// <summary>The actions offered for a connection, and the one chosen by default.</summary>
        public static (List<string> Actions, string Default) GetActions(SourceConnectionCheck check)
        {
            if (check.IsInvalid)
            {
                return (new List<string> { ActionSkip }, ActionSkip);
            }

            var couldNotConnect = !check.Validated && !string.IsNullOrEmpty(check.ValidationError);
            var add = check.Existing switch
            {
                ExistingConnectionStates.Configured => ActionUpdate,
                ExistingConnectionStates.Discovered => ActionAddExplicitly,
                _ => couldNotConnect ? ActionAddAnyway : ActionAdd
            };
            // Something that needs a closer look isn't done unless it's chosen
            var defaultAction = couldNotConnect || check.Existing == ExistingConnectionStates.Discovered || !string.IsNullOrEmpty(check.CollectedByOtherService)
                ? ActionSkip
                : add;
            return (new List<string> { add, ActionSkip }, defaultAction);
        }

        /// <summary>Everything worth knowing about adding the connection, besides whether it connected.</summary>
        public static string GetNotes(SourceConnectionCheck check)
        {
            var notes = new List<string>();
            switch (check.Existing)
            {
                case ExistingConnectionStates.Configured:
                    notes.Add($"Already configured as {check.ExistingConnectionID}.  Updating it replaces its options and keeps its ConnectionID and schedules.");
                    break;
                case ExistingConnectionStates.Discovered:
                    notes.Add($"Already monitored automatically via the \"{check.DiscoveredVia}\" option.  Adding it lets you configure it explicitly - it won't create a duplicate instance.");
                    break;
            }

            if (!string.IsNullOrEmpty(check.CollectedByOtherService))
            {
                notes.Add($"Already collected by {check.CollectedByOtherService} - adding it here would collect it twice.");
            }

            if (check.IsDeletedInRepository)
            {
                notes.Add("Marked deleted in the repository.");
            }

            if (check.ServerNameIsNull)
            {
                notes.Add("@@SERVERNAME returned NULL.  Consider fixing this using sp_addserver.");
            }

            if (check.SlowQueryWillBeDisabled)
            {
                notes.Add("Slow query capture will be disabled - it needs an extended events session, which isn't supported (SQL 2012 or later, Standard or Enterprise edition on Amazon RDS).");
            }

            if (check.DeadlockScheduleMissing)
            {
                notes.Add("Deadlock collection has no schedule.");
            }

            return string.Join("  ", notes);
        }

        /// <summary>The decisions for each connection, in the order of the checks - null for a connection that's skipped.</summary>
        public IReadOnlyList<SourceConnectionDecisions> Decisions =>
            dgv.Rows.Cast<DataGridViewRow>()
                .OrderBy(row => (int)row.Tag)
                .Select(row => GetDecisions(row.Cells["Action"].Value as string, chkDefaultDeadlockSchedule.Checked))
                .ToList();

        /// <summary>
        /// Restore connections that are marked deleted in the repository.  Not from the check box's Visible, which is
        /// false once the dialog has closed.
        /// </summary>
        public bool RestoreDeleted => isRestoreOffered && chkRestoreDeleted.Checked;

        public static SourceConnectionDecisions GetDecisions(string action, bool useDefaultDeadlockSchedule) => action switch
        {
            null or ActionSkip => null,
            _ => new SourceConnectionDecisions
            {
                // Chosen with the error in front of them
                AddUnvalidated = true,
                UpdateExisting = action == ActionUpdate,
                AddDiscovered = action == ActionAddExplicitly,
                UseDefaultDeadlockSchedule = useDefaultDeadlockSchedule
            }
        };
    }
}
