using DBADash;
using DBADash.Messaging;
using DBADashGUI.CustomReports;
using DBADashGUI.Messaging;
using DBADashGUI.Performance;
using DBADashGUI.SchemaCompare;
using DBADashGUI.Theme;
using DBADashSharedGUI;
using System;
using System.Data;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBADashGUI.Deadlocks
{
    /// <summary>
    /// The plans cached on the instance for one deadlock statement, with the execution stats that go with
    /// them.  Lives under the process grid in the deadlock viewer, so picking a process and looking at what
    /// it was running stays one view rather than a second window.
    ///
    /// A deadlock graph identifies its statements by sql handle - the handle of the whole batch or module -
    /// so there is rarely exactly one plan behind it: several statements, and several plans per statement
    /// after a recompile or under different SET options.  Showing a single plan would quietly pick one of
    /// those and present it as "the" plan, so the list is the honest answer; the row matching the graph's
    /// statement offset is flagged and sorted to the top.
    ///
    /// Nothing here fetches plan XML.  Each row carries the plan handle and offsets that the running-queries
    /// plan path already knows how to collect, so <see cref="QueryPlanActions"/> does that work and the plan
    /// lands in the repository on the way past, exactly as it does from the running queries screen.
    /// </summary>
    internal sealed class DeadlockPlansControl : UserControl
    {
        private readonly DBADashContext _context;
        private readonly ToolStripStatusLabel _status;

        private readonly DBADashDataGridView _grid;
        private readonly ToolStripLabel _caption = new();
        private readonly ToolStripButton _refresh;

        private string _sqlHandle;
        private int _statementStart;

        /// <summary>
        /// The database the statement ran in, taken from the graph.  Only used to scope the Query Store
        /// lookup - the plan cache is instance-wide, so it plays no part in listing the plans.
        /// </summary>
        private string _databaseName;

        private bool _loading;

        /// <summary>Raised when the user closes the panel, so the host can collapse it.</summary>
        internal event EventHandler CloseRequested;

        // Columns added on top of what the instance returns, so that each row carries everything
        // QueryPlanActions reads.
        private const string PlanActionColumn = "plan_action";

        private const string InstanceIdColumn = "InstanceID";
        private const string HasPlanColumn = "has_plan";
        private const string PlanTextColumn = "query_plan_text";

        private const string QueryHashColumn = "query_hash";
        private const string StatementTextColumn = "statement_text";

        private static readonly string[] HiddenColumns =
        {
            "plan_handle", "statement_start_offset", "statement_end_offset", "database_name", "sql_handle",
            "InstanceDisplayName", InstanceIdColumn, HasPlanColumn, PlanTextColumn
        };

        /// <summary>
        /// True when the plans can be listed at all: it takes a message to the instance the graph came from.
        /// </summary>
        internal static bool CanShow(DBADashContext context) =>
            context is { InstanceID: > 0 } && context.CanMessage;

        internal DeadlockPlansControl(DBADashContext context, ToolStripStatusLabel status)
        {
            _context = context;
            _status = status;

            _grid = new DBADashDataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false
            };
            _grid.DataBindingComplete += (_, _) => ConfigureColumns();
            _grid.CellContentClick += Grid_CellContentClick;
            _grid.CellDoubleClick += Grid_CellDoubleClick;

            _refresh = new ToolStripButton("Refresh", Properties.Resources._112_RefreshArrow_Green_16x16_72,
                async (_, _) => await LoadAsync())
            {
                DisplayStyle = ToolStripItemDisplayStyle.Image,
                ToolTipText = "Re-read the plan cache on the instance."
            };

            var toolbar = new ToolStrip { GripStyle = ToolStripGripStyle.Hidden };
            toolbar.Items.Add(_refresh);
            toolbar.Items.Add(new ToolStripSeparator());
            toolbar.Items.Add(_caption);
            toolbar.Items.Add(new ToolStripButton("Close", null, (_, _) => CloseRequested?.Invoke(this, EventArgs.Empty))
            {
                Alignment = ToolStripItemAlignment.Right,
                ToolTipText = "Hide the plans panel."
            });

            Controls.Add(_grid);
            Controls.Add(toolbar);
        }

        // ---------------------------------------------------------------- loading

        /// <summary>Lists the cached plans for a statement, replacing whatever the panel was showing.</summary>
        internal async Task ShowStatementAsync(string sqlHandle, int statementStart, string databaseName,
            string caption)
        {
            // Don't swap the panel over to another statement mid-flight: the reply for the first one would
            // land in a grid captioned for the second.
            if (_loading)
            {
                _status.InvokeSetStatus("Waiting for the plan list already in progress...", string.Empty,
                    DashColors.Warning);
                return;
            }

            _sqlHandle = sqlHandle;
            _statementStart = statementStart;
            _databaseName = databaseName;
            _caption.Text = caption;
            _grid.DataSource = null;

            await LoadAsync();
        }

        private async Task LoadAsync()
        {
            if (_loading) return;

            var handle = _sqlHandle?.Trim().HexStringToByteArray();
            if (handle is not { Length: > 0 })
            {
                _status.InvokeSetStatus("The graph doesn't carry a usable sql handle for this statement.",
                    string.Empty, DashColors.Warning);
                return;
            }

            var message = new DeadlockStatementPlansMessage
            {
                ConnectionID = _context.ConnectionID,
                CollectAgent = _context.CollectAgent,
                ImportAgent = _context.ImportAgent,
                SqlHandle = handle,
                StatementStart = _statementStart
            };

            _loading = true;
            _refresh.Enabled = false;
            try
            {
                _status.InvokeSetStatus("Reading the plan cache on the instance...", string.Empty,
                    DashColors.Information);
                await MessagingHelper.SendMessageAndProcessReply(message, _context, _status, ProcessReply,
                    Guid.NewGuid());
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex, "Error listing cached plans");
            }
            finally
            {
                _loading = false;
                RunOnUi(() => _refresh.Enabled = true);
            }
        }

        private Task ProcessReply(ResponseMessage reply, Guid messageGroup, MessagingHelper.SetStatusDelegate setStatus)
        {
            if (reply.Type != ResponseMessage.ResponseTypes.Success)
            {
                setStatus(reply.Message, reply.Exception?.ToString(), DashColors.Fail);
                return Task.CompletedTask;
            }

            var table = reply.Data is { Tables.Count: > 0 } ? reply.Data.Tables[0] : null;
            if (table is not { Rows.Count: > 0 })
            {
                setStatus("The batch or module is no longer in the plan cache on the instance.", string.Empty,
                    DashColors.Warning);
                RunOnUi(() => _grid.DataSource = null);
                return Task.CompletedTask;
            }

            RunOnUi(() => Bind(table));

            // Worth saying every time: these are the plans cached now, which a recompile since the deadlock
            // would have replaced.
            var plural = table.Rows.Count == 1 ? string.Empty : "s";
            setStatus($"{table.Rows.Count} plan{plural} cached now - not necessarily the plan that deadlocked.",
                string.Empty, DashColors.Green);
            return Task.CompletedTask;
        }

        private void Bind(DataTable table)
        {
            // The plan collection path works off a row, so give it every column it reads - including the
            // ones only its "no messaging, here's a script to run yourself" fallback needs.  has_plan starts
            // false: whether the repository already holds the plan isn't known here, and collecting is cheap.
            table.Columns.Add(InstanceIdColumn, typeof(int));
            table.Columns.Add(HasPlanColumn, typeof(bool));
            table.Columns.Add(PlanTextColumn, typeof(string));
            table.Columns.Add(PlanActionColumn, typeof(string));
            table.Columns.Add("database_name", typeof(string));
            table.Columns.Add("sql_handle", typeof(string));
            table.Columns.Add("InstanceDisplayName", typeof(string));

            foreach (DataRow row in table.Rows)
            {
                row[InstanceIdColumn] = _context.InstanceID;
                row[HasPlanColumn] = false;
                row["database_name"] = (object)_databaseName ?? DBNull.Value;
                row["sql_handle"] = (object)_sqlHandle ?? DBNull.Value;
                row["InstanceDisplayName"] = (object)_context.InstanceName ?? DBNull.Value;
            }

            _grid.DataSource = table;

            for (var i = 0; i < table.DefaultView.Count; i++)
            {
                RefreshPlanAction(table.DefaultView[i]);
            }

            _grid.ApplyTheme(DBADashUser.SelectedTheme);
        }

        /// <summary>
        /// Sets the row's link text from what the plan path can actually do with it - collect it, view one
        /// already collected, or nothing at all when the row has no plan handle to work from.
        /// </summary>
        private static void RefreshPlanAction(DataRowView row)
        {
            var action = QueryPlanActions.Determine(row, out _);
            row[PlanActionColumn] = action == QueryPlanActions.PlanAction.None
                ? string.Empty
                : QueryPlanActions.ActionText(action);
        }

        /// <summary>
        /// Runs UI work for a reply that arrives after the panel has gone away.  The reply loop keeps running
        /// until the conversation ends, so this can outlive the viewer.
        /// </summary>
        private void RunOnUi(Action action)
        {
            if (IsDisposed || Disposing || !IsHandleCreated) return;
            try
            {
                Invoke(action);
            }
            catch (ObjectDisposedException)
            {
                // Closed between the check and the call - there is nothing left to update.
            }
        }

        // ---------------------------------------------------------------- columns

        private void ConfigureColumns()
        {
            foreach (var name in HiddenColumns)
            {
                var hidden = _grid.Columns[name];
                if (hidden != null) hidden.Visible = false;
            }

            SetHeader("source", "Source");
            SetHeader("is_deadlock_statement", "Deadlock Stmt");
            SetHeader(StatementTextColumn, "Statement");
            SetHeader("execution_count", "Executions", "N0");
            SetHeader("avg_cpu_ms", "Avg CPU (ms)", "N1");
            SetHeader("total_cpu_ms", "Total CPU (ms)", "N1");
            SetHeader("avg_duration_ms", "Avg Duration (ms)", "N1");
            SetHeader("total_duration_ms", "Total Duration (ms)", "N1");
            SetHeader("avg_logical_reads", "Avg Logical Reads", "N0");
            SetHeader("avg_physical_reads", "Avg Physical Reads", "N0");
            SetHeader("avg_writes", "Avg Writes", "N0");
            SetHeader("last_execution_time", "Last Execution");
            SetHeader("creation_time", "Plan Created");
            SetHeader(QueryHashColumn, "Query Hash");
            SetHeader("query_plan_hash", "Plan Hash");
            SetHeader(PlanActionColumn, "Plan");

            LinkifyColumn(PlanActionColumn, "View or collect the execution plan for this statement.");
            LinkifyColumn(QueryHashColumn, "Find this statement in Query Store.");

            var planAction = _grid.Columns[PlanActionColumn];
            if (planAction != null) planAction.DisplayIndex = 0;

            _grid.AutoResizeColumnsWithMaxColumnWidth();
        }

        private void SetHeader(string name, string header, string format = null)
        {
            var col = _grid.Columns[name];
            if (col == null) return;
            col.HeaderText = header;
            if (format == null) return;
            col.DefaultCellStyle.Format = format;
            col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        }

        private void LinkifyColumn(string name, string toolTip)
        {
            var col = _grid.Columns[name];
            if (col is null or DataGridViewLinkColumn) return;

            var index = col.Index;
            var link = new DataGridViewLinkColumn
            {
                Name = col.Name,
                HeaderText = col.HeaderText,
                DataPropertyName = col.DataPropertyName,
                ToolTipText = toolTip,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                TrackVisitedState = false,
                LinkColor = DashColors.LinkColor,
                ActiveLinkColor = DashColors.LinkColor,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells
            };
            _grid.Columns.RemoveAt(index);
            _grid.Columns.Insert(index, link);
        }

        // ---------------------------------------------------------------- actions

        private async void Grid_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (_grid.Rows[e.RowIndex].DataBoundItem is not DataRowView row) return;

            var column = _grid.Columns[e.ColumnIndex].Name;
            try
            {
                switch (column)
                {
                    case PlanActionColumn:
                        if (string.IsNullOrEmpty(row[PlanActionColumn] as string)) return;
                        await QueryPlanActions.Execute(row, _status);
                        RefreshPlanAction(row); // A collected plan is a viewable one from here on
                        break;

                    case QueryHashColumn:
                        ShowQueryStore(row);
                        break;
                }
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex, "Error running the plan action");
            }
        }

        /// <summary>Opens Query Store for the row's statement, which the query hash identifies exactly.</summary>
        private void ShowQueryStore(DataRowView row)
        {
            var queryHash = (row[QueryHashColumn] as string)?.HexStringToByteArray();
            if (queryHash is not { Length: > 0 }) return;

            var context = _context.DeepCopy();
            context.DatabaseName = _databaseName;

            var frm = new QueryStoreViewer
            {
                Context = context,
                QueryHash = queryHash
            };
            frm.ShowSingleInstance();
        }

        private void Grid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (_grid.Columns[e.ColumnIndex].Name != StatementTextColumn) return;

            var sql = _grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value as string;
            if (string.IsNullOrWhiteSpace(sql)) return;

            using var frm = new CodeEditorForm { Code = sql, Syntax = CodeEditor.CodeEditorModes.SQL };
            frm.EditEnabled = false;
            frm.ShowDialog();
        }
    }
}
