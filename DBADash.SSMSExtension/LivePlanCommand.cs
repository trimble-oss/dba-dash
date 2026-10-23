using System;
using System.ComponentModel.Design;
using System.Globalization;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace DBADash.SSMSExtension
{
    /// <summary>
    /// "Open Live Plan in DBA Dash Visualizer" - follows the running query on a results grid row, such as one from
    /// sp_BlitzWho, sp_WhoIsActive or sp_who2, in the app's live plan viewer, which refreshes it as the query runs.  The
    /// row gives the session (session_id or spid) and, where the grid has one, the request's start time (start_time or
    /// request_start_time), so a session that has moved on to another request isn't mistaken for it.  The app connects
    /// to the instance itself, with the query window's connection (<see cref="QueryWindowConnection"/>) - SSMS can't
    /// hand over a live plan, only a snapshot of one.  On the results grid's right-click menu, visible only for a grid
    /// with a session column.
    /// </summary>
    internal sealed class LivePlanCommand
    {
        public const int CommandId = 0x0102;

        private static readonly string[] SessionColumns = { "session_id", "spid" };
        private static readonly string[] StartTimeColumns = { "start_time", "request_start_time" };

        private LivePlanCommand(OleMenuCommandService commandService)
        {
            var command = new OleMenuCommand(Execute, new CommandID(OpenCommand.CommandSet, CommandId));
            command.BeforeQueryStatus += OnBeforeQueryStatus;
            commandService.AddCommand(command);
        }

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            if (await package.GetServiceAsync(typeof(IMenuCommandService)) is OleMenuCommandService commandService)
            {
                new LivePlanCommand(commandService);
            }
        }

        private void OnBeforeQueryStatus(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (sender is not OleMenuCommand command) return;

            var relevant = ResultsGridReader.TryGetFocused()?.ColumnIndex(SessionColumns) >= 0;            command.Visible = relevant;
            command.Enabled = relevant;
        }

        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                var resultSet = ResultsGridReader.TryGetFocusedRow(out var row);
                var sessionColumn = resultSet?.ColumnIndex(SessionColumns) ?? -1;
                if (sessionColumn < 0)
                {
                    Warn("Could not read the selected row's session.  Select a cell on the row of the query to follow.");
                    return;
                }

                int sessionId;
                try
                {
                    sessionId = Convert.ToInt32(resultSet.GetValue(row, sessionColumn));
                }
                catch
                {
                    Warn("The selected row doesn't have a session id.");
                    return;
                }

                if (sessionId <= 0)
                {
                    Warn("The selected row doesn't have a session id.");
                    return;
                }

                // The start time is what says the session is still running the request on the row. One that's there but
                // can't be read stops here rather than being left out, which would follow whatever the session runs now.
                var startTimeColumn = resultSet.ColumnIndex(StartTimeColumns);
                var startTimeValue = startTimeColumn >= 0 ? resultSet.GetValue(row, startTimeColumn) : null;
                DateTime? startTime = null;
                if (startTimeValue != null)
                {
                    if (!TryReadStartTime(startTimeValue, out var started))
                    {
                        Warn($"Could not read the selected row's {resultSet.ColumnNames[startTimeColumn]} ({startTimeValue}) as a date and time.");
                        return;
                    }
                    startTime = started;
                }

                var connection = QueryWindowConnection.TryGetFocused(out var reason);
                if (connection == null)
                {
                    Warn(reason);
                    return;
                }

                VisualizerLauncher.OpenLivePlan(connection, sessionId, startTime, OpenCommand.DocumentName());
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error opening the live plan in DBA Dash Visualizer:\n\n" + ex.Message,
                    "DBA Dash Visualizer", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// A start time from the grid: a datetime column as it is; a datetimeoffset as the local time it holds, which is
        /// what sys.dm_exec_requests.start_time is; text - a procedure that formats its times - in the formats SQL Server
        /// and this machine write dates in.
        /// </summary>
        internal static bool TryReadStartTime(object value, out DateTime startTime)
        {
            switch (value)
            {
                case DateTime dateTime:
                    startTime = dateTime;
                    return true;
                case DateTimeOffset offset:
                    startTime = offset.DateTime;
                    return true;
                case string text:
                    text = text.Trim();
                    return DateTime.TryParseExact(text, SqlDateTimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out startTime)
                           || DateTime.TryParse(text, CultureInfo.CurrentCulture, DateTimeStyles.None, out startTime);
                default:
                    startTime = default;
                    return false;
            }
        }

        private static readonly string[] SqlDateTimeFormats =
        {
            "yyyy-MM-dd HH:mm:ss.FFFFFFF", "yyyy-MM-ddTHH:mm:ss.FFFFFFF", "yyyy-MM-dd HH:mm:ss", "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-dd HH:mm"
        };

        private static void Warn(string message) =>
            MessageBox.Show(message, "DBA Dash Visualizer", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }
}
