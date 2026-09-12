using System.Drawing;

namespace DBADashGUI.CustomReports
{
    /// <summary>
    /// A button on a report's toolbar that swaps the view to a related report - see
    /// <see cref="CustomReport.SwitchTo"/>.
    ///
    /// <para>For two reports that answer the same question two ways, where hosting them as separate tabs
    /// would put the second one out of sight.  The switch is a navigation like any other, so Back returns
    /// to the report switched from.</para>
    /// </summary>
    public class ReportSwitch
    {
        /// <summary>Procedure name of the system report to switch to.</summary>
        public string ProcedureName { get; set; }

        public string Text { get; set; }

        public string ToolTipText { get; set; }

        public Image Image { get; set; }
    }
}
