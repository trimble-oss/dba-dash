using DBADashGUI.Theme;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI
{
    public partial class HourSelection : Form
    {
        public HourSelection()
        {
            InitializeComponent();
            this.ApplyTheme();
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public List<int> SelectedHours
        {
            get => chkHours.CheckedIndices.Cast<int>().ToList();
            set
            {
                ClearChecks();
                if (value != null)
                {
                    foreach (int h in value)
                    {
                        if (h is >= 0 and < 24)
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

        private void BttnAll_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < 24; i++)
            {
                chkHours.SetItemCheckState(i, CheckState.Checked);
            }
        }

        private void ClearChecks()
        {
            for (int i = 0; i < 24; i++)
            {
                chkHours.SetItemCheckState(i, CheckState.Unchecked);
            }
        }

        private void BttnNone_Click(object sender, EventArgs e)
        {
            ClearChecks();
        }

        private void BttnToggle_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < 24; i++)
            {
                chkHours.SetItemCheckState(i, chkHours.GetItemCheckState(i) == CheckState.Checked ? CheckState.Unchecked : CheckState.Checked);
            }
        }

        private void Bttn9to5_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < 24; i++)
            {
                chkHours.SetItemCheckState(i, i is >= 9 and < 17 ? CheckState.Checked : CheckState.Unchecked);
            }
        }

        private void BttnOK_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
        }
    }
}