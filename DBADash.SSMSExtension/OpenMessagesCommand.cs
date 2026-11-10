using System;
using System.ComponentModel.Design;
using System.Data;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using Task = System.Threading.Tasks.Task;

namespace DBADash.SSMSExtension
{
    /// <summary>
    /// "Open Messages in DBA Dash Visualizer" - the Messages tab on its own, for its STATISTICS IO / TIME output read into
    /// tables when the query returned no result sets to open with it (the results grid's own commands bring the messages
    /// along).  On the Messages tab's right-click menu - SSMS's IDM_SQLWB_SQLRESMSG_CONTEXT, which
    /// VSTextEditorTabPage.OnShowPopupMenu shows for its text pages, so Results to Text gets it too - and the Tools menu,
    /// visible only while one of those pages has focus.
    /// </summary>
    internal sealed class OpenMessagesCommand
    {
        public const int CommandId = 0x0103;

        private OpenMessagesCommand(OleMenuCommandService commandService)
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
                new OpenMessagesCommand(commandService);
            }
        }

        private void OnBeforeQueryStatus(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (sender is not OleMenuCommand command) return;

            var relevant = MessagesReader.FocusedTextPage() != null;
            command.Visible = relevant;
            command.Enabled = relevant;
        }

        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                if (!VisualizerLauncher.CheckAppVersion()) return;

                var messages = MessagesReader.ReadFocusedTextPage();
                if (messages.Rows.Count == 0 && messages.ExtendedProperties["Error"] is string error)
                {
                    MessageBox.Show("Could not read the messages:\n\n" + error,
                        "DBA Dash Visualizer", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // A DataSet of just the messages, which the app opens on its Messages and Statistics tabs alone.
                using var dataSet = new DataSet("Results");
                dataSet.ExtendedProperties["Title"] = OpenCommand.WithDocumentName("Messages");
                dataSet.Tables.Add(messages);

                var tempFile = ThreadHelper.JoinableTaskFactory.Run(
                    "DBA Dash Visualizer",
                    async (progress, cancellationToken) =>
                    {
                        await System.Threading.Tasks.TaskScheduler.Default;
                        progress.Report(new ThreadedWaitDialogProgressData("Writing messages...", isCancelable: true));
                        return VisualizerLauncher.SaveGridToTemp(dataSet, cancellationToken);
                    });

                VisualizerLauncher.OpenFile(tempFile);
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
    }
}
