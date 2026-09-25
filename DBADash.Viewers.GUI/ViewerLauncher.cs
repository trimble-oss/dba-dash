using DBADash.Deadlock;
using DBADash.Deadlock.Model;
using DBADash.QueryPlan;
using DBADash.QueryPlan.Model;
using DBADashGUI.Deadlocks;
using DBADashGUI.QueryPlans;
using System.IO;
using System.Text;

namespace DBADashGUI.Viewers
{
    /// <summary>
    /// Opens query plans and deadlock graphs in the viewers, from text or from a file.  Needs no repository connection
    /// and no monitored instance - a plan or graph someone emailed you opens the same as one from a report.
    /// </summary>
    public static class ViewerLauncher
    {
        /// <summary>
        /// The host used for a plan or graph opened with none of its own - from a file, by drag and drop or the Open
        /// button.  The DBA Dash GUI sets one so those still offer AI analysis; the stand-alone viewer leaves it null.
        /// </summary>
        public static IViewerHost DefaultHost { get; set; }

        /// <summary>

        /// Open a query plan in the built-in viewer.

        ///

        /// This used to write a .sqlplan to temp and hand it to whatever was registered for the

        /// extension, which required SSMS (or Plan Explorer) to be installed and did nothing useful

        /// when it was not.  That route is still a click away from the viewer's Open With button -

        /// see <see cref="WriteQueryPlanTempFile"/>.

        /// </summary>

        /// <summary>

        /// Opens a plan in the viewer.  <paramref name="context"/> says where it came from, where the

        /// caller knows - it names the instance on an AI analysis and records one against it.  Optional

        /// because a plan can arrive from a file or from a grid with no instance behind it.

        /// </summary>

        public static void ShowQueryPlan(string plan, string fileName = null, IViewerHost host = null)

        {

            ExecutionPlan parsed;

            try

            {

                parsed = PlanParser.Parse(plan);

            }

            catch (PlanParseException ex)

            {

                throw new InvalidOperationException("Invalid execution plan: " + ex.Message, ex);

            }



            // On a tab of the plan window already open, if there is one, so plans can be compared.

            host ??= DefaultHost;
            OnUIThread(() => QueryPlanViewerForm.Open(parsed, plan, fileName, host));

        }



        /// <summary>

        /// Run <paramref name="action"/> on the UI thread, waiting for it and passing back anything it

        /// throws.  Plans often arrive on a background thread - a plan collected through the messaging

        /// service is handed over from the thread pool - and the viewer is a window, with WPF editors

        /// in it that need the STA UI thread.

        /// </summary>

        private static void OnUIThread(Action action)

        {

            var ui = Application.OpenForms.Cast<Form>().FirstOrDefault(f => f.IsHandleCreated && !f.IsDisposed);



            if (ui is { InvokeRequired: true })

            {

                ui.Invoke(action);

            }

            else

            {

                action();

            }

        }



        /// <summary>

        /// The file types the plan viewer opens.  .sqlplan is what SSMS saves a plan as; the same

        /// XML also turns up saved as .xml, both of which the parser reads.

        /// </summary>

        public const string QueryPlanFileFilter =

            "Query plan (*.sqlplan)|*.sqlplan|XML (*.xml)|*.xml|All files (*.*)|*.*";



        /// <summary>

        /// Prompts for .sqlplan files and opens them in the viewer, a tab each.  Needs no repository

        /// connection and no monitored instance - a plan someone emailed you opens the same as one

        /// from a report.

        /// </summary>

        public static void OpenQueryPlanFile(IWin32Window owner = null)

        {

            using var dialog = new OpenFileDialog

            {

                Filter = QueryPlanFileFilter,

                Title = @"Open Query Plan",

                Multiselect = true

            };



            if (dialog.ShowDialog(owner) != DialogResult.OK) return;

            foreach (var file in dialog.FileNames) ShowQueryPlanFile(file);

        }



        /// <summary>Opens a plan file in the viewer, reporting a bad file rather than throwing.</summary>

        public static void ShowQueryPlanFile(string path)

        {

            try

            {

                // SSMS saves plans as UTF-16 and other tools as UTF-8, so the encoding is detected

                // from the preamble rather than assumed.

                using var reader = new StreamReader(path, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

                ShowQueryPlan(reader.ReadToEnd(), Path.GetFileName(path));

            }

            catch (Exception ex)

            {

                CommonShared.ShowExceptionDialog(

                    ex,

                    "Error opening query plan",

                    text: $"{path} could not be opened as a query plan.");

            }

        }



        /// <summary>

        /// Writes a plan to a temp .sqlplan so it can be handed to another application - SSMS, Plan

        /// Explorer - from the viewer's Open With button.

        ///

        /// Validated through the viewer's own parser rather than <see cref="the plan parser"/>,

        /// which wants a root of ShowPlanXML: the viewer opens a plan nested in an extended events

        /// envelope or a query_plan column too, and those used to reach here only to be refused.

        /// The envelope is dropped on the way out, because it is this viewer that reads past one and

        /// not the application the file is being handed to.

        /// </summary>

        public static string WriteQueryPlanTempFile(string plan, string fileName = null)

        {

            if (!PlanParser.TryExtractShowPlanXml(plan, out var showPlanXml))

            {

                throw new InvalidOperationException("Invalid execution plan");

            }



            var path = CommonShared.GetFilePath(fileName, ".sqlplan");

            File.WriteAllText(path, showPlanXml, Encoding.Unicode);

            return path;

        }



        /// <summary>

        /// Open a deadlock graph in the built-in viewer.

        ///

        /// This used to write a .xdl to temp and hand it to whatever was registered for the

        /// extension, which required SSMS (or Plan Explorer) to be installed and did nothing useful

        /// when it was not.  That route is still a click away from the viewer's Open With menu - see

        /// <see cref="WriteDeadlockGraphTempFile"/>.

        /// </summary>

        /// <param name="host">

        /// The instance the graph came from, where the caller knows it.  Supplying it lights up the actions

        /// that need to go back to the source instance - collecting the current plan for a statement, and the

        /// Query Store lookup.  Without it the viewer still shows everything the graph itself carries.

        /// </param>

        public static void ShowDeadlockGraph(string dlGraph, string fileName = null, IViewerHost host = null)

        {

            IReadOnlyList<DeadlockGraph> graphs;

            try

            {

                graphs = DeadlockParser.Parse(dlGraph);

            }

            catch (DeadlockParseException ex)

            {

                throw new InvalidOperationException("Invalid deadlock graph: " + ex.Message, ex);

            }



            // On a tab of the deadlock window already open, if there is one, so deadlocks can be compared.

            host ??= DefaultHost;
            OnUIThread(() => DeadlockViewerForm.Open(graphs, dlGraph, fileName, host));

        }



        /// <summary>

        /// The file types the deadlock viewer opens.  .xdl is what SQL Server and SSMS save a graph

        /// as; the same XML also turns up saved as .xml, and inside an XE event envelope, both of

        /// which the parser handles.

        /// </summary>

        public const string DeadlockFileFilter =

            "Deadlock graph (*.xdl)|*.xdl|XML (*.xml)|*.xml|All files (*.*)|*.*";



        /// <summary>

        /// Prompts for .xdl files and opens them in the viewer, a tab each.  Needs no repository

        /// connection and no monitored instance - a graph someone emailed you opens the same as one

        /// from a report, minus the actions that go back to the source instance.

        /// </summary>

        public static void OpenDeadlockGraphFile(IWin32Window owner = null)

        {

            using var dialog = new OpenFileDialog

            {

                Filter = DeadlockFileFilter,

                Title = @"Open Deadlock Graph",

                Multiselect = true

            };



            if (dialog.ShowDialog(owner) != DialogResult.OK) return;

            foreach (var file in dialog.FileNames) ShowDeadlockGraphFile(file, owner);

        }



        /// <summary>Opens a deadlock graph file in the viewer, reporting a bad file rather than throwing.</summary>

        public static void ShowDeadlockGraphFile(string path, IWin32Window owner = null)

        {

            try

            {

                ShowDeadlockGraph(File.ReadAllText(path), Path.GetFileName(path));

            }

            catch (Exception ex)

            {

                CommonShared.ShowExceptionDialog(

                    ex,

                    "Error opening deadlock graph",

                    text: $"{path} could not be opened as a deadlock graph.");

            }

        }



        /// <summary>

        /// Write the graph to a .xdl in temp, for handing to another application - SSMS, or SentryOne Plan

        /// Explorer.  Offered alongside the built-in viewer rather than instead of it.

        /// </summary>

        public static string WriteDeadlockGraphTempFile(string dlGraph, string fileName = null)

        {

            if (!DeadlockParser.IsDeadlockXml(dlGraph))

            {

                throw new InvalidOperationException("Invalid deadlock graph");

            }



            var path = CommonShared.GetFilePath(fileName, ".xdl");

            File.WriteAllText(path, dlGraph);

            return path;

        }
    }
}
