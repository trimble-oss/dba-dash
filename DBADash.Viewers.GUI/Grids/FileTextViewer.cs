using DBADashGUI.CustomReports;
using DBADashGUI.SchemaCompare;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBADashGUI.Grids
{
    /// <summary>
    /// Shows the text of the file a grid was read from in the code viewer - the JSON or XML as it was written, for when
    /// the grid isn't the clearest way to see it, or to see exactly what was written.  The viewer's Format button
    /// formats it on request.
    /// </summary>
    public static class FileTextViewer
    {
        /// <summary>
        /// Text longer than this - 50 MB, near enough - is checked with the user before it's all read: the code viewer
        /// gets slow with that much.  Counted in the text read, so a .gz is judged by what it decompresses to.
        /// </summary>
        public const int ConfirmLength = 50 * 1024 * 1024;

        /// <summary>
        /// Reads <paramref name="path"/> off the UI thread - a log can run to hundreds of MB - and opens it in the code
        /// viewer as written, highlighted as XML or JSON by its extension.  The first <see cref="ConfirmLength"/>
        /// characters are read before asking whether to read the rest.
        /// </summary>
        public static async void Show(IWin32Window owner, string path)
        {
            try
            {
                using var reader = GridSerializer.OpenText(path);
                var (text, more) = await Task.Run(() => ReadUpTo(reader, ConfirmLength));
                if (more)
                {
                    var decompressed = path.EndsWith(".gz", StringComparison.OrdinalIgnoreCase) ? " once decompressed" : string.Empty;
                    if (MessageBox.Show(owner,
                            $"{Path.GetFileName(path)} is more than {ConfirmLength / (1024 * 1024):N0} MB of text{decompressed}, and could be slow to show.  Show it anyway?",
                            "View File Text", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                    {
                        return;
                    }

                    text += await Task.Run(reader.ReadToEnd);
                }

                CommonShared.ShowCodeViewer(text, Path.GetFileName(path),
                    GridSerializer.IsXmlText(path) ? CodeEditor.CodeEditorModes.XML : CodeEditor.CodeEditorModes.Json,
                    format: false, unformattedLabel: "as written in the file");
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex, "Error reading file", text: $"{path} could not be read.");
            }
        }

        /// <summary>
        /// Reads at most <paramref name="limit"/> characters, a block at a time, so a small file doesn't cost a buffer of
        /// the limit's size.  <c>More</c> is true when the text goes on past it.
        /// </summary>
        public static (string Text, bool More) ReadUpTo(TextReader reader, int limit)
        {
            var text = new StringBuilder();
            var buffer = new char[Math.Min(limit, 81920)];
            while (text.Length < limit)
            {
                var read = reader.Read(buffer, 0, Math.Min(buffer.Length, limit - text.Length));
                if (read == 0) return (text.ToString(), false);
                text.Append(buffer, 0, read);
            }

            return (text.ToString(), reader.Peek() >= 0);
        }
    }
}
