using DBADashGUI.Theme;
using Markdig;
using System.Drawing;

namespace DBADashSharedGUI
{
    /// <summary>
    /// Turns the Markdown a model returns into HTML styled for the current theme.
    ///
    /// Shared because more than one place shows a generated answer, and an answer that reads well in
    /// one and badly in the other looks like a bug in whichever the reader saw second.
    /// </summary>
    public static class MarkdownRenderer
    {
        /// <summary>
        /// Raw HTML is disabled.  The Markdown here is a model answer about data read from a
        /// monitored instance - statement text and object names reach the prompt - so anything the
        /// model emits is treated as text to display rather than markup to run in WebView2.
        /// </summary>
        private static readonly MarkdownPipeline Pipeline =
            new MarkdownPipelineBuilder().UseAdvancedExtensions().DisableHtml().Build();

        /// <summary>A complete HTML document, ready for <see cref="WebView2Wrapper.NavigateToLargeString"/>.</summary>
        public static string ToThemedHtml(string markdown)
        {
            var theme = ThemeExtensions.CurrentTheme;
            var foreground = ColorTranslator.ToHtml(theme.ForegroundColor);
            var background = ColorTranslator.ToHtml(theme.BackgroundColor);
            var codeBackground = ColorTranslator.ToHtml(theme.InputBackColor);
            var accent = ColorTranslator.ToHtml(theme.LinkColor);
            var body = Markdown.ToHtml(markdown ?? string.Empty, Pipeline);

            return $$"""
                     <html>
                     <head>
                       <meta charset='utf-8'/>
                       <meta http-equiv='Content-Security-Policy' content="default-src 'none'; style-src 'unsafe-inline'; font-src 'self'; img-src 'none';"/>
                       <style>
                         body { font-family: Segoe UI, Arial, sans-serif; margin: 16px; color: {{foreground}}; background: {{background}}; }
                         h1,h2,h3 { margin: 8px 0; }
                         ul { padding-left: 20px; }
                         code { background: {{codeBackground}}; padding: 2px 4px; border-radius: 3px; }
                         pre { background: {{codeBackground}}; padding: 10px; border-radius: 4px; overflow-x: auto; }
                         /* A conversation renders as one document, with the reader's own questions as
                            blockquotes and a rule between turns, so the thread can be followed without
                            having to work out who said what. */
                         blockquote { margin: 16px 0; padding: 8px 12px; border-left: 3px solid {{accent}};
                                      background: {{codeBackground}}; border-radius: 0 4px 4px 0; }
                         blockquote p { margin: 4px 0; }
                         hr { border: none; border-top: 1px solid {{codeBackground}}; margin: 20px 0; }
                         table { border-collapse: collapse; margin: 8px 0; }
                         th, td { border: 1px solid {{codeBackground}}; padding: 4px 8px; text-align: left; }
                       </style>
                     </head>
                     <body>{{body}}</body>
                     </html>
                     """;
        }
    }
}
