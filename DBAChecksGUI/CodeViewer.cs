using ICSharpCode.TextEditor.Document;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBAChecksGUI
{
    public partial class CodeViewer : Form
    {
        public CodeViewer()
        {
            InitializeComponent();
        }

        public string SQL
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

        private void CodeViewer_Load(object sender, EventArgs e)
        {
            HighlightingManager.Manager.AddSyntaxModeFileProvider(new AppSyntaxModeProvider());
            txtCode.SetHighlighting("SQL");
        }

        private void bttnCopy_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(txtCode.Text);
        }
    }
}
