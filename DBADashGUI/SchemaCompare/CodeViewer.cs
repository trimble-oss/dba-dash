using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBADashGUI
{
    public partial class CodeViewer : Form
    {
        public CodeViewer()
        {
            InitializeComponent();
            codeEditor1.ShowLineNumbers = true;
        }

        public string SQL
        {
            get
            {
                return codeEditor1.Text;
            }
            set
            {
                codeEditor1.Text = value;
            }
        }

        private void bttnCopy_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(codeEditor1.Text);
        }

        private void tsLineNumbers_Click(object sender, EventArgs e)
        {
            codeEditor1.ShowLineNumbers = !codeEditor1.ShowLineNumbers;
        }
    }
}
