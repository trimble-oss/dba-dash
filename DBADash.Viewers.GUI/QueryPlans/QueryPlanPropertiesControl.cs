using DBADashGUI.Viewers;
using DBADash.QueryPlan.Layout;
using DBADash.QueryPlan.Model;
using DBADashGUI.Controls;
using DBADashGUI.CustomReports;
using DBADashGUI.SchemaCompare;
using DBADashGUI.Theme;
using DBADashSharedGUI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace DBADashGUI.QueryPlans
{
    /// <summary>
    /// The properties of whatever is selected in the plan, as SSMS shows them.
    ///
    /// Showplan carries far more per operator than any node caption could hold, and which of it
    /// matters depends entirely on what is being investigated - so all of it goes here and the node
    /// keeps the two or three figures worth glancing at.
    ///
    /// A grid rather than a tree: the values are read by scanning down a column, most of the
    /// properties are leaves, and the grid is the thing the rest of DBA Dash is made of - so it is
    /// themed, sortable off, and copies to the clipboard with the shortcut people already use.
    /// </summary>
    public sealed class QueryPlanPropertiesControl : UserControl, IThemedControl
    {
        private readonly DBADashDataGridView _grid = new()
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            RowHeadersVisible = false,
            ColumnHeadersVisible = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = true,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None
        };

        private readonly Label _empty = new()
        {
            Dock = DockStyle.Fill,
            TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
            Text = "Select an operator to see its properties."
        };

        /// <summary>
        /// Cards above the properties, in the style the session viewer uses for its insights: the
        /// selected operator's warnings and missing index, worst first, then what the operator does -
        /// or, with no operator selected, everything worth knowing about the statement.  The grid holds
        /// every figure; the cards are the few things worth reading before it.
        ///
        /// Laid out by hand from this control's size - see <see cref="LayoutCards"/> - rather than
        /// auto sized: the panel starts hidden, WinForms does not lay out a hidden docked control,
        /// and auto sized cards measured their text against a default width the first time they
        /// were shown, came out many lines tall and pushed the grid off the bottom.  Scrolls rather
        /// than growing past <see cref="MaxCardShare"/> of the height, so the grid always has room.
        /// </summary>
        private readonly Panel _cardPanel = new()
        {
            Dock = DockStyle.Top,
            AutoScroll = true,
            Padding = new Padding(6, 6, 6, 2),
            Visible = false
        };

        private readonly List<(InsightCard Card, Label Text)> _cards = new();

        /// <summary>The most of the panel's height the cards take before they scroll.</summary>
        private const double MaxCardShare = 0.5;

        private const int CardGap = 6;

        private Font _boldFont;

        private PlanNode _node;

        private PlanStatement _statement;

        private bool _showDescriptions = true;

        /// <summary>
        /// What a row of the grid can show in full: the value laid out for reading, and whether it is
        /// offered as a link.  Held on the row's Tag.
        /// </summary>
        private sealed class RowInfo
        {
            /// <summary>The viewer's title.</summary>
            public string Title { get; init; }

            /// <summary>The whole value, laid out for reading.  Null for a row with nothing more to show.</summary>
            public string FullText { get; init; }

            /// <summary>
            /// A link whatever its length: an expression - a predicate, defined values - which is
            /// easier read laid out a term to a line than scanned along one, and the missing index
            /// script.
            /// </summary>
            public bool AlwaysLink { get; init; }

            /// <summary>
            /// <see cref="FullText"/> already says what the expressions in it are, so the notes the
            /// viewer adds beneath a value are left off.
            /// </summary>
            public bool NotesIncluded { get; init; }

            /// <summary>More than the row's height shows - set each time the rows are sized.</summary>
            public bool Clipped { get; set; }

            public bool IsLink => FullText is not null && (AlwaysLink || Clipped);
        }

        /// <summary>
        /// The most lines a value wraps onto before the rest is left to the viewer a click away, or
        /// zero for no limit.  One puts every property on a single line, as SSMS does.
        /// </summary>
        private int _maxLines;

        /// <summary>The choices on the grid's right click menu, as its item text and line limit.</summary>
        private static readonly (string Text, int Lines)[] RowLineChoices =
        [
            ("Single Line", 1),
            ("Up to 3 Lines", 3),
            ("Up to 10 Lines", 10),
            ("Show Everything", 0)
        ];

        private const int ValueColumnIndex = 1;

        /// <summary>Space between a cell's edge and its text, matching the grid's own cells.</summary>
        private const int CellInset = 3;

        private const TextFormatFlags WrapFlags =
            TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl | TextFormatFlags.NoPrefix;

        private const TextFormatFlags SingleLineFlags =
            TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix;

        private Color _linkColor = ThemeExtensions.CurrentTheme.LinkColor;

        /// <summary>Underlined versions of the fonts links are drawn in, made once each.</summary>
        private readonly Dictionary<Font, Font> _linkFonts = new();

        private bool _sizingRows;

        public QueryPlanPropertiesControl()
        {
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Property",
                HeaderText = "Property",
                Width = 190,
                SortMode = DataGridViewColumnSortMode.NotSortable
            });

            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Value",
                HeaderText = "Value",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                SortMode = DataGridViewColumnSortMode.NotSortable
            });

            _maxLines = LoadRowLines();

            _grid.CellClick += Grid_CellClick;
            _grid.CellDoubleClick += Grid_CellDoubleClick;
            _grid.CellMouseMove += Grid_CellMouseMove;
            _grid.CellMouseLeave += (_, _) => _grid.Cursor = Cursors.Default;
            _grid.CellPainting += Grid_CellPainting;
            _grid.ColumnWidthChanged += (_, _) => SizeRowsLater();
            _grid.SizeChanged += (_, _) => SizeRowsLater();
            _grid.FontChanged += (_, _) => SizeRowsLater();
            AddGridMenuItems();

            Controls.Add(_grid);
            Controls.Add(_empty);

            // Added last so it is docked first, above the grid that fills the rest.
            Controls.Add(_cardPanel);
            _grid.Visible = false;
        }

        /// <summary>
        /// Say what the selected operator does above its properties.  The viewer's setting, so it
        /// follows the tooltip: someone who knows the operators wants neither.
        /// </summary>
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public bool ShowDescriptions
        {
            get => _showDescriptions;
            set
            {
                _showDescriptions = value;
                ShowCards();
            }
        }

        /// <summary>
        /// Raised when a card's link asks to go to an operator - the one a warning is on, or the one
        /// reading a table that wants an index - for the host to select it in the plan.
        /// </summary>
        public event EventHandler<PlanOperator> OperatorRequested;

        /// <summary>
        /// Show a node's properties, or the statement's overview when <paramref name="node"/> is null
        /// or the statement root: every warning and missing index in it, and what else is worth
        /// knowing, above the statement's own properties.
        /// </summary>
        public void Show(PlanNode node, PlanStatement statement)
        {
            _grid.Rows.Clear();

            _node = node;
            _statement = node?.Statement ?? statement;
            ShowCards();

            var properties = node?.Operator?.Properties ?? _statement?.Properties;

            if (properties is null || properties.Count == 0)
            {
                _grid.Visible = false;
                _empty.Visible = _cards.Count == 0;
                _empty.Text = _statement is null
                    ? "Select an operator to see its properties."
                    : "This operator reported no properties.";
                return;
            }

            _empty.Visible = false;
            _grid.Visible = true;

            Add(properties, 0);

            // The CREATE INDEX statement in the grid too, where Ctrl+C and a double click reach it.
            var missing = node?.Operator is { } op
                ? _statement.MissingIndexesFor(op)
                : _statement.MissingIndexes;

            foreach (var index in missing) AddMissingIndex(index);

            AddCombinedWarnings();

            SizeRows();

            // Scrolled back to the top: the panel is re-filled on every selection change, and
            // leaving it where the last operator's list was scrolled to shows the wrong rows.
            if (_grid.Rows.Count > 0)
            {
                try
                {
                    _grid.FirstDisplayedScrollingRowIndex = 0;
                }
                catch (InvalidOperationException)
                {
                    // "No room is available to display rows": in a very short panel the cards can
                    // take all the height.  With no rows on screen there is no scroll position to
                    // put right, and the grid starts at the top when it is given room again.
                }
            }
        }

        /// <summary>
        /// Everything shown, as text, for pasting into a ticket.  Flattened with the same
        /// indentation the grid uses so the structure survives the paste.
        /// </summary>
        public string ToText()
        {
            var text = new StringBuilder();

            foreach (DataGridViewRow row in _grid.Rows)
            {
                text.Append(row.Cells[0].Value).Append('\t').AppendLine(Convert.ToString(row.Cells[1].Value));
            }

            return text.ToString();
        }

        /// <summary>
        /// Flatten the property tree into rows, indenting the children.
        ///
        /// Indentation rather than expandable groups: the nesting is only ever one or two deep, and a
        /// panel that opens collapsed hides the figures the reader selected the operator to see.
        /// </summary>
        private void Add(IReadOnlyList<PlanProperty> properties, int depth)
        {
            foreach (var property in properties)
            {
                var hasScript = !string.IsNullOrEmpty(property.Script);
                var index = _grid.Rows.Add(
                    new string(' ', depth * 4) + property.Name,
                    hasScript ? "View Script" : property.Value ?? string.Empty);

                // A heading with a script - the set options - is a link to it, the way the missing
                // index's T-SQL row is.  The script says what it is for, so no notes go beneath it.
                _grid.Rows[index].Tag = hasScript
                    ? new RowInfo
                    {
                        Title = ViewerTitle(property.Name),
                        FullText = property.Script,
                        AlwaysLink = true,
                        NotesIncluded = true
                    }
                    : new RowInfo
                    {
                        Title = ViewerTitle(property.Name),
                        FullText = property.ReadableValue,
                        AlwaysLink = property.IsExpression
                    };

                if (hasScript)
                {
                    _grid.Rows[index].Cells[ValueColumnIndex].ToolTipText =
                        "Click to open the SET statements that reproduce these options, to run in SSMS.";
                }

                if (property.IsExpression && ShowsExpressionValues(property.Name))
                {
                    AddExpressionValues(property, depth + 1);
                }

                // A heading with no value of its own carries the emphasis instead, so the groups are
                // findable when scrolling a long list.
                if (!property.HasChildren) continue;

                _grid.Rows[index].DefaultCellStyle.Font = _boldFont ??= new Font(Font, FontStyle.Bold);

                Add(property.Children, depth + 1);
            }
        }

        /// <summary>
        /// The most values listed under one predicate before the rest are left to the viewer, which
        /// lists them all.  A generated predicate can refer to dozens, and a grid that lists every one
        /// pushes the operator's other properties off the panel.
        /// </summary>
        private const int MaxInlineExpressionValues = 8;

        /// <summary>
        /// The properties whose Expr names are listed under them.  A predicate reading
        /// [Expr1014] IS NOT NULL says nothing until the value is beside it; the other expression
        /// properties are definitions or lists of columns, which read fine without.
        /// </summary>
        private static bool ShowsExpressionValues(string name) =>
            name is "Predicate" or "Seek Predicates";

        /// <summary>
        /// Under a predicate, the plan's own values it refers to - the same ones the viewer writes out
        /// beneath it (see <see cref="PlanScripts.ExpressionsUsedIn"/>) - one row each, so the
        /// predicate reads in full without a click.
        /// </summary>
        private void AddExpressionValues(PlanProperty property, int depth)
        {
            var used = PlanScripts.ExpressionsIn(_statement, property.Value);

            foreach (var expression in used.Take(MaxInlineExpressionValues))
            {
                var row = _grid.Rows.Add(new string(' ', depth * 4) + expression.Name, expression.DefinitionOneLine);
                _grid.Rows[row].Tag = new RowInfo
                {
                    Title = expression.DisplayName,
                    FullText = PlanScripts.Expression(expression),
                    NotesIncluded = true
                };
            }

            if (used.Count <= MaxInlineExpressionValues) return;

            // The rest are in the viewer, which opens on the predicate with every value under it.
            var more = _grid.Rows.Add(
                new string(' ', depth * 4) + "...",
                (used.Count - MaxInlineExpressionValues).ToString(CultureInfo.CurrentCulture) + " more - click to see all");

            _grid.Rows[more].Tag = new RowInfo
            {
                Title = ViewerTitle(property.Name),
                FullText = property.ReadableValue,
                AlwaysLink = true
            };
        }

        /// <summary>The viewer's title: the property, and the operator it belongs to.</summary>
        private string ViewerTitle(string property) =>
            _node?.Operator is { } op
                ? property + " - " + op.DisplayName + " (node " + op.NodeId.ToString(CultureInfo.InvariantCulture) + ")"
                : property;

        /// <summary>
        /// A missing index as a group of rows, ending with its CREATE INDEX statement on one line -
        /// the grid shows one line a row - and double clicking that row opens it formatted.
        /// </summary>
        private void AddMissingIndex(PlanMissingIndex index)
        {
            var heading = _grid.Rows.Add("Missing Index", string.Empty);
            _grid.Rows[heading].DefaultCellStyle.Font = _boldFont ??= new Font(Font, FontStyle.Bold);

            AddRow("Impact", index.Impact.ToString("0.#", CultureInfo.CurrentCulture) + "%");
            AddRow("Table", index.QualifiedTableName);
            if (index.EqualityColumns.Count > 0) AddRow("Equality", string.Join(", ", index.EqualityColumns));
            if (index.InequalityColumns.Count > 0) AddRow("Inequality", string.Join(", ", index.InequalityColumns));
            if (index.IncludedColumns.Count > 0) AddRow("Include", string.Join(", ", index.IncludedColumns));

            // The cell holds the statement on its own, ready to run where the row is copied; the row
            // opens the annotated script - the same text the Missing Indexes tab shows, which on a temp
            // table is where the note about declaring the index on the CREATE TABLE lives.
            var script = _grid.Rows.Add("    T-SQL", index.CreateStatementOneLine);
            _grid.Rows[script].Tag = new RowInfo
            {
                Title = "Missing Index",
                FullText = PlanScripts.MissingIndex(_statement, index),
                AlwaysLink = true
            };
            _grid.Rows[script].Cells[ValueColumnIndex].ToolTipText =
                "Click to open the CREATE INDEX statement with the optimizer's notes.";

            void AddRow(string name, string value)
            {
                var row = _grid.Rows.Add("    " + name, value);
                _grid.Rows[row].Tag = new RowInfo { Title = "Missing Index - " + name, FullText = value.Replace(", ", Environment.NewLine) };
            }
        }

        /// <summary>
        /// The combined warnings - four or more of the same kind, said once on the card - listed in
        /// the grid, each row opening every one of them, on its node, in the viewer.  The card's
        /// summary is cut short and the tooltip cannot be clicked; this is where a combined warning is
        /// read in full without leaving for the Warnings tab.
        /// </summary>
        private void AddCombinedWarnings()
        {
            if (_statement is null) return;

            var insights = _node?.Operator is { } op
                ? PlanInsights.ForOperator(op, _statement)
                : PlanInsights.ForStatement(_statement);

            foreach (var insight in insights.Where(i => i.IsCombinedWarning))
            {
                var title = insight.Text.Split('\n')[0];

                var heading = _grid.Rows.Add("Warnings", string.Empty);
                _grid.Rows[heading].DefaultCellStyle.Font = _boldFont ??= new Font(Font, FontStyle.Bold);

                var value = insight.WarningCount.ToString(CultureInfo.CurrentCulture) + " - click to see each";
                var row = _grid.Rows.Add("    " + title, value);
                _grid.Rows[row].Tag = new RowInfo
                {
                    Title = title,
                    FullText = insight.FullText,
                    AlwaysLink = true
                };
                _grid.Rows[row].Cells[ValueColumnIndex].ToolTipText = "Click to see every one of these warnings, on its node.";
            }
        }

        private RowInfo InfoAt(int rowIndex) =>
            rowIndex >= 0 && rowIndex < _grid.Rows.Count ? _grid.Rows[rowIndex].Tag as RowInfo : null;

        /// <summary>A value shown as a link opens in full with one click, as links do everywhere else in DBA Dash.</summary>
        private void Grid_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == ValueColumnIndex && InfoAt(e.RowIndex) is { IsLink: true } info) ShowFull(info);
        }

        /// <summary>Anywhere on a row with more to show, for the property name as well as the value.</summary>
        private void Grid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex != ValueColumnIndex && InfoAt(e.RowIndex) is { IsLink: true } info) ShowFull(info);
        }

        private void Grid_CellMouseMove(object sender, DataGridViewCellMouseEventArgs e)
        {
            var link = e.ColumnIndex == ValueColumnIndex && InfoAt(e.RowIndex) is { IsLink: true };
            _grid.Cursor = link ? Cursors.Hand : Cursors.Default;
        }

        /// <summary>
        /// The whole value in the code viewer, with the plan's own expressions that it refers to
        /// written out underneath: a predicate testing Expr1011 is unreadable until something says
        /// what Expr1011 is, and here there is room to say it.
        /// </summary>
        private void ShowFull(RowInfo info) =>
            CommonShared.ShowCodeViewer(
                info.NotesIncluded ? info.FullText : info.FullText + PlanScripts.ExpressionsUsedIn(_statement, info.FullText),
                info.Title,
                CodeEditor.CodeEditorModes.SQL);

        private static void ShowScript(string script) =>
            CommonShared.ShowCodeViewer(script, "Missing Index", CodeEditor.CodeEditorModes.SQL);

        // ---------------------------------------------------------------- row heights and painting

        /// <summary>
        /// Each row as tall as its value needs, up to <see cref="_maxLines"/> lines, and marked as a
        /// link when that cuts it short.  Run again whenever the value column changes width, which
        /// changes where every value wraps.
        ///
        /// Sized here rather than by the grid's own auto sizing, which has no upper limit: one
        /// generated predicate would make a row taller than the panel.
        /// </summary>
        private void SizeRows()
        {
            if (_sizingRows || _grid.Rows.Count == 0) return;

            _sizingRows = true;
            try
            {
                // Twice at most: rows growing can bring in the vertical scroll bar, which narrows the
                // value column and moves where everything wraps.  The event that says so arrives while
                // this is running, so it is checked for here instead.
                for (var pass = 0; pass < 2; pass++)
                {
                    var width = _grid.Columns[ValueColumnIndex].Width - (CellInset * 2);
                    if (width <= 0) return;

                    SizeRows(width);
                    if (_grid.Columns[ValueColumnIndex].Width - (CellInset * 2) == width) break;
                }
            }
            finally
            {
                _sizingRows = false;
            }

            _grid.Invalidate();
        }

        private bool _sizeRowsPending;

        /// <summary>
        /// <see cref="SizeRows()"/> once the grid has finished what it is doing.  A resize arrives in
        /// the middle of the grid laying out its fill column, and the grid refuses a row height change
        /// until that is over.  Several resizes in a row - a splitter being dragged - are sized once.
        /// </summary>
        private void SizeRowsLater()
        {
            if (_sizeRowsPending || !IsHandleCreated) return;

            _sizeRowsPending = true;
            BeginInvoke(() =>
            {
                _sizeRowsPending = false;
                if (!IsDisposed) SizeRows();
            });
        }

        private void SizeRows(int width)
        {
            var singleHeight = _grid.RowTemplate.Height;

            foreach (DataGridViewRow row in _grid.Rows)
            {
                var text = Convert.ToString(row.Cells[ValueColumnIndex].Value) ?? string.Empty;
                var font = row.DefaultCellStyle.Font ?? _grid.DefaultCellStyle.Font ?? _grid.Font;
                var lineHeight = LineHeight(font);

                var lines = text.Length == 0
                    ? 1
                    : Math.Max(1, (int)Math.Round(
                        TextRenderer.MeasureText(text, font, new Size(width, int.MaxValue), WrapFlags).Height / (double)lineHeight));

                var shown = _maxLines <= 0 ? lines : Math.Min(lines, _maxLines);
                var height = singleHeight + ((shown - 1) * lineHeight);
                if (row.Height != height) row.Height = height;

                if (row.Tag is RowInfo info) info.Clipped = lines > shown;
            }
        }

        private readonly Dictionary<Font, int> _lineHeights = new();

        /// <summary>The height of one line of text, measured once per font - it is needed for every cell painted.</summary>
        private int LineHeight(Font font)
        {
            if (!_lineHeights.TryGetValue(font, out var height))
            {
                height = TextRenderer.MeasureText("Ag", font, new Size(int.MaxValue, int.MaxValue), WrapFlags).Height;
                _lineHeights[font] = height;
            }

            return height;
        }

        /// <summary>
        /// Draws the text itself, so it wraps exactly where <see cref="SizeRows"/> measured it to -
        /// the grid's own wrapping breaks lines by different rules and would leave rows a line short or
        /// a line over - and so a value with more to it can be drawn as a link.
        /// </summary>
        private void Grid_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            e.PaintBackground(e.CellBounds, true);

            var selected = (e.State & DataGridViewElementStates.Selected) != 0;
            var info = InfoAt(e.RowIndex);
            var isLink = e.ColumnIndex == ValueColumnIndex && info is { IsLink: true };
            var font = isLink ? LinkFont(e.CellStyle.Font) : e.CellStyle.Font;
            var color = selected ? e.CellStyle.SelectionForeColor : isLink ? _linkColor : e.CellStyle.ForeColor;

            // Single line rows are centred as the grid centres them; the first line of a taller row
            // sits where a single line would, so every name lines up with its value's first line.
            var lineHeight = LineHeight(e.CellStyle.Font);
            var top = Math.Max(0, (_grid.RowTemplate.Height - lineHeight) / 2);
            var bounds = new Rectangle(
                e.CellBounds.Left + CellInset,
                e.CellBounds.Top + top,
                Math.Max(0, e.CellBounds.Width - (CellInset * 2)),
                Math.Max(0, e.CellBounds.Height - top));

            var wraps = e.ColumnIndex == ValueColumnIndex && e.CellBounds.Height > _grid.RowTemplate.Height;
            TextRenderer.DrawText(e.Graphics, Convert.ToString(e.FormattedValue), font, bounds, color, wraps ? WrapFlags : SingleLineFlags);

            e.Paint(e.ClipBounds, DataGridViewPaintParts.Border | DataGridViewPaintParts.Focus);
            e.Handled = true;
        }

        private Font LinkFont(Font font)
        {
            if (!_linkFonts.TryGetValue(font, out var link))
            {
                link = new Font(font, font.Style | FontStyle.Underline);
                _linkFonts[font] = link;
            }

            return link;
        }

        /// <summary>Row height on the grid's right click menu, remembered for next time.</summary>
        private void AddGridMenuItems()
        {
            foreach (var menu in new[] { _grid.CellContextMenu, _grid.ColumnContextMenu })
            {
                var view = new ToolStripMenuItem("View Full Value", null, (_, _) =>
                {
                    if (InfoAt(_grid.ClickedRowIndex) is { FullText: not null } info) ShowFull(info);
                });

                var rowHeight = new ToolStripMenuItem("Row Height");

                foreach (var (text, lines) in RowLineChoices)
                {
                    rowHeight.DropDownItems.Add(new ToolStripMenuItem(text, null, (_, _) => SetMaxLines(lines)) { Tag = lines });
                }

                menu.Opening += (_, _) =>
                {
                    view.Visible = menu == _grid.CellContextMenu;
                    view.Enabled = InfoAt(_grid.ClickedRowIndex) is { FullText: not null };

                    foreach (ToolStripMenuItem item in rowHeight.DropDownItems) item.Checked = (int)item.Tag == _maxLines;
                };

                menu.Items.Insert(0, view);
                menu.Items.Insert(1, rowHeight);
                menu.Items.Insert(2, new ToolStripSeparator());
            }
        }

        private void SetMaxLines(int lines)
        {
            _maxLines = lines;
            SizeRows();

            try
            {
                ViewerSettings.QueryPlanPropertyRowLines = lines;
                ViewerSettings.Save();
            }
            catch (Exception ex)
            {
                // A preference that cannot be saved is not worth interrupting anyone over.
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        private static int LoadRowLines()
        {
            var lines = ViewerSettings.QueryPlanPropertyRowLines;
            return RowLineChoices.Any(c => c.Lines == lines) ? lines : 3;
        }

        private static void CopyScript(string script)
        {
            try
            {
                Clipboard.SetText(script);
            }
            catch (Exception ex)
            {
                // Another application holding the clipboard open is not worth an error dialog.
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        /// <summary>
        /// Rebuild the cards.  For an operator: its warnings and missing index, worst first, then what
        /// it does when descriptions are on.  For the statement, or nothing selected: everything worth
        /// knowing about the statement - see <see cref="PlanInsights.ForStatement"/> - each with a
        /// link to the operator it is about.
        /// </summary>
        private void ShowCards()
        {
            foreach (var (card, _) in _cards) card.Dispose();
            _cards.Clear();
            _cardPanel.Controls.Clear();

            foreach (var (severity, text, actions, tooltips) in CardsFor())
            {
                var card = new InsightCard
                {
                    Padding = new Padding(InsightCard.AccentWidth + InsightCard.IconGutter, 12, 12, 12),
                    FillColor = InsightCard.FillFor(severity),
                    AccentColor = InsightCard.AccentFor(severity),
                    Icon = severity
                };

                var label = InsightCard.CreateContentLabel(text, actions, tooltips);
                label.AutoSize = false;
                label.Dock = DockStyle.Fill;
                label.BackColor = card.FillColor;
                label.ForeColor = InsightCard.TextFor(severity);
                label.Font = severity == InsightCard.CardIcon.Critical ? (_boldFont ??= new Font(Font, FontStyle.Bold)) : Font;

                card.Controls.Add(label);
                _cardPanel.Controls.Add(card);
                _cards.Add((card, label));
            }

            LayoutCards();
            _cardPanel.Visible = _cards.Count > 0;
        }

        /// <summary>
        /// The most operators a card offers to go to.  A card that covers a dozen of them is a card
        /// with a dozen links on it, which is the wall of text combining them was meant to avoid; the
        /// text already says how many there are.
        /// </summary>
        private const int MaxGoToLinks = 5;

        /// <summary>What a link's tooltip shows of an expression before the viewer is needed.</summary>
        private const int TooltipExpressionLength = 160;

        private IEnumerable<(InsightCard.CardIcon Severity, string Text, Dictionary<string, Action> Actions,
            Dictionary<string, string> Tooltips)> CardsFor()
        {
            if (_statement is null) yield break;

            var overview = _node?.Operator is null;
            var insights = _node?.Operator is { } op
                ? PlanInsights.ForOperator(op, _statement)
                : PlanInsights.ForStatement(_statement);

            foreach (var insight in insights)
            {
                var actions = new Dictionary<string, Action>();
                var tooltips = new Dictionary<string, string>();
                var links = new List<string>();

                if (insight.MissingIndex is { } index)
                {
                    var script = PlanScripts.MissingIndex(_statement, index);
                    actions["view"] = () => ShowScript(script);
                    actions["copy"] = () => CopyScript(script);
                    links.Add("[View T-SQL](action:view)");
                    links.Add("[Copy T-SQL](action:copy)");
                }

                // On the overview, where the card is not already about the selected operator.  One
                // that covers several operators offers each of them: it is one card because the
                // warning is one thing to know, but going to look means going to one of them.
                if (overview)
                {
                    foreach (var target in insight.Operators.Take(MaxGoToLinks))
                    {
                        var node = target.NodeId.ToString(CultureInfo.InvariantCulture);
                        var key = "go-" + node;

                        actions[key] = () => OperatorRequested?.Invoke(this, target);
                        links.Add("[" +
                                  (insight.Operators.Count == 1
                                      ? "Go to " + target.DisplayName + " (node " + node + ")"
                                      : links.Count == 0 ? "Go to node " + node : "node " + node) +
                                  "](" + InsightCard.ActionScheme + key + ")");
                    }
                }

                var text = LinkExpressions(insight.Text, actions, tooltips);

                // The card only has room for the first couple of a combined warning's details; the
                // rest are otherwise only on the Warnings tab.  A link opens every one, on its node,
                // in the viewer.
                if (insight.IsCombinedWarning)
                {
                    var full = insight.FullText!;
                    actions["all"] = () => CommonShared.ShowCodeViewer(full, insight.Text.Split('\n')[0], CodeEditor.CodeEditorModes.None);
                    links.Add("[Show all " + insight.WarningCount.ToString(CultureInfo.InvariantCulture) + "](" +
                              InsightCard.ActionScheme + "all)");
                }

                // A bare "\n": LinkLabel places links after a "\r\n" one character late.
                if (links.Count > 0) text += "\n" + string.Join("   ", links);

                yield return (IconFor(insight.Severity), text, actions, tooltips);
            }

            if (overview && insights.Count == 0)
            {
                yield return (InsightCard.CardIcon.Information, "No warnings or missing indexes in this statement.", null, null);
            }

            if (!overview && _showDescriptions)
            {
                yield return (InsightCard.CardIcon.Information, PlanOperatorDescriptions.For(_node.Operator), null, null);
            }
        }

        /// <summary>
        /// Every name of the plan's own - [Expr1011] - in <paramref name="text"/>, as a link to what
        /// it means.
        ///
        /// A warning about a conversion names the value it converted and nothing else, and the value
        /// is one the optimiser invented: the reader is told the estimate is wrong because of
        /// Expr1011 and has no way, from the card, to find out what Expr1011 is.  Hovering gives the
        /// definition, and clicking opens it with everything it is built from written out.
        /// </summary>
        private string LinkExpressions(string text, IDictionary<string, Action> actions, IDictionary<string, string> tooltips)
        {
            if (_statement is null || _statement.Expressions.Count == 0 || string.IsNullOrEmpty(text)) return text;

            var linked = new StringBuilder();
            var copied = 0;

            foreach (var (index, length, name) in PlanExpressions.ReferencesIn(text))
            {
                if (_statement.ExpressionNamed(name) is not { } expression) continue;

                linked.Append(text, copied, index - copied);
                copied = index + length;

                var key = "expression-" + name;
                actions[key] = () => ShowExpression(expression);
                tooltips[InsightCard.ActionScheme + key] = ExpressionTooltip(expression);

                // The brackets go with the link: what is left is the name, underlined, which is what
                // reads as something to click.
                linked.Append('[').Append(name).Append("](").Append(InsightCard.ActionScheme).Append(key).Append(')');
            }

            return linked.Append(text, copied, text.Length - copied).ToString();
        }

        /// <summary>What hovering an expression link says: what it is, and where it comes from.</summary>
        private static string ExpressionTooltip(PlanExpression expression)
        {
            var tip = new StringBuilder(expression.Name)
                .Append(" = ")
                .AppendLine(Shorten(expression.Definition, TooltipExpressionLength));

            if (expression.IsNested)
            {
                tip.Append("In full: ").AppendLine(Shorten(expression.Expanded, TooltipExpressionLength));
            }

            if (expression.DefinedByDescription is { Length: > 0 } definedBy)
            {
                tip.Append("Worked out by ").AppendLine(definedBy);
            }

            return tip.Append("Click to see it in full.").ToString();
        }

        /// <summary>One line of at most <paramref name="maxLength"/> characters, for a tooltip.</summary>
        private static string Shorten(string text, int maxLength)
        {
            var line = string.Join(' ', text.Split((char[])null, StringSplitOptions.RemoveEmptyEntries));
            return line.Length <= maxLength ? line : line[..maxLength] + "...";
        }

        private static void ShowExpression(PlanExpression expression) =>
            CommonShared.ShowCodeViewer(
                PlanScripts.Expression(expression),
                "Expression " + expression.DisplayName,
                CodeEditor.CodeEditorModes.SQL);

        private static InsightCard.CardIcon IconFor(PlanWarningSeverity severity) => severity switch
        {
            PlanWarningSeverity.Critical => InsightCard.CardIcon.Critical,
            PlanWarningSeverity.Warning => InsightCard.CardIcon.Warning,
            _ => InsightCard.CardIcon.Information
        };

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            LayoutCards();
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);

            // The rows are rebuilt as well as the cards: the headings are drawn in the bold font being
            // thrown away here.
            _grid.Rows.Clear();
            _boldFont?.Dispose();
            _boldFont = null;

            foreach (var font in _linkFonts.Values) font.Dispose();
            _linkFonts.Clear();
            _lineHeights.Clear();

            if (_statement is not null) Show(_node, _statement);
            else ShowCards();
        }

        /// <summary>
        /// Each card as tall as its text needs, wrapped to this control's width - which is known
        /// whether or not the cards are showing, unlike their own - stacked, and scrolling once they
        /// would take more than <see cref="MaxCardShare"/> of the height.
        /// </summary>
        private void LayoutCards()
        {
            if (_cards.Count == 0) return;

            var width = ClientSize.Width - _cardPanel.Padding.Horizontal;
            if (width <= 0) return;

            var heights = Heights(width);
            var total = Total(heights);
            var limit = (int)(ClientSize.Height * MaxCardShare);
            var scrolls = limit > 0 && total > limit;

            // The scroll bar takes its width from the cards, which can make them a line taller.
            if (scrolls)
            {
                width -= SystemInformation.VerticalScrollBarWidth;
                heights = Heights(width);
                total = Total(heights);
            }

            var y = _cardPanel.Padding.Top + _cardPanel.AutoScrollPosition.Y;
            for (var i = 0; i < _cards.Count; i++)
            {
                _cards[i].Card.SetBounds(_cardPanel.Padding.Left, y, width, heights[i]);
                y += heights[i] + CardGap;
            }

            _cardPanel.AutoScrollMinSize = new Size(0, total);
            _cardPanel.Height = scrolls ? limit : total;
        }

        private int[] Heights(int width)
        {
            var heights = new int[_cards.Count];

            for (var i = 0; i < _cards.Count; i++)
            {
                var (card, label) = _cards[i];
                var textWidth = Math.Max(width - card.Padding.Horizontal, 20);
                var text = InsightCard.ContentHeight(label, textWidth);

                heights[i] = Math.Max(text, InsightCard.IconSize) + card.Padding.Vertical;
            }

            return heights;
        }

        private int Total(int[] heights) =>
            heights.Sum() + (CardGap * (heights.Length - 1)) + _cardPanel.Padding.Vertical;

        public void ApplyTheme(BaseTheme theme)
        {
            BackColor = theme.BackgroundColor;
            _empty.ForeColor = theme.ForegroundColor;

            // The cards keep their own colours; only their corners round into the panel behind.
            _cardPanel.BackColor = theme.BackgroundColor;
            _grid.ApplyTheme(theme);
            _linkColor = theme.LinkColor;
        }
    }
}
