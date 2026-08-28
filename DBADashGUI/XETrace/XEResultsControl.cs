using DBADashGUI.CustomReports;
using DBADashGUI.SchemaCompare;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI.XETrace
{
    /// <summary>
    /// Reusable live-results view for extended-events streams (dynamic, per-event schema).  Shows a results grid with
    /// clickable SQL-text columns and an SSMS-style pivoted Field/Value detail grid for the selected row.  Batches are
    /// merged in via <see cref="AppendEvents"/>.  Extracted from the ad-hoc XE trace UI so the Extended Events watch view
    /// shares the same rendering.
    /// </summary>
    public sealed class XEResultsControl : UserControl
    {
        private readonly SplitContainer _split;
        private readonly DBADashDataGridView _dgvXE;
        private readonly DBADashDataGridView _dgvDetail;
        private DataTable _events;

        // SQL-text columns shown as links to the code viewer.  Matched by name (the schema is dynamic).
        private static readonly HashSet<string> SqlTextColumns = new(StringComparer.OrdinalIgnoreCase)
        { "batch_text", "statement", "sql_text", "text" };

        // Execution-plan columns shown as links to the graphical plan viewer (e.g. query_post_execution_showplan).
        // Matched by name (the schema is dynamic).
        private static readonly HashSet<string> PlanColumns = new(StringComparer.OrdinalIgnoreCase)
        { "showplan_xml", "query_plan", "showplan", "plan" };

        // Deadlock-graph columns shown as links to the graphical deadlock viewer (e.g. xml_deadlock_report).
        // Matched by name (the schema is dynamic).
        private static readonly HashSet<string> DeadlockColumns = new(StringComparer.OrdinalIgnoreCase)
        { "xml_report", "deadlock_graph", "deadlock" };

        // Columns rendered as clickable links (SQL text -> code viewer, plan/deadlock XML -> graphical viewers).
        private static bool IsLinkColumn(string key) =>
            SqlTextColumns.Contains(key) || PlanColumns.Contains(key) || DeadlockColumns.Contains(key);

        // Numeric metric columns for which Group By pre-selects all applicable aggregations (when captured).
        private static readonly HashSet<string> MetricColumns = new(StringComparer.OrdinalIgnoreCase)
        { "cpu_time", "duration", "physical_reads", "logical_reads", "writes", "row_count", "spills" };

        public XEResultsControl()
        {
            _dgvXE = new DBADashDataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                GroupByDefaultCountPercent = true,
                RowHeadersVisible = false,
                GroupByColumnInitializer = InitGroupByColumn
            };

            _dgvDetail = new DBADashDataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                ReadOnly = true,
                AutoGenerateColumns = true,
                RowHeadersVisible = false,
            };

            _split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal };
            _split.Panel1.Controls.Add(_dgvXE);
            _split.Panel2.Controls.Add(_dgvDetail);
            Controls.Add(_split);

            _dgvXE.DataBindingComplete += (_, _) => { LinkifyColumns(); PinInstanceColumn(); };
            _dgvXE.CellContentClick += DgvXE_CellContentClick;
            _dgvXE.SelectionChanged += (_, _) => UpdateDetailGrid();
            _dgvDetail.DataBindingComplete += DgvDetail_DataBindingComplete;
            _dgvDetail.CellContentClick += DgvDetail_CellContentClick;
            _dgvDetail.CellDoubleClick += DgvDetail_CellDoubleClick;

            Load += (_, _) =>
            {
                try { _split.SplitterDistance = (int)(_split.Height * 0.65); } catch { /* not sized yet */ }
            };
        }

        /// <summary>Total events currently shown.</summary>
        public int RowCount => _events?.Rows.Count ?? 0;

        /// <summary>The events currently shown (as displayed, i.e. timestamps already in the app time zone), or null.
        /// Exposed so hosts can save the grid contents to a file.</summary>
        public DataTable CurrentEvents => _events;

        /// <summary>Merges a batch of events (varying schema) into the grid, rebinding when new columns appear.</summary>
        public void AppendEvents(DataTable batch)
        {
            if (InvokeRequired) { Invoke(new Action(() => AppendEvents(batch))); return; }
            var converted = batch.Copy();
            ConvertTimestampToLocal(converted);
            if (_events == null)
            {
                _events = converted;
                _dgvXE.DataSource = _events.DefaultView;
            }
            else
            {
                var before = _events.Columns.Count;
                _events.Merge(converted, false, MissingSchemaAction.Add);
                if (_events.Columns.Count != before)
                {
                    _dgvXE.DataSource = null;
                    _dgvXE.DataSource = _events.DefaultView;
                }
            }
        }

        /// <summary>
        /// Replaces the grid contents with a complete table (e.g. a loaded history or file).
        /// <paramref name="convertTimestampToLocal"/> is true when the source holds UTC timestamps (live/history/.xel);
        /// pass false for a DBA Dash-native file whose timestamps are already in the app time zone, otherwise the
        /// conversion would be applied a second time and shift them.
        /// </summary>
        public void LoadEvents(DataTable events, bool convertTimestampToLocal = true)
        {
            Clear();
            _events = events?.Copy();
            if (convertTimestampToLocal) ConvertTimestampToLocal(_events);
            _dgvXE.DataSource = _events?.DefaultView;
        }

        /// <summary>
        /// Events are shredded with UTC timestamps (for correct sorting/newest-N on the service); convert the
        /// <c>timestamp</c> column to the app's display time zone here, matching the rest of the GUI.
        /// </summary>
        private static void ConvertTimestampToLocal(DataTable dt)
        {
            if (dt == null || !dt.Columns.Contains("timestamp") ||
                dt.Columns["timestamp"].DataType != typeof(DateTime))
            {
                return;
            }
            DateHelper.ConvertUTCToAppTimeZone(ref dt, new List<string> { "timestamp" });
        }

        public void Clear()
        {
            _events = null;
            _dgvXE.DataSource = null;
            _dgvXE.Columns.Clear();
            _dgvDetail.DataSource = null;
            _dgvDetail.Columns.Clear();
        }

        private static void InitGroupByColumn(GroupByColumnConfig cfg)
        {
            if (cfg.IsGroupKey || !MetricColumns.Contains(cfg.HeaderText)) return;
            if (cfg.IsNumeric)
            {
                cfg.IncludeSum = true;
                cfg.IncludeSumPercent = true;
                cfg.IncludeAvg = true;
            }
            if (cfg.IsComparable)
            {
                cfg.IncludeMin = true;
                cfg.IncludeMax = true;
            }
        }

        // The source-instance column added in a multi-instance run (see XETraceController.InstanceColumn).  Kept
        // left-most so the merged grid reads "which replica, then the event".
        private const string InstanceColumn = "Instance";

        /// <summary>Moves the source-instance column (present only in a multi-instance run) to the far left.</summary>
        private void PinInstanceColumn()
        {
            var col = _dgvXE.Columns[InstanceColumn];
            if (col == null) return;
            col.DisplayIndex = 0;
            col.Frozen = Common.FreezeKeyColumn;
        }

        private void LinkifyColumns()
        {
            for (var i = 0; i < _dgvXE.Columns.Count; i++)
            {
                var col = _dgvXE.Columns[i];
                if (col is DataGridViewLinkColumn) continue;
                var key = string.IsNullOrEmpty(col.DataPropertyName) ? col.Name : col.DataPropertyName;
                if (!IsLinkColumn(key)) continue;

                var link = new DataGridViewLinkColumn
                {
                    Name = col.Name,
                    HeaderText = col.HeaderText,
                    DataPropertyName = col.DataPropertyName,
                    Width = col.Width,
                    SortMode = col.SortMode,
                    TrackVisitedState = false,
                    LinkColor = DashColors.LinkColor,
                    ActiveLinkColor = DashColors.LinkColor
                };
                _dgvXE.Columns.RemoveAt(i);
                _dgvXE.Columns.Insert(i, link);
            }
        }

        private void DgvXE_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (_dgvXE.Columns[e.ColumnIndex] is not DataGridViewLinkColumn col) return;
            var key = string.IsNullOrEmpty(col.DataPropertyName) ? col.Name : col.DataPropertyName;
            ShowValue(key, _dgvXE.Rows[e.RowIndex].Cells[e.ColumnIndex].Value as string, col.HeaderText);
        }

        /// <summary>
        /// Opens a linked cell value: plan XML in the graphical plan viewer, a deadlock graph in the deadlock
        /// viewer, SQL text in the code viewer.  XML that fails validation falls back to the raw-XML code viewer.
        /// </summary>
        private static void ShowValue(string key, string text, string header)
        {
            if (string.IsNullOrEmpty(text)) return;
            if (PlanColumns.Contains(key) && Common.IsValidExecutionPlan(text))
            {
                Common.ShowQueryPlan(text);
                return;
            }
            if (DeadlockColumns.Contains(key) && Common.IsValidDeadlockGraph(text))
            {
                Common.ShowDeadlockGraph(text);
                return;
            }
            // SQL-text columns render as SQL; a plan/deadlock column that isn't valid XML-of-that-kind shows as raw XML.
            var mode = PlanColumns.Contains(key) || DeadlockColumns.Contains(key)
                ? CodeEditor.CodeEditorModes.XML
                : CodeEditor.CodeEditorModes.SQL;
            Common.ShowCodeViewer(text, header, mode);
        }

        private void UpdateDetailGrid()
        {
            var row = _dgvXE.CurrentRow;
            if (row == null || row.Index < 0)
            {
                _dgvDetail.DataSource = null;
                return;
            }

            var dt = new DataTable();
            dt.Columns.Add("Field", typeof(string));
            dt.Columns.Add("Value", typeof(string));
            foreach (var col in _dgvXE.Columns.Cast<DataGridViewColumn>().OrderBy(c => c.DisplayIndex))
            {
                var cell = row.Cells[col.Index];
                var value = col.ValueType == typeof(string) ? cell.Value : cell.FormattedValue;
                var text = value is null or DBNull ? string.Empty : value.ToString();
                if (string.IsNullOrEmpty(text)) continue;
                dt.Rows.Add(col.HeaderText.Replace("\n", " "), text);
            }
            _dgvDetail.DataSource = dt.DefaultView;
        }

        private void DgvDetail_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            if (_dgvDetail.Columns.Contains("Field")) _dgvDetail.Columns["Field"].Width = 220;
            if (!_dgvDetail.Columns.Contains("Value")) return;
            _dgvDetail.Columns["Value"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            var valueIndex = _dgvDetail.Columns["Value"].Index;
            foreach (DataGridViewRow row in _dgvDetail.Rows)
            {
                var field = row.Cells[0].Value as string;
                var text = row.Cells[valueIndex].Value as string;
                if (!string.IsNullOrEmpty(field) && IsLinkColumn(field) && !string.IsNullOrEmpty(text))
                {
                    row.Cells[valueIndex] = new DataGridViewLinkCell
                    { TrackVisitedState = false, LinkColor = DashColors.LinkColor, ActiveLinkColor = DashColors.LinkColor };
                }
            }
        }

        private void DgvDetail_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (_dgvDetail.Rows[e.RowIndex].Cells[e.ColumnIndex] is not DataGridViewLinkCell) return;
            var field = _dgvDetail.Rows[e.RowIndex].Cells[0].Value as string;
            ShowValue(field, _dgvDetail.Rows[e.RowIndex].Cells[e.ColumnIndex].Value as string, field);
        }

        private void DgvDetail_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var field = _dgvDetail.Rows[e.RowIndex].Cells[0].Value as string;
            ShowValue(field, _dgvDetail.Rows[e.RowIndex].Cells[1].Value as string, field);
        }
    }
}