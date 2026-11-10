using DBADashGUI.Theme;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace DBADashGUI.Grids
{
    /// <summary>
    /// A query's results from the SSMS extension with its messages beside them, as SSMS lays them out: Results, then
    /// Messages - and Statistics, when SET STATISTICS IO or TIME printed anything, read from the messages into tables.
    /// Tabs of the document's own, so the statistics are plainly about the whole query rather than another result set.
    ///
    /// The extension sends the messages as a table of their own in the result sets' DataSet, marked by
    /// <see cref="KindProperty"/> - a DBA Dash too old to know it shows them as just another table.
    /// </summary>
    public sealed class QueryResultsViewerControl : UserControl
    {
        /// <summary>The table extended property that says what a table holds, where it's not a result set.</summary>
        public const string KindProperty = "Kind";

        /// <summary><see cref="KindProperty"/>'s value for the query's messages: a line a row.</summary>
        public const string MessagesKind = "Messages";

        /// <summary>The messages table's extended property saying why the extension couldn't read them, where it couldn't.</summary>
        public const string ErrorProperty = "Error";

        /// <summary>
        /// The messages table's extended property holding the query that ran - the query window's selection, or all of
        /// it - so a single procedure call's own time can be taken as the total.
        /// </summary>
        public const string QueryTextProperty = "QueryText";

        /// <summary>
        /// The messages table's extended property saying where the query text came from: <see cref="ExecutedSource"/> where
        /// the extension saw it run, or "Editor" where it was read from the query window when opened, and may have changed.
        /// </summary>
        public const string QueryTextSourceProperty = "QueryTextSource";

        public const string ExecutedSource = "Executed";

        private readonly SubTabControl _tabs = new();

        private readonly DBADashGUI.SchemaCompare.CodeEditor _messagesEditor;

        /// <summary>The text on the Messages tab - the messages, or why they couldn't be read.</summary>
        public string MessagesText => _messagesEditor.CurrentText;

        /// <summary>Opens the Messages tab's find bar, as Ctrl+F does there.</summary>
        public void FindInMessages()
        {
            SelectTab("Messages");
            _messagesEditor.OpenSearch();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // Ctrl+F on the Messages tab finds in the messages, wherever on the tab focus is - its header or toolbar too.
            if (keyData == (Keys.Control | Keys.F) && SelectedTabName == "Messages")
            {
                FindInMessages();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        // The result sets' DataSet, the messages taken out - and the messages, to put back in when saving.
        private readonly DataSet _resultSets;
        private readonly SsmsMessages _messages;

        /// <param name="resultSets">
        /// The DataSet <paramref name="results"/> shows - the file's, with the messages taken out - so they can be saved
        /// together again.  Null for none.
        /// </param>
        public QueryResultsViewerControl(Control results, string title, string tabToolTip, SsmsMessages ssmsMessages,
            DataSet resultSets = null)
        {
            Results = results;
            Title = title;
            TabToolTip = tabToolTip;
            MessagesError = ssmsMessages.Error;
            _messages = ssmsMessages;
            _resultSets = resultSets ?? new DataSet("Results");

            // The DataSet viewer's own Save All saves the messages with every result set, as it was opened.
            if (results is DataSetViewerControl dataSetViewer) dataSetViewer.AlsoSave = () => MessagesTable(_messages);
            // A single result set's grid saves just the grid; this saves it with the messages, as the Messages tab does.
            if (results is GridViewerControl grid)
            {
                grid.AddExportItem(new ToolStripMenuItem(SaveWithMessagesText, DBADash.Viewers.GUI.Properties.Resources.Save_16x,
                    (_, _) => PromptSave()) { ToolTipText = SaveWithMessagesTip });
            }
            // Parsed once for both the whole query's statistics and its parts', where it's split for comparing.
            var query = StatisticsMessages.ParseQuery(ssmsMessages.QueryText, mayHaveChanged: !ssmsMessages.QueryTextExecuted);
            Statistics = StatisticsMessages.Parse(ssmsMessages.Text, query);

            var messages = ssmsMessages.Text;
            if (ssmsMessages.Error != null)
            {
                messages = $"The SSMS extension couldn't read the Messages tab: {ssmsMessages.Error}";
            }
            else if (string.IsNullOrWhiteSpace(messages))
            {
                messages = "(The Messages tab was empty.)";
            }

            // No results when the Messages tab was opened on its own (the extension's Open Messages) - a query that
            // returned none, opened for its statistics.
            if (results != null) _tabs.AddTab("Results", results);
            // DBA Dash's code editor, for its Ctrl+F find bar (and F3, Find on the right-click menu) - read only, with no
            // highlighting, as the messages are text, not SQL.
            _messagesEditor = new DBADashGUI.SchemaCompare.CodeEditor
            {
                Mode = DBADashGUI.SchemaCompare.CodeEditor.CodeEditorModes.None,
                IsReadOnly = true,
                WordWrap = false,
                ShowLineNumbers = false,
                Text = messages.ReplaceLineEndings("\r\n")
            };
            var messagesPage = new Panel();
            messagesPage.Controls.Add(new System.Windows.Forms.Integration.ElementHost { Dock = DockStyle.Fill, Child = _messagesEditor });
            messagesPage.Controls.Add(MessagesToolbar(results != null));
            _tabs.AddTab("Messages", messagesPage);
            // PRINT 'DBADash-Compare' between the parts of the query: each part is a run of its own, compared.
            var split = ssmsMessages.Error == null ? StatisticsMessages.SplitRuns(ssmsMessages.Text, query) : null;
            Runs = split ?? (Statistics.HasStatistics
                ? [new StatisticsRun(Statistics.SingleStatement ?? title ?? "Query", Statistics)]
                : []);
            IsSplit = split != null;

            if (Statistics.HasStatistics)
            {
                _tabs.AddTab("Statistics", IsSplit
                    ? new StatisticsCompareControl(split, title)
                    : new StatisticsViewerControl(Statistics, title));
                // Opened for them, with no results to show first.
                if (results == null) _tabs.SelectedIndex = _tabs.TabCount - 1;
            }

            Controls.Add(_tabs);
            this.ApplyTheme();
        }

        /// <summary>The result set's grid, the DataSet viewer for several, or null for the messages alone.</summary>
        public Control Results { get; }

        /// <summary>The name of the tab showing - Results, Messages or Statistics.</summary>
        public string SelectedTabName => _tabs.SelectedTab?.Text;

        /// <summary>The statistics across all of the messages, however they're split.</summary>
        public StatisticsReport Statistics { get; }

        /// <summary>
        /// The runs to compare this tab's statistics by, with another tab's: each part of the query, where it was split at
        /// PRINT 'DBADash-Compare', or the whole query as one run.  Empty with no statistics.
        /// </summary>
        public IReadOnlyList<StatisticsRun> Runs { get; }

        /// <summary>True when the query was split at PRINT 'DBADash-Compare', and its Statistics tab compares the parts.</summary>
        public bool IsSplit { get; }

        /// <summary>Why the extension couldn't read the messages, or null.</summary>
        public string MessagesError { get; }

        public string Title { get; }

        public string TabToolTip { get; }

        /// <summary>Shows the named tab - Results, Messages or Statistics.  False if there isn't one of that name.</summary>
        public bool SelectTab(string name) => _tabs.SelectTabByText(name);

        public string[] TabNames => _tabs.TabNames;

        /// <summary>
        /// Save, over the messages: the results and messages together, as the SSMS extension sent them, so the file opens
        /// back on these same tabs - statistics and all, read again from the messages.
        /// </summary>
        private ToolStrip MessagesToolbar(bool hasResults)
        {
            var toolbar = new ToolStrip { GripStyle = ToolStripGripStyle.Hidden, Dock = DockStyle.Top };
            toolbar.Items.Add(new ToolStripButton(hasResults ? SaveWithMessagesText : "Save Messages...",
                DBADash.Viewers.GUI.Properties.Resources.Save_16x, (_, _) => PromptSave())
            {
                ToolTipText = SaveWithMessagesTip
            });
            return toolbar;
        }

        private const string SaveWithMessagesText = "Save Results and Messages...";

        private const string SaveWithMessagesTip = "One file that opens back on these tabs, with its statistics.  An older " +
                                                   "DBA Dash shows the messages as a table of their own.";

        /// <summary>Only the XML keeps the extended properties that mark the messages as messages.</summary>
        private const string SaveFilter = "Compressed XML (*.xml.gz)|*.xml.gz|XML (*.xml)|*.xml";

        private void PromptSave()
        {
            var name = Title ?? "Query";
            foreach (var c in System.IO.Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
            using var dialog = new SaveFileDialog
            {
                Filter = SaveFilter,
                FileName = name + DBADashGUI.CustomReports.GridSerializer.CompressedXmlExtension
            };
            if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;

            try
            {
                Save(dialog.FileName);
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex, "Error saving results and messages");
            }
        }

        /// <summary>
        /// Writes the result sets and the messages to <paramref name="path"/> - the file the SSMS extension hands over,
        /// which opens back on these tabs.  Titled as this tab is, where the DataSet wasn't.
        /// </summary>
        public void Save(string path)
        {
            if (_resultSets.ExtendedProperties["Title"] is not string) _resultSets.ExtendedProperties["Title"] = Title;
            WithTable(_resultSets, MessagesTable(_messages),
                () => DBADashGUI.CustomReports.GridSerializer.SaveDataSet(_resultSets, path));
        }

        /// <summary>
        /// Runs <paramref name="action"/> with <paramref name="table"/> in <paramref name="dataSet"/> - named apart from its
        /// own tables - and takes it out again after, so a DataSet on show isn't left changed.
        /// </summary>
        internal static void WithTable(DataSet dataSet, DataTable table, Action action)
        {
            var name = table.TableName;
            for (var n = 2; dataSet.Tables.Contains(table.TableName); n++) table.TableName = $"{name} ({n})";
            dataSet.Tables.Add(table);
            try
            {
                action();
            }
            finally
            {
                dataSet.Tables.Remove(table);
                table.Dispose();
            }
        }

        /// <summary>
        /// The messages as the SSMS extension sends them: a line a row, marked by <see cref="KindProperty"/>, with why they
        /// couldn't be read and the query text where there are - what <see cref="TakeMessages"/> reads back.
        /// </summary>
        public static DataTable MessagesTable(SsmsMessages messages)
        {
            var table = new DataTable(MessagesKind);
            table.ExtendedProperties[KindProperty] = MessagesKind;
            if (messages.Error != null) table.ExtendedProperties[ErrorProperty] = messages.Error;
            if (messages.QueryText != null)
            {
                table.ExtendedProperties[QueryTextProperty] = messages.QueryText;
                table.ExtendedProperties[QueryTextSourceProperty] = messages.QueryTextExecuted ? ExecutedSource : "Editor";
            }
            table.Columns.Add("Message", typeof(string));

            // TakeMessages ends every line with a newline, the last too - so that's one line, not an empty one after it.
            var text = messages.Text ?? string.Empty;
            if (text.EndsWith('\n')) text = text[..^1];
            if (messages.Error == null || text.Length > 0)
            {
                table.BeginLoadData();
                foreach (var line in text.Split('\n')) table.Rows.Add(line);
                table.EndLoadData();
            }
            return table;
        }

        /// <summary>
        /// Takes the messages table the SSMS extension adds out of <paramref name="dataSet"/>, leaving only result sets,
        /// and returns what it holds - or null, leaving the DataSet as it was, if there isn't one.
        /// </summary>
        public static SsmsMessages TakeMessages(DataSet dataSet)
        {
            var table = dataSet.Tables.Cast<DataTable>()
                .FirstOrDefault(t => string.Equals(t.ExtendedProperties[KindProperty] as string, MessagesKind, StringComparison.Ordinal));
            if (table == null) return null;

            var text = new StringBuilder();
            foreach (DataRow row in table.Rows)
            {
                text.Append(row[0] as string).Append('\n');
            }

            // An extension too old to say where the text came from read it from the editor when opening.
            var messages = new SsmsMessages(text.ToString(), table.ExtendedProperties[ErrorProperty] as string,
                table.ExtendedProperties[QueryTextProperty] as string,
                string.Equals(table.ExtendedProperties[QueryTextSourceProperty] as string, ExecutedSource, StringComparison.Ordinal));
            dataSet.Tables.Remove(table);
            table.Dispose();
            return messages;
        }
    }

    /// <summary>
    /// A query's messages as the SSMS extension sends them: their text, why they couldn't be read if they couldn't, and
    /// the query that printed them, where it could be read - and whether that's the text the extension saw run, or only
    /// what was in the editor when they were opened.
    /// </summary>
    public sealed record SsmsMessages(string Text, string Error = null, string QueryText = null, bool QueryTextExecuted = false);
}
