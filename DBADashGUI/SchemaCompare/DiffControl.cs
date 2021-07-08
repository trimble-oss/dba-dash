using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBADashGUI
{
    public partial class DiffControl : UserControl
    {
        public DiffControl()
        {
            InitializeComponent();
            codeEditor1.ShowLineNumbers = true;
        }

        private string oldText;
        private string newText;
        private ViewMode mode;

        public enum ViewMode
        {
            Inline,
            Diff,
            Code
        }

        [Description("How to view the diff. e.g. inline, side by side"), Category("Diff")]
        public ViewMode Mode
        {
            get
            {
                return mode;
            }
            set
            {
                mode = value;
                setMode();
            }
        }

        private void setMode()
        {
            elCodeEditor.Visible = (mode == ViewMode.Code);
            elDiffViewer.Visible = (mode != ViewMode.Code);
            if (mode == ViewMode.Diff)
            {
                diffViewer1.ShowSideBySide();
            }
            if (mode == ViewMode.Inline)
            {
                diffViewer1.ShowInline();
            }
        }

        [Description("Old text to compare"), Category("Diff")]
        public string OldText
        {
            get
            {
                return oldText;
            }
            set
            {
                oldText = value;
                diffViewer1.OldText = value;
                copyLeftToolStripMenuItem.Enabled = value != null && value.Length > 0; ;
            }
        }
        [Description("New text to compare"), Category("Diff")]
        public string NewText
        {
            get
            {
                return newText;
            }
            set
            {
                newText = value;
                codeEditor1.Text = value;
                diffViewer1.NewText = value;
                copyRightToolStripMenuItem.Enabled =value!=null && value.Length > 0;
            }
        }


        private void tsInline_Click(object sender, EventArgs e)
        {
            Mode = ViewMode.Inline;
        }


        private void tsCode_Click(object sender, EventArgs e)
        {
            Mode = ViewMode.Code;
        }


        private void copyLeftToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (oldText.Length > 0)
            {
                Clipboard.SetText(oldText);
            }
        }

        private void copyRightToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (newText.Length > 0)
            {
                Clipboard.SetText(newText);
            }
        }

        private void tsDiff_Click(object sender, EventArgs e)
        {
            Mode = ViewMode.Diff;
        }
    }
}
