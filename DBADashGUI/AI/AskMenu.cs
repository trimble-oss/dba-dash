#nullable enable
using Serilog;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI.AI
{
    /// <summary>
    /// The Ask button: a sparkle that drops down the AI tools the user can hand a deadlock, a plan or an instance's
    /// performance to - see <see cref="AgentTarget.Available"/>.  Hidden when there is none.  The menu is built each time
    /// it opens, and <see cref="RefreshAll"/> shows or hides the buttons after Options > Connect AI Tools, so a tool
    /// registered in the meantime appears without a restart.
    /// </summary>
    internal static class AskMenu
    {
        private static readonly Lazy<Bitmap> Sparkle = new(() => DrawSparkle(32));

        /// <summary>Raised on the UI thread when the tools available may have changed.</summary>
        private static event Action? AvailabilityChanged;

        /// <summary>Shows or hides every Ask button, for the tools available now.</summary>
        public static void RefreshAll() => AvailabilityChanged?.Invoke();

        /// <param name="ask">What to hand over, read when a tool is picked; null when there is nothing to ask about, after telling the user why.</param>
        /// <param name="owner">For messages.</param>
        /// <returns>The button, hidden while there is no tool; null when this copy of DBA Dash has no MCP server to hand to.</returns>
        public static ToolStripItem? Create(Func<AgentAsk?> ask, Func<IWin32Window?> owner)
        {
            if (!System.IO.File.Exists(AgentHandoff.McpExe)) return null;

            var button = new ToolStripDropDownButton("Ask", Sparkle.Value)
            {
                DisplayStyle = ToolStripItemDisplayStyle.Image,
                ToolTipText = "Ask an AI tool - Claude Code, Cursor and others - to analyse this, using DBA Dash's MCP server",
                ImageScaling = ToolStripItemImageScaling.SizeToFit,
                Available = AgentTarget.Available().Count > 0
            };

            void Refresh() => button.Available = AgentTarget.Available().Count > 0;
            AvailabilityChanged += Refresh;
            button.Disposed += (_, _) => AvailabilityChanged -= Refresh;

            // A placeholder, so the arrow shows and the menu opens: it is rebuilt from what is available every time.
            button.DropDownItems.Add(new ToolStripMenuItem("..."));
            button.DropDownOpening += (_, _) =>
            {
                button.DropDown.SuspendLayout();
                button.DropDownItems.Clear();
                var targets = AgentTarget.Available();
                foreach (var target in targets)
                {
                    if (target is CopyPromptTarget && targets.Count > 1)
                    {
                        button.DropDownItems.Add(new ToolStripSeparator());
                    }

                    button.DropDownItems.Add(new ToolStripMenuItem(target.MenuText, ProgramIcon(target.ProgramPath),
                        (_, _) => Launch(target, ask, owner()))
                    {
                        ToolTipText = target.ToolTip
                    });
                }

                if (targets.Count == 0)
                {
                    button.DropDownItems.Add(new ToolStripMenuItem("No AI tools - see Options > Connect AI Tools") { Enabled = false });
                }

                button.DropDown.ResumeLayout();
            };

            return button;
        }

        private static void Launch(AgentTarget target, Func<AgentAsk?> ask, IWin32Window? owner)
        {
            try
            {
                if (ask() is not { } request) return;
                var message = target.Launch(AgentHandoff.Prepare(request));
                if (message is not null)
                {
                    MessageBox.Show(owner, message, target.Name, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Handing over to {Agent} failed", target.Name);
                MessageBox.Show(owner, $"Couldn't hand this to {target.Name}: {ex.Message}", target.Name, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>The icon of an installed program, read once per path.  Null when there is none or it can't be read: the item shows text only.</summary>
        private static Image? ProgramIcon(string? path)
        {
            if (path is null) return null;
            return ProgramIcons.GetOrAdd(path, p =>
            {
                try
                {
                    using var icon = Icon.ExtractAssociatedIcon(p);
                    return icon?.ToBitmap();
                }
                catch (Exception ex) when (ex is ArgumentException or System.IO.IOException or UnauthorizedAccessException)
                {
                    Log.Debug(ex, "No icon for {Program}", p);
                    return null;
                }
            });
        }

        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, Image?> ProgramIcons = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Two four-pointed stars, the usual mark for AI, in a violet that reads on light and dark toolbars.  Drawn rather
        /// than shipped as a resource, at twice the toolbar size so it stays sharp when scaled for high DPI.
        /// </summary>
        internal static Bitmap DrawSparkle(int size)
        {
            var bitmap = new Bitmap(size, size);
            using var g = Graphics.FromImage(bitmap);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var brush = new SolidBrush(Color.FromArgb(124, 77, 255));

            var s = size / 16f;
            Star(g, brush, 6.5f * s, 9.5f * s, 6.3f * s);
            Star(g, brush, 12.6f * s, 3.4f * s, 3.2f * s);
            return bitmap;
        }

        private static void Star(Graphics g, Brush brush, float cx, float cy, float r)
        {
            var w = r * 0.27f;
            g.FillPolygon(brush, new[]
            {
                new PointF(cx, cy - r), new PointF(cx + w, cy - w), new PointF(cx + r, cy), new PointF(cx + w, cy + w),
                new PointF(cx, cy + r), new PointF(cx - w, cy + w), new PointF(cx - r, cy), new PointF(cx - w, cy - w)
            });
        }
    }
}
