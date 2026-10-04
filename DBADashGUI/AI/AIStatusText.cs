#nullable enable
using System;
using System.Collections.Generic;
using System.Text;

namespace DBADashGUI.AI
{
    /// <summary>
    /// The tooltip behind an AI tab's status line, which is where a message clipped on the line itself
    /// is read in full.
    /// </summary>
    internal static class AIStatusText
    {
        private const int LineLength = 100;

        /// <summary>
        /// One paragraph per note, wrapped.  A tooltip only wraps at the widest a window can be - several
        /// thousand pixels - so a long message left as one line runs off the edge of the screen, which
        /// would cut off the very text the tooltip is there to show.
        /// </summary>
        internal static string ToolTip(IEnumerable<string> parts)
        {
            var text = new StringBuilder();
            foreach (var part in parts)
            {
                if (text.Length > 0) text.AppendLine();
                Wrap(part, text);
            }
            return text.ToString();
        }

        private static void Wrap(string paragraph, StringBuilder text)
        {
            var lineStart = text.Length;
            foreach (var word in paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                if (text.Length > lineStart)
                {
                    if (text.Length - lineStart + 1 + word.Length > LineLength)
                    {
                        text.AppendLine();
                        lineStart = text.Length;
                    }
                    else
                    {
                        text.Append(' ');
                    }
                }
                text.Append(word);
            }
        }
    }
}
