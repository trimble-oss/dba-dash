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
            // Ctrl+F opens a find bar (F3 / Shift+F3 for next and previous), read only or not.
            searchPanel = ICSharpCode.AvalonEdit.Search.SearchPanel.Install(txtCode);
            searchPanel.Style = (System.Windows.Style)Resources["SearchPanelStyle"];
            searchPanel.MarkerBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(0x80, 0xFF, 0xD5, 0x4F));
            txtCode.ContextMenu = BuildContextMenu();
            Mode = CodeEditorModes.SQL;
        }

        private readonly ICSharpCode.AvalonEdit.Search.SearchPanel searchPanel;

        /// <summary>Find on the right click menu too, where it is easier to come across than Ctrl+F.</summary>
        private System.Windows.Controls.ContextMenu BuildContextMenu()
        {
            var menu = new System.Windows.Controls.ContextMenu();
            var find = new System.Windows.Controls.MenuItem { Header = "Find...", InputGestureText = "Ctrl+F" };
            // Opened once the menu has closed: closing hands focus back to the editor, which would take it from the find box.
            var findClicked = false;
            find.Click += (_, _) => findClicked = true;
            menu.Closed += (_, _) =>
            {
                if (!findClicked) return;
                findClicked = false;
                Dispatcher.BeginInvoke(OpenSearch, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            };
            menu.Items.Add(find);
            menu.Items.Add(new System.Windows.Controls.Separator());
            var cut = new System.Windows.Controls.MenuItem { Header = "Cut", Command = System.Windows.Input.ApplicationCommands.Cut };
            menu.Items.Add(cut);
            menu.Items.Add(new System.Windows.Controls.MenuItem { Header = "Copy", Command = System.Windows.Input.ApplicationCommands.Copy });
            // Most often what is wanted from a viewer: everything, without selecting it first.
            var copyAll = new System.Windows.Controls.MenuItem { Header = "Copy All" };
            copyAll.Click += (_, _) => System.Windows.Clipboard.SetText(txtCode.Text);
            menu.Items.Add(copyAll);
            var paste = new System.Windows.Controls.MenuItem { Header = "Paste", Command = System.Windows.Input.ApplicationCommands.Paste };
            menu.Items.Add(paste);
            menu.Items.Add(new System.Windows.Controls.Separator());
            menu.Items.Add(new System.Windows.Controls.MenuItem { Header = "Select All", Command = System.Windows.Input.ApplicationCommands.SelectAll });

            // Read only can change after the menu is built, so decided each time it opens.
            menu.Opened += (_, _) =>
            {
                var editable = !txtCode.IsReadOnly;
                cut.Visibility = paste.Visibility = editable ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
                copyAll.IsEnabled = txtCode.Document.TextLength > 0;
            };
            return menu;
        }

        /// <summary>Opens the find bar, seeded with the selection when there is a short one.</summary>
        public void OpenSearch()
        {
            var selected = txtCode.SelectedText;
            if (!string.IsNullOrEmpty(selected) && !selected.Contains('\n')) searchPanel.SearchPattern = selected;
            searchPanel.Open();
            // The template is only applied once the panel is first shown, so its text box is looked for afterwards.
            Dispatcher.BeginInvoke(() =>
            {
                if (searchPanel.Template?.FindName("PART_searchTextBox", searchPanel) is not System.Windows.Controls.TextBox box) return;
                box.Focus();
                System.Windows.Input.Keyboard.Focus(box);
                box.SelectAll();
            }, System.Windows.Threading.DispatcherPriority.Input);
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

            // Getting rid of the temporary root element and formatting the XML.  Nodes rather than Elements: a value can
            // be a processing instruction alone - sp_BlitzLock returns each query as <?query ... ?> - or have text or
            // comments at the top level, and those would otherwise be dropped, leaving nothing to show.
            foreach (var node in tempRoot.Nodes())
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