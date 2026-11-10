using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;

namespace DBADash.SSMSExtension
{
    /// <summary>
    /// Reads the query window's Messages tab, to go to DBA Dash with its result sets - where the app shows it on a tab
    /// of its own, and reads any SET STATISTICS IO / TIME output in it into tables.
    ///
    /// The Messages tab is a VSTextEditorTabPage: a VS text editor, not a WinForms control with text of its own.  It's
    /// found among the pages of the results tab control (DisplaySqlResultsTabControl) the focused grid is on - the one
    /// whose ImageIndex is the control's static MessagesImageIndex - or failing that through the query window
    /// (SqlScriptEditorControl.m_sqlResultsControl.m_textMessagesPage).  Its text is what its writer (ResultsWriter, a
    /// ShellBufferWriter) has put in the editor's buffer, plus anything the writer is still holding in its
    /// StringBuilder.  Like the rest of what this extension reads, none of it is a supported API, so it's reached by
    /// name and says why if a future SSMS build changes it.  Confirmed against the SSMS 22 install.
    /// </summary>
    internal static class MessagesReader
    {
        /// <summary>
        /// The table's "Kind" extended property, which the app knows the messages table by (QueryResultsViewerControl).
        /// A DBA Dash too old to know it shows the table as just another result set.
        /// </summary>
        private const string KindProperty = "Kind";
        private const string MessagesKind = "Messages";

        /// <summary>Why the messages couldn't be read, for the app to say in place of them.</summary>
        private const string ErrorProperty = "Error";

        private const string TabControlTypeName = "Microsoft.SqlServer.Management.UI.VSIntegration.Editors.DisplaySqlResultsTabControl";
        private const string TextPageTypeName = "Microsoft.SqlServer.Management.UI.VSIntegration.Editors.VSTextEditorTabPage";

        /// <summary>
        /// The Messages tab of the query window focus is in, as a table to add to the result sets' DataSet: its text a
        /// line a row, or - where it couldn't be read - no rows and the reason why.  Must be called on the UI thread:
        /// the text buffer is the editor's.
        /// </summary>
        public static DataTable ReadFocused()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return ReadPage(() => FromResultsTabControl() ?? FromQueryWindow());
        }

        /// <summary>
        /// The text page focus is in - the Messages tab, or the Results tab with results to text, both of which show
        /// <see cref="OpenMessagesCommand"/> on their right-click menu - as <see cref="ReadFocused"/> reads the Messages
        /// tab.  Must be called on the UI thread.
        /// </summary>
        public static DataTable ReadFocusedTextPage()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return ReadPage(FocusedTextPage);
        }

        /// <summary>
        /// The text page focus is in, or null: up from the focused window - the editor's own native window, so up its
        /// native parents to the WinForms control hosting it - to a VSTextEditorTabPage.  Not the XML results page
        /// (xmlEditor), which holds an XML value, not messages.  Cheap enough for BeforeQueryStatus.
        /// </summary>
        public static Control FocusedTextPage()
        {
            for (var control = Control.FromChildHandle(ResultsGridReader.GetFocus()); control != null; control = control.Parent)
            {
                if (!ResultsGridReader.IsOfType(control, TextPageTypeName)) continue;
                return control.IsDisposed || GetField(control, "xmlEditor") is true ? null : control;
            }

            return null;
        }

        private static DataTable ReadPage(Func<Control> findPage)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            string text;
            string error;
            try
            {
                text = TryRead(findPage(), out error);
            }
            catch (Exception ex)
            {
                text = null;
                error = $"{ex.GetType().Name}: {ex.Message}";
            }

            var table = new DataTable(MessagesKind);
            table.ExtendedProperties[KindProperty] = MessagesKind;
            table.Columns.Add("Message", typeof(string));

            // The query, for the app to tell whether it's a single procedure call - whose own time is the total, rather
            // than the sum of its statements' - and whether it's the text that ran, or only what's in the editor now.
            var (query, executed) = QueryText();
            if (query != null)
            {
                table.ExtendedProperties[QueryTextProperty] = ResultsGridReader.ValidXmlText(query);
                table.ExtendedProperties[QueryTextSourceProperty] = executed ? ExecutedSource : EditorSource;
            }

            if (text == null)
            {
                table.ExtendedProperties[ErrorProperty] = error ?? "Unknown error.";
                return table;
            }

            table.BeginLoadData();
            foreach (var line in text.Replace("\r\n", "\n").TrimEnd('\n').Split('\n'))
            {
                table.Rows.Add(ResultsGridReader.ValidXmlText(line.TrimEnd('\r')));
            }
            table.EndLoadData();
            return table;
        }

        /// <summary>
        /// The query that ran: the text the query window executed, where <see cref="ExecutedQueryTracker"/> saw it run,
        /// and Executed is true.  Otherwise - the extension loaded after it ran, say - what executing the window now would
        /// run (its selection, or all of it), which is what ran unless it's been changed since.  Null if it can't be read.
        /// </summary>
        private static (string Text, bool Executed) QueryText()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                var document = (ServiceProvider.GlobalProvider.GetService(typeof(EnvDTE.DTE)) as EnvDTE.DTE)?.ActiveDocument;
                if (ExecutedQueryTracker.ExecutedText(document) is { } executed) return (executed, true);
                return (ExecutedQueryTracker.EditorText(document), false);
            }
            catch
            {
                // Without it, the app just adds up the statements.
                return (null, false);
            }
        }

        private const string QueryTextProperty = "QueryText";

        /// <summary>Where the query text came from: <see cref="ExecutedSource"/>, or <see cref="EditorSource"/> - read when opened.</summary>
        private const string QueryTextSourceProperty = "QueryTextSource";
        private const string ExecutedSource = "Executed";
        private const string EditorSource = "Editor";

        private static string TryRead(Control page, out string error)
        {
            if (page == null)
            {
                error = "The Messages tab wasn't found - this version of SSMS may not be supported.";
                return null;
            }

            // The writer's buffer and what it hasn't flushed to it yet.
            var writer = GetProperty(page, "ResultsWriter");
            var buffer = GetField(writer, "buffer") ?? GetProperty(GetProperty(page, "TextView"), "TextBuffer");
            if (buffer == null)
            {
                error = "The Messages tab's text buffer wasn't found - this version of SSMS may not be supported.";
                return null;
            }

            var text = new StringBuilder(GetProperty(buffer, "Text") as string ?? string.Empty);
            if (GetField(writer, "stringBuilder") is StringBuilder pending) text.Append(pending);

            error = null;
            return text.ToString();
        }

        /// <summary>
        /// The Messages page of the results tab control the focused grid is on.  Up from the focus to the tab control, which
        /// holds the grid's own Results page, then across to the page with the Messages icon.
        /// </summary>
        private static Control FromResultsTabControl()
        {
            var tabControl = ResultsGridReader.FocusedAncestor(TabControlTypeName) as TabControl;
            if (tabControl == null) return null;

            var messagesIndex = tabControl.GetType()
                .GetProperty("MessagesImageIndex", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                ?.GetValue(null) as int?;
            var pages = tabControl.TabPages.Cast<TabPage>().Where(p => !p.IsDisposed && ResultsGridReader.IsOfType(p, TextPageTypeName)).ToList();

            return pages.FirstOrDefault(p => messagesIndex != null && p.ImageIndex == messagesIndex)
                   ?? pages.FirstOrDefault(p => string.Equals(p.Text, MessagesKind, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>The query window's Messages page: SqlScriptEditorControl.m_sqlResultsControl.m_textMessagesPage.</summary>
        private static Control FromQueryWindow()
        {
            var window = ResultsGridReader.FocusedQueryWindow();
            var results = GetField(window, "m_sqlResultsControl");
            return GetField(results, "m_textMessagesPage") is Control { IsDisposed: false } page ? page : null;
        }

        /// <summary>A private field, looked for on the object's own type and then each type it derives from.</summary>
        private static object GetField(object target, string name)
        {
            for (var type = target?.GetType(); type != null; type = type.BaseType)
            {
                var field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field != null) return field.GetValue(target);
            }

            return null;
        }

        /// <summary>
        /// A property, the most derived type's first - looked for a type at a time, so one a derived type hides with
        /// <c>new</c> isn't an ambiguous match.
        /// </summary>
        private static object GetProperty(object target, string name)
        {
            for (var type = target?.GetType(); type != null; type = type.BaseType)
            {
                var property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (property != null) return property.GetValue(target);
            }

            return null;
        }
    }
}
