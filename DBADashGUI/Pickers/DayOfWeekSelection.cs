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
    public partial class DayOfWeekSelection : Form
    {
        public DayOfWeekSelection()
        {
            InitializeComponent();
        }

        public List<int> DaysOfWeekSelected
        {
            get
            {
                var selected = new List<int>();
                return chkDayOfWeek.CheckedIndices.Cast<int>().Select(i=>i+=1).ToList();
            }
            set
            {
                clearChecks();
                if (value != null)
                {
                    foreach (int dow in value)
                    {
                        if (dow >= 1 && dow < 8)
                        {
                            chkDayOfWeek.SetItemCheckState(dow-1, CheckState.Checked);
                        }
                        else
                        {
                            throw new ArgumentOutOfRangeException("DaysOfWeekSelected", dow, "Invalid day specified in list. Values 1..7 expected.");
                        }
                    }
                }
            }
        }


        private void clearChecks()
        {
            for (int i = 0; i < 7; i++)
            {
                chkDayOfWeek.SetItemCheckState(i, CheckState.Unchecked);
            }
        }

        private void bttnNone_Click(object sender, EventArgs e)
        {
            clearChecks();
        }

        private void bttnALL_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < 7; i++)
            {
                chkDayOfWeek.SetItemCheckState(i, CheckState.Checked);
            }
        }

        private void bttnToggle_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < 7; i++)
            {
                chkDayOfWeek.SetItemCheckState(i, chkDayOfWeek.GetItemCheckState(i) == CheckState.Checked ? CheckState.Unchecked : CheckState.Checked);
            }
        }

        private void bttnOK_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
        }

        private void bttnWeekday_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < 5; i++)
            {
                chkDayOfWeek.SetItemCheckState(i, CheckState.Checked);
            }
            for (int i = 5; i < 7; i++)
            {
                chkDayOfWeek.SetItemCheckState(i, CheckState.Unchecked);
            }
        }
    }
}
