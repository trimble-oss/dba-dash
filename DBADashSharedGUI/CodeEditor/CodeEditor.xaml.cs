using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Media;
using System.Xml.Linq;
using DBADashGUI.Theme;
using DBADashSharedGUI;
using UserControl = System.Windows.Controls.UserControl;

namespace DBADashGUI.SchemaCompare
{
    /// <summary>
    /// Interaction logic for CodeEditor.xaml
    /// </summary>
    public partial class CodeEditor : UserControl, IThemedControl
    {
        public EventHandler TextChanged;

        public CodeEditor()
        {
            InitializeComponent();
            txtCode.TextChanged += TxtCode_TextChanged;
            Mode = CodeEditorModes.SQL;
        }

        public bool IsReadOnly
        {
            get => txtCode.IsReadOnly;
            set => txtCode.IsReadOnly = value;
        }

        private void TxtCode_TextChanged(object sender, EventArgs e)
        {
            TextChanged?.Invoke(this, e);
        }

        private CodeEditorModes mode = CodeEditorModes.SQL;
        private string currentHighlighting;

        private void SetHighlighting(byte[] highlightingResource)
        {
            using var stream = new MemoryStream(highlightingResource);
            using var reader = new System.Xml.XmlTextReader(stream);
            txtCode.SyntaxHighlighting =
                ICSharpCode.AvalonEdit.Highlighting.Xshd.HighlightingLoader.Load(reader,
                    ICSharpCode.AvalonEdit.Highlighting.HighlightingManager.Instance);
        }

        public bool ShowLineNumbers
        {
            get => txtCode.ShowLineNumbers;
            set => txtCode.ShowLineNumbers = value;
        }

        public bool WordWrap
        {
            get => txtCode.WordWrap;
            set => txtCode.WordWrap = value;
        }

        private static byte[] GetResource(string resourceName)
        {
            // assuming the resource is located at "NamespaceName.ResourceName.extension"
            var assembly = Assembly.GetExecutingAssembly();

            using Stream stream = assembly.GetManifestResourceStream(resourceName);
            using MemoryStream ms = new();
            stream.CopyTo(ms);
            byte[] resourceBytes = ms.ToArray();
            return resourceBytes;
        }

        private void SetHighlighting()
        {
            var theme = ThemeExtensions.CurrentTheme;
            string resourceName = null;
            if (mode == CodeEditorModes.SQL && theme is DarkTheme)
            {
                resourceName = "DBADashSharedGUI.SyntaxHighlighting.SQL-Dark.xshd";
            }
            else if (mode == CodeEditorModes.SQL)
            {
                resourceName = "DBADashSharedGUI.SyntaxHighlighting.SQL-Light.xshd";
            }
            else if (mode == CodeEditorModes.PowerShell)
            {
                resourceName = "DBADashSharedGUI.SyntaxHighlighting.PowerShell.xshd";
            }
            else if (mode == CodeEditorModes.XML)
            {
                try
                {
                    txtCode.Text = FormatXml(_text);
                }
                catch
                {
                    // ignored
                }
                txtCode.SyntaxHighlighting = ICSharpCode.AvalonEdit.Highlighting.HighlightingManager.Instance.GetDefinitionByExtension(".xml");
                return;
            }
            else if (mode == CodeEditorModes.Json)
            {
                resourceName = "DBADashSharedGUI.SyntaxHighlighting.Json.xshd";
            }
            else if (mode == CodeEditorModes.Markdown)
            {
                txtCode.SyntaxHighlighting = ICSharpCode.AvalonEdit.Highlighting.HighlightingManager.Instance.GetDefinitionByExtension(".md");
                return;
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

        public static string FormatXml(string xml)
        {
            if (string.IsNullOrWhiteSpace(xml))
                return xml;

            // Wrapping in a temporary root element to form valid XML in cases where we have multiple root nodes
            var tempRoot = XElement.Parse($"<root>{xml}</root>");

            var formattedXmlBuilder = new StringBuilder();

            // Getting rid of the temporary root element and formatting the XML
            foreach (var node in tempRoot.Elements())
            {
                var formattedNode = node.ToString(SaveOptions.None);
                formattedXmlBuilder.AppendLine(formattedNode);
            }

            return formattedXmlBuilder.ToString();
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
            None,
            XML,
            Json,
            Markdown
        }

        private string _text;

        public string Text
        {
            get => _text;
            set
            {
                _text = value;
                txtCode.Text = value;
                var theme = ThemeExtensions.CurrentTheme;
                SetHighlighting();
                if (theme is DarkTheme && Mode != CodeEditorModes.SQL)
                {
                    theme = new BaseTheme();
                }
                txtCode.Background = new SolidColorBrush(theme.CodeEditorBackColor.ToMediaColor());
                txtCode.Foreground = new SolidColorBrush(theme.CodeEditorForeColor.ToMediaColor());
            }
        }

        public void ApplyTheme(BaseTheme theme)
        {
            SetHighlighting();
        }
    }
}