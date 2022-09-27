using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media.Animation;

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
            if (!DisposeOnClose && e.CloseReason == CloseReason.UserClosing ) // Option to prevent form from disposing (useful for single instance)
            {
                e.Cancel = true;
                this.Hide();
            }
        }
    }
}
