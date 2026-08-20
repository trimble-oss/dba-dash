using DBADashGUI.Theme;
using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace DBADashGUI.XETrace
{
    /// <summary>
    /// A single row in the Extended Events session list: a running/stopped status icon, the session name as a link
    /// (loads the DDL), a start/stop button and a watch button.  Details (start time, targets, event count, status)
    /// are shown in a tooltip.  Dumb view - it raises <see cref="StartStopClicked"/> / <see cref="WatchClicked"/> /
    /// <see cref="NameClicked"/> and lets <see cref="ExtendedEventsViewer"/> do the messaging.  Owns its theming
    /// (<see cref="IThemedControl"/>) so the inline icon buttons blend into the row instead of rendering as filled
    /// themed buttons.
    /// </summary>
    internal sealed class XESessionRow : UserControl, IThemedControl
    {
        private const string ColName = "Name";
        private const string ColIsRunning = "IsRunning";
        private const string ColStartTime = "StartTime";
        private const string ColEventCount = "EventCount";
        private const string ColTargetTypes = "TargetTypes";

        private const int RowHeight = 28;
        private const int IconWidth = 24;
        private const int NameWidth = 360;
        private const int ButtonWidth = 28;

        // The play/stop status icons are 32px; pre-scale to 16px once so the buttons render them crisply.  Faded copies
        // signal an action that isn't permitted: a real disabled button would swallow the hover so its tooltip (the
        // reason) never shows, so the buttons stay enabled but look inert and their click is gated instead.
        private static readonly Image StartIcon = ScaleTo(Properties.Resources.StatusAnnotations_Play_32xLG_color, 16);
        private static readonly Image StopIcon = ScaleTo(Properties.Resources.StatusAnnotations_Stop_32xLG_color, 16);
        private static readonly Image WatchIcon = Properties.Resources.Watch_16x;
        private static readonly Image ViewIcon = Properties.Resources.DataTable_16x;
        private static readonly Image StartIconFaded = StartIcon.Fade();
        private static readonly Image StopIconFaded = StopIcon.Fade();
        private static readonly Image WatchIconFaded = WatchIcon.Fade();
        private static readonly Image ViewIconFaded = ViewIcon.Fade();

        private readonly TableLayoutPanel _layout;
        private readonly PictureBox _statusIcon = new() { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.CenterImage };
        private readonly LinkLabel _name = new() { Dock = DockStyle.Fill, AutoSize = false, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true, LinkBehavior = LinkBehavior.HoverUnderline };
        private readonly Button _startStop = new() { Dock = DockStyle.Fill, FlatStyle = FlatStyle.Flat };
        private readonly Button _watch = new() { Dock = DockStyle.Fill, FlatStyle = FlatStyle.Flat };
        private readonly Button _viewData = new() { Dock = DockStyle.Fill, FlatStyle = FlatStyle.Flat };
        private readonly ToolTip _toolTip = new() { AutoPopDelay = 20000, InitialDelay = 400, ReshowDelay = 100 };

        private bool _canManage = true;
        private bool _canWatch = true;
        private bool _hasReadableTarget;

        public string SessionName { get; private set; }
        public bool IsRunning { get; private set; }

        public event EventHandler StartStopClicked;
        public event EventHandler WatchClicked;
        public event EventHandler ViewDataClicked;
        public event EventHandler NameClicked;

        public XESessionRow()
        {
            AutoScaleMode = AutoScaleMode.None;
            Height = RowHeight;
            Width = IconWidth + NameWidth + (ButtonWidth * 3);
            Margin = new Padding(0);

            _startStop.FlatAppearance.BorderSize = 0;
            _watch.FlatAppearance.BorderSize = 0;
            _viewData.FlatAppearance.BorderSize = 0;

            _layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 5,
                RowCount = 1,
                Margin = new Padding(0)
            };
            _layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, IconWidth));
            _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, NameWidth));
            _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, ButtonWidth));
            _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, ButtonWidth));
            _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, ButtonWidth));
            _layout.Controls.Add(_statusIcon, 0, 0);
            _layout.Controls.Add(_name, 1, 0);
            _layout.Controls.Add(_startStop, 2, 0);
            _layout.Controls.Add(_watch, 3, 0);
            _layout.Controls.Add(_viewData, 4, 0);
            Controls.Add(_layout);

            // Clicks are gated on the policy so a not-permitted button reads as inert without being truly disabled
            // (a disabled button can't show a tooltip explaining why).
            _startStop.Click += (_, _) => { if (_canManage) StartStopClicked?.Invoke(this, EventArgs.Empty); };
            _watch.Click += (_, _) => { if (_canWatch) WatchClicked?.Invoke(this, EventArgs.Empty); };
            _viewData.Click += (_, _) => { if (_canWatch) ViewDataClicked?.Invoke(this, EventArgs.Empty); };
            _name.LinkClicked += (_, _) => NameClicked?.Invoke(this, EventArgs.Empty);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _toolTip?.Dispose();
            base.Dispose(disposing);
        }

        public void Bind(DataRow row)
        {
            SessionName = Field(row, ColName) as string ?? string.Empty;
            IsRunning = Field(row, ColIsRunning) is { } r && r != DBNull.Value && Convert.ToBoolean(r);
            _hasReadableTarget = HasReadableTarget(Field(row, ColTargetTypes) as string);

            _name.Text = SessionName;
            _statusIcon.Image = IsRunning ? Properties.Resources.StartLog_16x : Properties.Resources.StopLog_16x;

            _startStop.Image = IsRunning ? StopIcon : StartIcon;
            _watch.Image = WatchIcon;
            _watch.Visible = IsRunning; // watching only applies to a running session
            _viewData.Image = ViewIcon;
            // Viewing reads the session's live target, so it needs the session running and a readable (event_file /
            // ring_buffer) target - a session with only a histogram/pair_matching target has no event stream to show.
            _viewData.Visible = IsRunning && _hasReadableTarget;

            var details = BuildDetails(row);
            _toolTip.SetToolTip(_name, details);
            _toolTip.SetToolTip(_statusIcon, details);
            _toolTip.SetToolTip(_startStop, IsRunning ? "Stop" : "Start");
            _toolTip.SetToolTip(_watch, "Watch");
            _toolTip.SetToolTip(_viewData, "View captured data");
        }

        /// <summary>
        /// Applies the service's per-session policy: disables (and explains) the start/stop and watch buttons for a
        /// session the collect agent isn't permitted to manage / watch, so the user isn't offered an action that would
        /// only be rejected server-side.
        /// </summary>
        public void SetPolicy(bool canManage, bool canWatch)
        {
            _canManage = canManage;
            _startStop.Image = canManage
                ? (IsRunning ? StopIcon : StartIcon)
                : (IsRunning ? StopIconFaded : StartIconFaded);
            _startStop.Cursor = canManage ? Cursors.Hand : Cursors.Default;
            _toolTip.SetToolTip(_startStop, canManage
                ? (IsRunning ? "Stop" : "Start")
                : "This session is protected by the service configuration and can't be started or stopped here.");

            // Viewing captured data is the same read as watching, so it's gated by the same per-session watch policy.
            _canWatch = canWatch;
            _watch.Image = canWatch ? WatchIcon : WatchIconFaded;
            _watch.Cursor = canWatch ? Cursors.Hand : Cursors.Default;
            _toolTip.SetToolTip(_watch, canWatch
                ? "Watch"
                : "Watching this session isn't permitted by the service configuration.");

            _viewData.Image = canWatch ? ViewIcon : ViewIconFaded;
            _viewData.Cursor = canWatch ? Cursors.Hand : Cursors.Default;
            _toolTip.SetToolTip(_viewData, canWatch
                ? "View captured data"
                : "Viewing this session's data isn't permitted by the service configuration.");
        }

        /// <summary>The target must carry an event stream to view - only event_file and ring_buffer qualify.</summary>
        private static bool HasReadableTarget(string targetTypes) =>
            !string.IsNullOrEmpty(targetTypes) &&
            (targetTypes.IndexOf("event_file", StringComparison.OrdinalIgnoreCase) >= 0 ||
             targetTypes.IndexOf("ring_buffer", StringComparison.OrdinalIgnoreCase) >= 0);

        public void ApplyTheme(BaseTheme theme)
        {
            // Match the list/panel background (BackgroundColor) rather than PanelBackColor, which is an off-white
            // SystemColors.Control that stands out against the white list.
            var back = theme.BackgroundColor;
            BackColor = back;
            _layout.BackColor = back;
            _statusIcon.BackColor = back;

            _name.BackColor = back;
            _name.LinkColor = theme.LinkColor;
            _name.ActiveLinkColor = theme.LinkColor;

            foreach (var btn in new[] { _startStop, _watch, _viewData })
            {
                btn.BackColor = back;
                btn.FlatAppearance.MouseOverBackColor = theme.SelectedTabBackColor;
                btn.FlatAppearance.MouseDownBackColor = theme.SelectedTabBackColor;
            }
        }

        private static string BuildDetails(DataRow row)
        {
            var running = Field(row, ColIsRunning) is { } r && r != DBNull.Value && Convert.ToBoolean(r);
            var startTime = Field(row, ColStartTime);
            var targets = Field(row, ColTargetTypes) as string;
            var events = Field(row, ColEventCount);

            var lines = new System.Text.StringBuilder();
            lines.Append("Status: ").AppendLine(running ? "Running" : "Stopped");
            if (running && startTime is { } st && st != DBNull.Value)
            {
                // dm_xe_sessions.create_time is server-local, so show it as-is (no UTC conversion).
                lines.Append("Started: ").AppendLine(Convert.ToDateTime(st).ToString("g"));
            }
            if (!string.IsNullOrEmpty(targets)) lines.Append("Targets: ").AppendLine(targets);
            if (events is { } ec && ec != DBNull.Value) lines.Append("Events: ").Append(Convert.ToInt32(ec));
            return lines.ToString().TrimEnd();
        }

        private static object Field(DataRow row, string column) =>
            row.Table.Columns.Contains(column) ? row[column] : null;

        private static Image ScaleTo(Image source, int size)
        {
            var bmp = new Bitmap(size, size);
            using var g = Graphics.FromImage(bmp);
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.DrawImage(source, 0, 0, size, size);
            return bmp;
        }
    }
}
