using DBADashGUI.CustomReports;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI.XETrace
{
    /// <summary>
    /// "Trace History" system report - lists persisted ad-hoc XE trace sessions (<c>XE.XETraceSession</c>) for the
    /// instances in context, with per-row links to view the generated DDL, view the captured data and delete the
    /// trace.  Requires the AdhocXE role (<see cref="CustomReport.ReportVisibilityRole"/>).
    ///
    /// A requester filter is added to the toolbar: it defaults to the current user ("My traces") and can be switched
    /// to "All users" by admins.  Non-admins are locked to their own traces - the filter is fixed and the proc restricts
    /// them server-side (RequestedBy = SUSER_SNAME()) - so they can only see, and therefore only delete, their own.
    /// Admins also get a "Deleted" toggle (a built-in boolean picker driving <c>@IncludeDeleted</c>) to show
    /// soft-deleted traces, which are retained for audit but hidden by default.
    /// </summary>
    internal class XETraceSessionsView : CustomReportView
    {
        private const string MyTracesLabel = "My traces";
        private const string AllUsersLabel = "All users";
        private const string AllUsersParam = "@AllUsers";
        private const string IncludeDeletedParam = "@IncludeDeleted";

        private ToolStripComboBox _requesterFilter;
        private bool _updatingFilter;

        public XETraceSessionsView()
        {
            Report = Instance;
            PreventReportOverwrite = true;
            AddRequesterFilter();
            AddBulkDeleteControls();
        }

        private void AddRequesterFilter()
        {
            ToolStrip.Items.Add(new ToolStripLabel("Requester:"));
            _requesterFilter = new ToolStripComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 130 };
            _requesterFilter.Items.Add(MyTracesLabel);
            _requesterFilter.SelectedIndex = 0; // default to the current user
            _requesterFilter.SelectedIndexChanged += (_, _) => { if (!_updatingFilter) RefreshData(); };
            ToolStrip.Items.Add(_requesterFilter);
        }

        private void AddBulkDeleteControls()
        {
            var delete = new ToolStripDropDownButton("Delete") { DisplayStyle = ToolStripItemDisplayStyle.Text };
            var selected = new ToolStripMenuItem("Delete selected", null, (_, _) => _ = BulkDeleteAsync(selectedOnly: true))
            { ToolTipText = "Delete the captured data for the selected traces." };
            var all = new ToolStripMenuItem("Delete all (visible)", null, (_, _) => _ = BulkDeleteAsync(selectedOnly: false))
            { ToolTipText = "Delete the captured data for every trace currently shown in the grid (respects the current filter)." };
            delete.DropDownItems.Add(selected);
            delete.DropDownItems.Add(all);
            ToolStrip.Items.Add(delete);
        }

        /// <summary>
        /// Bulk soft-delete: gathers the target trace sessions - either the selected rows or every visible (filtered)
        /// row - skipping any already deleted, confirms, then deletes and refreshes.  Ownership is enforced the same way
        /// as the per-row delete: non-admins only ever see their own traces, so every candidate row is theirs to delete.
        /// </summary>
        private async System.Threading.Tasks.Task BulkDeleteAsync(bool selectedOnly)
        {
            var ids = CollectDeletableSessionIds(selectedOnly);
            var scope = selectedOnly ? "selected" : "visible";
            if (ids.Count == 0)
            {
                MessageBox.Show(this, $"There are no {scope} traces to delete.", "Delete Traces",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (MessageBox.Show(this,
                    $"Delete the captured data for {ids.Count} {scope} trace{(ids.Count == 1 ? string.Empty : "s")}?\r\n\r\n" +
                    "The captured events and .xel are removed and cannot be recovered.  A record of each trace is kept for audit.",
                    "Delete Traces", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                return;
            }
            try
            {
                await XETraceRepo.DeleteManyAsync(ids);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Delete Traces", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // Always refresh: DeleteManyAsync deletes sequentially and rethrows on the first failure, so a mid-loop
                // error can still leave earlier rows already soft-deleted - refresh so the grid reflects what was removed.
                RefreshData();
            }
        }

        /// <summary>
        /// The session ids of the rows to bulk-delete: the selected rows, or every currently visible (filtered) row,
        /// across the report's grids.  Rows already soft-deleted are skipped (there's nothing left to delete).
        /// </summary>
        private List<long> CollectDeletableSessionIds(bool selectedOnly)
        {
            var ids = new List<long>();
            foreach (var grid in Grids)
            {
                if (!grid.Columns.Contains("XETraceSessionID")) continue;
                var rows = selectedOnly
                    ? grid.SelectedCells.Cast<DataGridViewCell>().Select(c => c.OwningRow).Distinct()
                    : grid.Rows.Cast<DataGridViewRow>();
                foreach (var row in rows)
                {
                    if (row.IsNewRow || !row.Visible) continue; // Visible excludes rows hidden by a value filter
                    if (grid.Columns.Contains("DeletedDate") && row.Cells["DeletedDate"].Value.DBNullToNull() != null) continue;
                    if (row.Cells["XETraceSessionID"].Value.DBNullToNull() is { } v) ids.Add(Convert.ToInt64(v));
                }
            }
            return ids.Distinct().ToList();
        }

        /// <summary>
        /// Brings the requester filter into line with the current user's admin status: admins get the "All users"
        /// option; non-admins are locked to their own traces (the option is removed and the combo disabled).  Run on
        /// every refresh - and deliberately not latched - because <see cref="DBADashUser.IsAdmin"/> isn't populated
        /// until the user's roles are loaded, which can be after this control is first constructed (it's created up
        /// front in Main.AddTabs) and even after an early refresh, so the state must be able to self-correct.
        /// </summary>
        private void ConfigureRequesterFilter()
        {
            if (_requesterFilter == null) return;
            var isAdmin = DBADashUser.IsAdmin;
            _updatingFilter = true; // suppress the SelectedIndexChanged -> RefreshData re-entrancy while we mutate the list
            try
            {
                var hasAllUsers = _requesterFilter.Items.Contains(AllUsersLabel);
                if (isAdmin && !hasAllUsers)
                {
                    _requesterFilter.Items.Add(AllUsersLabel);
                }
                else if (!isAdmin && hasAllUsers)
                {
                    // A non-admin only ever sees their own traces - drop the option (reselecting "My traces" first).
                    if (AllUsersLabel.Equals(_requesterFilter.SelectedItem as string)) _requesterFilter.SelectedIndex = 0;
                    _requesterFilter.Items.Remove(AllUsersLabel);
                }
                _requesterFilter.Enabled = isAdmin;
                _requesterFilter.ToolTipText = isAdmin ? null : "You can only view your own traces.";
            }
            finally
            {
                _updatingFilter = false;
            }
        }

        /// <summary>
        /// The "Deleted" toggle (a built-in boolean menu-bar picker driving <c>@IncludeDeleted</c>) is admin-only.  It's
        /// added to / removed from the report's pickers here - before the base SetContext (re)builds the toolbar pickers
        /// - so a non-admin never sees it and their @IncludeDeleted stays at the proc default (deleted traces hidden).
        /// Roles are loaded by the time a tab is shown, and this re-runs on every context change, so it self-corrects.
        /// </summary>
        protected override void OnContextChanged(bool isDrillDown)
        {
            base.OnContextChanged(isDrillDown);
            var pickers = Report?.Pickers;
            if (pickers == null) return;
            var existing = pickers.FirstOrDefault(p =>
                p.ParameterName.Equals(IncludeDeletedParam, StringComparison.OrdinalIgnoreCase));
            if (DBADashUser.IsAdmin && existing == null)
            {
                pickers.Add(Picker.CreateBooleanPicker(IncludeDeletedParam, "Deleted", defaultValue: false,
                    trueString: "Show deleted", falseString: "Hide deleted", menuBar: true));
            }
            else if (!DBADashUser.IsAdmin && existing != null)
            {
                pickers.Remove(existing);
            }
        }

        /// <summary>
        /// The Deleted / Deleted By columns are only meaningful when soft-deleted traces are being shown, so they're
        /// hidden by default (see the column metadata) and revealed only while the admin "Show deleted" toggle is on.
        /// </summary>
        protected override void OnPostGridRefresh()
        {
            base.OnPostGridRefresh();
            var showingDeleted = IncludeDeletedSelected();
            foreach (var grid in Grids)
            {
                if (grid.Columns.Contains("DeletedDate")) grid.Columns["DeletedDate"].Visible = showingDeleted;
                if (grid.Columns.Contains("DeletedBy")) grid.Columns["DeletedBy"].Visible = showingDeleted;
                grid.AutoResizeColumnsWithMaxColumnWidth(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            }
        }

        /// <summary>Whether the report is currently including soft-deleted traces (the admin "Show deleted" toggle).</summary>
        private bool IncludeDeletedSelected()
        {
            var p = customParams.FirstOrDefault(x =>
                x.Param.ParameterName.Equals(IncludeDeletedParam, StringComparison.OrdinalIgnoreCase));
            // Only when the picker actually supplied a value (admins) - a non-admin's param stays at its unsent default.
            return p is { UseDefaultValue: false } && p.Param.Value is not (null or DBNull) && Convert.ToBoolean(p.Param.Value);
        }

        /// <summary>
        /// Pushes the requester filter into the report's <c>@AllUsers</c> parameter before each refresh.  Only an admin
        /// who has picked "All users" sends 1 (show everyone); otherwise 0, and the proc restricts to the caller's own
        /// traces server-side (RequestedBy = SUSER_SNAME()).
        /// </summary>
        protected override void OnBeforeRefresh()
        {
            base.OnBeforeRefresh();
            ConfigureRequesterFilter();
            var param = customParams.FirstOrDefault(p =>
                p.Param.ParameterName.Equals(AllUsersParam, StringComparison.OrdinalIgnoreCase));
            if (param == null) return;

            var allUsers = DBADashUser.IsAdmin && _requesterFilter?.SelectedItem as string == AllUsersLabel;
            param.UseDefaultValue = false;
            param.Param.Value = allUsers ? 1 : 0;
        }

        #region Report definition

        private static ColumnMetadata Hidden() => new() { Visible = false };

        public static SystemReport Instance => new()
        {
            ViewType = typeof(XETraceSessionsView),
            ReportName = "XE Trace History",
            Description = "History of ad-hoc Extended Events traces - view the generated DDL, view the captured data, or delete a trace.",
            SchemaName = "dbo",
            ProcedureName = "XETraceSessionReport_Get",
            QualifiedProcedureName = "XE.XETraceSessionReport_Get",
            ReportVisibilityRole = "AdhocXE",
            CanEditReport = false,
            // @AllUsers is driven by the requester combo and @Days by a menu-bar picker, so the generic Parameters
            // button is redundant here (and would let a user edit parameters that OnBeforeRefresh overrides).
            HideParametersButton = true,
            CustomReportResults = new Dictionary<int, CustomReportResult>
            {
                [0] = new CustomReportResult
                {
                    ResultName = "Trace History",
                    Columns = new Dictionary<string, ColumnMetadata>
                    {
                        ["XETraceSessionID"] = new() { Alias = "ID", Description = "Trace session identifier" },
                        ["InstanceGroupName"] = new() { Alias = "Instance" },
                        ["RunInstances"] = new() { Alias = "Trace Instances", Description = "For a multi-instance run, all instances traced together (blank for a single-instance trace)" },
                        ["RequestedBy"] = new() { Alias = "Requested By", Description = "The user who ran the trace" },
                        ["EventTypes"] = new() { Alias = "Events", Description = "Events captured by the trace" },
                        // The clickable Notes cell shows NotesDisplay (the note, or a prompt to add one); the raw Notes
                        // column is hidden and is what the edit link reads/writes.
                        ["NotesDisplay"] = new()
                        {
                            Alias = "Notes",
                            Description = "Free-text note for this trace (e.g. \"Capture for issue #1234\").  Click to add or edit (your own traces, or any if admin).",
                            Link = new XETraceEditNotesLinkColumnInfo()
                        },
                        ["Notes"] = Hidden(),
                        ["StatusDescription"] = new()
                        {
                            Alias = "Status",
                            Highlighting = new CellHighlightingRuleSet("StatusColor") { IsStatusColumn = true }
                        },
                        ["StartTime"] = new() { Alias = "Start Time", Description = "Time the trace started (local time)" },
                        ["EndTime"] = new() { Alias = "End Time", Description = "Time the trace ended (local time)" },
                        ["MaxDurationSeconds"] = new() { Alias = "Max Duration (s)", FormatString = "N0" },
                        ["TotalEvents"] = new() { Alias = "Events Captured", FormatString = "N0" },
                        ["TargetTypeDescription"] = new() { Alias = "Target", Description = "Resolved trace target" },
                        ["Xel"] = new()
                        {
                            Alias = ".xel",
                            Description = "Download the captured .xel file (only shown when one was captured)",
                            Link = new XETraceXelLinkColumnInfo()
                        },
                        ["ErrorMessage"] = new() { Alias = "Error", Description = "Error message for a failed trace" },
                        // Hidden unless the admin "Show deleted" toggle is on - see OnPostGridRefresh.
                        ["DeletedDate"] = new() { Alias = "Deleted", Visible = false, Description = "When the trace was deleted (its captured data was removed; the record is kept for audit)" },
                        ["DeletedBy"] = new() { Alias = "Deleted By", Visible = false, Description = "The user who deleted the trace" },
                        ["DDL"] = new()
                        {
                            Alias = "DDL",
                            Description = "View the CREATE EVENT SESSION DDL generated for this trace",
                            Link = new TextLinkColumnInfo
                            {
                                TargetColumn = "GeneratedDDL",
                                TextHandling = SchemaCompare.CodeEditor.CodeEditorModes.SQL
                            }
                        },
                        ["View Data"] = new()
                        {
                            Alias = "View Data",
                            Description = "View the events captured by this trace",
                            Link = new XETraceViewDataLinkColumnInfo()
                        },
                        ["Delete"] = new()
                        {
                            Alias = "Delete",
                            Description = "Delete this trace's captured data (events + .xel).  The record is retained for audit.",
                            Link = new XETraceDeleteLinkColumnInfo()
                        },
                        // Hidden technical columns
                        ["InstanceID"] = Hidden(),
                        ["StatusColor"] = Hidden(),
                        ["RunGroupID"] = Hidden(),
                        ["GeneratedDDL"] = Hidden(),
                    }
                }.SetDisplayIndexBasedOnColumnOrder()
            },
            Params = new Params
            {
                ParamList = new List<Param>
                {
                    new() { ParamName = "@InstanceIDs", ParamType = "IDS" },
                    new() { ParamName = "@Days", ParamType = "INT" },
                    new() { ParamName = "@AllUsers", ParamType = "BIT" },
                    new() { ParamName = "@IncludeDeleted", ParamType = "BIT" },
                }
            },
            Pickers = new List<Picker>
            {
                new()
                {
                    ParameterName = "@Days",
                    Name = "Days",
                    DefaultValue = 7,
                    DataType = typeof(int),
                    MenuBar = true,
                    PickerItems = new Dictionary<object, string>
                    {
                        [1] = "1 Day",
                        [2] = "2 Days",
                        [7] = "7 Days",
                        [14] = "14 Days",
                        [30] = "30 Days",
                        [60] = "60 Days",
                        [90] = "90 Days",
                        [180] = "180 Days",
                        [365] = "365 Days",
                    }
                }
            }
        };

        #endregion Report definition
    }
}
