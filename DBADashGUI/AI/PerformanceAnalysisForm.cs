using DBADash.PerformanceAnalysis;
using DBADashGUI.Performance;
using DBADashGUI.Theme;
using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBADashGUI.AI
{
    /// <summary>
    /// AI analysis of an instance's performance over a window, on the same principle as the plan and
    /// deadlock viewers: the reader sees exactly what would be sent, chooses what goes, and nothing leaves
    /// until they press the button.
    ///
    /// The window comes from the range the reader is looking at, capped to a week - see
    /// <see cref="PerformanceAnalysisWindow"/>.  Everything for it is read once; switching a section on or
    /// off, changing how many rows it sends, or dropping the baseline rebuilds the preview from what was
    /// read, so the size readout answers "what if I leave this out" at once.
    ///
    /// A form of its own rather than a panel on the Performance tab, so the charts stay in view while the
    /// reader reads the answer.
    /// </summary>
    internal sealed class PerformanceAnalysisForm : Form
    {
        private const int IncludeColumn = 0;
        private const int SectionColumn = 1;
        private const int TopNColumn = 2;
        private const int TimeSeriesColumn = 3;
        private const int SizeColumn = 4;

        private readonly int _instanceId;
        private readonly string _instance;

        private readonly DateTimePicker _from = NewPicker();
        private readonly DateTimePicker _to = NewPicker();
        private readonly Button _reload = new() { Text = "Reload", AutoSize = true };
        private readonly ComboBox _focus = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200 };
        private readonly CheckBox _baseline = new() { Text = "Compare with the same window a week earlier", AutoSize = true, Checked = true };
        private readonly Label _windowNote = new() { AutoSize = true, MaximumSize = new Size(380, 0) };

        private readonly DataGridView _sections = new()
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.CellSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            EditMode = DataGridViewEditMode.EditOnEnter
        };

        private readonly TextBox _preview = new()
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            WordWrap = false,
            ScrollBars = ScrollBars.Both,
            Font = new Font("Consolas", 9f)
        };

        private readonly ToolStripStatusLabel _status = new() { Spring = true, TextAlign = ContentAlignment.MiddleLeft };

        private PerformanceAnalysisOptions _options;
        private PerformanceAnalysisData _data;
        private PerformanceAnalysisPayload _payload;
        private CancellationTokenSource _loading;

        /// <summary>Set while the grid is being filled, so filling it is not taken for the reader changing it.</summary>
        private bool _populating;

        private PerformanceAnalysisForm(int instanceId, string instance, PerformanceAnalysisFocus focus)
        {
            _instanceId = instanceId;
            _instance = instance;
            _options = PerformanceAnalysisOptions.Defaults(focus);

            Text = $"AI Performance Analysis - {instance}";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(1200, 760);
            MinimumSize = new Size(800, 500);

            _focus.Items.AddRange([PerformanceAnalysisPayload.DescribeFocus(PerformanceAnalysisFocus.General), PerformanceAnalysisPayload.DescribeFocus(PerformanceAnalysisFocus.LatchesAndSpinlocks)]);
            _focus.SelectedIndex = focus == PerformanceAnalysisFocus.LatchesAndSpinlocks ? 1 : 0;

            BuildLayout();
            SetWindow(DateRange.FromUTC, DateRange.ToUTC);
            PopulateSections();

            _reload.Click += async (_, _) => await LoadAsync();
            _focus.SelectedIndexChanged += (_, _) => ChangeFocus();
            _baseline.CheckedChanged += (_, _) =>
            {
                _options.IncludeBaseline = _baseline.Checked;
                RebuildPayload();
            };

            _sections.CurrentCellDirtyStateChanged += (_, _) =>
            {
                // A checkbox only reports its new value once the edit is committed, which would otherwise wait for the
                // reader to click somewhere else.
                if (_sections.IsCurrentCellDirty && _sections.CurrentCell is DataGridViewCheckBoxCell)
                {
                    _sections.CommitEdit(DataGridViewDataErrorContexts.Commit);
                }
            };
            _sections.CellValidating += SectionsCellValidating;
            _sections.CellValueChanged += SectionsCellValueChanged;

            Shown += async (_, _) => await LoadAsync();
            FormClosed += (_, _) => _loading?.Cancel();

            this.ApplyTheme();
        }

        /// <summary>Opens the analysis for an instance, over the range the reader is looking at.</summary>
        internal static void Open(IWin32Window owner, int instanceId, string instance, PerformanceAnalysisFocus focus = PerformanceAnalysisFocus.General)
        {
            var form = new PerformanceAnalysisForm(instanceId, instance, focus);
            form.Show(owner);
        }

        private static DateTimePicker NewPicker() => new()
        {
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "yyyy-MM-dd HH:mm",
            Width = 140
        };

        private void BuildLayout()
        {
            var window = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, Padding = new Padding(6) };
            window.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            window.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            window.Controls.Add(new Label { Text = "Focus", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
            window.Controls.Add(_focus, 1, 0);
            window.Controls.Add(new Label { Text = "From", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 1);
            window.Controls.Add(_from, 1, 1);
            window.Controls.Add(new Label { Text = "To", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 2);
            var toRow = new FlowLayoutPanel { AutoSize = true, Margin = Padding.Empty, WrapContents = false };
            toRow.Controls.Add(_to);
            toRow.Controls.Add(_reload);
            window.Controls.Add(toRow, 1, 2);
            window.Controls.Add(_baseline, 0, 3);
            window.SetColumnSpan(_baseline, 2);
            window.Controls.Add(_windowNote, 0, 4);
            window.SetColumnSpan(_windowNote, 2);

            _sections.Columns.Add(new DataGridViewCheckBoxColumn { HeaderText = "Send", FillWeight = 14 });
            _sections.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Section", ReadOnly = true, FillWeight = 46 });
            _sections.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Top N", FillWeight = 16, ToolTipText = $"Rows named individually (1-{PerformanceAnalysisOptions.MaxTopN}); the rest are summarised." });
            _sections.Columns.Add(new DataGridViewCheckBoxColumn { HeaderText = "Over time", FillWeight = 20, ToolTipText = "Send a series over the window as well as the summary.  Most of a section's size." });
            _sections.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Size", ReadOnly = true, FillWeight = 22 });

            var left = new Panel { Dock = DockStyle.Fill };
            left.Controls.Add(_sections);
            left.Controls.Add(window);

            var tabs = new TabControl { Dock = DockStyle.Fill };
            var request = new TabPage("Request") { ToolTipText = "Exactly what will be sent to the AI service." };
            request.Controls.Add(_preview);
            tabs.TabPages.Add(request);

            var split = new SplitContainer { Dock = DockStyle.Fill, FixedPanel = FixedPanel.Panel1 };
            split.Panel1.Controls.Add(left);
            split.Panel2.Controls.Add(tabs);

            var statusBar = new StatusStrip { ShowItemToolTips = true, SizingGrip = false };
            statusBar.Items.Add(_status);

            Controls.Add(split);
            Controls.Add(statusBar);

            Load += (_, _) => split.SplitterDistance = 420;
        }

        /// <summary>Sets the pickers to a window, capped to a week, and says what the cap did where it did anything.</summary>
        private void SetWindow(DateTime fromUtc, DateTime toUtc)
        {
            var window = PerformanceAnalysisWindow.Create(fromUtc, toUtc);
            _from.Value = window.FromUtc.ToAppTimeZone();
            _to.Value = window.ToUtc.ToAppTimeZone();

            var note = window.WasClamped
                ? $"The range selected is longer than {PerformanceAnalysisWindow.Describe(PerformanceAnalysisWindow.MaxLength)}, so the analysis covers its last {PerformanceAnalysisWindow.Describe(PerformanceAnalysisWindow.MaxLength)}.  "
                : string.Empty;

            if (DateRange.HasTimeOfDayFilter || DateRange.HasDayOfWeekFilter)
            {
                note += "Time of day and day of week filters are not applied to the analysis.";
            }

            _windowNote.Text = note;
            _windowNote.Visible = note.Length > 0;
        }

        private void ChangeFocus()
        {
            var focus = _focus.SelectedIndex == 1 ? PerformanceAnalysisFocus.LatchesAndSpinlocks : PerformanceAnalysisFocus.General;
            if (focus == _options.Focus) return;

            _options = PerformanceAnalysisOptions.Defaults(focus);
            _options.IncludeBaseline = _baseline.Checked;
            PopulateSections();
            RebuildPayload();
        }

        private void PopulateSections()
        {
            _populating = true;
            try
            {
                _sections.Rows.Clear();
                foreach (var section in _options.Sections)
                {
                    var index = _sections.Rows.Add(section.Include, PerformanceAnalysisOptions.Title(section.Section),
                        section.TopN?.ToString() ?? string.Empty, section.TimeSeries ?? false, string.Empty);
                    var row = _sections.Rows[index];
                    row.Tag = section.Section;

                    if (section.TopN is null)
                    {
                        row.Cells[TopNColumn].ReadOnly = true;
                    }

                    if (section.TimeSeries is null)
                    {
                        // Nothing to tick: hide the box rather than show one that does nothing.
                        row.Cells[TimeSeriesColumn] = new DataGridViewTextBoxCell { Value = string.Empty };
                        row.Cells[TimeSeriesColumn].ReadOnly = true;
                    }
                }
            }
            finally
            {
                _populating = false;
            }
        }

        private void SectionsCellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            if (e.ColumnIndex != TopNColumn || _sections.Rows[e.RowIndex].Cells[TopNColumn].ReadOnly) return;

            if (!int.TryParse(Convert.ToString(e.FormattedValue), out var value) || value < 1 || value > PerformanceAnalysisOptions.MaxTopN)
            {
                _status.Text = $"Top N is a number from 1 to {PerformanceAnalysisOptions.MaxTopN}.";
                e.Cancel = true;
            }
        }

        private void SectionsCellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (_populating || e.RowIndex < 0) return;

            var row = _sections.Rows[e.RowIndex];
            var section = _options[(PerformanceSection)row.Tag];
            switch (e.ColumnIndex)
            {
                case IncludeColumn:
                    section.Include = row.Cells[IncludeColumn].Value is true;
                    break;
                case TopNColumn when int.TryParse(Convert.ToString(row.Cells[TopNColumn].Value), out var top):
                    section.TopN = top;
                    break;
                case TimeSeriesColumn when section.TimeSeries is not null:
                    section.TimeSeries = row.Cells[TimeSeriesColumn].Value is true;
                    break;
                default:
                    return;
            }

            RebuildPayload();
        }

        /// <summary>Reads everything for the window in the pickers, then shows the payload it makes.</summary>
        private async Task LoadAsync()
        {
            if (_to.Value <= _from.Value)
            {
                _status.Text = "The window must end after it starts.";
                return;
            }

            _loading?.Cancel();
            _loading = new CancellationTokenSource();
            var token = _loading.Token;

            SetWindow(_from.Value.AppTimeZoneToUtc(), _to.Value.AppTimeZoneToUtc());
            var window = PerformanceAnalysisWindow.Create(_from.Value.AppTimeZoneToUtc(), _to.Value.AppTimeZoneToUtc());

            _reload.Enabled = false;
            _status.Text = "Loading performance data...";
            _preview.Text = string.Empty;

            try
            {
                var data = await PerformanceAnalysisLoader.LoadAsync(_instanceId, _instance, window, token);
                if (token.IsCancellationRequested || IsDisposed) return;

                _data = data;
                MarkEmptySections();
                RebuildPayload();
            }
            catch (OperationCanceledException)
            {
                // A newer load, or the form closing.
            }
            catch (Exception ex)
            {
                _status.Text = $"Could not load performance data: {ex.Message}";
                _status.ToolTipText = ex.ToString();
            }
            finally
            {
                if (!IsDisposed) _reload.Enabled = true;
            }
        }

        /// <summary>
        /// Says which sections have nothing in this window.  They can still be sent - "no blocking" is worth
        /// knowing - but a latch section for an instance that does not collect them is only noise, so those
        /// start unticked.
        /// </summary>
        private void MarkEmptySections()
        {
            var empty = PerformanceAnalysisLoader.Empty(_data).ToHashSet();
            _populating = true;
            try
            {
                foreach (DataGridViewRow row in _sections.Rows)
                {
                    var section = (PerformanceSection)row.Tag;
                    var isEmpty = empty.Contains(section) || _data.Errors.ContainsKey(section);
                    row.DefaultCellStyle.ForeColor = isEmpty ? DashColors.NotApplicable : Color.Empty;

                    if (isEmpty && section is PerformanceSection.Latches or PerformanceSection.Spinlocks)
                    {
                        _options[section].Include = false;
                        row.Cells[IncludeColumn].Value = false;
                    }
                }
            }
            finally
            {
                _populating = false;
            }
        }

        private void RebuildPayload()
        {
            if (_data is null) return;

            _payload = PerformanceAnalysisPayload.Build(_data, _options, utc => utc.ToAppTimeZone(), DescribeTimeZone());
            var preview = _payload.ToPreview();
            _preview.Text = preview.Replace("\n", Environment.NewLine).Replace("\r\r", "\r");

            var empty = PerformanceAnalysisLoader.Empty(_data).ToHashSet();
            foreach (DataGridViewRow row in _sections.Rows)
            {
                var section = (PerformanceSection)row.Tag;
                var sent = _payload.Sections.FirstOrDefault(s => s.Section == section);
                row.Cells[SizeColumn].Value = _data.Errors.ContainsKey(section) ? "failed"
                    : sent is not null ? $"{(sent.Content.Length + sent.Title.Length) / 1024d:0.0} KB"
                    : empty.Contains(section) ? "no data"
                    : string.Empty;
                row.Cells[SizeColumn].ToolTipText = _data.Errors.TryGetValue(section, out var error) ? error : string.Empty;
            }

            _status.Text = $"Request size: {PerformanceAnalysisPayload.DescribeSize(preview.Length)}" +
                           (_payload.IsLarge ? ".  This is large - consider leaving out a section or a time series." : string.Empty);
            _status.ForeColor = _payload.IsLarge ? DashColors.Warning : Color.Empty;
            _status.ToolTipText = string.Empty;
        }

        private static string DescribeTimeZone()
        {
            var zone = DateHelper.AppTimeZone;
            var offset = zone.GetUtcOffset(DateTime.UtcNow);
            return $"{zone.Id}, UTC{(offset < TimeSpan.Zero ? "-" : "+")}{offset:hh\\:mm}";
        }
    }
}
