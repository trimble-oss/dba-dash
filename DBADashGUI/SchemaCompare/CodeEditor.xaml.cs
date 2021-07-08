using ICSharpCode.AvalonEdit.Highlighting;

using System.Reflection;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.IO;
namespace DBADashGUI.SchemaCompare
{
    /// <summary>
    /// Interaction logic for CodeEditor.xaml
    /// </summary>
    public partial class CodeEditor : UserControl
    {
        public CodeEditor()
        {
            InitializeComponent();

            Mode = CodeEditorModes.SQL;

        }

        private CodeEditorModes mode = CodeEditorModes.SQL;

        private void setHighlighting(byte[] highlightingResource)
        {
            using (var stream = new MemoryStream(highlightingResource))
            {
                using (var reader = new System.Xml.XmlTextReader(stream))
                {
                    txtCode.SyntaxHighlighting =
                        ICSharpCode.AvalonEdit.Highlighting.Xshd.HighlightingLoader.Load(reader,
                        ICSharpCode.AvalonEdit.Highlighting.HighlightingManager.Instance);
                }
            }
        }

        public CodeEditorModes Mode
        {
            get
            {
                return mode;
            }
            set
            {
                if (value == CodeEditorModes.SQL)
                {
                    setHighlighting(Properties.Resources.SQL_Mode);
                }
                else if(value== CodeEditorModes.PowerShell)
                {
                    setHighlighting(Properties.Resources.PowerShell_Mode);
                }
                else
                {
                    txtCode.SyntaxHighlighting = null;
                }
                mode = value;
            }
        }

        public enum CodeEditorModes
        {
            SQL,
            PowerShell,
            None
        }

        public string Text
        {
            get
            {
                return txtCode.Text;
            }
            set
            {
                txtCode.Text = value;
            }
        }
    }
}
