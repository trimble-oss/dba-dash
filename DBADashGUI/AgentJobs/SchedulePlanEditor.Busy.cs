using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI.AgentJobs
{
    /// <summary>
    /// Busy hours in plan mode: shaded on the preview in the plan's time, and a toolbar button to edit them
    /// </summary>
    internal sealed partial class SchedulePlanEditor
    {
        private IReadOnlyList<(DateTime Start, DateTime End)> _busyUtc = Array.Empty<(DateTime Start, DateTime End)>();

        /// <summary>The Busy Hours button was clicked: the host opens the busy periods editor, and sets <see cref="BusyPeriodsUtc"/> again after</summary>
        public event EventHandler EditBusyHoursRequested;

        /// <summary>
        /// When the instance is busy, in UTC (dbo.BusyPeriods_Get), covering the selected range and the predicted preview -
        /// the host fetches them with the plan's data
        /// </summary>
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public IReadOnlyList<(DateTime Start, DateTime End)> BusyPeriodsUtc
        {
            get => _busyUtc;
            set
            {
                _busyUtc = value ?? Array.Empty<(DateTime Start, DateTime End)>();
                ShowBusyOnChart();
            }
        }

        private ToolStripItem CreateBusyItem() =>
            new ToolStripButton("Busy Hours...", null, (_, _) => EditBusyHoursRequested?.Invoke(this, EventArgs.Empty))
            {
                ToolTipText = "When the instance is busy - e.g. weekdays 08:00 to 18:00 - shaded behind the runs"
            };

        /// <summary>
        /// The busy hours shaded on the chart: into server time by the server's offset (as <see cref="ToUtc"/> adds it),
        /// then shown as the runs are (<see cref="Display"/>)
        /// </summary>
        private void ShowBusyOnChart()
        {
            if (_preview == null) return;
            _preview.BusyPeriods = _busyUtc.Select(b => (Display(b.Start.AddMinutes(-_whatIfUtcOffset)), Display(b.End.AddMinutes(-_whatIfUtcOffset)))).ToList();
        }
    }
}
