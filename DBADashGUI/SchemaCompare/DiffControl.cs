using System;
using System.ComponentModel;
using System.Windows.Forms;
using System.Windows.Media;
using DBADashGUI.Theme;

namespace DBADashGUI
{
    public partial class DiffControl : UserControl, IThemedControl
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
            get => mode;
            set
            {
                mode = value;
                SetMode();
            }
        }

        private void SetMode()
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
            get => oldText;
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
            get => newText;
            set
            {
                newText = value;
                codeEditor1.Text = value;
                diffViewer1.NewText = value;
                copyRightToolStripMenuItem.Enabled = value != null && value.Length > 0;
            }
        }

        private void TsInline_Click(object sender, EventArgs e)
        {
            Mode = ViewMode.Inline;
        }

        private void TsCode_Click(object sender, EventArgs e)
        {
            Mode = ViewMode.Code;
        }

        private void CopyLeftToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (oldText.Length > 0)
            {
                Clipboard.SetText(oldText);
            }
        }

        private void CopyRightToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (newText.Length > 0)
            {
                Clipboard.SetText(newText);
            }
        }

        private void TsDiff_Click(object sender, EventArgs e)
        {
            Mode = ViewMode.Diff;
        }

        public void ApplyTheme(BaseTheme theme)
        {
            this.Controls.ApplyTheme(theme);
            diffViewer1 = new DiffPlex.Wpf.Controls.DiffViewer();
            elDiffViewer.Child = diffViewer1;
            diffViewer1.Foreground = new SolidColorBrush(theme.ForegroundColor.ToMediaColor());
            diffViewer1.Background= new SolidColorBrush(theme.BackgroundColor.ToMediaColor());
            diffViewer1.NewText = newText;
            diffViewer1.OldText = oldText;
            codeEditor1.Text = newText;
            SetMode();
        }
    }
}