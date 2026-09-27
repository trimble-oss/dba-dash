using System;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace DBADash.SSMSExtension
{
    /// <summary>
    /// Gives the deadlock graph a real right-click "Open in DBA Dash Visualizer" item, even though
    /// SSMS's own deadlock control has no native context menu to attach to (confirmed: unlike
    /// ShowPlanControl, neither DeadlockControl nor its base GraphCtrl override WndProc or expose a
    /// menu of their own - the only menu-related member either has is GraphCtrl's double-click event).
    ///
    /// Since nothing intercepts the standard WinForms right-click pathway, setting the control's own
    /// ContextMenuStrip is enough - WndProc's default WM_CONTEXTMENU handling picks it up like any
    /// other control.
    ///
    /// The only work left is noticing when a deadlock tab exists to attach it to. EnvDTE's
    /// WindowActivated looked like the natural fit but never fired for this tab - it likely isn't
    /// surfaced as a distinct EnvDTE Window the way a query editor tab is. A poll works regardless of
    /// that: FocusedControlFinder walks the *whole* window tree from whatever currently has focus, not
    /// just the active tab, so it finds a DeadlockControl the moment one exists anywhere in the SSMS
    /// window, independent of which tab is active when the timer happens to tick.
    /// </summary>
    internal sealed class DeadlockContextMenuHook
    {
        // Object, not bool: ConditionalWeakTable requires a reference-type value, and there is nothing
        // more meaningful to store here than "this control has already been wired".
        private static readonly ConditionalWeakTable<Control, object> Wired = new();

        private readonly Timer _timer = new() { Interval = 1000 };

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            var hook = new DeadlockContextMenuHook();
            hook._timer.Tick += (_, _) => hook.TryAttach();
            hook._timer.Start();
        }

        private void TryAttach()
        {
            try
            {
                var control = FocusedControlFinder.Find(DeadlockXmlReader.DeadlockControlTypeName);
                if (control == null || Wired.TryGetValue(control, out _)) return;

                Wired.Add(control, null);

                var menu = new ContextMenuStrip();
                menu.Items.Add("Open in DBA Dash Visualizer", IconResource.DBADash, (_, _) => Execute(control));
                control.ContextMenuStrip = menu;
            }
            catch
            {
                // Best effort - a deadlock tab simply keeps its Tools-menu/keybinding path instead.
            }
        }

        private static void Execute(Control control)
        {
            try
            {
                var deadlockXml = DeadlockXmlReader.TryRead(control);
                if (string.IsNullOrEmpty(deadlockXml))
                {
                    MessageBox.Show("Could not read the deadlock graph XML.",
                        "DBA Dash Visualizer", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                VisualizerLauncher.OpenDeadlock(deadlockXml);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error opening the deadlock graph in DBA Dash Visualizer:\n\n" + ex.Message,
                    "DBA Dash Visualizer", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
