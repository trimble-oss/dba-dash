using DBADashGUI.Controls;
using DBADashGUI.Theme;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI.Grids
{
    /// <summary>
    /// The info card at the top of the statistics views: a bold headline of totals, a line of details under it, and a
    /// note on how far they can be trusted - a warning card where they can't be, fully.  Themes itself rather than
    /// leaving it to the recursive theming, which would paint the card's labels the window's colours rather than the
    /// card's.
    /// </summary>
    internal sealed class StatisticsCard : Panel, IThemedControl
    {
        private readonly InsightCard _card;
        private readonly Label[] _labels;
        private readonly InsightCard.CardIcon _icon;

        public StatisticsCard(string headline, string details, string note, bool warning)
        {
            Dock = DockStyle.Top;
            Padding = new Padding(8, 8, 8, 4);
            _icon = warning ? InsightCard.CardIcon.Warning : InsightCard.CardIcon.Information;

            _card = new InsightCard
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(InsightCard.AccentWidth + InsightCard.IconGutter, 10, 12, 10),
                Icon = _icon,
                FillColor = InsightCard.FillFor(_icon),
                AccentColor = InsightCard.AccentFor(_icon)
            };

            var font = new Font("Segoe UI", 9.75F);
            _labels =
            [
                CardLabel(headline, new Font(font, FontStyle.Bold), new Padding(0, 0, 0, 2)),
                CardLabel(details, font, new Padding(0, 0, 0, 4)),
                CardLabel(note, new Font("Segoe UI", 8.25F, FontStyle.Italic), Padding.Empty)
            ];
            // Docked to the top in reverse: the last added is docked first, so ends up at the top.
            foreach (var label in Enumerable.Reverse(_labels)) _card.Controls.Add(label);

            Controls.Add(_card);
            Relayout();
        }

        private static Label CardLabel(string text, Font font, Padding padding) => new()
        {
            Text = text,
            Font = font,
            AutoSize = true,
            Dock = DockStyle.Top,
            Margin = Padding.Empty,
            Padding = padding,
            UseMnemonic = false
        };

        /// <summary>Wraps the labels to the card's width, and makes the card as tall as they need.</summary>
        private void Relayout()
        {
            var width = ClientSize.Width - Padding.Horizontal - _card.Padding.Horizontal;
            if (width <= 0) return;

            var height = 0;
            foreach (var label in _labels)
            {
                label.MaximumSize = new Size(width, 0);
                height += InsightCard.ContentHeight(label, width);
            }

            Height = Math.Max(height, InsightCard.IconSize) + _card.Padding.Vertical + Padding.Vertical;
        }

        protected override void OnClientSizeChanged(EventArgs e)
        {
            base.OnClientSizeChanged(e);
            Relayout();
        }

        public void ApplyTheme(BaseTheme theme)
        {
            // The card blends its rounded corners into this panel; its text keeps the card's own colours.
            BackColor = theme.BackgroundColor;
            foreach (var label in _labels)
            {
                label.BackColor = InsightCard.FillFor(_icon);
                label.ForeColor = InsightCard.TextFor(_icon);
            }
            _card.Invalidate();
        }
    }
}
