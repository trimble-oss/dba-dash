using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using DBADashSharedGUI;
using Markdig;
using Markdig.Syntax;
using Markdig.SyntaxHighlighting;

namespace DBADashGUI.SchemaCompare
{
    public partial class CodeEditorForm : Form
    {
        private readonly CodeEditor codeEditor1 = new();
        private readonly ElementHost elHost = new();
        private bool isWebViewInit;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public bool DisposeOnClose { get; set; } = true;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool EditEnabled
        {
            get => !codeEditor1.IsReadOnly;
            set
            {
                codeEditor1.IsReadOnly = !value;
                this.Text = value ? "Code Editor" : "Code Viewer";
                pnlDialog.Visible = value;
                UpdateFormatButton();
            }
        }

        /// <summary>
        /// Whether XML or JSON starts formatted - null for the mode's default, XML formatted and JSON as given.  The
        /// Format button switches it either way - see <see cref="CodeEditor.Format"/>.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool? Format
        {
            get => codeEditor1.Format;
            set
            {
                codeEditor1.Format = value;
                UpdateFormatButton();
            }
        }

        private string unformattedLabel = DefaultUnformattedLabel;

        /// <summary>What the text is when it isn't formatted, if nothing more is known: the text the viewer was given.</summary>
        public const string DefaultUnformattedLabel = "as given";

        /// <summary>
        /// What the text is when it isn't formatted, for the Format button's tooltip - "as written in the file" for a
        /// file's text, "as the cell holds it" for a grid cell's, which a JSON file's nested value is a one-line copy of.
        /// Says where the text came from, so the unformatted text isn't taken for something it isn't.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string UnformattedLabel
        {
            get => unformattedLabel;
            set
            {
                unformattedLabel = string.IsNullOrWhiteSpace(value) ? DefaultUnformattedLabel : value;
                UpdateFormatButton();
            }
        }

        /// <summary>Formats the XML or JSON shown, or puts it back as it was given.</summary>
        private readonly ToolStripButton tsFormat = new()
        {
            DisplayStyle = ToolStripItemDisplayStyle.Image,
            Image = DBADashSharedGUI.Properties.Resources.PrettyCode,
            ImageTransparentColor = Color.Magenta,
            Name = "tsFormat",
            Visible = false
        };

        /// <summary>
        /// Only for XML and JSON in a viewer: in an editor, switching would replace what's been typed with the text
        /// given.  Pressed while the text shown is formatted, and off - saying why - for text that can't be.
        /// </summary>
        private void UpdateFormatButton()
        {
            tsFormat.Visible = CodeEditor.CanFormat(codeEditor1.Mode) && codeEditor1.IsReadOnly;
            tsFormat.Checked = codeEditor1.IsFormatted;
            tsFormat.Enabled = codeEditor1.FormatError == null;
            tsFormat.Text = codeEditor1.FormatError != null
                ? "Format - this text can't be formatted: " + codeEditor1.FormatError
                : codeEditor1.IsFormatted
                    ? $"Format - click to show the text {unformattedLabel}"
                    : $"Format - showing the text {unformattedLabel}; click to indent it, a node or property to a line";
        }

        private void TsFormat_Click(object sender, EventArgs e)
        {
            Cursor = Cursors.WaitCursor;
            try
            {
                Format = !codeEditor1.IsFormatted;
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public CodeEditor.CodeEditorModes Syntax
        {
            get => codeEditor1.Mode;
            set
            {
                codeEditor1.Mode = value;
                UpdateFormatButton();
                splitContainer1.Panel2Collapsed = value != CodeEditor.CodeEditorModes.Markdown;
                tsMarkdownView.Visible = value == CodeEditor.CodeEditorModes.Markdown;
                _ = ShowMarkdownPreview();
            }
        }

        private async Task ShowMarkdownPreview()
        {
            if (codeEditor1.Mode != CodeEditor.CodeEditorModes.Markdown) return;
            if (!isWebViewInit) return;
            lblError.Visible = false;
            try
            {
                var pipeline = new MarkdownPipelineBuilder()
                    .UseAdvancedExtensions()
                    .UseSyntaxHighlighting()
                    .Build();

                var html = Markdig.Markdown.ToHtml(codeEditor1.txtCode.Text, pipeline);

                var success = await webView2Wrapper.NavigateToLargeString(html);
                ToggleMarkdown(EditEnabled | !success, true);
            }
            catch (Exception ex)
            {
                ToggleMarkdown(true, false);
                lblError.Text = "Error loading markdown preview";
                lblError.ToolTipText = ex.Message;
                lblError.Visible = true;
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Code
        {
            // CurrentText rather than the editor's Text: that's only what was last set, while CurrentText holds any
            // edits made since, and the formatting XML mode applied.
            get => codeEditor1.CurrentText;
            set
            {
                // Through the editor's Text rather than straight into txtCode: XML mode formats the editor's own copy
                // of the text, so setting Syntax to XML after Code would otherwise replace the code with nothing.
                codeEditor1.Text = value;
                UpdateFormatButton();
            }
        }

        public CodeEditorForm()
        {
            InitializeComponent();
            tsFormat.Click += TsFormat_Click;
            toolStrip1.Items.Insert(toolStrip1.Items.IndexOf(tsWrapText) + 1, tsFormat);
            InitializeAsync();
            splitContainer1.Panel2Collapsed = codeEditor1.Mode != CodeEditor.CodeEditorModes.Markdown;
            tsMarkdownView.Visible = codeEditor1.Mode == CodeEditor.CodeEditorModes.Markdown;
            elHost.Dock = DockStyle.Fill;
            splitContainer1.Panel1.Controls.Add(elHost);
            elHost.Child = codeEditor1;
            codeEditor1.TextChanged += async (_, _) =>
            {
                await ShowMarkdownPreview();
            };
        }

        private async void InitializeAsync()
        {
            isWebViewInit = true;
            await ShowMarkdownPreview();
        }

        private void TsLineNumbers_Click(object sender, EventArgs e)
        {
            codeEditor1.ShowLineNumbers = !codeEditor1.ShowLineNumbers;
        }

        private void BttnCopy_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(Code);
        }

        private void TsSave_Click(object sender, EventArgs e)
        {
            var filter = Syntax switch
            {
                CodeEditor.CodeEditorModes.SQL => "SQL Scripts|*.sql|Text Files|*.txt",
                CodeEditor.CodeEditorModes.PowerShell => "PowerShell Scripts|*.ps1|Text Files|*.txt",
                CodeEditor.CodeEditorModes.XML => "XML Files|*.xml|Text Files|*.txt",
                _ => "Text Files|*.txt"
            };
            var sfd = new SaveFileDialog() { Filter = filter, Title = "Save" };
            if (sfd.ShowDialog() != DialogResult.OK || string.IsNullOrEmpty(sfd.FileName)) return;
            try
            {
                System.IO.File.WriteAllText(sfd.FileName, Code);
                MessageBox.Show("File saved successfully", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex, "Error saving file");
            }
        }

        private void TsWrapText_Click(object sender, EventArgs e)
        {
            codeEditor1.WordWrap = !codeEditor1.WordWrap;
        }

        private void CodeViewer_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!DisposeOnClose && e.CloseReason == CloseReason.UserClosing) // Option to prevent form from disposing (useful for single instance)
            {
                e.Cancel = true;
                Hide();
            }
        }

        private void MarkdownToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ToggleMarkdown(true, false);
        }

        private void ToggleMarkdown(bool markDownIsVisible, bool previewIsVisible)
        {
            splitContainer1.Panel1Collapsed = !markDownIsVisible;
            splitContainer1.Panel2Collapsed = !previewIsVisible;
            markdownToolStripMenuItem.Checked = markDownIsVisible && !previewIsVisible;
            markdownPreviewToolStripMenuItem.Checked = markDownIsVisible && previewIsVisible;
            previewToolStripMenuItem.Checked = previewIsVisible && !markDownIsVisible;
        }

        private void MarkdownPreviewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ToggleMarkdown(true, true);
        }

        private void PreviewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ToggleMarkdown(false, true);
        }
    }
}