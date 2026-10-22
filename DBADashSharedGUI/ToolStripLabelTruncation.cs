using System;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace DBADashGUI
{
    /// <summary>
    /// Enables automatic ellipsis truncation for a <see cref="ToolStripLabel"/> so a long caption is
    /// clipped to the space that is actually available on its owning <see cref="ToolStrip"/> instead of
    /// overflowing (pushing other items off the bar or into the overflow menu).
    ///
    /// Usage:
    ///   myLabel.EnableAutoTruncate();              // enable once (typically in Load)
    ///   myLabel.SetTruncatedText("some caption");  // set/replace the caption thereafter
    ///
    /// The full (untruncated) caption is exposed via the label's ToolTipText.  Pass a
    /// <c>toolTipFormatter</c> to customise the tooltip (e.g. append a hint).
    /// </summary>
    public static class ToolStripLabelTruncation
    {
        private sealed class State
        {
            public string FullText = string.Empty;
            public ToolStrip Owner;
            public LayoutEventHandler OwnerLayout;
            public Func<string, string> ToolTipFormatter;
            public int LastAvailable = -1;
            public bool Applying;
            public bool Subscribed;
        }

        // Per-label state; entries are collected automatically once the label is GC'd.
        private static readonly ConditionalWeakTable<ToolStripLabel, State> States = new();

        // Extra slack (px) so the ellipsised text never sits hard against the item edge.
        private const int TextPadding = 6;

        // Slack (px) kept free on the toolbar so rounding between measured and laid-out item widths can
        // never tip the total past the bar width - which would make the ToolStrip drop a (right-aligned)
        // sibling such as a Close button off the strip.
        private const int ReserveMargin = 12;

        public static void EnableAutoTruncate(this ToolStripLabel label, Func<string, string> toolTipFormatter = null)
        {
            if (label == null) throw new ArgumentNullException(nameof(label));
            // Preserve any full text already supplied via SetTruncatedText (which can run before this
            // when the owner sets the caption prior to the control's Load); only seed from the label's
            // current text on first use.
            if (!States.TryGetValue(label, out var state))
            {
                state = new State { FullText = label.Text ?? string.Empty };
                States.Add(label, state);
            }
            state.ToolTipFormatter = toolTipFormatter;
            label.AutoSize = true; // we truncate the caption itself, so the label always sizes to content

            if (!state.Subscribed)
            {
                state.Subscribed = true;
                // Re-hook if the label is later moved to a different ToolStrip.
                label.OwnerChanged += (_, _) => HookOwner(label, state);
                // Unhook when the label is disposed: the Layout handler is held by the owning ToolStrip, so
                // if the strip outlives the label the subscription would otherwise keep the label alive.
                label.Disposed += (_, _) => Unhook(label, state);
            }

            HookOwner(label, state);
            Apply(label, state);
        }

        private static void Unhook(ToolStripLabel label, State state)
        {
            if (state.Owner != null && state.OwnerLayout != null)
            {
                state.Owner.Layout -= state.OwnerLayout;
            }
            state.Owner = null;
            States.Remove(label);
        }

        /// <summary>
        /// Sets the label caption, truncating it (with an ellipsis) to the current available width.
        /// </summary>
        public static void SetTruncatedText(this ToolStripLabel label, string text)
        {
            if (label == null) throw new ArgumentNullException(nameof(label));
            var state = States.GetOrCreateValue(label);
            state.FullText = text ?? string.Empty;
            state.LastAvailable = -1; // force a recompute even if the width hasn't changed
            if (state.Owner == null) HookOwner(label, state);
            Apply(label, state);
        }

        private static void HookOwner(ToolStripLabel label, State state)
        {
            var owner = label.Owner;
            if (ReferenceEquals(owner, state.Owner)) return;

            if (state.Owner != null && state.OwnerLayout != null)
            {
                state.Owner.Layout -= state.OwnerLayout;
            }

            state.Owner = owner;
            if (owner == null) return;

            // Recompute whenever the toolbar lays out - this covers resizing as well as sibling items
            // changing width (e.g. a dropdown caption changing). A re-entrancy guard stops the layout we
            // trigger by resizing the label from looping.
            state.OwnerLayout ??= (_, _) => Apply(label, state);
            owner.Layout += state.OwnerLayout;
        }

        private static void Apply(ToolStripLabel label, State state)
        {
            if (state.Applying) return; // ignore layout events raised by our own resize
            if (label.Owner == null) return;

            state.Applying = true;
            try
            {
                label.ToolTipText = state.ToolTipFormatter != null
                    ? state.ToolTipFormatter(state.FullText)
                    : state.FullText;

                if (string.IsNullOrEmpty(state.FullText))
                {
                    label.Text = string.Empty;
                    state.LastAvailable = -1;
                    return;
                }

                var available = GetAvailableWidth(label);
                if (available == state.LastAvailable) return; // nothing changed - skip the measuring work
                state.LastAvailable = available;

                // The label auto-sizes to its text, so truncating the caption to the available width is all
                // that is needed: a caption that fits is returned unchanged (and the label hugs the right
                // edge); a longer one is clipped with an ellipsis whose width stays within the reserved
                // space, so the label never grows past it.
                label.Text = Truncate(state.FullText, available - TextPadding, label.Font);
            }
            finally
            {
                state.Applying = false;
            }
        }

        /// <summary>
        /// Available width (px) for the label = toolbar content width minus every other visible item.
        /// Uses each item's actual laid-out <see cref="ToolStripItem.Width"/> (which includes its image,
        /// dropdown arrow and padding); the label itself is excluded and every other item is counted
        /// whether it is on the main strip or in the overflow menu, so its footprint is always reserved.
        /// </summary>
        private static int GetAvailableWidth(ToolStripLabel label)
        {
            var ts = label.Owner;
            var used = 0;
            foreach (ToolStripItem item in ts.Items)
            {
                if (ReferenceEquals(item, label) || !item.Visible) continue;
                used += item.Width + item.Margin.Horizontal;
            }
            var available = ts.DisplayRectangle.Width - used - label.Margin.Horizontal - ReserveMargin;
            return Math.Max(10, available);
        }

        private static int MeasureWidth(string text, Font font) =>
            TextRenderer.MeasureText(text, font).Width;

        private static string Truncate(string text, int maxWidth, Font font)
        {
            if (maxWidth <= 0) return string.Empty;
            if (MeasureWidth(text, font) <= maxWidth) return text;

            const string ellipsis = "…";
            if (MeasureWidth(ellipsis, font) > maxWidth) return ellipsis;

            // Binary search for the longest prefix that fits (keeps this cheap during live resizing).
            int lo = 0, hi = text.Length;
            while (lo < hi)
            {
                var mid = (lo + hi + 1) / 2;
                var candidate = text.Substring(0, mid).TrimEnd() + ellipsis;
                if (MeasureWidth(candidate, font) <= maxWidth)
                {
                    lo = mid;
                }
                else
                {
                    hi = mid - 1;
                }
            }
            return lo <= 0 ? ellipsis : text.Substring(0, lo).TrimEnd() + ellipsis;
        }
    }
}
