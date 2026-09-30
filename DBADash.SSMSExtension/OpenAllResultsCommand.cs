using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using Task = System.Threading.Tasks.Task;

namespace DBADash.SSMSExtension
{
    /// <summary>
    /// "Open All Results in DBA Dash Visualizer" - every result set in the query window at once, rather than the one
    /// grid <see cref="OpenCommand"/> opens. They go over as a single DataSet, a table per result set, which the app
    /// opens on one tab with the result sets listed down the side. On the results grid's right-click menu and the Tools
    /// menu, visible only when the query window focus is in has more than one results grid - with just the one, it
    /// would do nothing OpenCommand doesn't.
    /// </summary>
    internal sealed class OpenAllResultsCommand
    {
        public const int CommandId = 0x0101;

        private OpenAllResultsCommand(OleMenuCommandService commandService)
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
                new OpenAllResultsCommand(commandService);
            }
        }

        private void OnBeforeQueryStatus(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (sender is not OleMenuCommand command) return;

            var relevant = ResultsGridReader.CountInFocusedWindow() > 1;
            command.Visible = relevant;
            command.Enabled = relevant;
        }

        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                var resultSets = ResultsGridReader.TryGetAll(out var unreadable);
                if (resultSets.Count == 0)
                {
                    MessageBox.Show(
                        "Could not read the results grids. This version of SSMS may not be supported.",
                        "DBA Dash Visualizer", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Rather than open fewer result sets than there are, looking like all of them.
                if (unreadable > 0 && MessageBox.Show(
                        $"{unreadable:N0} of {resultSets.Count + unreadable:N0} results grids could not be read. " +
                        $"Open the other {resultSets.Count:N0}?",
                        "DBA Dash Visualizer", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                {
                    return;
                }

                OpenResultSets(resultSets);
            }
            catch (OperationCanceledException)
            {
                // Cancelled from the wait dialog.
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error opening in DBA Dash Visualizer:\n\n" + ex.Message,
                    "DBA Dash Visualizer", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Copies every result set into one DataSet and opens it - on a background thread under SSMS's cancellable wait
        /// dialog, as <see cref="OpenCommand"/> does for one.
        /// </summary>
        private static void OpenResultSets(List<ResultsGridReader.ResultSet> resultSets)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (!VisualizerLauncher.CheckAppVersion()) return;

            // Worked out here on the UI thread, where the document's name can be read.
            var tables = new List<(ResultsGridReader.ResultSet ResultSet, string Name, string Title)>();
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var resultSet in resultSets)
            {
                var name = OpenCommand.ResultSetName(resultSet);
                var unique = name;
                for (var i = 2; !names.Add(unique); i++) unique = $"{name} ({i})";
                tables.Add((resultSet, unique, OpenCommand.ResultSetTitle(resultSet)));
            }

            var title = OpenCommand.WithDocumentName("All results");
            if (resultSets.Any(r => !r.IsComplete)) title += " (partial)";

            var tempFile = ThreadHelper.JoinableTaskFactory.Run(
                "DBA Dash Visualizer",
                async (progress, cancellationToken) =>
                {
                    await System.Threading.Tasks.TaskScheduler.Default;

                    using var dataSet = new DataSet("Results");
                    dataSet.ExtendedProperties["Title"] = title;

                    for (var i = 0; i < tables.Count; i++)
                    {
                        var (resultSet, name, tableTitle) = tables[i];
                        var caption = $"Reading {name} ({i + 1} of {tables.Count})...";
                        var totalRows = resultSet.RowCount;

                        dataSet.Tables.Add(resultSet.ReadTable(tableTitle, rows => progress.Report(
                                new ThreadedWaitDialogProgressData(caption, $"{rows:N0} of {totalRows:N0} rows", isCancelable: true)),
                            cancellationToken, name));
                    }

                    progress.Report(new ThreadedWaitDialogProgressData("Writing results...", isCancelable: true));
                    return VisualizerLauncher.SaveGridToTemp(dataSet, cancellationToken);
                });

            VisualizerLauncher.OpenFile(tempFile);
        }
    }
}
