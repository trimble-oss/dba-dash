using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBAChecksGUI
{
    class DBAChecksStatus
    {
        public enum DBAChecksStatusEnum
        {
            Critical = 1,
            Warning = 2,
            NA = 3,
            OK = 4
        }

        public static Color GetStatusColour(DBAChecksStatusEnum status)
        {
            if (status == DBAChecksStatusEnum.Critical)
            {
                return Color.Red;
            }
            if (status == DBAChecksStatusEnum.Warning)
            {
                return Color.Yellow;
            }
            if (status == DBAChecksStatusEnum.NA)
            {
                return Color.LightGray;
            }
            if (status == DBAChecksStatusEnum.OK)
            {
                return Color.Green;
            }
            return Color.Pink;
        }

        public static void SetProgressBarColor(DBAChecksStatusEnum status, ref CustomProgressControl.DataGridViewProgressBarCell pCell )
        {
            if (status == DBAChecksStatus.DBAChecksStatusEnum.OK)
            {
                pCell.ProgressBarColorFrom = Color.MintCream;
                pCell.ProgressBarColorTo = Color.Green;
            }
            else if (status == DBAChecksStatus.DBAChecksStatusEnum.Warning)
            {
                pCell.ProgressBarColorFrom = Color.LightYellow;
                pCell.ProgressBarColorTo = Color.Yellow;
            }
            else if (status == DBAChecksStatus.DBAChecksStatusEnum.Critical)
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
