using DBADashGUI.CustomReports;
using DBADashGUI.ShellIntegration;
using Serilog;
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
        /// Adds "Open with" items for every type the viewers handle - .sqlplan and .xdl alike - to a Settings menu,
        /// the same regardless of which tab, plan or deadlock, is in front: since they share one window there's no
        /// reason the offer should only cover the type on the active tab.  Read fresh each time the menu opens, as
        /// the registration can change from another copy of DBA Dash, or from the user in Default Apps, while this
        /// window is sitting open.
        /// </summary>
        internal static void AddFileAssociationMenuItems(ToolStripDropDownItem menu)
        {
            var appName = Identity.DisplayName;
            var toggles = FileAssociation.All
                .Select(association => (association, item: new ToolStripMenuItem(
                    $"Open {association.Extension} Files with {appName}", null, (_, _) => ToggleFileAssociation(association))))
                .ToList();

            foreach (var (_, item) in toggles) menu.DropDownItems.Add(item);

            var makeDefault = new ToolStripMenuItem($"Make {appName} the Default...", null, (_, _) => MakeDefault())
            {
                ToolTipText = "Opens Settings > Default apps.  Windows only lets you choose the default yourself."
            };
            menu.DropDownItems.Add(makeDefault);

            AddSsmsExtensionMenuItems(menu.DropDownItems);

            menu.DropDownOpening += (_, _) =>
            {
                foreach (var (association, item) in toggles)
                {
                    try
                    {
                        var registered = association.RegisteredExePath;
                        item.Checked = association.IsRegisteredToThisCopy;
                        item.ToolTipText = item.Checked || registered == null
                            ? $"Offer {appName} in Explorer's Open with menu for {FriendlyName(association)} files.  They open in this viewer{(Identity.IsFullGui ? " without starting the full GUI" : string.Empty)}."
                            : $"Currently registered to another copy of {appName}:\n{registered}\n\nClick to use this copy instead.";
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "Unable to read the {extension} file association", association.Extension);
                    }
                }

                makeDefault.Enabled = FileAssociation.All.Where(a => !a.IsOptIn).Any(a => !a.IsDefaultHandler);
            };
        }

        /// <summary>
        /// Adds Install and Uninstall SSMS Extension items - to the viewers' Settings menu, and to the DBA Dash GUI's
        /// Options menu.  Not every build has the SSMS extension embedded (see DBADash.Viewers.GUI.csproj) - a dev
        /// build of just this project still works, it just doesn't offer either of these.
        /// </summary>
        public static void AddSsmsExtensionMenuItems(ToolStripItemCollection items)
        {
            if (!SsmsExtensionInstaller.IsAvailable) return;

            items.Add(new ToolStripMenuItem("Install SSMS Extension...", null, (_, _) => InstallSsmsExtension())
            {
                ToolTipText = "Adds \"Open in DBA Dash Visualizer\" to results grids, execution plans and deadlock graphs in SQL Server Management Studio."
            });
            items.Add(new ToolStripMenuItem("Uninstall SSMS Extension...", null, (_, _) => UninstallSsmsExtension())
            {
                ToolTipText = "Removes the SSMS extension via VSIXInstaller."
            });
        }

        private static string FriendlyName(FileAssociation association) => association.Extension switch
        {
            ".sqlplan" => "execution plan (.sqlplan)",
            ".xdl" => "deadlock graph (.xdl)",
            ".xml" => "plan or deadlock graph (.xml)",
            _ => association.Extension
        };

        private static void ToggleFileAssociation(FileAssociation association)
        {
            try
            {
                if (association.IsRegisteredToThisCopy) association.Unregister();
                else association.Register();
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex, $"Error updating the {association.Extension} file association");
            }
        }

        internal static void InstallSsmsExtension()
        {
            try
            {
                SsmsExtensionInstaller.Install();
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex, "Error installing the SSMS extension");
            }
        }

        private static void UninstallSsmsExtension()
        {
            try
            {
                SsmsExtensionInstaller.Uninstall();
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex, "Error uninstalling the SSMS extension");
            }
        }

        /// <summary>Registers every defaultable type not already registered to this copy, then opens Default apps
        /// to it - the navigation is the same regardless of which association it's called on. Leaves .xml alone:
        /// see <see cref="FileAssociation.IsOptIn"/>.</summary>
        private static void MakeDefault()
        {
            try
            {
                foreach (var association in FileAssociation.All.Where(a => !a.IsOptIn && !a.IsRegisteredToThisCopy))
                {
                    association.Register();
                }
                FileAssociation.QueryPlan.OpenDefaultAppsSettings();
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex, "Could not open Default apps settings");
            }
        }

        /// <summary>
        /// The files the viewers open, for the Open dialog: .sqlplan and .xdl, the .xml either of them is also saved
        /// as, and the grid files DBA Dash exports (<see cref="GridSerializer"/>) - which is also how the SSMS extension
        /// hands over a results grid.
        /// </summary>
        public const string OpenFileFilter =
            "Query plans, deadlock graphs and grids (*.sqlplan;*.xdl;*.xml;*.json;*.json.gz;*.xml.gz)|*.sqlplan;*.xdl;*.xml;*.json;*.json.gz;*.xml.gz|" +
            "Query plan (*.sqlplan)|*.sqlplan|Deadlock graph (*.xdl)|*.xdl|XML (*.xml)|*.xml|" +
            "Grid (*.json;*.json.gz;*.xml.gz)|*.json;*.json.gz;*.xml.gz|All files (*.*)|*.*";

        /// <summary>
        /// A grid file DBA Dash exports - by extension alone.  A grid saved as a bare .xml can't be told apart from a
        /// plan or deadlock graph that way, so <see cref="ViewerLauncher.ShowXmlFile"/> goes by its content instead.
        /// Only the compressed extensions DBA Dash writes, not any .gz, so an unrelated archive isn't taken for one.
        /// </summary>
        private static bool IsGridFile(string file) =>
            file.EndsWith(GridSerializer.JsonExtension, StringComparison.OrdinalIgnoreCase) ||
            file.EndsWith(GridSerializer.CompressedJsonExtension, StringComparison.OrdinalIgnoreCase) ||
            file.EndsWith(GridSerializer.CompressedXmlExtension, StringComparison.OrdinalIgnoreCase);

        /// <summary>True for a file the viewers can open, by its extension - what a window accepts dropped on it.</summary>
        public static bool CanOpen(string file) =>
            IsGridFile(file) ||
            Path.GetExtension(file).ToLowerInvariant() is ".sqlplan" or ".xdl" or ".xml";

        /// <summary>
        /// Opens each file in the viewer for its type, on tabs of the one window - plans, deadlock graphs and grids alike.
        /// A file that can't be read is reported and doesn't stop the rest.
        /// </summary>
        public static void OpenFiles(IEnumerable<string> files)
        {
            var list = files.ToList();
            foreach (var file in list)
            {
                // .sqlplan and .xdl are unambiguous, as are the grid files; anything else - a bare .xml, or
                // no extension at all - is all three's other extension, so ShowXmlFile decides from the content.
                if (IsGridFile(file))
                {
                    ViewerLauncher.ShowGridFile(file);
                }
                else if (Path.GetExtension(file).Equals(FileAssociation.QueryPlan.Extension,
                        StringComparison.OrdinalIgnoreCase))
                {
                    ViewerLauncher.ShowQueryPlanFile(file);
                }
                else if (Path.GetExtension(file).Equals(FileAssociation.DeadlockGraph.Extension,
                        StringComparison.OrdinalIgnoreCase))
                {
                    ViewerLauncher.ShowDeadlockGraphFile(file);
                }
                else
                {
                    ViewerLauncher.ShowXmlFile(file);
                }
            }

            // Only when the SSMS extension just handed over a file: that's when it's in use, and when the version it
            // recorded is the one that did.
            if (list.Any(SsmsExtensionInstaller.IsFromExtension)) ViewerForm.OfferSsmsExtensionUpdate();
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
        /// window has been closed.  With no files - or none that could be opened - shows <see cref="ViewerForm.ShowEmpty"/>
        /// instead of leaving the process with nothing on screen.
        ///
        /// Explorer starts a copy per file.  The first copy opens them all - on tabs of one window - and the rest
        /// hand their files to it and return at once.  See <see cref="ViewerInstance"/>.
        /// </summary>
        public static void Run(IEnumerable<string> files)
        {
            ViewerForm.IsStandalone = true;

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

            if (!AnyVisibleForms()) ViewerForm.ShowEmpty();

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
