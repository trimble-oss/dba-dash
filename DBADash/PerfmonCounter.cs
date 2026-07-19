using System;
using System.Collections.Generic;
using System.Linq;

namespace DBADash
{
    /// <summary>
    /// Defines an OS-level performance counter collected from the host via WMI
    /// (the raw <c>Win32_PerfRawData_*</c> classes, cooked over the collection interval) rather than
    /// from <c>sys.dm_os_performance_counters</c>.
    ///
    /// Collection identifiers (<see cref="WmiClass"/> / <see cref="WmiProperty"/>) are what we
    /// actually query. Storage identifiers (<see cref="ObjectName"/> / <see cref="CounterName"/> /
    /// <see cref="InstanceName"/>) are the perfmon-style triad written to the existing
    /// PerformanceCounters table so the whole GUI / alerting stack works unchanged.
    ///
    /// There is no clean bidirectional map between perfmon names ("% Processor Time") and WMI
    /// property names ("PercentProcessorTime"), so we carry both. Display names fall back to the
    /// WMI names when not supplied.
    /// </summary>
    public class PerfmonCounter
    {
        /// <summary>
        /// Prefix applied to the stored object_name so perfmon counters can never clash with the DMV
        /// performance counters in dbo.Counters.  The DMV collection strips everything up to the first
        /// colon (STUFF(...CHARINDEX(':')...)), so every DMV-stored object_name is colon-free - a colon
        /// in this prefix therefore guarantees perfmon names are disjoint from DMV names by construction.
        /// Also makes the source identifiable: object_name LIKE 'PerfMon:%'.
        /// </summary>
        public const string ObjectNamePrefix = "PerfMon:";

        /// <summary>Prefix of the raw WMI perf classes we collect from (e.g. Win32_PerfRawData_PerfOS_Processor).</summary>
        public const string RawClassPrefix = "Win32_PerfRawData_";

        /// <summary>WMI class to query, e.g. "Win32_PerfRawData_PerfOS_Processor".</summary>
        public string WmiClass { get; set; }

        /// <summary>WMI property holding the cooked value, e.g. "PercentProcessorTime".</summary>
        public string WmiProperty { get; set; }

        /// <summary>
        /// Perfmon object name stored in the DB, e.g. "Processor". Falls back to <see cref="WmiClass"/>.
        /// </summary>
        public string ObjectName { get; set; }

        /// <summary>
        /// Perfmon counter name stored in the DB, e.g. "% Processor Time". Falls back to <see cref="WmiProperty"/>.
        /// </summary>
        public string CounterName { get; set; }

        /// <summary>
        /// Instance to collect. "*" collects every instance the class returns (matched on the WMI
        /// <c>Name</c> property). For single-instance classes (e.g. Memory) use "" or "*".
        /// </summary>
        public string InstanceName { get; set; } = "*";

        /// <summary>
        /// The PERF_* counter type (the WMI <c>CounterType</c> qualifier) that drives how the raw value is
        /// cooked.  Persisted at discovery time so collection needs no per-sample WMI schema lookup.
        /// 0 = unknown (e.g. a hand-edited config): the collector resolves it from the class schema
        /// (process-cached) at collection time instead.
        /// </summary>
        public int CounterType { get; set; }

        /// <summary>
        /// Companion base WMI property (e.g. <c>&lt;WmiProperty&gt;_Base</c>) needed to cook fraction /
        /// average-timer counters, else null.  Persisted alongside <see cref="CounterType"/>.
        /// </summary>
        public string BaseWmiProperty { get; set; }

        public string EffectiveObjectName => string.IsNullOrEmpty(ObjectName) ? WmiClass : ObjectName;

        public string EffectiveCounterName => string.IsNullOrEmpty(CounterName) ? WmiProperty : CounterName;

        /// <summary>
        /// Curated default counters, focused on OS-level signals that COMPLEMENT DBA Dash's existing
        /// SQL telemetry rather than duplicate it.  Collected from the raw (Win32_PerfRawData_*) classes
        /// and cooked over the collection interval, so rates are true interval averages and fractional
        /// values (disk latency) keep full precision.  Deliberately excluded:
        ///   - machine CPU % breakdown        -> covered by the CPU collection (dm_os_ring_buffers)
        ///   - available / committed memory    -> covered by the DMV counters (dm_os_sys_memory / Memory Manager)
        ///   - scheduler / runnable / pending IO -> covered by the DMV counters (dm_os_schedulers)
        /// Disk latency (Avg. Disk sec/Read/Write) is included now that raw cooking preserves precision.
        /// The _Total Processor % counters cook correctly because the raw _Total instance is already the
        /// per-core average (not a sum across cores), so no instance-count division is needed.
        /// </summary>
        /// CounterType / BaseWmiProperty are the values WMI reports for these well-known counters, hardcoded
        /// so the default set needs no schema lookup at all (the collector still resolves the type from the
        /// class schema for any counter that lacks it - see <see cref="CounterType"/>).
        public static List<PerfmonCounter> DefaultCounters() => new()
        {
            new() { WmiClass = "Win32_PerfRawData_PerfOS_Processor",   WmiProperty = "PercentProcessorTime",   ObjectName = "Processor",         CounterName = "% Processor Time",           InstanceName = "_Total", CounterType = 558957824 /* PERF_100NSEC_TIMER_INV */ },
            new() { WmiClass = "Win32_PerfRawData_PerfOS_Processor",   WmiProperty = "PercentPrivilegedTime",  ObjectName = "Processor",         CounterName = "% Privileged Time",          InstanceName = "_Total", CounterType = 542180608 /* PERF_100NSEC_TIMER */ },
            new() { WmiClass = "Win32_PerfRawData_PerfOS_System",      WmiProperty = "ProcessorQueueLength",   ObjectName = "System",            CounterName = "Processor Queue Length",     InstanceName = "",       CounterType = 65536 /* PERF_COUNTER_RAWCOUNT */ },
            new() { WmiClass = "Win32_PerfRawData_PerfOS_Memory",      WmiProperty = "PagesPersec",            ObjectName = "Memory",            CounterName = "Pages/sec",                  InstanceName = "",       CounterType = 272696320 /* PERF_COUNTER_COUNTER */ },
            new() { WmiClass = "Win32_PerfRawData_PerfDisk_PhysicalDisk", WmiProperty = "DiskReadsPersec",     ObjectName = "PhysicalDisk",      CounterName = "Disk Reads/sec",             InstanceName = "_Total", CounterType = 272696320 /* PERF_COUNTER_COUNTER */ },
            new() { WmiClass = "Win32_PerfRawData_PerfDisk_PhysicalDisk", WmiProperty = "DiskWritesPersec",    ObjectName = "PhysicalDisk",      CounterName = "Disk Writes/sec",            InstanceName = "_Total", CounterType = 272696320 /* PERF_COUNTER_COUNTER */ },
            new() { WmiClass = "Win32_PerfRawData_PerfDisk_PhysicalDisk", WmiProperty = "DiskBytesPersec",     ObjectName = "PhysicalDisk",      CounterName = "Disk Bytes/sec",             InstanceName = "_Total", CounterType = 272696576 /* PERF_COUNTER_BULK_COUNT */ },
            new() { WmiClass = "Win32_PerfRawData_PerfDisk_PhysicalDisk", WmiProperty = "AvgDisksecPerRead",   ObjectName = "PhysicalDisk",      CounterName = "Avg. Disk sec/Read",         InstanceName = "_Total", CounterType = 805438464 /* PERF_AVERAGE_TIMER */, BaseWmiProperty = "AvgDisksecPerRead_Base" },
            new() { WmiClass = "Win32_PerfRawData_PerfDisk_PhysicalDisk", WmiProperty = "AvgDisksecPerWrite",  ObjectName = "PhysicalDisk",      CounterName = "Avg. Disk sec/Write",        InstanceName = "_Total", CounterType = 805438464 /* PERF_AVERAGE_TIMER */, BaseWmiProperty = "AvgDisksecPerWrite_Base" },
            new() { WmiClass = "Win32_PerfRawData_Tcpip_NetworkInterface", WmiProperty = "BytesTotalPersec",   ObjectName = "Network Interface", CounterName = "Bytes Total/sec",            InstanceName = "*",      CounterType = 272696576 /* PERF_COUNTER_BULK_COUNT */ },
        };

        /// <summary>
        /// Parse a compact counter spec of the form "WmiClass:WmiProperty" or "WmiClass:WmiProperty:Instance"
        /// (e.g. "Win32_PerfRawData_PerfOS_Processor:PercentProcessorTime:_Total").  Instance defaults to "*".
        /// <para>
        /// <see cref="CounterType"/> / <see cref="BaseWmiProperty"/> are deliberately left unset (0 / null):
        /// the collector resolves them from the class schema at collection time (see <see cref="CounterType"/>),
        /// so a spec never needs the raw PERF_* type or base property.
        /// </para>
        /// Throws <see cref="ArgumentException"/> for a malformed spec or a class that isn't a raw perf class.
        /// </summary>
        public static PerfmonCounter ParseSpec(string spec)
        {
            if (string.IsNullOrWhiteSpace(spec))
                throw new ArgumentException("Counter spec is empty.");

            // Class and property are WMI identifiers (no colons); an instance name can contain a colon
            // (e.g. a drive "C:"), so split off only the first two segments and keep the rest as the instance.
            var parts = spec.Split(':', 3);
            var wmiClass = parts[0].Trim();
            var wmiProperty = parts.Length > 1 ? parts[1].Trim() : string.Empty;
            var instance = parts.Length > 2 ? parts[2].Trim() : "*";
            if (instance.Length == 0) instance = "*";

            if (wmiClass.Length == 0 || wmiProperty.Length == 0)
                throw new ArgumentException($"Invalid counter spec '{spec}'. Expected WmiClass:WmiProperty[:Instance].");
            if (!wmiClass.StartsWith(RawClassPrefix, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException($"Invalid counter class '{wmiClass}'. Perfmon collection uses the raw '{RawClassPrefix}*' classes.");

            return new PerfmonCounter
            {
                WmiClass = wmiClass,
                WmiProperty = wmiProperty,
                ObjectName = PerfmonCounterDiscovery.DeriveObjectName(wmiClass),
                InstanceName = instance
                // CounterType left 0 / BaseWmiProperty null - resolved by the collector from the class schema.
            };
        }

        /// <summary>
        /// Parse a comma-separated list of counter specs (see <see cref="ParseSpec"/>).  An empty / whitespace
        /// input yields an empty list.  Throws <see cref="ArgumentException"/> on the first malformed spec.
        /// </summary>
        public static List<PerfmonCounter> ParseSpecList(string specs)
        {
            var list = new List<PerfmonCounter>();
            if (string.IsNullOrWhiteSpace(specs)) return list;
            foreach (var raw in specs.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                list.Add(ParseSpec(raw));
            }
            return list;
        }

        /// <summary>
        /// Build a counter list from the curated defaults and/or a comma-separated spec list, de-duplicating
        /// on the (object, counter, instance) key that is unique in dbo.Counters.  Throws
        /// <see cref="ArgumentException"/> on a malformed spec.
        /// </summary>
        public static List<PerfmonCounter> BuildList(bool includeDefaults, string specs)
        {
            var list = new List<PerfmonCounter>();
            if (includeDefaults) list.AddRange(DefaultCounters());
            foreach (var c in ParseSpecList(specs))
            {
                if (!list.Any(existing => SameCounter(existing, c))) list.Add(c);
            }
            return list;
        }

        /// <summary>
        /// True if two counters are the same counter, compared by their stable WMI identity
        /// (class / property / instance) rather than the display names, which can vary by config.
        /// </summary>
        public static bool SameCounter(PerfmonCounter a, PerfmonCounter b) =>
            string.Equals(a.WmiClass, b.WmiClass, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(a.WmiProperty, b.WmiProperty, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(a.InstanceName ?? "*", b.InstanceName ?? "*", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Canonical display names for the well-known counters, keyed by WMI identity.  This is the single
        /// source of the friendly (object_name, counter_name) so the same WMI counter always stores under one
        /// deterministic name regardless of how it was configured (defaults vs discovery vs hand-edited CLI).
        /// </summary>
        private static readonly Dictionary<string, (string ObjectName, string CounterName)> CanonicalNames =
            DefaultCounters().ToDictionary(
                c => c.WmiClass + "|" + c.WmiProperty,
                c => (c.EffectiveObjectName, c.EffectiveCounterName),
                StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Resolve the display (object_name, counter_name) to store for a given WMI identity: the curated
        /// name for a well-known counter, else derived (<see cref="PerfmonCounterDiscovery.DeriveObjectName"/>
        /// / the WMI property).  Deterministic, so a given WMI counter never fragments across two names.
        /// </summary>
        public static (string ObjectName, string CounterName) ResolveStorageNames(string wmiClass, string wmiProperty) =>
            CanonicalNames.TryGetValue(wmiClass + "|" + wmiProperty, out var names)
                ? names
                : (PerfmonCounterDiscovery.DeriveObjectName(wmiClass), wmiProperty);
    }
}
