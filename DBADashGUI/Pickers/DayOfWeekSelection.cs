using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using DBADashGUI.Theme;

namespace DBADashGUI
{
    public partial class DayOfWeekSelection : Form
    {
        public DayOfWeekSelection()
        {
            InitializeComponent();
            this.ApplyTheme();
        }

        public List<int> DaysOfWeekSelected
        {
            get
            {
                var selected = new List<int>();
                return chkDayOfWeek.CheckedIndices.Cast<int>().Select(i => i + 1).ToList();
            }
            set
            {
                ClearChecks();
                if (value != null)
                {
                    foreach (int dow in value)
                    {
                        if (dow is >= 1 and < 8)
                        {
                            chkDayOfWeek.SetItemCheckState(dow - 1, CheckState.Checked);
                        }
                        else
                        {
                            throw new ArgumentOutOfRangeException("DaysOfWeekSelected", dow, "Invalid day specified in list. Values 1..7 expected.");
                        }
                    }
                }
            }
        }


        private void ClearChecks()
        {
            for (int i = 0; i < 7; i++)
            {
                chkDayOfWeek.SetItemCheckState(i, CheckState.Unchecked);
            }
        }

        private void BttnNone_Click(object sender, EventArgs e)
        {
            ClearChecks();
        }

        private void BttnALL_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < 7; i++)
            {
                chkDayOfWeek.SetItemCheckState(i, CheckState.Checked);
            }
        }

        private void BttnToggle_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < 7; i++)
            {
                chkDayOfWeek.SetItemCheckState(i, chkDayOfWeek.GetItemCheckState(i) == CheckState.Checked ? CheckState.Unchecked : CheckState.Checked);
            }
        }

        private void BttnOK_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
        }

        private void BttnWeekday_Click(object sender, EventArgs e)
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
