using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DBADash.Messaging;

namespace DBADashGUI.AgentJobs
{
    internal class HelpJobInfoExtended : HelpJobInfo
    {
        public Color LastRunOutcomeColor => GetLastRunOutcomeColor(LastRunOutcome);

        public static Color GetLastRunOutcomeColor(int? value)
        {
            return value switch
            {
                0 => DashColors.Fail,
                1 => DashColors.Success,
                3 => DashColors.Warning,
                5 => DashColors.AvoidanceZone,
                _ => DashColors.AvoidanceZone
            };
        }

        public HelpJobInfoExtended(DataTable dtJob) : base(dtJob)
        {
        }
    }
}