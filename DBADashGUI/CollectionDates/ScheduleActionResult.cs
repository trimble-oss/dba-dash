using System.Drawing;
using System.Windows.Forms;

namespace DBADashGUI.CollectionDates
{
    /// <summary>
    /// The outcome of a schedule change or pause, to show the user.  A warning means it was done, but something else
    /// didn't work - e.g. the repository couldn't be told about a pause, so alerts aren't muted.
    /// </summary>
    internal sealed record ScheduleActionResult(bool Succeeded, string Message, bool IsWarning = false)
    {
        public Color StatusColor => !Succeeded ? DashColors.Fail : IsWarning ? DashColors.Warning : DashColors.Success;

        public MessageBoxIcon Icon => !Succeeded ? MessageBoxIcon.Error : IsWarning ? MessageBoxIcon.Warning : MessageBoxIcon.Information;
    }
}
