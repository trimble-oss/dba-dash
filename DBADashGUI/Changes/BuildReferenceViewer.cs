using System;
using System.Windows.Forms;

namespace DBADashGUI.Changes
{
    public partial class BuildReferenceViewer : Form
    {
        public BuildReferenceViewer()
        {
            InitializeComponent();
        }

        private void BuildReferenceViewer_Load(object sender, EventArgs e)
        {
            buildReference1.RefreshData();
        }
    }
}