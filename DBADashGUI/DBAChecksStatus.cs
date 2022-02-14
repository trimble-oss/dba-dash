using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBADashGUI
{
    public class DBADashStatus
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
                return DashColors.Fail; 
            }
            else if (status == DBADashStatusEnum.Warning)
            {
                return DashColors.Warning; 
            }
            else if (status == DBADashStatusEnum.NA)
            {
                return DashColors.GrayLight; 
            }
            else if (status == DBADashStatusEnum.OK)
            {
                return DashColors.Success; 
            }
            return DashColors.RedPale; 
        }

        public static void SetProgressBarColor(DBADashStatusEnum status, ref CustomProgressControl.DataGridViewProgressBarCell pCell )
        {
            if (status == DBADashStatus.DBADashStatusEnum.OK)
            {
                pCell.ProgressBarColorFrom = DashColors.GreenPale;
                pCell.ProgressBarColorTo = DashColors.Green;
            }
            else if (status == DBADashStatus.DBADashStatusEnum.Warning)
            {
                pCell.ProgressBarColorFrom = DashColors.YellowPale;
                pCell.ProgressBarColorTo = DashColors.YellowDark;
            }
            else if (status == DBADashStatus.DBADashStatusEnum.Critical)
            {
                pCell.ProgressBarColorFrom = DashColors.RedPale;
                pCell.ProgressBarColorTo = DashColors.RedDark;
            }
            else
            {
                pCell.ProgressBarColorFrom = DashColors.ProgressBarFrom;
                pCell.ProgressBarColorTo = DashColors.ProgressBarTo;
            }
        }
    }
}
