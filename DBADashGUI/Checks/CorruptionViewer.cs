using System;
using System.Windows.Forms;

namespace DBADashGUI
{
    public partial class CorruptionViewer : Form, ISetContext
    {
        public CorruptionViewer()
        {
            InitializeComponent();
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