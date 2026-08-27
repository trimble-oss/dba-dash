using DBADash.Messaging;
using DBADash.XE;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace DBADashGUI.XETrace
{
    /// <summary>The user's trace request as configured in the GUI, plus the repo-audit projections of it.</summary>
    public class XETraceConfig
    {
        public XETraceEventType Events { get; set; } = XETraceEventType.RpcCompleted | XETraceEventType.SqlBatchCompleted;

        public List<XEFilter> Filters { get; set; } = new();

        /// <summary>Arbitrary extra events (beyond the RPC/Batch/Error shortcuts) chosen from the catalog.</summary>
        public List<XETraceEventDef> ExtraEvents { get; set; } = new();

        /// <summary>
        /// Every selected event (the built-in RPC/Batch/Error shortcuts plus <see cref="ExtraEvents"/>) as a typed
        /// definition carrying its data columns from the catalog.  This is what the service builds the session DDL
        /// from; the columns let it apply each data-column filter only to events that expose it (and the severity
        /// floor only to events with a <c>severity</c> column).  Built by the GUI, which has the catalog.
        /// </summary>
        public List<XETraceEventDef> EventDefs { get; set; } = new();

        /// <summary>Global actions ("global fields") captured on every event.  Defaults to the standard set.</summary>
        public List<XEActionDef> GlobalActions { get; set; } = new(XETraceDefinition.DefaultGlobalActions);

        /// <summary>Per-event customizable-column toggles (the <c>SET</c> options), keyed by event name.</summary>
        public Dictionary<string, List<XECustomization>> EventCustomizations { get; set; } = new();

        /// <summary>Target preference; <see cref="XETraceTargetPreference.Auto"/> = live streaming (including on Azure SQL DB).</summary>
        public XETraceTargetPreference Target { get; set; } = XETraceTargetPreference.Auto;

        public int MaxDurationSeconds { get; set; } = 300;

        public int BatchIntervalSeconds { get; set; } = 5;

        /// <summary>Event sampling: capture ~1 in N events (0/1 = no sampling).  Entered in the UI as a percentage.</summary>
        public int SampleN { get; set; }

        /// <summary>Capture the native .xel file (event_file target only) for a Save-as-.xel download.</summary>
        public bool CaptureXel { get; set; }

        /// <summary>Comma-separated event names for the audit column (e.g. "RpcCompleted, module_end").</summary>
        public string EventTypesCsv
        {
            get
            {
                var parts = new List<string>();
                if (Events != 0) parts.Add(Events.ToString());
                parts.AddRange(ExtraEvents.ConvertAll(e => e.Name));
                return string.Join(", ", parts);
            }
        }

        public string FiltersJson =>
            Filters is { Count: > 0 } ? JsonConvert.SerializeObject(Filters) : null;
    }
}
