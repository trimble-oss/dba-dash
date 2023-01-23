using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBADashGUI.Performance
{
    public partial class WaitSummaryDialog : Form, ISetContext
    {
        public WaitSummaryDialog()
        {
            InitializeComponent();
        }

        DBADashContext context;

        public void SetContext(DBADashContext context)
        {
            this.context = context; 
        }

        private void WaitSummaryDialog_Load(object sender, EventArgs e)
        {
            waitsSummary1.SetContext(context); // Set Context on load for auto resize to work
        }
    }
}
