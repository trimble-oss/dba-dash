using System;
using System.ComponentModel.Design;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace DBADash.SSMSExtension
{
    /// <summary>
    /// "Open in DBA Dash Visualizer" - one command handling execution plans and deadlock graphs alike,
    /// rather than a separate command per kind. It sits on SSMS's execution plan right-click menu and on
    /// the Tools menu, visible on either only while something it can actually read is focused: a plan
    /// tab, a deadlock tab, or a plain XML tab holding one of the two (e.g. sp_BlitzLock's deadlock
    /// output, read by ActiveEditorXmlReader rather than SSMS's own graphical viewers).
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

            var relevant = FocusedControlFinder.Find(ShowPlanXmlReader.ShowPlanControlTypeName) != null
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
                    "Could not read an execution plan or deadlock graph.\n\n" +
                    "Make sure a plan tab, a deadlock graph tab, or an XML tab holding one is focused.",
                    "DBA Dash Visualizer", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error opening in DBA Dash Visualizer:\n\n" + ex.Message,
                    "DBA Dash Visualizer", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
