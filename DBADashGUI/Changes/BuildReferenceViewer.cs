using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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