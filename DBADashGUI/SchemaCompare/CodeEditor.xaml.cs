using System.IO;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Media;
using DBADashGUI.Theme;

namespace DBADashGUI.SchemaCompare
{
    /// <summary>
    /// Interaction logic for CodeEditor.xaml
    /// </summary>
    public partial class CodeEditor : UserControl, IThemedControl
    {
        public CodeEditor()
        {
            InitializeComponent();

            Mode = CodeEditorModes.SQL;
        }

        private CodeEditorModes mode = CodeEditorModes.SQL;
        private string currentHighlighting;

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

        private byte[] GetResource(string resourceName)
        {
            // assuming the resource is located at "NamespaceName.ResourceName.extension"
            var assembly = Assembly.GetExecutingAssembly();

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    stream.CopyTo(ms);
                    byte[] resourceBytes = ms.ToArray();
                    return resourceBytes;
                }
            }
        }

        private void SetHighlighting()
        {
            var theme = DBADashUser.SelectedTheme;
            string resourceName = null;
            if (mode == CodeEditorModes.SQL && theme is DarkTheme)
            {
                resourceName = "DBADashGUI.SyntaxHighlighting.SQL-Dark.xshd";
            }
            else if (mode == CodeEditorModes.SQL)
            {
                resourceName = "DBADashGUI.SyntaxHighlighting.SQL-Light.xshd";
            }
            else if (mode == CodeEditorModes.PowerShell)
            {
                resourceName = "DBADashGUI.SyntaxHighlighting.PowerShell.xshd";
            }
            else if (mode == CodeEditorModes.None)
            {
                resourceName = null;
            }

            if (resourceName == null)
            {
                txtCode.SyntaxHighlighting = null;
            }
            else if (currentHighlighting != resourceName)
            {
                SetHighlighting(GetResource(resourceName));
            }
            currentHighlighting = resourceName;
        }

        public CodeEditorModes Mode
        {
            get => mode;
            set
            {
                mode = value;
                SetHighlighting();
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
                var theme = DBADashUser.SelectedTheme;
                SetHighlighting();
                this.txtCode.Background = new SolidColorBrush(theme.CodeEditorBackColor.ToMediaColor());
                this.txtCode.Foreground = new SolidColorBrush(theme.CodeEditorForeColor.ToMediaColor());
                txtCode.Text = value;
            }
        }

        public void ApplyTheme(BaseTheme theme)
        {
            SetHighlighting();
        }
    }
}