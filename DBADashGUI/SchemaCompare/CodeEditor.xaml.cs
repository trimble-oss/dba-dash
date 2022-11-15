using System.IO;
using System.Windows.Controls;
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

        private void SetHighlighting(byte[] highlightingResource)
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

        public bool ShowLineNumbers
        {
            get
            {
                return txtCode.ShowLineNumbers;
            }
            set
            {
                txtCode.ShowLineNumbers = value;
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
                    SetHighlighting(Properties.Resources.SQL_Mode);
                }
                else if (value == CodeEditorModes.PowerShell)
                {
                    SetHighlighting(Properties.Resources.PowerShell_Mode);
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
