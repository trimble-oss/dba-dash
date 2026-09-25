using DBADashGUI.Deadlocks;
using DBADashGUI.QueryPlans;
using DBADashGUI.ShellIntegration;
using System.IO;

namespace DBADashGUI.Viewers
{
    /// <summary>
    /// How the application hosting the viewers presents itself: the name shown in Explorer's Open with menu and in the
    /// viewers' settings menus, and the names its file associations are registered under.
    ///
    /// The DBA Dash GUI and the stand-alone viewer app each register their own, so both can be offered side by side for
    /// a file type without one taking the registration over from the other.
    /// </summary>
    /// <param name="DisplayName">Shown to the user - "DBA Dash".</param>
    /// <param name="RegistryName">
    /// What the registry keys are named after - "DBADash" gives the ProgIDs DBADash.QueryPlan and DBADash.DeadlockGraph.
    /// Changing it leaves whatever an earlier name registered behind, so the DBA Dash GUI must not.
    /// </param>
    /// <param name="Description">The application's description in Settings > Default apps.</param>
    /// <param name="IsFullGui">
    /// True when the host is the full DBA Dash GUI, which the viewers can say opening a file avoids starting.
    /// </param>
    public sealed record ViewerAppIdentity(string DisplayName, string RegistryName, string Description, bool IsFullGui);

    /// <summary>
    /// What both apps that host the viewers - the DBA Dash GUI, when it is started with a file, and the stand-alone viewer
    /// - need to run them: who the app is, and the loop that opens the files it is given.
    /// </summary>
    public static class ViewerApp
    {
        /// <summary>
        /// Set at start-up, before anything from <see cref="FileAssociation"/> is used.  The default is the DBA Dash GUI,
        /// which is the identity its existing registrations were made under.
        /// </summary>
        public static ViewerAppIdentity Identity { get; set; } = new(
            "DBA Dash",
            "DBADash",
            "SQL Server monitoring - includes viewers for deadlock graphs (.xdl) and execution plans (.sqlplan)",
            true);

        /// <summary>
        /// Items the host adds to the end of each viewer's Settings menu - the stand-alone viewer's About and Check for
        /// Updates, say.  Each is made afresh for the menu it goes in, as an item can only be in one menu.
        /// </summary>
        public static IList<Func<ToolStripItem>> SettingsMenuItems { get; } = new List<Func<ToolStripItem>>();

        /// <summary>Adds the host's <see cref="SettingsMenuItems"/> to a Settings menu, after a separator.</summary>
        internal static void AddSettingsMenuItems(ToolStripItemCollection items)
        {
            if (SettingsMenuItems.Count == 0) return;

            items.Add(new ToolStripSeparator());
            foreach (var create in SettingsMenuItems) items.Add(create());
        }

        /// <summary>
        /// The files the viewers open, for the Open dialog: .sqlplan and .xdl, and the .xml either of them is also saved
        /// as.
        /// </summary>
        public const string OpenFileFilter =
            "Query plans and deadlock graphs (*.sqlplan;*.xdl;*.xml)|*.sqlplan;*.xdl;*.xml|" +
            "Query plan (*.sqlplan)|*.sqlplan|Deadlock graph (*.xdl)|*.xdl|XML (*.xml)|*.xml|All files (*.*)|*.*";

        /// <summary>
        /// Opens each file in the viewer for its type - plans on tabs of one window, deadlock graphs on tabs of another.
        /// A file that can't be read is reported and doesn't stop the rest.
        /// </summary>
        public static void OpenFiles(IEnumerable<string> files)
        {
            foreach (var file in files)
            {
                // Which viewer by extension, falling back to the deadlock viewer for the .xml both file
                // types also get saved as - it reports a file it cannot read, which is a better answer than
                // guessing at the content and being confidently wrong about it.
                if (Path.GetExtension(file).Equals(FileAssociation.QueryPlan.Extension,
                        StringComparison.OrdinalIgnoreCase))
                {
                    ViewerLauncher.ShowQueryPlanFile(file);
                }
                else
                {
                    ViewerLauncher.ShowDeadlockGraphFile(file);
                }
            }
        }

        /// <summary>Prompts for plans and deadlock graphs and returns the ones chosen; empty if cancelled.</summary>
        public static IReadOnlyList<string> PromptForFiles(IWin32Window owner = null)
        {
            using var dialog = new OpenFileDialog
            {
                Filter = OpenFileFilter,
                Title = @"Open Query Plan or Deadlock Graph",
                Multiselect = true
            };

            return dialog.ShowDialog(owner) == DialogResult.OK ? dialog.FileNames : [];
        }

        /// <summary>
        /// Runs the viewers for <paramref name="files"/> as the only thing in the process, and returns when the last
        /// window has been closed.
        ///
        /// Explorer starts a copy per file.  The first copy opens them all - plans on tabs of one window - and the rest
        /// hand their files to it and return at once.  See <see cref="ViewerInstance"/>.
        /// </summary>
        public static void Run(IEnumerable<string> files)
        {
            DeadlockViewerForm.IsStandalone = true;
            QueryPlanViewerForm.IsStandalone = true;

            // Full paths: another copy opening them resolves a relative path against its own folder.
            var paths = files.Select(Path.GetFullPath).ToList();

            using var instance = ViewerInstance.Claim();
            if (!instance.IsPrimary && instance.Forward(paths)) return;

            var closing = false;
            instance.Listen(forwarded =>
            {
                // Once the last window has closed the process is on its way out, and a file opened now
                // would vanish with it - the sender opens it instead.
                if (closing) return false;

                OpenFiles(forwarded);
                return true;
            });

            OpenFiles(paths);

            // Nothing opened - each failure has already been reported.
            if (!AnyVisibleForms()) return;

            // Exit once every window is closed - not just the viewers, as a viewer can open other windows that
            // would otherwise be closed out from under the user.  Idle runs once the close has been processed.
            Application.Idle += (_, _) =>
            {
                if (AnyVisibleForms()) return;

                closing = true;
                Application.ExitThread();
            };
            Application.Run();
        }

        private static bool AnyVisibleForms() => Application.OpenForms.Cast<Form>().Any(f => f.Visible);
    }
}
