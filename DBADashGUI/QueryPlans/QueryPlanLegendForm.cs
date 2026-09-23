using DBADash.QueryPlan.Model;
using DBADash.QueryPlan.Skia;
using DBADashGUI.Theme;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI.QueryPlans
{
    /// <summary>
    /// The legend and help for the plan viewer: what the icons, markers, arrows and bars on a plan
    /// mean, and how to move around it.
    ///
    /// A window of its own rather than a panel, and not modal, so it can sit beside the plan while a
    /// marker is being looked up - a legend is read against the thing it explains.  One window is
    /// shared by every plan open: it is about the viewer rather than about a plan.
    ///
    /// The words come from <see cref="PlanLegend"/> and the pictures are drawn by the plan's own
    /// renderer in the plan's own palette, so the legend follows the theme and cannot show anything a
    /// plan does not.
    /// </summary>
    internal sealed class QueryPlanLegendForm : Form
    {
        private static QueryPlanLegendForm _current;

        private readonly FlowLayoutPanel _stack = new()
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            Padding = new Padding(12, 8, 12, 12)
        };

        private readonly PlanFonts _fonts = new();
        private readonly PlanRenderer _renderer;
        private readonly ToolTip _toolTip = new() { AutoPopDelay = 30000, InitialDelay = 300 };

        /// <summary>
        /// Everything that is as wide as the window, with how much narrower than the window it is:
        /// wrapping text needs a width to wrap to, and a flow layout gives none.
        /// </summary>
        private readonly List<(Control Control, int Indent)> _fitted = [];

        private readonly List<Bitmap> _pictures = [];

        // Made once and shared by every label that uses them, rather than one font a label.
        private Font _bold;
        private Font _title;
        private Font _heading;

        private QueryPlanLegendForm()
        {
            Text = "Query Plan Legend";
            Width = 780;
            Height = 760;
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            KeyPreview = true;

            try
            {
                Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }

            var theme = ThemeExtensions.CurrentTheme;
            _renderer = new PlanRenderer(_fonts, QueryPlanPaletteMapper.FromTheme(theme));

            _bold = new Font(Font, FontStyle.Bold);
            _title = new Font(Font.FontFamily, 15f, FontStyle.Bold);
            _heading = new Font(Font.FontFamily, 12f, FontStyle.Bold);

            Build();
            Controls.Add(_stack);

            _stack.SizeChanged += (_, _) => Fit();
            KeyDown += (_, e) =>
            {
                if (e.KeyCode != Keys.Escape) return;
                e.Handled = true;
                Close();
            };

            this.ApplyTheme(theme);
        }

        /// <summary>
        /// Show the legend, opening the window if it is not already.  Beside the plan window that asked
        /// for it, and brought to the front if it was already open.
        /// </summary>
        public static void ShowLegend(IWin32Window owner)
        {
            if (_current is { IsDisposed: false })
            {
                if (_current.WindowState == FormWindowState.Minimized) _current.WindowState = FormWindowState.Normal;
                _current.Activate();
                return;
            }

            _current = new QueryPlanLegendForm();

            // Owned so it stays over the plan window rather than behind it, and closes with it.
            _current.Show(owner);
        }

        // ---------------------------------------------------------------- building

        private void Build()
        {
            AddHeading("Query plan legend", _title, spaceAbove: 0);

            foreach (var section in PlanLegend.Sections)
            {
                AddHeading(section.Title, _heading);

                if (section.Introduction is not null) AddText(section.Introduction, 0);

                if (section.Compact)
                {
                    AddOperators(section);
                    continue;
                }

                foreach (var entry in section.Entries) AddEntry(entry);
            }

            AddHeading("Keys and mouse", _heading);

            foreach (var control in PlanLegend.Controls) AddControl(control);

            // A scrolling flow panel does not scroll its bottom padding into view, so the last line
            // would sit against the edge of the window; an empty control at the end holds it open.
            _stack.Controls.Add(new Panel { Height = Scaled(32), Width = 1, Margin = Padding.Empty });
        }

        private void AddHeading(string text, Font font, int spaceAbove = 14)
        {
            var label = new Label
            {
                Text = text,
                AutoSize = true,
                Font = font,
                Margin = new Padding(0, spaceAbove, 0, 4)
            };

            Add(label, 0);
        }

        private void AddText(string text, int indent, bool bold = false)
        {
            var label = new Label
            {
                Text = text,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 4)
            };

            if (bold) label.Font = _bold;
            Add(label, indent);
        }

        /// <summary>A picture beside a title and what it means.</summary>
        private void AddEntry(PlanLegendEntry entry)
        {
            var size = SampleSize(entry);
            var gap = Scaled(10);

            var row = new TableLayoutPanel
            {
                AutoSize = true,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0, 0, 0, Scaled(6))
            };

            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, size.Width + gap));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            row.Controls.Add(Picture(entry, size), 0, 0);

            var text = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Margin = Padding.Empty
            };

            var title = new Label { Text = entry.Title, AutoSize = true, Font = _bold, Margin = Padding.Empty };
            var description = new Label { Text = entry.Description, AutoSize = true, Margin = new Padding(0, 1, 0, 0) };
            text.Controls.Add(title);
            text.Controls.Add(description);

            row.Controls.Add(text, 1, 0);

            // The text is as wide as the window less the picture beside it, and the scroll bar.
            _fitted.Add((description, size.Width + gap));
            Add(row, 0);
        }

        /// <summary>The operators, by colour family, as a wrapping grid of icon and name with the description on a tooltip.</summary>
        private void AddOperators(PlanLegendSection section)
        {
            var width = Scaled(180);

            foreach (var family in section.Entries.GroupBy(PlanLegend.CategoryOf))
            {
                AddText(PlanOperatorClassifier.CategoryName(family.Key), 0, bold: true);

                var flow = new FlowLayoutPanel
                {
                    AutoSize = true,
                    FlowDirection = FlowDirection.LeftToRight,
                    WrapContents = true,
                    Margin = new Padding(0, 0, 0, Scaled(6))
                };

                foreach (var entry in family)
                {
                    var tile = new TableLayoutPanel
                    {
                        Width = width,
                        Height = Scaled(34),
                        ColumnCount = 2,
                        RowCount = 1,
                        Margin = Padding.Empty
                    };

                    var size = SampleSize(entry);
                    tile.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, size.Width + Scaled(10)));
                    tile.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

                    var picture = Picture(entry, size);
                    picture.Anchor = AnchorStyles.Left;

                    var name = new Label
                    {
                        Text = entry.Title,
                        AutoSize = false,
                        Dock = DockStyle.Fill,
                        TextAlign = ContentAlignment.MiddleLeft,
                        AutoEllipsis = true
                    };

                    tile.Controls.Add(picture, 0, 0);
                    tile.Controls.Add(name, 1, 0);

                    // On everything in the tile, so the pointer is over the description wherever it is.
                    foreach (var control in new Control[] { tile, picture, name })
                    {
                        _toolTip.SetToolTip(control, entry.Description);
                    }

                    flow.Controls.Add(tile);
                }

                // A wrapping flow needs the width to wrap to, so it is fitted like the text is.
                Add(flow, 0);
            }
        }

        private void AddControl(PlanLegendControl control)
        {
            var row = new TableLayoutPanel
            {
                AutoSize = true,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0, 0, 0, Scaled(3))
            };

            var keyWidth = Scaled(220);
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, keyWidth));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            row.Controls.Add(new Label { Text = control.Input, AutoSize = true, Font = _bold, Margin = Padding.Empty }, 0, 0);

            var action = new Label { Text = control.Action, AutoSize = true, Margin = Padding.Empty };
            row.Controls.Add(action, 1, 0);

            _fitted.Add((action, keyWidth));
            Add(row, 0);
        }

        private void Add(Control control, int indent)
        {
            _stack.Controls.Add(control);
            _fitted.Add((control, indent));
        }

        // ---------------------------------------------------------------- sizing

        /// <summary>
        /// Puts everything at the window's width.  Wrapped text gets a maximum width and works out its
        /// own height; the rows and grids are as wide as the window, which is what makes them wrap.
        /// </summary>
        private void Fit()
        {
            var available = _stack.ClientSize.Width - _stack.Padding.Horizontal - SystemInformation.VerticalScrollBarWidth;
            if (available <= 0) return;

            _stack.SuspendLayout();

            foreach (var (control, indent) in _fitted)
            {
                var width = Math.Max(Scaled(120), available - indent);

                if (control is Label)
                {
                    control.MaximumSize = new Size(width, 0);
                }
                else
                {
                    control.MaximumSize = new Size(width, 0);
                    control.MinimumSize = new Size(width, 0);
                }
            }

            _stack.ResumeLayout(true);
        }

        private int Scaled(int pixels) => (int)Math.Round(pixels * DeviceDpi / 96.0);

        private Size SampleSize(PlanLegendEntry entry)
        {
            var size = PlanRenderer.LegendSampleSize(entry);
            var scale = DeviceDpi / 96.0;

            return new Size((int)Math.Ceiling(size.Width * scale), (int)Math.Ceiling(size.Height * scale));
        }

        /// <summary>
        /// The entry's picture, drawn by the renderer at this screen's scale so it is as sharp as the
        /// plan is.
        /// </summary>
        private PictureBox Picture(PlanLegendEntry entry, Size size)
        {
            var scale = (float)(DeviceDpi / 96.0);

            using var surface = SKSurface.Create(new SKImageInfo(size.Width, size.Height));
            surface.Canvas.Clear(_renderer.Palette.Background);
            surface.Canvas.Scale(scale);
            _renderer.DrawLegendSample(surface.Canvas, entry, scale);

            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var stream = new MemoryStream(data.ToArray());
            using var decoded = new Bitmap(stream);

            // Copied out of the stream, which the bitmap would otherwise hold open.
            var bitmap = new Bitmap(decoded);
            _pictures.Add(bitmap);

            return new PictureBox
            {
                Image = bitmap,
                Size = size,
                SizeMode = PictureBoxSizeMode.Normal,
                Margin = Padding.Empty,
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);

            if (ReferenceEquals(_current, this)) _current = null;

            foreach (var picture in _pictures) picture.Dispose();

            _renderer.Dispose();
            _fonts.Dispose();
            _toolTip.Dispose();
            _bold.Dispose();
            _title.Dispose();
            _heading.Dispose();
            Dispose();
        }
    }
}
