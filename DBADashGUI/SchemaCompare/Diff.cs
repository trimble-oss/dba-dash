using DocumentFormat.OpenXml.Drawing;
using System;
using System.Windows.Forms;
using static DBADashGUI.DiffControl;

namespace DBADashGUI
{
    public partial class Diff : Form
    {
        private readonly DiffControl diff1 = new();

        public Diff()
        {
            InitializeComponent();
        }

        public ViewMode Mode
        {
            get => diff1.Mode; set => diff1.Mode = value;
        }

        public void SetText(string oldText, string newText, ViewMode mode = ViewMode.Diff)
        {
            diff1.ApplyTheme(DBADashUser.SelectedTheme);
            AddDiffControl();
            diff1.OldText = oldText;
            diff1.NewText = newText;
            diff1.Mode = mode;
        }

        private void AddDiffControl()
        {
            if (!this.Controls.Contains(diff1))
            {
                this.Controls.Add(diff1);
                diff1.Dock = DockStyle.Fill;
            }
        }

        private void Diff_Load(object sender, EventArgs e)
        {
            AddDiffControl();
        }
    }
}