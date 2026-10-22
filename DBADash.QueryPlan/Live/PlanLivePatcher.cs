using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

namespace DBADash.QueryPlan.Live
{
    /// <summary>
    /// Brings a live plan up to date with newer counters, so a running query can be followed without fetching its plan
    /// again.
    ///
    /// sys.dm_exec_query_statistics_xml builds the whole showplan on every call - for a big plan that is a lot of XML to
    /// make, send through messaging and read, all to change a few numbers.  The shape of the plan doesn't change while
    /// the statement runs, and sys.dm_exec_query_profiles gives the same counters as a few small rows, so the plan is
    /// fetched once and its RunTimeInformation rewritten from the rows on each refresh.  The result is parsed like any
    /// other plan, which keeps everything worked out from the counters - own times, warnings, insights - right without
    /// a second way of calculating them.
    /// </summary>
    public static class PlanLivePatcher
    {
        /// <summary>
        /// <paramref name="planXml"/> with each operator's runtime counters replaced by <paramref name="counters"/>, and the
        /// statement's CPU and elapsed time set where they are given.  Operators with no counters are left as they are:
        /// a Compute Scalar SQL Server doesn't measure has none either way.
        /// </summary>
        public static string Apply(string planXml, IEnumerable<PlanLiveCounters> counters, long? cpuMs = null, long? elapsedMs = null)
        {
            ArgumentNullException.ThrowIfNull(planXml);
            ArgumentNullException.ThrowIfNull(counters);

            var document = XDocument.Parse(planXml);
            var root = document.Root ?? throw new PlanParseException("The plan has no root element.");
            var ns = root.Name.Namespace;

            var queryPlan = LivePlanElement(root, ns);
            if (queryPlan is null) return planXml;

            var byNode = counters.GroupBy(c => c.NodeId).ToDictionary(g => g.Key, g => g.OrderBy(c => c.Thread).ToList());

            foreach (var relOp in queryPlan.Descendants(ns + "RelOp"))
            {
                if (!int.TryParse((string?)relOp.Attribute("NodeId"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var nodeId)) continue;
                if (!byNode.TryGetValue(nodeId, out var threads)) continue;

                ApplyThreads(RunTimeInformation(relOp, ns), threads, ns);
            }

            if (queryPlan.Element(ns + "QueryTimeStats") is { } timeStats)
            {
                if (cpuMs is { } cpu) timeStats.SetAttributeValue("CpuTime", Text(cpu));
                if (elapsedMs is { } elapsed) timeStats.SetAttributeValue("ElapsedTime", Text(elapsed));
            }

            return document.ToString(SaveOptions.DisableFormatting);
        }

        /// <summary>
        /// The QueryPlan the counters belong to.  sys.dm_exec_query_statistics_xml returns the statement that is running,
        /// but node ids are only unique within a statement, so where there is more than one the one already carrying
        /// runtime information is taken.
        /// </summary>
        private static XElement? LivePlanElement(XElement root, XNamespace ns)
        {
            var plans = root.Descendants(ns + "QueryPlan").ToList();
            return plans.FirstOrDefault(p => p.Descendants(ns + "RunTimeInformation").Any()) ?? plans.FirstOrDefault();
        }

        /// <summary>
        /// The operator's RunTimeInformation, added where it has none - an operator that hadn't started when the plan was
        /// captured.  Placed where the showplan schema puts it: after the output list, warnings and memory fractions.
        /// </summary>
        private static XElement RunTimeInformation(XElement relOp, XNamespace ns)
        {
            if (relOp.Element(ns + "RunTimeInformation") is { } existing) return existing;

            var element = new XElement(ns + "RunTimeInformation");
            var before = relOp.Elements()
                .LastOrDefault(e => e.Name == ns + "OutputList" || e.Name == ns + "Warnings" || e.Name == ns + "MemoryFractions");

            if (before is not null) before.AddAfterSelf(element);
            else relOp.AddFirst(element);

            return element;
        }

        private static void ApplyThreads(XElement runTimeInformation, IReadOnlyList<PlanLiveCounters> threads, XNamespace ns)
        {
            var existing = runTimeInformation.Elements(ns + "RunTimeCountersPerThread").ToList();

            foreach (var counters in threads)
            {
                var thread = existing.FirstOrDefault(e => (string?)e.Attribute("Thread") == Text(counters.Thread));
                if (thread is null)
                {
                    // A thread that wasn't running when the plan was captured.  The execution mode is the operator's,
                    // so it is taken from a thread that has one.
                    thread = new XElement(ns + "RunTimeCountersPerThread", new XAttribute("Thread", Text(counters.Thread)));
                    var mode = existing.Select(e => (string?)e.Attribute("ActualExecutionMode")).FirstOrDefault(m => m is not null);
                    if (mode is not null) thread.SetAttributeValue("ActualExecutionMode", mode);

                    runTimeInformation.Add(thread);
                    existing.Add(thread);
                }

                thread.SetAttributeValue("ActualRows", Text(counters.Rows));
                thread.SetAttributeValue("ActualEndOfScans", Text(counters.EndOfScans));
                thread.SetAttributeValue("ActualExecutions", Text(counters.Executions));

                // Showplan only gives rebinds and rewinds for the operators they mean something for - spools, mostly - so
                // they are updated where the plan has them rather than added to every operator.
                SetIfPresent(thread, "ActualRebinds", counters.Rebinds);
                SetIfPresent(thread, "ActualRewinds", counters.Rewinds);

                // Not collected under lightweight profiling, where the DMV reports zero.  Updated where the plan already
                // has them, and added where there is something to say, so a plan without timings doesn't gain a row of
                // zero times that would read as measured.
                SetIfKnown(thread, "ActualRowsRead", counters.RowsRead);
                SetIfKnown(thread, "ActualElapsedms", counters.ElapsedMs);
                SetIfKnown(thread, "ActualCPUms", counters.CpuMs);
                SetIfKnown(thread, "ActualScans", counters.Scans);
                SetIfKnown(thread, "ActualLogicalReads", counters.LogicalReads);
                SetIfKnown(thread, "ActualPhysicalReads", counters.PhysicalReads);
                SetIfKnown(thread, "ActualReadAheads", counters.ReadAheads);
                SetIfKnown(thread, "ActualLobLogicalReads", counters.LobLogicalReads);
                SetIfKnown(thread, "ActualLobPhysicalReads", counters.LobPhysicalReads);
            }
        }

        private static void SetIfPresent(XElement thread, string name, long value)
        {
            if (thread.Attribute(name) is not null) thread.SetAttributeValue(name, Text(value));
        }

        private static void SetIfKnown(XElement thread, string name, long? value)
        {
            if (value is not { } known) return;
            if (known > 0 || thread.Attribute(name) is not null) thread.SetAttributeValue(name, Text(known));
        }

        private static string Text(long value) => value.ToString(CultureInfo.InvariantCulture);
    }
}
