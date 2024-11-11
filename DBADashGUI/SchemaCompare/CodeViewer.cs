using DBADashGUI.Theme;
using System;
using System.Windows.Forms;
using DBADashGUI.SchemaCompare;

namespace DBADashGUI
{
    public partial class CodeViewer : Form
    {
        private readonly CodeEditor codeEditor1;

        public CodeViewer()
        {
            InitializeComponent();
            codeEditor1 = new CodeEditor() { ShowLineNumbers = true };
            elementHost1.Child = codeEditor1;
        }

        public bool DisposeOnClose { get; set; } = true;

        public CodeEditor.CodeEditorModes Language
        {
            get => codeEditor1.Mode;
            set => codeEditor1.Mode = value;
        }

        public string Code
        {
            get => codeEditor1.Text;
            set
            {
                codeEditor1.Text = value;
                this.ApplyTheme(DBADashUser.SelectedTheme);
            }
        }

        private void BttnCopy_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(codeEditor1.Text);
        }

        private void TsLineNumbers_Click(object sender, EventArgs e)
        {
            codeEditor1.ShowLineNumbers = !codeEditor1.ShowLineNumbers;
        }

        private void CodeViewer_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!DisposeOnClose && e.CloseReason == CloseReason.UserClosing) // Option to prevent form from disposing (useful for single instance)
            {
                e.Cancel = true;
                Hide();
            }
        }

        private void tsWrapText_Click(object sender, EventArgs e)
        {
            codeEditor1.WordWrap = !codeEditor1.WordWrap;
        }

        private void TsSave_Click(object sender, EventArgs e)
        {
            var filter = Language switch
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
    }
}