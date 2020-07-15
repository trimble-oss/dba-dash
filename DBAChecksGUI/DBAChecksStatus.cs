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
    }
}
