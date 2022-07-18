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
    public partial class HourSelection : Form
    {
        public HourSelection()
        {
            InitializeComponent();
        }

        public List<int> SelectedHours
        {
            get
            {
                var selected = new List<int>();
                return chkHours.CheckedIndices.Cast<int>().ToList();
            }
            set
            {
                clearChecks();
                if (value != null)
                {
                    foreach (int h in value)
                    {
                        if (h >= 0 && h < 24)
                        {
                            chkHours.SetItemCheckState(h, CheckState.Checked);
                        }
                        else
                        {
                            throw new ArgumentOutOfRangeException("SelectedHours", h, "Invalid hour specified in list");
                        }
                    }
                }
            }
        }

        private void bttnAll_Click(object sender, EventArgs e)
        {
           for(int i = 0; i < 24; i++) {
                chkHours.SetItemCheckState(i,  CheckState.Checked);
           }
        }

        private void clearChecks()
        {
            for (int i = 0; i < 24; i++)
            {
                chkHours.SetItemCheckState(i, CheckState.Unchecked);
            }
        }

        private void bttnNone_Click(object sender, EventArgs e)
        {
            clearChecks();
        }

        private void bttnToggle_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < 24; i++)
            {              
                chkHours.SetItemCheckState(i, chkHours.GetItemCheckState(i) == CheckState.Checked ? CheckState.Unchecked : CheckState.Checked);
            }
        }

        private void bttn9to5_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < 24; i++)
            {
                chkHours.SetItemCheckState(i, i>=9 && i<17  ? CheckState.Checked : CheckState.Unchecked);
            }
        }

        private void bttnOK_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
        }
    }
}
