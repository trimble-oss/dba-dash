using DBADash.JobSchedules;
using DBADashGUI.SchemaCompare;
using System;
using System.Windows.Forms;

namespace DBADashGUI.AgentJobs
{
    /// <summary>
    /// Script Changes in the plan editor: T-SQL to make the moves on the instance, which puts them back when run again with
    /// @Rollback = 1.  DBA Dash doesn't run it - it's for the user to review and run.
    /// </summary>
    internal sealed partial class SchedulePlanEditor
    {
        private ToolStripItem[] CreateScriptItems()
        {
            var script = new ToolStripButton("Script Changes", Properties.Resources.SQLScript_16x, (_, _) => ScriptChanges())
            {
                ToolTipText = "Script the moves to run on the instance.  The script puts the schedules back when run again with @Rollback = 1.  Nothing is run from here."
            };
            return new ToolStripItem[] { new ToolStripSeparator(), script };
        }

        /// <summary>Open the script for the plan's changes in a window of its own</summary>
        public void ScriptChanges()
        {
            var (changes, splits) = _plan.Changes();
            var script = AgentScheduleScripter.Build(changes, splits, InstanceName, DateTime.UtcNow);
            if (script.Changes == 0)
            {
                _whatIfWarning = script.Skipped.Count == 0
                    ? "Nothing to script: move some jobs first (drag their bars, type in Move By or Suggest Start Times)."
                    : "Nothing to script: " + string.Join("; ", script.Skipped) + ".";
                RefreshWhatIf();
                return;
            }
            // A window of its own for each plan scripted, so an earlier script (and its rollback) isn't replaced
            var viewer = new CodeViewer { Language = CodeEditor.CodeEditorModes.SQL, Code = script.Script, Text = $"Schedule Moves - {InstanceName}" };
            viewer.ShowSingleInstance(forceNewInstance: true);
        }
    }
}
