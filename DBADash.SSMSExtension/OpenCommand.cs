using System;
using System.ComponentModel.Design;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using Task = System.Threading.Tasks.Task;

namespace DBADash.SSMSExtension
{
    /// <summary>
    /// "Open in DBA Dash Visualizer" - one command handling execution plans, deadlock graphs and result sets alike,
    /// rather than a separate command per kind. It sits on SSMS's execution plan and results grid right-click menus
    /// and on the Tools menu, visible on any of them only while something it can actually read is focused: a
    /// results grid, a plan tab, a deadlock tab, or a plain XML tab holding one of the two (e.g. sp_BlitzLock's
    /// deadlock output, read by ActiveEditorXmlReader rather than SSMS's own graphical viewers).
    /// </summary>
    internal sealed class OpenCommand
    {
        public static readonly Guid CommandSet = new Guid("0cc07265-7442-475f-99f3-41177ed25148");
        public const int CommandId = 0x0100;

        private OpenCommand(OleMenuCommandService commandService)
        {
            var menuCommandId = new CommandID(CommandSet, CommandId);
            var command = new OleMenuCommand(Execute, menuCommandId);
            command.BeforeQueryStatus += OnBeforeQueryStatus;
            commandService.AddCommand(command);
        }

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            if (await package.GetServiceAsync(typeof(IMenuCommandService)) is OleMenuCommandService commandService)
            {
                new OpenCommand(commandService);
            }
        }

        private void OnBeforeQueryStatus(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (sender is not OleMenuCommand command) return;

            var relevant = ResultsGridReader.IsFocused()
                           || FocusedControlFinder.Find(ShowPlanXmlReader.ShowPlanControlTypeName) != null
                           || FocusedControlFinder.Find(DeadlockXmlReader.DeadlockControlTypeName) != null
                           || ActiveEditorXmlReader.IsRelevant();
            command.Visible = relevant;
            command.Enabled = relevant;
        }

        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                // The grid first, and only when it's the grid focus is actually in: FocusedControlFinder searches the
                // whole query window, so with a grid focused it would still find the plan on the Execution plan tab
                // beside it. For the same reason, a grid that's focused but can't be read is reported here rather than
                // falling through to the searches below, which would open that plan instead.
                if (ResultsGridReader.IsFocused())
                {
                    var resultSet = ResultsGridReader.TryGetFocused();
                    if (resultSet != null)
                    {
                        OpenResultSet(resultSet);
                    }
                    else
                    {
                        MessageBox.Show(
                            "Could not read this results grid. This version of SSMS may not be supported.",
                            "DBA Dash Visualizer", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    return;
                }

                var planXml = ShowPlanXmlReader.TryRead();
                if (!string.IsNullOrEmpty(planXml))
                {
                    VisualizerLauncher.OpenPlan(planXml);
                    return;
                }

                var deadlockXml = DeadlockXmlReader.TryRead();
                if (!string.IsNullOrEmpty(deadlockXml))
                {
                    VisualizerLauncher.OpenDeadlock(deadlockXml);
                    return;
                }

                var editorXml = ActiveEditorXmlReader.TryRead();
                if (!string.IsNullOrEmpty(editorXml))
                {
                    VisualizerLauncher.OpenXml(editorXml);
                    return;
                }

                MessageBox.Show(
                    "Could not read a result set, execution plan or deadlock graph.\n\n" +
                    "Make sure a results grid, a plan tab, a deadlock graph tab, or an XML tab holding one is focused.",
                    "DBA Dash Visualizer", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
        /// Copies the result set to a temp file and opens it. The copy runs on a background thread - a result set
        /// can be millions of rows - with SSMS's own cancellable wait dialog up if it takes more than a moment, the
        /// same way SSMS's Save Results As reads the grid off the UI thread.
        /// </summary>
        private static void OpenResultSet(ResultsGridReader.ResultSet resultSet)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // Before reading what may be millions of rows, not after, in case there's no app new enough to open them.
            if (!VisualizerLauncher.CheckAppVersion()) return;

            var title = ResultSetTitle(resultSet);
            var totalRows = resultSet.RowCount;

            var tempFile = ThreadHelper.JoinableTaskFactory.Run(
                "DBA Dash Visualizer",
                async (progress, cancellationToken) =>
                {
                    await System.Threading.Tasks.TaskScheduler.Default;

                    var table = resultSet.ReadTable(title, rows => progress.Report(new ThreadedWaitDialogProgressData(
                            "Reading results...", $"{rows:N0} of {totalRows:N0} rows", isCancelable: true)),
                        cancellationToken);

                    progress.Report(new ThreadedWaitDialogProgressData("Writing results...", isCancelable: true));
                    return VisualizerLauncher.SaveGridToTemp(table, cancellationToken);
                });

            VisualizerLauncher.OpenFile(tempFile);
        }

        /// <summary>
        /// What the viewer's tab is called: the query window's document and which of its result sets this is - e.g.
        /// "SQLQuery1.sql - Result 2" - rather than the temp file's random name.
        /// </summary>
        internal static string ResultSetTitle(ResultsGridReader.ResultSet resultSet)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var title = WithDocumentName(ResultSetName(resultSet));
            if (!resultSet.IsComplete) title += " (partial)";
            return title;
        }

        /// <summary>"Result 2" - which of the query's result sets this is, where the grid says.</summary>
        internal static string ResultSetName(ResultsGridReader.ResultSet resultSet) =>
            resultSet.Index is { } index ? $"Result {index + 1}" : "Results";

        /// <summary>The query window's document name ahead of <paramref name="title"/>, if it can be had.</summary>
        internal static string WithDocumentName(string title)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            string document = null;
            try
            {
                document = (ServiceProvider.GlobalProvider.GetService(typeof(EnvDTE.DTE)) as EnvDTE.DTE)?.ActiveDocument?.Name;
            }
            catch
            {
                // Just a caption - go without the document's name.
            }

            return string.IsNullOrEmpty(document) ? title : document + " - " + title;
        }
    }
}
