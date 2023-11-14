using DBADashGUI.Theme;
using System;
using System.Windows.Forms;
using DBADashGUI.SchemaCompare;

namespace DBADashGUI
{
    public partial class CodeViewer : Form
    {
        public CodeViewer()
        {
            InitializeComponent();
            codeEditor1.ShowLineNumbers = true;
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
                this.Hide();
            }
        }

        private void tsWrapText_Click(object sender, EventArgs e)
        {
            codeEditor1.WordWrap = !codeEditor1.WordWrap;
        }
    }
}