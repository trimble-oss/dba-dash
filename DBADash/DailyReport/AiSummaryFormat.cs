using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;

namespace DBADash.DailyReport
{
    /// <summary>One block of an AI summary: a paragraph, or a bullet point</summary>
    public record AiSummaryBlock(bool IsBullet, string Text);

    /// <summary>
    /// Turns the AI summary into paragraphs and bullet points for the email.  The service asks for an overview sentence and "- **Topic:** ..."
    /// points, but models vary, so this also accepts *, • and numbered bullets, headings and wrapped lines.  Only bold survives as formatting;
    /// any other markdown is stripped rather than shown.  The text is HTML encoded before bold is applied, so the summary can't add markup.
    /// </summary>
    public static class AiSummaryFormat
    {
        private static readonly Regex Bullet = new(@"^\s*(?:[-*•]|\d+[.)])\s+(?<text>.*)$", RegexOptions.Compiled);
        private static readonly Regex Heading = new(@"^\s*#{1,6}\s*(?<text>.*?)\s*#*\s*$", RegexOptions.Compiled);
        private static readonly Regex Bold = new(@"\*\*(?<text>[^*]+?)\*\*|__(?<text>[^_]+?)__", RegexOptions.Compiled);
        private static readonly Regex Italic = new(@"(?<![*\w])\*(?<text>[^*\s][^*]*?)\*(?![*\w])", RegexOptions.Compiled);

        public static List<AiSummaryBlock> Parse(string summary)
        {
            var blocks = new List<AiSummaryBlock>();
            if (string.IsNullOrWhiteSpace(summary)) return blocks;

            var continues = false; // the previous line had text, so an indented or plain line carries on its block
            foreach (var raw in summary.Replace("\r\n", "\n").Split('\n'))
            {
                if (string.IsNullOrWhiteSpace(raw))
                {
                    continues = false;
                    continue;
                }

                var bullet = Bullet.Match(raw);
                if (bullet.Success && bullet.Groups["text"].Value.Trim().Length > 0)
                {
                    blocks.Add(new AiSummaryBlock(true, bullet.Groups["text"].Value.Trim()));
                    continues = true;
                    continue;
                }

                var line = raw.Trim();
                var heading = Heading.Match(line);
                if (heading.Success)
                {
                    // A heading becomes a bold paragraph of its own
                    if (heading.Groups["text"].Value.Length > 0) blocks.Add(new AiSummaryBlock(false, $"**{heading.Groups["text"].Value}**"));
                    continues = false;
                    continue;
                }

                if (continues && blocks.Count > 0)
                {
                    var last = blocks[^1];
                    blocks[^1] = last with { Text = $"{last.Text} {line}" };
                }
                else
                {
                    blocks.Add(new AiSummaryBlock(false, line));
                }
                continues = true;
            }
            return blocks;
        }

        /// <summary>The block's text as HTML: encoded, with **bold** as &lt;strong&gt; and any other markdown removed</summary>
        public static string ToHtml(string text)
        {
            var html = WebUtility.HtmlEncode(text);
            html = Bold.Replace(html, m => $"<strong>{m.Groups["text"].Value}</strong>");
            return StripMarks(html);
        }

        /// <summary>The block's text with the markdown removed, for the plain text part</summary>
        public static string ToText(string text) => StripMarks(Bold.Replace(text, m => m.Groups["text"].Value));

        private static string StripMarks(string s)
        {
            s = Italic.Replace(s, m => m.Groups["text"].Value);
            return s.Replace("**", string.Empty).Replace("`", string.Empty);
        }
    }
}
