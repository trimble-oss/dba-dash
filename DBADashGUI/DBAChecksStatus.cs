using System;
using System.Drawing;

namespace DBADashGUI
{
    public class DBADashStatus
    {
        public enum DBADashStatusEnum
        {
            Critical = 1,
            Warning = 2,
            NA = 3,
            OK = 4,
            Acknowledged = 5,
            WarningLow = 6,
            Information = 7
        }

        public static Color GetStatusBackColor(DBADashStatusEnum status)
        {
            return status switch
            {
                DBADashStatusEnum.Critical => DBADashUser.SelectedTheme.CriticalBackColor,
                DBADashStatusEnum.Warning => DBADashUser.SelectedTheme.WarningBackColor,
                DBADashStatusEnum.NA => DBADashUser.SelectedTheme.NotApplicableBackColor,
                DBADashStatusEnum.OK => DBADashUser.SelectedTheme.SuccessBackColor,
                DBADashStatusEnum.Acknowledged => DBADashUser.SelectedTheme.AcknowledgedBackColor,
                DBADashStatusEnum.WarningLow => DBADashUser.SelectedTheme.WarningLowBackColor,
                DBADashStatusEnum.Information => DBADashUser.SelectedTheme.InformationBackColor,
                _ => DashColors.RedPale
            };
        }

        public static Color GetStatusForeColor(DBADashStatusEnum status)
        {
            return status switch
            {
                DBADashStatusEnum.Critical => DBADashUser.SelectedTheme.CriticalForeColor,
                DBADashStatusEnum.Warning => DBADashUser.SelectedTheme.WarningForeColor,
                DBADashStatusEnum.NA => DBADashUser.SelectedTheme.NotApplicableForeColor,
                DBADashStatusEnum.OK => DBADashUser.SelectedTheme.SuccessForeColor,
                DBADashStatusEnum.Acknowledged => DBADashUser.SelectedTheme.AcknowledgedForeColor,
                DBADashStatusEnum.WarningLow => DBADashUser.SelectedTheme.WarningLowForeColor,
                DBADashStatusEnum.Information => DBADashUser.SelectedTheme.InformationForeColor,
                _ => DashColors.RedPale
            };
        }

        public static DBADashStatusEnum? ConvertToDBADashStatusEnum(int value)
        {
            if (Enum.IsDefined(typeof(DBADashStatusEnum), value))
            {
                return (DBADashStatusEnum)value;
            }
            else
            {
                return null;
            }
        }

        public static void SetProgressBarColor(DBADashStatusEnum status, ref CustomProgressControl.DataGridViewProgressBarCell pCell)
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