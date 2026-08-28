using DBADashGUI.CustomReports;
using System;
using System.Windows.Forms;

namespace DBADashGUI.XETrace
{
    /// <summary>
    /// Editable "Notes" link for the Trace History report: prompts for the row's free-text note (e.g. "Capture for
    /// issue #1234"), saves it, and refreshes the report.  The cell shows <c>NotesDisplay</c> (the note, or a prompt
    /// to add one) while the raw value being edited comes from the hidden <c>Notes</c> column.  Ownership is enforced
    /// server-side (<c>XE.XETraceSession_Notes_Upd</c> rejects editing another user's trace unless the caller is
    /// db_owner); the client also only ever shows non-admins their own sessions, so a visible row is safe here.
    /// </summary>
    internal class XETraceEditNotesLinkColumnInfo : LinkColumnInfo
    {
        private const int MaxNotesLength = 1000; // matches XE.XETraceSession.Notes NVARCHAR(1000)

        public string SessionIdColumn { get; set; } = "XETraceSessionID";
        public string NotesColumn { get; set; } = "Notes";

        public override void Navigate(DBADashContext context, DataGridViewRow row, int selectedTableIndex, ContainerControl sender)
        {
            var sessionVal = row.Cells[SessionIdColumn].Value.DBNullToNull();
            if (sessionVal == null) return;
            var sessionId = Convert.ToInt64(sessionVal);

            var current = row.DataGridView.Columns.Contains(NotesColumn)
                ? row.Cells[NotesColumn].Value.DBNullToNull() as string ?? string.Empty
                : string.Empty;

            var note = current;
            if (DBADashSharedGUI.CommonShared.ShowInputDialog(ref note, "Trace Notes",
                    description: $"Note for trace session {sessionId} (max {MaxNotesLength} characters):") != DialogResult.OK)
            {
                return;
            }
            if (note != null && note.Length > MaxNotesLength) note = note[..MaxNotesLength];
            if (string.Equals((note ?? string.Empty).Trim(), current.Trim(), StringComparison.Ordinal)) return; // unchanged

            _ = SaveAsync(sessionId, note, sender);
        }

        private static async System.Threading.Tasks.Task SaveAsync(long sessionId, string note, ContainerControl sender)
        {
            try
            {
                await XETraceRepo.UpdateNotesAsync(sessionId, note);
                (sender as CustomReportView)?.RefreshData();
            }
            catch (Exception ex)
            {
                MessageBox.Show(sender, ex.Message, "Trace Notes", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    /// <summary>
    /// "View Data" link for the Trace History report: opens a read-only viewer over the events already captured for
    /// the row's trace session (or the whole merged run when the row carries a RunGroupID).
    /// </summary>
    internal class XETraceViewDataLinkColumnInfo : LinkColumnInfo
    {
        public string SessionIdColumn { get; set; } = "XETraceSessionID";
        public string RunGroupColumn { get; set; } = "RunGroupID";
        public string DeletedDateColumn { get; set; } = "DeletedDate";

        public override void Navigate(DBADashContext context, DataGridViewRow row, int selectedTableIndex, ContainerControl sender)
        {
            var sessionVal = row.Cells[SessionIdColumn].Value.DBNullToNull();
            if (sessionVal == null) return;
            var sessionId = Convert.ToInt64(sessionVal);

            // A deleted trace has had its captured data removed - there's nothing to view, so say so rather than
            // opening an empty grid.  (Deleted rows are only visible via the admin "Show deleted" toggle.)
            if (row.DataGridView.Columns.Contains(DeletedDateColumn) &&
                row.Cells[DeletedDateColumn].Value.DBNullToNull() != null)
            {
                MessageBox.Show(sender,
                    "The captured data for this trace has been deleted, so there is nothing to view.\r\n\r\n" +
                    "The record is retained for audit only.",
                    "Trace Data", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            Guid? runGroup = null;
            if (row.DataGridView.Columns.Contains(RunGroupColumn) &&
                row.Cells[RunGroupColumn].Value.DBNullToNull() is Guid g)
            {
                runGroup = g;
            }

            XETraceLauncher.LaunchStoredData(sender, sessionId, runGroup, $"Session {sessionId}");
        }
    }

    /// <summary>
    /// ".xel" download link for the Trace History report: saves the captured event_file bytes for the row's trace to
    /// a chosen path.  The proc only emits link text for rows that actually captured a file, so this only fires where a
    /// download is available.
    /// </summary>
    internal class XETraceXelLinkColumnInfo : LinkColumnInfo
    {
        public string SessionIdColumn { get; set; } = "XETraceSessionID";

        public override void Navigate(DBADashContext context, DataGridViewRow row, int selectedTableIndex, ContainerControl sender)
        {
            var sessionVal = row.Cells[SessionIdColumn].Value.DBNullToNull();
            if (sessionVal == null) return;
            var sessionId = Convert.ToInt64(sessionVal);
            _ = SaveXelAsync(sessionId, sender);
        }

        private static async System.Threading.Tasks.Task SaveXelAsync(long sessionId, ContainerControl sender)
        {
            try
            {
                var xel = await XETraceRepo.GetXelAsync(sessionId);
                if (xel is not { Length: > 0 })
                {
                    MessageBox.Show(sender, "No .xel file is stored for this trace.", "Download .xel",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                using var dlg = new SaveFileDialog
                {
                    Filter = "Extended Events (*.xel)|*.xel",
                    FileName = $"DBADashTrace_{sessionId}.xel"
                };
                if (dlg.ShowDialog(sender) != DialogResult.OK) return;
                System.IO.File.WriteAllBytes(dlg.FileName, xel);
            }
            catch (Exception ex)
            {
                MessageBox.Show(sender, ex.Message, "Download .xel", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    /// <summary>
    /// "Delete" link for the Trace History report: soft-deletes the row's trace (after confirmation) - the captured
    /// events and .xel are removed but the session record is retained for audit - then refreshes the report.  Ownership
    /// is enforced server-side (<c>XE.XETraceSession_Del</c> rejects deleting another user's trace unless the caller is
    /// db_owner); the client also only ever shows non-admins their own sessions, so a visible row is always safe here.
    /// </summary>
    internal class XETraceDeleteLinkColumnInfo : LinkColumnInfo
    {
        public string SessionIdColumn { get; set; } = "XETraceSessionID";
        public string DeletedDateColumn { get; set; } = "DeletedDate";

        public override void Navigate(DBADashContext context, DataGridViewRow row, int selectedTableIndex, ContainerControl sender)
        {
            var sessionVal = row.Cells[SessionIdColumn].Value.DBNullToNull();
            if (sessionVal == null) return;
            var sessionId = Convert.ToInt64(sessionVal);

            // Already deleted (only visible via the admin "Show deleted" toggle) - its data is gone, so there's nothing
            // to delete.  Say so rather than running a no-op delete.
            if (row.DataGridView.Columns.Contains(DeletedDateColumn) &&
                row.Cells[DeletedDateColumn].Value.DBNullToNull() != null)
            {
                MessageBox.Show(sender, "This trace has already been deleted - its captured data has been removed.",
                    "Delete Trace", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (MessageBox.Show(sender,
                    $"Delete the captured data for trace session {sessionId}?\r\n\r\nThe captured events and .xel are removed and cannot be recovered.  A record of the trace is kept for audit.",
                    "Delete Trace", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                return;
            }

            _ = DeleteAsync(sessionId, sender);
        }

        private static async System.Threading.Tasks.Task DeleteAsync(long sessionId, ContainerControl sender)
        {
            try
            {
                await XETraceRepo.DeleteAsync(sessionId);
                (sender as CustomReportView)?.RefreshData();
            }
            catch (Exception ex)
            {
                MessageBox.Show(sender, ex.Message, "Delete Trace", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
