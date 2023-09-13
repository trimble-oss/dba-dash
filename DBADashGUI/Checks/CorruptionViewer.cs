using System;
using System.Windows.Forms;
using DBADashGUI.Theme;

namespace DBADashGUI
{
    public partial class CorruptionViewer : Form, ISetContext
    {
        public CorruptionViewer()
        {
            InitializeComponent();
            this.ApplyTheme();
        }

        public void SetContext(DBADashContext context)
        {
            corruption1.SetContext(context);
            corruption1.RefreshData();
        }

        private void CorruptionViewer_Load(object sender, EventArgs e)
        {
        }
    }
}