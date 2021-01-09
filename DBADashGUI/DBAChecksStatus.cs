using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBADashGUI
{
    class DBADashStatus
    {
        public enum DBADashStatusEnum
        {
            Critical = 1,
            Warning = 2,
            NA = 3,
            OK = 4
        }

        public static Color GetStatusColour(DBADashStatusEnum status)
        {
            if (status == DBADashStatusEnum.Critical)
            {
                return Color.Red;
            }
            if (status == DBADashStatusEnum.Warning)
            {
                return Color.Yellow;
            }
            if (status == DBADashStatusEnum.NA)
            {
                return Color.LightGray;
            }
            if (status == DBADashStatusEnum.OK)
            {
                return Color.Green;
            }
            return Color.Pink;
        }

        public static void SetProgressBarColor(DBADashStatusEnum status, ref CustomProgressControl.DataGridViewProgressBarCell pCell )
        {
            if (status == DBADashStatus.DBADashStatusEnum.OK)
            {
                pCell.ProgressBarColorFrom = Color.MintCream;
                pCell.ProgressBarColorTo = Color.Green;
            }
            else if (status == DBADashStatus.DBADashStatusEnum.Warning)
            {
                pCell.ProgressBarColorFrom = Color.LightYellow;
                pCell.ProgressBarColorTo = Color.Yellow;
            }
            else if (status == DBADashStatus.DBADashStatusEnum.Critical)
            {
                pCell.ProgressBarColorFrom = Color.LightSalmon;
                pCell.ProgressBarColorTo = Color.Red;
            }
            else
            {
                pCell.ProgressBarColorFrom = Color.Azure;
                pCell.ProgressBarColorTo = Color.LightSkyBlue;
            }
        }
    }
}
