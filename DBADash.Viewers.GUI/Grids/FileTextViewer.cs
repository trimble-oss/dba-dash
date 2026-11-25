using DBADashGUI.CustomReports;
using DBADashGUI.SchemaCompare;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBADashGUI.Grids
{
    /// <summary>
    /// Shows the text of the file a grid was read from in the code viewer - the JSON or XML as it is, for when the grid
    /// isn't the clearest way to see it, or to see exactly what was written.
    /// </summary>
    internal static class FileTextViewer
    {
        /// <summary>A file this size or larger is checked with the user first: the code viewer gets slow with that much text.</summary>
        private const long ConfirmLength = 50L * 1024 * 1024;

        /// <summary>
        /// Reads <paramref name="path"/> off the UI thread - a log can run to hundreds of MB - and opens it in the code
        /// viewer, highlighted as XML or JSON by its extension.
        /// </summary>
        public static async void Show(IWin32Window owner, string path)
        {
            try
            {
                var length = new FileInfo(path).Length;
                if (length >= ConfirmLength &&
                    MessageBox.Show(owner,
                        $"{Path.GetFileName(path)} is {length / (1024.0 * 1024):N0} MB, and could be slow to show as text.  Show it anyway?",
                        "View File Text", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                {
                    return;
                }

                var text = await Task.Run(() => GridSerializer.ReadText(path));
                CommonShared.ShowCodeViewer(text, Path.GetFileName(path),
                    GridSerializer.IsXmlText(path) ? CodeEditor.CodeEditorModes.XML : CodeEditor.CodeEditorModes.Json);
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex, "Error reading file", text: $"{path} could not be read.");
            }
        }
    }
}
