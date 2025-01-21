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
        public bool DisposeOnClose { get; set; } = true;

        public bool EditEnabled
        {
            get => !codeEditor1.IsReadOnly;
            set
            {
                codeEditor1.IsReadOnly = !value;
                this.Text = value ? "Code Editor" : "Code Viewer";
                pnlDialog.Visible = value;
            }
        }

        public CodeEditor.CodeEditorModes Syntax
        {
            get => codeEditor1.Mode;
            set
            {
                codeEditor1.Mode = value;
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

        public string Code
        {
            get => codeEditor1.txtCode.Text;
            set
            {
                codeEditor1.txtCode.Text = value;
                ShowMarkdownPreview();
            }
        }

        public CodeEditorForm()
        {
            InitializeComponent();
            InitializeAsync();
            elHost.Dock = DockStyle.Fill;
            splitContainer1.Panel1.Controls.Add(elHost);
            elHost.Child = codeEditor1;
            codeEditor1.TextChanged += (_, _) =>
            {
                ShowMarkdownPreview();
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
                MessageBox.Show($"Error saving file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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